using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using dotnetscrape_lib.DataObjects;
using dotnetscrape_lib;
using System.Linq;
using HCPDotNetDAL;
using dotnetscrape_constants;

namespace HCPDotNetBLL
{
    public class OrdersBL
    {
        private ParallelOptions options;

        private  ILogger _logger;

        private  bool _useQuantitySqlForSalesCounts;

        private void Initialize(ILogger logger)
        {
            _logger = logger;
            _useQuantitySqlForSalesCounts = false;
            options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
            };
        }
        public OrdersBL(ILogger logger)
        {
            Initialize(logger);
        }

        public OrdersBL(ILogger logger, bool useQuanitySqlForSalesCounts)
        {
            Initialize(logger);
            _useQuantitySqlForSalesCounts = useQuanitySqlForSalesCounts;
        }

        public string ConnectionString { get; set; }

        public void Dispose()
        {

        }

        private OrdersDA CreateDA()
        {
            return new OrdersDA { ConnectionString = ConnectionString };
        }
        private ConcurrentBag<PartSkuInventoryMetric> GetInitialSKUInventoryMetrics(int maxSalesVelocityDays)
        {
            _logger.LogDebug($"[START] - GetInitialSKUInventoryMetrics({maxSalesVelocityDays})");
            var bag = new ConcurrentBag<PartSkuInventoryMetric>();

            //Get all rows
            using (var da = CreateDA())
            {
                var table = da.GetInitialSKUInventoryMetrics(maxSalesVelocityDays);
                foreach (DataRow row in table.Rows)
                {
                    bag.Add(new PartSkuInventoryMetric(row));
                }
            }
            _logger.LogDebug($"[DONE] - GetInitialSKUInventoryMetrics({maxSalesVelocityDays})");
            return bag;
        }
        private ConcurrentBag<PartSkuInventoryMetric> GetSkuSoldCounts(ConcurrentBag<PartSkuInventoryMetric> bag)
        {
            _logger.LogDebug($"[START] - GetSkuSoldCounts()");
            Parallel.ForEach(bag, options, (item) =>
            {
                using (var da = CreateDA())
                {
                    item.SetQuantitySoldInLastXDays_SKU(da.GetSkusSold(item.SKU, (int)Math.Truncate(item.SalesVelocityInDays),_useQuantitySqlForSalesCounts));
                    item.CalculationMethod = (_useQuantitySqlForSalesCounts) ? 
                    PartSkuInventoryMetricCalculationMethodEnum.UsingOrderQuantity 
                    : PartSkuInventoryMetricCalculationMethodEnum.UsingOrderCount;
                }
            });
            var newBag = new ConcurrentBag<PartSkuInventoryMetric>();
            foreach (var item in bag.Where(a => a.QuantitySoldInLastXDays_SKU > 0))
            {
                newBag.Add(item);
            }
            _logger.LogDebug($"[DONE] - GetSkuSoldCounts()");
            return newBag;
        }
        private void GetOnOrderInTransitCountsForParts(ConcurrentBag<PartSkuInventoryMetric> bag)
        {
            _logger.LogDebug($"[START] - GetOnOrderInTransitCountsForParts()");
            using (var da = CreateDA())
            {
                var table = da.GetOnOrderInTransitCountByForParts();
                foreach (DataRow row in table.Rows)
                {
                    var partNumber = DataBaseValue.GetValue<string>(row, "PartNumber", string.Empty);
                    var orderStatus = DataBaseValue.GetValue<string>(row, "OrderStatus", string.Empty);
                    double orderCount = decimal.ToDouble(DataBaseValue.GetValue<decimal>(row, "OrderCount", 0));
                    if (string.IsNullOrWhiteSpace(partNumber) || string.IsNullOrWhiteSpace(orderStatus)) continue;

                    var items = bag.Where(item => string.Equals(item.PartNumber, partNumber, StringComparison.OrdinalIgnoreCase));
                    if (!items.Any()) continue;

                    foreach (var item in items)
                    {
                        switch (orderStatus)
                        {
                            case SKUInventoryContants.OrderStatusOnOrder:
                                item.SetOnOrder_Part(orderCount);
                                break;
                            case SKUInventoryContants.OrderStatusInTransit:
                                item.SetInTransit_Part(orderCount);
                                break;
                        }
                    }
                }
            }
            _logger.LogDebug($"[DONE] - GetOnOrderInTransitCountsForParts()");
        }
        private void GetActualOnHandCounts(ConcurrentBag<PartSkuInventoryMetric> bag)
        {
            _logger.LogDebug($"[START] - GetActualOnHandCounts()");
            Parallel.ForEach(bag, options, (item) =>
            {
                using (var da = CreateDA())
                {
                    item.SetActualOnHand_Part(da.GetActualOnHand(item.PartNumber));
                }
            });
            _logger.LogDebug($"[DONE] - GetActualOnHandCounts()");
        }
        private ConcurrentBag<PartSkuInventoryMetric> GetSKUInventoryMetricsForOrderCalculation(int maxSalesVelocityDays)
        {
            _logger.LogDebug($"[START] - GetSKUInventoryMetricsForOrderCalculation()");
            var bag = GetInitialSKUInventoryMetrics(maxSalesVelocityDays);
            bag = GetSkuSoldCounts(bag);
            GetOnOrderInTransitCountsForParts(bag);
            GetActualOnHandCounts(bag);

            Parallel.ForEach(bag, options, (item) =>
            {
                _logger.LogDebug($"[START] - PartSkuInventoryMetric.CalculateQuantityToOrder({item.SKU})");
                item.CalculateQuantityToOrder();
                _logger.LogDebug($"[DONE] - PartSkuInventoryMetric.CalculateQuantityToOrder({item.SKU})");
            });
            _logger.LogDebug($"[DONE] - GetSKUInventoryMetricsForOrderCalculation()");
            return bag;
        }

        public List<PartInventoryTarget> GetPartInventoryTargetWithOrderCountOverride()
        {
            var list = new List<PartInventoryTarget>();

            //Get all rows
            using (var da = CreateDA())
            {
                var table = da.GetPartInventoryTargetWithOrderCountOverride();
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PartInventoryTarget(row));
                }
            }

            return list;
        }

        public List<string> GetPartNumbersFromSKUInventoryMetricsForOrderCalculation(int maxSaleVelocityDays)
        {
            return GetSKUInventoryMetricsForOrderCalculation(maxSaleVelocityDays)
                   .Select(psim => psim.PartNumber).Distinct().ToList();
        }
        private void CommitSkuMetrics(ConcurrentBag<PartSkuInventoryMetric> metrics)
        {
            _logger.LogDebug($"[START] - CommitSkuMetrics()");
            Parallel.ForEach(metrics, options, (metric) =>
            {
                UpdateSkuInventoryMetric(metric);
            });
            _logger.LogDebug($"[DONE] - CommitSkuMetrics()");
        }


        public long GetNextPO_Number()
        {
            using (var da = CreateDA())
            {
                return da.GetNextPO_Number();
            }
        }
        public List<string> GetSkusOrderedOverAllTime()
        {
            using (var da = CreateDA())
            {
                return da.GetValidSkusOrderedOverAllTime();
            }
        }
        public List<string> PerformOrderCalculations(int maxSalesVelocityDays)
        {
            _logger.LogDebug($"[START] - PerformOrderCalculations()");
            var bag = GetSKUInventoryMetricsForOrderCalculation(maxSalesVelocityDays);
            CommitSkuMetrics(bag);
            _logger.LogDebug($"[DONE] - PerformOrderCalculations()");
            return bag.Select(psim => psim.PartNumber).Distinct().ToList();
        }
        public void InsertEmptySkuInventoryMetric(string sku)
        {
            using (var da = CreateDA())
            {
                da.InsertSkuInventoryMetric(sku);
            }
        }
        public PartSkuInventoryMetric GetSkuInventoryMetric(string sku)
        {
            using (var da = CreateDA())
            {
                var table = da.GetSkuInventoryMetricBySku(sku);
                var metric = new PartSkuInventoryMetric();
                if (table.Rows.Count > 0)
                {
                    metric = new PartSkuInventoryMetric(table.Rows[0]);
                }
                return metric;
            }
        }
        public List<PartSkuInventoryMetric> GetSkuInventoryMetrics()
        {
            var list = new List<PartSkuInventoryMetric>();
            
            //Get all rows
            using (var da = CreateDA())
            {
                var table = da.GetAllSkuInventoryMetrics();
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PartSkuInventoryMetric(row));
                }
            }

            return list;
        }
        public List<PartSkuInventoryMetric> GetSkuInventoryMetricsThatRequireOrdering()
        {
            var list = new List<PartSkuInventoryMetric>();

            //Get all rows
            using (var da = CreateDA())
            {
                var table = da.GetSkuInventoryMetricByOrderingStatus($"{PartSkuInventoryMetricOrderingStatus.RequiresOrdering}");
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PartSkuInventoryMetric(row));
                }
            }
            return list;
        }

        public void UpdateSkuInventoryMetric(PartSkuInventoryMetric metric)
        {
            using (var da = CreateDA())
            {
                long index = da.InsertSkuInventoryMetric(metric.SKU);
                metric.PartSkuInventoryMetricId = index;
                da.UpdateSkuInventoryMetric(
                    metric.PartSkuInventoryMetricId,
                    metric.ActualOnHand_SKU,
                    metric.ActualOnHand_Part,
                    metric.LeadTimeInDays,
                    metric.OnOrder_SKU,
                    metric.OnOrder_Part,
                    metric.InTransit_SKU,
                    metric.InTransit_Part,
                    metric.IncomingUnits_SKU,
                    metric.IncomingUnits_Part,
                    metric.DaysInStock,
                    metric.QuantitySoldInLastXDays_SKU,
                    metric.QuantitySoldInLastXDays_Part,
                    metric.SalesVelocityInDays,
                    metric.ForecastedGrowthPercentage,
                    metric.SalesVelocity_SKU,
                    metric.SalesVelocity_Part,
                    metric.ReorderBufferInDays,
                    metric.QuantityToOrder_SKU,
                    metric.QuantityToOrder_Part,
                    metric.OrderStatusAsString,
                    metric.OrderDate,
                    metric.CalculationMethodAsString
                    );
            }
        }
        public void UpdateSkuInventoryMetricOrderingStatusByPartNumber(string partNumber, PartSkuInventoryMetricOrderingStatus orderingStatus)
        {
            using (var da = CreateDA())
            {
                da.UpdateSkuInventoryMetricOrderingStatusByPartNumber(partNumber, $"{orderingStatus}");
            }
        }
        private PartOrderRequestPartOrderIn CreatePartOrderIn(PartSkuInventoryMetric metric)
        {
            string[] partNumberParts = metric.PartNumber.Split(" ".ToCharArray());
            if (partNumberParts.Count() != 2)
            {
                throw new ArgumentException($"PartNumber ({metric.PartNumber}) in PartSkuInventoryMetric is not in right format");
            }
            return new PartOrderRequestPartOrderIn
            {
                LineAbbrev = partNumberParts[0],
                PartNumber = partNumberParts[1],
                OrderQty = Convert.ToDecimal(metric.QuantityToOrder_Part),
                PartMessage = string.Empty
            };
        }
        private PartOrderRequestPartOrderIn CreatePartOrderIn(PartInventoryTarget target)
        {
            string[] partNumberParts = target.PartNumber.Split(" ".ToCharArray());
            if (partNumberParts.Count() != 2)
            {
                throw new ArgumentException($"PartNumber ({target.PartNumber}) in PartInventoryTarget is not in right format");
            }
            return new PartOrderRequestPartOrderIn
            {
                LineAbbrev = partNumberParts[0],
                PartNumber = partNumberParts[1],
                OrderQty = Convert.ToDecimal(target.OrderCountOverride),
                PartMessage = string.Empty,
                OrderCountOverriden = true
               
            };
        }
        public PartOrderRequest GenerateOrder(string storeId, string accountPassword)
        {
            PartOrderRequest request = new PartOrderRequest
            {
                PONumber = string.Empty,
                StoreID = storeId,
                AccountPassword = accountPassword
            };

            _logger.LogInfo($"Generating Order with PO_Number = {request.PONumber}");

            _logger.LogDebug("[START] - Getting SKU Metrics that Require Ordering");
            var metrics = GetSkuInventoryMetricsThatRequireOrdering();
            var partInventoryTargets = GetPartInventoryTargetWithOrderCountOverride();
            _logger.LogDebug("[DONE] - Getting SKU Metrics that Require Ordering");

            var partInMap = new Dictionary<string, PartOrderRequestPartOrderIn>();
            foreach(var metric in metrics)
            {
                if (!metric.AutoOrderEnabled)
                {
                    _logger.LogDebug($"PartNumber = [{metric.PartNumber}] is disabled for auto ordering.  The system will not generate an order for this part.");
                    continue;
                }

                _logger.LogDebug($"PartNumber = [{metric.PartNumber}] is enabled for auto ordering.  The system will generate an order for {metric.QuantityToOrder_Part.ToString("N0")} parts.");
                if (partInMap.ContainsKey(metric.PartNumber))
                {
                    partInMap[metric.PartNumber].OrderQty += Convert.ToDecimal(metric.QuantityToOrder_Part);
                }
                else
                {
                    PartOrderRequestPartOrderIn partOrderRequest = null;
                    try
                    {
                        partOrderRequest = CreatePartOrderIn(metric);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogError(ex.Message);
                        continue;
                    }

                    partInMap.Add(metric.PartNumber, partOrderRequest);
                }
            }

            //Process Overrides
            foreach(var target in partInventoryTargets)
            {
                if (!target.AutoOrderEnabled)
                {
                    _logger.LogDebug($"PartNumber = [{target.PartNumber}] has an overriden order count but is disabled for auto ordering.  The system will not generate an order for this part.");
                    continue;
                }
                if (partInMap.ContainsKey(target.PartNumber))
                {
                    //Override Count
                    var partIn = partInMap[target.PartNumber];
                    partIn.OrderQty = target.OrderCountOverride;
                    partIn.OrderCountOverriden = true;
                }
                else
                {
                    PartOrderRequestPartOrderIn partOrderReqeust = null;
                    try
                    {
                        partOrderReqeust = CreatePartOrderIn(target);
                    }
                    catch(ArgumentException ex)
                    {
                        _logger.LogError(ex.Message);
                        continue;
                    }
                    partInMap.Add(target.PartNumber, partOrderReqeust);
                }
            }
            request.PartOrderIn = partInMap.Values.Where(p => p.OrderQty >= 1).ToList();
            return request;
        }

        public void ResetPartInventoryTargetOrderCountOverride(List<string> partNumbers)
        {
            using (var da = CreateDA())
            {
                foreach (var partNumber in partNumbers)
                {
                    da.ResetPartInventoryTargetOrderCountOverride(partNumber);
                }
            }
        }

        private PartPurchase CreatePartPurchase(string po_Number, PartOrderRequestPartOrderIn orderIn, PartOrderResponsePartOrderOut orderOut)
        {
            var partPurchase = new PartPurchase
            {
                PO_Number = po_Number,
                PartNumber = orderIn.SMPartNumber,
                OrderCount = Convert.ToInt32(orderIn.OrderQty),
                OrderStatus = (string.IsNullOrWhiteSpace(orderOut.TAMSErrorMsg)) ? PartPurchaseOrderStatus.OnOrder : PartPurchaseOrderStatus.StatusError,
                OrderDateTime = DateTime.Now,
                DotNetQtyOnHand = Convert.ToInt32(orderOut.QtyOnHand),
                DotNetPriceType = orderOut.Price.PriceType,
                DotNetListPrice = (orderOut.Price.ListPrice.HasValue) ? orderOut.Price.ListPrice.Value : (decimal)0.0,
                DotNetYourCost = (orderOut.Price.YourCost.HasValue) ? orderOut.Price.YourCost.Value : (decimal)0.0,
                DotNetTAMSErrorMsg = orderOut.TAMSErrorMsg
            };
            return partPurchase;

        }

        private PartPurchase SetPartPurchase(PartPurchase partPurchase, PartOrderResponsePartOrderOut orderOut)
        {
            partPurchase.OrderStatus = (string.IsNullOrWhiteSpace(orderOut.TAMSErrorMsg)) ? PartPurchaseOrderStatus.OnOrder : PartPurchaseOrderStatus.StatusError;
            partPurchase.DotNetQtyOnHand = Convert.ToInt32(orderOut.QtyOnHand);
            partPurchase.DotNetPriceType = orderOut.Price.PriceType;
            partPurchase.DotNetListPrice = (orderOut.Price.ListPrice.HasValue) ? orderOut.Price.ListPrice.Value : (decimal)0.0;
            partPurchase.DotNetYourCost = (orderOut.Price.YourCost.HasValue) ? orderOut.Price.YourCost.Value : (decimal)0.0;
            partPurchase.DotNetTAMSErrorMsg = orderOut.TAMSErrorMsg;
            partPurchase.HasPartOut = true;
            return partPurchase;
        }

        private PartPurchase CreatePartPurchase(string po_Number, PartOrderRequestPartOrderIn orderIn)
        {
            var partPurchase = new PartPurchase
            {
                PO_Number = po_Number,
                PartNumber = orderIn.SMPartNumber,
                OrderCount = Convert.ToInt32(orderIn.OrderQty),
                OrderDateTime = DateTime.Now,
                HasPartOut = false,
                OrderCountOverriden = orderIn.OrderCountOverriden
            };
            return partPurchase;
        }

        private List<PartPurchase> CreatePartPurchases(PartOrderRequest request , PartOrderResponse response)
        {
            var partPurchases = new Dictionary<string,PartPurchase>();
            string key;

            //Collect all partPurchases from PartOrderIn
            foreach (var orderIn in request.PartOrderIn)
            {
                key = $"{request.PONumber}{orderIn.SMPartNumber}";
                if (!partPurchases.ContainsKey(key))
                    partPurchases.Add(key, CreatePartPurchase(request.PONumber, orderIn));
            }

            //Process PartOrderOuts
            foreach(var orderOut in response.PartOrderOut)
            {
                key = $"{request.PONumber}{orderOut.SMPartNumber}";
                if (partPurchases.ContainsKey(key))
                {
                    partPurchases[key] = SetPartPurchase(partPurchases[key],orderOut);
                }
            }

            //Return only those partpurchases that have an OrderOut set to true
            return partPurchases.Values.Where(p => p.HasPartOut).ToList();
        }

        public List<PartPurchase> GetPartPurchasesByPONumber(string po_Number)
        {
            var list = new List<PartPurchase>();
            using (var da = CreateDA())
            {
                var table = da.GetPartPurchasesByPONumber(po_Number);
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PartPurchase(row));
                }
            }
            return list;
        }

        public List<PartPurchase> GetAllPartPurchases()
        {
            var list = new List<PartPurchase>();
            using (var da = CreateDA())
            {
                var table = da.GetPartAllPurchases();
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new PartPurchase(row));
                }
            }
            return list;
        }

        public void DeletePartPurchase(long partPurchaseId)
        {
            using (var da = CreateDA())
            {
                da.DeletePartPurchase(partPurchaseId);
            }
        }


        public List<string> GetDistinctPurchaseNumbersByStatus(PartPurchaseOrderStatus orderStatus)
        {
            using (var da = CreateDA())
            {
                return da.GetDistinctPurchaseNumbersByStatus($"{orderStatus}");
            }
        }

        public void UpdatePartPurchaseComments(long partPurchaseId, string comments)
        {
            using(var da = CreateDA())
            {
                da.UpdatePartPurchaseComments(partPurchaseId, comments);
            }
        }

        public void UpdatePartPurchaseOrderStatus(PartPurchase partPurchase, PartPurchaseOrderStatus orderStatus)
        {
            _logger.LogDebug($"[START] OrderBL.UpdatePartPurchaseOrderStatus(PartPurchaseId = {partPurchase.PartPurchaseId}, PO_Number = {partPurchase.PO_Number}, PartNumber = {partPurchase.PartNumber} OrderStatus={orderStatus}");
            using (var da = CreateDA())
            {
                da.UpdatePartPurchaseOrderStatus(partPurchase.PartPurchaseId, $"{orderStatus}");
            }
            _logger.LogDebug($"[DONE] OrderBL.UpdatePartPurchaseOrderStatus(PartPurchaseId = {partPurchase.PartPurchaseId}, PO_Number = {partPurchase.PO_Number}, PartNumber = {partPurchase.PartNumber} OrderStatus={orderStatus}");
        }

        public void SavePartPurchases(List<PartPurchase> partPurchases)
        {
            _logger.LogDebug($"[START] OrderBL.SavePartPurchases(Count = {partPurchases.Count()}");
            if (partPurchases.Any())
            {
                using (var da = CreateDA())
                {

                    try
                    {
                        //Save Blank InoviceSummary
                        _logger.LogDebug($"[START] Saving Empty Invoice Summary PlaceHolder for PO_Number = {partPurchases[0].PO_Number}");
                        da.SaveInvoiceSummary(Constants.NoStore,
                                                partPurchases[0].PO_Number,
                                                Constants.NoInvoice,
                                                string.Empty,
                                                string.Empty,
                                                Constants.ZeroAsDecimal,
                                                Constants.DefaultDate,
                                                -1, -1,
                                                Constants.ZeroAsDecimal,
                                                Constants.ZeroAsDecimal,
                                                Constants.ZeroAsDecimal,
                                                Constants.ZeroAsDecimal,
                                                Constants.ZeroAsDecimal,
                                                Constants.ZeroAsDecimal,
                                                string.Empty,
                                                true
                        );
                        _logger.LogDebug($"[DONE]  Saving Empty Invoice Summary PlaceHolder for PO_Number = {partPurchases[0].PO_Number}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Unable to save Empty Invoice Summary PlaceHolder for PO_Number = {partPurchases[0].PO_Number}.  Reason: {ex.Message}");
                    }
                    foreach (var partPurchase in partPurchases)
                    {
                        try
                        {
                            _logger.LogDebug($"[START] - OrderDA.InsertPartPurchase(PO_Number = {partPurchase.PO_Number}, PartNumber={partPurchase.PartNumber}, OrderCount = {partPurchase.OrderCount}");
                            da.InsertPartPurchase(
                            partPurchase.PO_Number,
                            partPurchase.PartNumber,
                            partPurchase.OrderCount,
                            partPurchase.OrderDateTime,
                            $"{partPurchase.OrderStatus}",
                            partPurchase.DotNetQtyOnHand,
                            partPurchase.DotNetNetQtyOnHand,
                            partPurchase.DotNetPriceType,
                            partPurchase.DotNetListPrice,
                            partPurchase.DotNetYourCost,
                            partPurchase.DotNetTAMSErrorMsg,
                            partPurchase.Comments,
                            partPurchase.OrderCountOverriden);
                            _logger.LogDebug($"[DONE] - OrderDA.InsertPartPurchase(PO_Number = {partPurchase.PO_Number}, PartNumber={partPurchase.PartNumber}, OrderCount = {partPurchase.OrderCount}");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error Inserting Record into PartPurchase - {ex.Message}");
                        }

                        //Save Empty PartReceiving PlaceHolder
                        try
                        {
                            _logger.LogDebug($"[START] Saving Empty PartReceiving PlaceHolder (PartNumber={partPurchase.PartNumber}, OrderCount={partPurchase.OrderCount}, QtyBilled(ReportedShippedCount) = {0}, UnitPrice={partPurchase.DotNetListPrice})");
                            da.SavePartReceiving(partPurchase.PO_Number,
                                                 partPurchase.PartNumber,
                                                 partPurchase.OrderCount,
                                                 Constants.NoInvoice, 
                                                 0, 
                                                 0, 
                                                 0, 
                                                 Constants.DefaultDate,
                                                 $"{Constants.NoneReceived}",
                                                 string.Empty, 
                                                 Constants.ZeroAsDecimal, 
                                                 string.Empty, 
                                                 string.Empty,
                                                 true);
                            _logger.LogDebug($"[DONE] Saving Empty PartReceiving PlaceHolder (PartNumber={partPurchase.PartNumber}, OrderCount={partPurchase.OrderCount}, QtyBilled(ReportedShippedCount) = {0}, UnitPrice={partPurchase.DotNetListPrice}))");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unable to save Empty PartReceiving PlaceHolder (PartNumber={partPurchase.PartNumber}, OrderCount={partPurchase.OrderCount}, QtyBilled(ReportedShippedCount) = {0}, UnitPrice={partPurchase.DotNetListPrice})  Reason: {ex.Message}");
                        }

                    }
                }
            }  
            _logger.LogDebug($"[DONE] OrderBL.SavePartPurchases(Count = {partPurchases.Count()}");
        }


        public void SavePartPurchaseTest(string PONumber)
        {
            List<PartPurchase> list = new List<PartPurchase>
            {
                new PartPurchase
                {
                    PO_Number = PONumber,
                    PartNumber = "AC 1",
                    OrderCount = 100,
                    OrderDateTime = DateTime.Now,
                    HasPartOut = false,
                    OrderCountOverriden = false
                },
                new PartPurchase
                {
                    PO_Number = PONumber,
                    PartNumber = "AC 10",
                    OrderCount = 100,
                    OrderDateTime = DateTime.Now,
                    HasPartOut = false,
                    OrderCountOverriden = false
                }
            };
            SavePartPurchases(list);
        }

        public List<PartPurchase> SaveOrderResponse(PartOrderRequest request, PartOrderResponse response)
        {
            var partPurchases = CreatePartPurchases(request, response);

            SavePartPurchases(partPurchases);

            partPurchases = GetPartPurchasesByPONumber(request.PONumber);
            //Only return those purchases that have a "OnOrder" status

            //Get All PartNumbers that Have Overriden Values Set and Then reset the PartTargetInventory records
            var partNumbers = request.PartOrderIn.Where(pIn => pIn.OrderCountOverriden).Select(pIn => pIn.SMPartNumber).ToList();
            ResetPartInventoryTargetOrderCountOverride(partNumbers);

            return partPurchases
                        .Where(p => p.OrderStatus == PartPurchaseOrderStatus.OnOrder).
                        ToList();
        }
        public void ResetSkuInventoryOrderingStatus(List<string> partNumbers)
        {
            foreach(var partNumber in partNumbers)
            {
                UpdateSkuInventoryMetricOrderingStatusByPartNumber(partNumber, PartSkuInventoryMetricOrderingStatus.Ordered);
            }
        }

        public List<ManualPurchaseOrder> GetManualPurchasesOrdersByStatus(ManualPurchaseOrderProcessingStatusEnum processingStatus)
        {
            var list = new List<ManualPurchaseOrder>();
            using (var da = CreateDA())
            {
                var table = da.GetManualPurchaseOrderByStatus($"{processingStatus}");
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new ManualPurchaseOrder(row));
                }
            }
            return list;
        }

        public void UpdateManualPurchaseOrderStatus(ManualPurchaseOrder mpo)
        {
            _logger.LogDebug($"[START] OrderBL.UpdateManualPurchaseOrdersStauts(ManualPurchaseOrderId={mpo.ManualPurchaseOrderId}, PO_Number = {mpo.PO_Number}, ProcessingStatus={mpo.ProcessingStatus}");
            using (var da = CreateDA())
            {
                da.UpdateManualPurchaseOrderStatus(mpo.ManualPurchaseOrderId,
                                                    $"{ManualPurchaseOrderProcessingStatusEnum.Processed}",
                                                    DateTime.Now);
            }
            _logger.LogDebug($"[DONE] OrderBL.UpdateManualPurchaseOrdersStauts(ManualPurchaseOrderId={mpo.ManualPurchaseOrderId}, PO_Number = {mpo.PO_Number}, ProcessingStatus={mpo.ProcessingStatus}");
        }

        public void SaveInvoiceData(List<PartPurchase> partPurchases, InvoiceData invoiceData)
        {
            if (invoiceData.DataStatus != InvoiceData.InvoiceDataStatusEnum.Complete) return;

            SaveInvoiceData(partPurchases, invoiceData.Summary, invoiceData.Details);
        }

        private void SaveInvoiceData(List<PartPurchase> partPurchases, POInvoiceResponse invoiceSummaries, List<InvoiceDetailResponse> details)
        {
            DateTime defaultDate = new DateTime(2777, 1, 1);
            
            string poNumber = invoiceSummaries.PONumber;
            
            foreach (var poi in invoiceSummaries.POInvoice)
            {

                string invoiceNumber = poi.InvoiceNumber;

                //If there are no details associated with this invoice POInvoiceResponse, then move on to next one.
                if (!details.Where(d => d.InvoiceNumber == invoiceNumber).Any()) continue;
                
                _logger.LogDebug($"[START] OrderBL.SaveInvoiceData(PO_Number = {poNumber}, InvoiceNumber={invoiceNumber}");
                using (var da = CreateDA())
                {

                    var detail = details.Where(d => d.InvoiceNumber == invoiceNumber).First();

                    try
                    {
                        _logger.LogDebug($"[START] Saving Invoice Summary");
                        da.SaveInvoiceSummary(invoiceSummaries.StoreID, poNumber, invoiceNumber, detail.TransactionType, detail.ErrorMsg,
                                              poi.InvoiceTotal, poi.InvoiceDateTime, detail.CounterPersonID, detail.SalesPersonID, detail.OtherCharges,
                                              detail.NonTaxableTotal, detail.TaxableTotal, detail.Tax1Total, detail.Tax2Total, detail.AdjustmentTotal, detail.Attention, false
                        );
                        _logger.LogDebug($"[DONE] Saving Invoice Summary");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Unable to save Invoice Summary.  Reason: {ex.Message}");
                    }

                    //collect unique detail lines by poNumber and partnumber
                    var dict = new Dictionary<string, InvoiceDetailResponseInvoiceDetailLine>();

                    foreach (var line in detail.InvoiceDetailLine)
                    {
                        string key = $"{poNumber}{line.SMPartNumber}";
                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, line);
                        }
                        else
                        {
                            var item = dict[key];
                            item.QtyBilled += line.QtyBilled;
                        }
                    }

                    foreach (var line in dict.Values)
                    {
                        //Ignore any that have only M in the LineAbbrev
                        if (string.Equals(line.LineAbbrev, "M", StringComparison.OrdinalIgnoreCase))
                            continue;
                        var partPurchaseSubList = partPurchases.Where(p => string.Equals(p.PartNumber, line.SMPartNumber, StringComparison.OrdinalIgnoreCase));
                        if (partPurchaseSubList.Any())
                        {
                            int quantityOrder = partPurchaseSubList.Sum(p => p.OrderCount);
                            int quantityBilled = (line.QtyBilled.HasValue) ? Convert.ToInt32(line.QtyBilled.Value) : (int)0;
                            decimal unitPrice = (line.UnitPrice.HasValue) ? line.UnitPrice.Value : (decimal)0.0;
                            try
                            {


                                try
                                {
                                    _logger.LogDebug($"[START] Saving PartReceiving (PartNumber={line.SMPartNumber}, OrderCount={quantityOrder}, QtyBilled(ReportedShippedCount) = {quantityBilled}, UnitPrice={unitPrice})");
                                    da.SavePartReceiving(poNumber, line.SMPartNumber, quantityOrder,
                                                         invoiceNumber, quantityBilled, 0, quantityBilled, defaultDate, "Invoiced",
                                                         string.Empty, unitPrice, line.Taxed, line.InvoiceMessageLine,false);
                                    _logger.LogDebug($"[DONE] Saving PartReceiving (PartNumber={line.SMPartNumber}, OrderCount={quantityOrder}, QtyBilled(ReportedShippedCount) = {quantityBilled}, UnitPrice={unitPrice})");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Unable to save PartReceiving.  Reason: {ex.Message}");
                                }



                                foreach (var partPurchase in partPurchaseSubList)
                                {
                                    //Only update status in partPurchase if the status of the partPurachas is OnOrder
                                    if (partPurchase.OrderStatus == PartPurchaseOrderStatus.OnOrder ||
                                       partPurchase.OrderStatus == PartPurchaseOrderStatus.None)
                                    {
                                        da.UpdatePartPurchaseOrderStatus(partPurchase.PartPurchaseId, $"{PartPurchaseOrderStatus.InTransit}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"There was an error when attempting to save a record to PartReceiving. {ex.Message}");
                            }
                        }
                    }
                }
                _logger.LogDebug($"[DONE] OrderBL.SaveInvoiceData(PO_Number = {poNumber}, InvoiceNumber={invoiceNumber}");
            }
            
        }

        public void UpdatePartPurchaseCounts(List<PartPurchase> partPurchases)
        {
            using(var da = CreateDA())
            {
                foreach(var pp in partPurchases)
                {
                    _logger.LogDebug($"Setting PartPurchase Count for (PartPurchaseId: {pp.PartPurchaseId}, PO_Number: {pp.PO_Number}, PartNumber: {pp.PartNumber}, OrderCount: {pp.OrderCount}, DotNetQtyOnHand: {pp.DotNetQtyOnHand}, DotNetNetQtyOnHand: {pp.DotNetNetQtyOnHand}");
                    da.UpdatePartPurchaseCounts(pp.PartPurchaseId, pp.OrderCount, pp.DotNetQtyOnHand, pp.DotNetNetQtyOnHand);
                }
            }
        }

        public void DeletePartPurchases(List<PartPurchase> partPurchases)
        {
            using (var da = CreateDA())
            {
                foreach (var pp in partPurchases)
                {
                    _logger.LogDebug($"Deleting PartPurchase record for (PartPurchaseId: {pp.PartPurchaseId}, PO_Number: {pp.PO_Number}, PartNumber: {pp.PartNumber}, OrderCount: {pp.OrderCount}, DotNetQtyOnHand: {pp.DotNetQtyOnHand}, DotNetNetQtyOnHand: {pp.DotNetNetQtyOnHand}");
                    da.DeletePartPurchase(pp.PartPurchaseId);
                }
            }
        }

        public void DeleteAllPartPurchasesWithOrderCountZero()
        {
            using (var da = CreateDA())
            {
                var count = da.DeleteAllPartPurchasesWithOrderCountZero();
                _logger.LogDebug($"Deleted {count} partPurchase records that had OrderCount = 0");
            }
        }
        public void CleanupDuplicatePartPurchases()
        {
            _logger.LogDebug("CleanupDuplicatePartPurchases() - Loading All PartPurchase Records with Duplicate PO_Number,PartNumber combinations");
            var all = GetAllPartPurchases();
            var updates = new Dictionary<string, PartPurchase>();
            var deletes = new List<PartPurchase>();

            
            foreach (var pp in all)
            {
                var key = $"{pp.PO_Number}{pp.PartNumber}".ToUpper();
                if (updates.ContainsKey(key))
                {
                    var update = updates[key];
                    update.IsDupe = true;
                    update.OrderCount += pp.OrderCount;
                    if(update.Comments == "Manual Order Entry")
                    {
                        update.DotNetQtyOnHand = update.OrderCount;
                    }
                    deletes.Add(pp);
                }
                else
                {
                    updates.Add(key, pp);
                }
            }

            var listToUpdate = updates.Values.Where(p => p.IsDupe).ToList();
            _logger.LogDebug($"Attempting to update {listToUpdate.Count()} partPurchase records marked as duplicates");
            UpdatePartPurchaseCounts(listToUpdate);

            _logger.LogDebug($"Attempting to delete {deletes.Count()} partPurchase records that were marked as duplicate");
            DeletePartPurchases(deletes);

            _logger.LogDebug($"Attempting to delete partPurchase records with OrderCount = 0");
            DeleteAllPartPurchasesWithOrderCountZero();
        }
    }
}
