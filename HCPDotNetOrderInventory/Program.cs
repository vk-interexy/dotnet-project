using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Linq;
using dotnetscrape_constants;
using dotnetscrape_lib.DataObjects;
using dotnetscrape_lib.DataObjects.DotNetB2B;
using dotnetscrape_lib;
using HCPDotNetBLL;
using System.Globalization;
using System.Threading;

namespace HCPDotNetOrderInventory
{
    class Program
    {
        private static readonly ParallelOptions options = new ParallelOptions();

        static readonly ILogger _logger = new Logger(Config.LogLevel);


        private static void Run(string[] args)
        {
            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);

            if (Config.RunOneTimeInsertOfPartPurchase)
            {
                GeneartePartPurchaseOneTime();
                return;
            }

            bool getPricingOnly = args.Contains("/c");



            OrdersBL bl = new OrdersBL(_logger, Config.UseSkuOrderQuanityForSalesVelocityCalculations)
            {
                ConnectionString = Config.ConnectionString
            };

            WebClient.Logger = _logger;


            _logger.LogInfo("Starting Ordering Process");

            try
            {
                _logger.LogInfo("[START] - Cleaning Up Part Purchase Table from duplicates and invalid records with OrderCount = 0");
                CleanupPartPurchases();
                _logger.LogInfo("[DONE] - Cleaning Up Part Purchase Table from duplicates and invalid records with OrderCount = 0");


                if (getPricingOnly)
                {
                    _logger.LogInfo("[START] - Getting Quantity and Pricing Information for sold parts and exiting.");
                    var partNumbersForQuantityUpdateOnly = bl.GetPartNumbersFromSKUInventoryMetricsForOrderCalculation(Config.MaxSalesVelocityDays);
                    UpdatePartCountsAndPrices(partNumbersForQuantityUpdateOnly);
                    _logger.LogInfo("[DONE] - Getting Quantity and Pricing Information for sold parts and exiting.");
                    return;
                }

                //Process Manual Purchase Orders for Invoicing
                _logger.LogInfo("[START] - Processing Invoice Data for MANUAL Purchase Orders");
                ProcessInvoiceDataForManualPurchaseOrders();
                _logger.LogInfo("[DONE] - Processing Invoice Data for MANUAL Purchase Orders");


                _logger.LogInfo("[START] - Processing Invoice Data for AUTO Purchase Orders");
                ProcessInvoiceDataForAutoPurchasedOrders();
                _logger.LogInfo("[DONE] - Processing Invoice Data for AUTO Purchase Orders");


                //Performing Order Calculations
                _logger.LogInfo($"[START] - Performing Order Calculations with MaxSalesVelocityDays = {Config.MaxSalesVelocityDays}");
                var partNumbers = bl.PerformOrderCalculations(Config.MaxSalesVelocityDays);

                //Getting Latest Quantities and Pricing information and saving to parts before starting ordering process.
                _logger.LogInfo("[START] - Getting Quantity and Pricing Information for sold parts.");
                UpdatePartCountsAndPrices(partNumbers);
                _logger.LogInfo("[DONE] - Getting Quantity and Pricing Information for sold parts.");


                _logger.LogInfo($"[DONE] - Performing Order Calculations with MaxSalesVelocityDays = {Config.MaxSalesVelocityDays}");

                //If the GlobalAutoOrderEnabled is disabled or "false" then simply exit.
                if (!Config.AutoOrderEnabledGlobal)
                {
                    _logger.LogInfo($"******** The system is configured with the Auto Order feature disabled globally.  Exiting application ********");
                    return;
                }


                _logger.LogInfo("[START] - Generating Order Data to send to DOTNET");
                var request = bl.GenerateOrder(Config.StoreID, Config.AccountPassword);
                _logger.LogInfo("[DONE] - Generating Order Data to send to DOTNET");

                if (!request.PartOrderIn.Any())
                {
                    _logger.LogInfo("******** There are no parts with order counts greater than or equal to 1.  No order will be submitted to DOTNET  *********");
                    return;
                }

                string po_number = $"{bl.GetNextPO_Number()}";
                _logger.LogInfo($"[START] - Submitting Order to DOTNET with PO Number: {po_number}");
                request.PONumber = po_number;
                var response = SubmitOrderToDotNet(request);
                _logger.LogInfo($"[DONE] - Submitting Order to DOTNET with PO Number: {po_number}");

                _logger.LogInfo("[START] - Saving Order Response Data to Database in partpurchase table");
                var partPurchases = bl.SaveOrderResponse(request, response);
                _logger.LogInfo("[DONE] - Saving Order Response Data to Database in partpurchase table");


                _logger.LogInfo("[START] - Updating partskuinventorymetrics Ordering Status field");
                bl.ResetSkuInventoryOrderingStatus(partPurchases.Select(p => p.PartNumber).Distinct().ToList());
                _logger.LogInfo("[DONE] - Updating partskuinventorymetrics Ordering Status field");

                _logger.LogInfo("[START] - Updating Part Quantity, List Price, and Cost");
                UpdatePartCountsAndPrices(partPurchases);
                _logger.LogInfo("[DONE] - Updating Part Quantity, List Price, and Cost");



            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}{Environment.NewLine}Stack Trace:{Environment.NewLine}{ex.StackTrace}" );
            }
        }

        
        static void Main(string[] args)
        {
            
            int retryCount = 3;
            bool getPricingOnly = args.Contains("/c");

            while (retryCount > 0)
            {
                try
                {
                    
                    string lockFileName = (getPricingOnly) ? "./lockFilePricingOnly.bin" : "./lockFileOrderProcess.bin";

                    using (var lockFile = File.Open(lockFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
                    {
                        Run(args);
                        break;
                    }

                }
                catch (IOException)
                {
                    _logger.LogInfo($"Another process is running.  Waiting 10 seconds and trying again.  RetryCount = {retryCount}");
                    Thread.Sleep(10000);
                    retryCount--;
                }
            }

            if(retryCount == 0)
            {
                _logger.LogInfo($"Exiting process because another process is running and the retry count has expired.");
                return;
            }
            
        }

        #region Core Processing Methods
        private static AutoPartPricing CreatePricing(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            var pricing = new AutoPartPricing();
            //Get Primary List Pricing
            var listPricing = (priceAvail.Price.Any(a => a.PriceType == "PRI")) ?
                priceAvail.Price.First(a => a.PriceType == "PRI")
                : null;

            //Get Core Pricing if it exists
            var corePricing =
            (priceAvail.Price.Any(a => a.PriceType == "COR")) ?
                priceAvail.Price.First(a => a.PriceType == "COR")
                : null;

            pricing.HasCore = false;
            pricing.PricingDetailProvided = false;

            //If list Pricing set, then set from 
            if (listPricing != null)
            {
                pricing.List = (listPricing.ListPrice != null) ? listPricing.ListPrice.Value : 0;
                pricing.Cost = (listPricing.YourCost != null) ? listPricing.YourCost.Value : 0;
                pricing.PricingDetailProvided = true;
            }

            if (corePricing != null)
            {
                pricing.Core = (corePricing.YourCost != null) ? corePricing.YourCost.Value : 0;
                pricing.HasCore = true;
                pricing.PricingDetailProvided = true;
            }
            return pricing;
        }

        private static List<DistributionCenterQuantity> CreateDCQuantities(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            var dcQtys = new List<DistributionCenterQuantity>();
            if (priceAvail.DCBalanceQty.HasValue)
            {
                dcQtys.Add(
                    new DistributionCenterQuantity
                    {
                        Name = Config.PrimaryDistributionCetner,
                        Quantity = priceAvail.DCBalanceQty.Value,
                        DeliveryTime = priceAvail.DeliveryTime
                    });
            }
            return dcQtys;
        }

        private static PriceAvailabilityRequest GenerateRequest(List<string> partNumbers)
        {
            var request = new PriceAvailabilityRequest
            {
                RequestID = Constants.MiscellaneousChargesString,
                StoreID = Config.StoreID,
                AccountPassword = Config.AccountPassword,
            };
            foreach (var partNumber in partNumbers)
            {
                string[] partNumberTokens = partNumber.Split(" ".ToCharArray());
                if (partNumberTokens.Length != 2 ||
                    string.IsNullOrWhiteSpace(partNumberTokens[0]) ||
                    string.IsNullOrWhiteSpace(partNumberTokens[1])
                )
                {
                    continue;
                }

                request.Part.Add(
                    new PriceAvailabilityRequestPart
                    {
                        LineAbbrev = partNumberTokens[0],
                        PartNumber = partNumberTokens[1],
                        PartMessage = Constants.DCQTYPartMessage

                    });
            }
            return request;

        }

        private static PriceAvailabilityResponse GetPricingAndAvaiablilty(PriceAvailabilityRequest request)
        {
            PriceAvailabilityResponse priceAvailabilityResponse = null;
            var method = "POST";
            var contentType = "text/xml; charset=utf-8";
            var requestBody = dotnetscrape_lib.Utilities.SerializeObject(request, false, true);
            var response = WebClient.Submit(Config.DotNetB2BApiUrl,
                method,
                requestBody,
                contentType)?.Result;
            if (!response.HasError)
            {
                priceAvailabilityResponse = dotnetscrape_lib.Utilities.Deserialize<PriceAvailabilityResponse>(response.Result, false);
                if (LogLevel.DEBUG >= Config.LogLevel)
                {
                    _logger.LogDebug($"{Environment.NewLine}{dotnetscrape_lib.Utilities.SerializeObject(priceAvailabilityResponse, false, true)}{Environment.NewLine}");
                }
            }
            else
            {
                if (LogLevel.DEBUG >= Config.LogLevel)
                {
                    _logger.LogDebug($"{Environment.NewLine}Response Error: {response.Error}{Environment.NewLine}Result:{Environment.NewLine}{response.Result}{Environment.NewLine}");
                }
            }
            return priceAvailabilityResponse;
        }

        private static PartsBL CreatePartsBL()
        {
            return new PartsBL { ConnectionString = Config.ConnectionString };
        }

        private static int UpdatePartInDatabase(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            int ret = 0;
            using (var bl = CreatePartsBL())
            {

                var part = bl.GetPart($"{priceAvail.LineAbbrev.Trim()} {priceAvail.PartNumber.Trim()}");
                if (part == null) return 0;

                _logger.LogDebug($"START - Updating Pricing and Availability for part number: {part.PartNumber}");

                part.Pricing = CreatePricing(priceAvail);
                part.Quantity = priceAvail.QtyOnHand ?? 0;
                part.FindItQuantities = new AutoPartQuantity
                {
                    DCQty = CreateDCQuantities(priceAvail)
                };

                ret = bl.UpdatePartPricingAndQuantity(part, Config.PrimaryDistributionCetner);
                if (ret > 0)
                {
                    Utilities.TotalPartCountProcessed++;
                }
                _logger.LogDebug($"DONE - Updating Pricing and Availability for part number: {part.PartNumber}");
            }
            return ret;
        }

        private static void UpdatePartCountsAndPrices(List<string> partNumbers)
        {
            var chunks = dotnetscrape_lib.Utilities.ChunkBy<string>(partNumbers, Config.ChunkSize);

            //Release memory of List 
            partNumbers = null;


            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                try
                {
                    PriceAvailabilityResponse response = null;
                    bool success = false;

                    for (short retry = 1; retry <= 3; retry++)
                    {
                        _logger.LogInfo($"********* START ATTEMPT #{retry} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                        response = GetPricingAndAvaiablilty(GenerateRequest(chunk));
                        if (response != null)
                        {
                            //If the response code is 2 then that means the "Store is not answer" and we should retry
                            if (response.StatusCode == 2)
                            {
                                _logger.LogInfo($"********* RETRYING ATTEMPT #{retry} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                                continue;
                            }
                            else
                            {
                                success = response.StatusCode == 0;
                                break;
                            }
                        }
                    }

                    string status = (success) ? "DONE" : $"FAILED WITH STATUS CODE ({response.StatusCode})";

                    _logger.LogInfo($"********* {status} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");

                    if (success)
                    {
                        _logger.LogInfo($"########## START - Updating database for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                        if (response != null)
                        {
                            Parallel.ForEach(response.PartPriceAvailability, options, (priceAvailability, loopState) =>
                            {
                                UpdatePartInDatabase(priceAvailability);
                            });
                        }
                        _logger.LogInfo($"########## DONE - Updating database for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

            }
        }

        private static void UpdatePartCountsAndPrices(List<PartPurchase> purchases)
        {
            PartsBL partBL = new PartsBL
            {
                ConnectionString = Config.ConnectionString
            };

            OrdersBL orderBl = new OrdersBL(_logger)
            {
                ConnectionString = Config.ConnectionString
            };

            foreach(var purchase in purchases)
            {
                var part = partBL.GetPart(purchase.PartNumber);
                decimal newQuantity = Convert.ToDecimal((purchase.DotNetNetQtyOnHand < 0) ? 0 : purchase.DotNetNetQtyOnHand);
                decimal newListPrice = purchase.DotNetListPrice;
                decimal newCost = purchase.DotNetYourCost;
                string comments = string.Empty;
                bool changed = false;

                if(newQuantity != part.Quantity)
                {
                    comments += $" [Quantity Changed to {newQuantity}] ";
                    part.Quantity = newQuantity;
                    changed = true;
                }

                if (newListPrice != part.Pricing.List)
                {
                    comments += $" [List Price Changed to {newListPrice}] ";
                    part.Pricing.List = newListPrice;
                    changed = true;
                }

                if (newCost != part.Pricing.Cost)
                {
                    comments += $" [Cost Changed to {newCost}] ";
                    part.Pricing.Cost = newCost;
                    changed = true;
                }

                if (changed)
                {
                    _logger.LogInfo($"Updating Part Quantity and Pricing with comment to partpurchase table for PO_Number {purchase.PO_Number} and PartNumber {purchase.PartNumber}. [Comment: {comments}]");
                    partBL.UpdatePartPricingAndQuantity(part, Config.PrimaryDistributionCetner);
                    orderBl.UpdatePartPurchaseComments(purchase.PartPurchaseId, comments);
                }
            }
        }

        private static PartOrderResponse SubmitOrderToDotNet(PartOrderRequest request)
        {
            if(!request.PartOrderIn.Any())
            {
                return new PartOrderResponse();
            }

            //If the GenerateTestResponseForOrderProcess setting is in the smp_settings table 
            //and is set to true then this logic will execute
            if (Config.GenerateTestResponseForOrderProcess)
            {
                var por = new PartOrderResponse()
                {
                    StoreID = request.StoreID,
                    StatusCode = 0,
                    StatusMessage = string.Empty
                };
                foreach (var poi in request.PartOrderIn)
                {
                    por.PartOrderOut.Add(new PartOrderResponsePartOrderOut
                    {
                        LineAbbrev = poi.LineAbbrev,
                        PartNumber = poi.PartNumber,
                        QtyOnHand = poi.OrderQty,
                        Price = new PartOrderResponsePartOrderOutPrice
                        {
                            PriceType = "PRI",
                            ListPrice = 10,
                            YourCost = 9
                        }
                    });
                }
                return por;
            }
            PartOrderResponse partOrderResponse = null;
            var method = "POST";
            var contentType = "text/xml; charset=utf-8";
            var requestBody = dotnetscrape_lib.Utilities.SerializeObject(request, false, true);
            var response = WebClient.Submit(Config.DotNetB2BApiUrl,
                method,
                requestBody,
                contentType)?.Result;
            if (!response.HasError)
            {
                partOrderResponse = dotnetscrape_lib.Utilities.Deserialize<PartOrderResponse>(response.Result, false);
                _logger.LogDebug($"{Environment.NewLine}{dotnetscrape_lib.Utilities.SerializeObject(partOrderResponse, false, true)}{Environment.NewLine}");
            }
            else
            {
                _logger.LogDebug($"{Environment.NewLine}Response Error: {response.Error}{Environment.NewLine}Result:{Environment.NewLine}{response.Result}{Environment.NewLine}");
            }
            return partOrderResponse;
        }

        private static InvoiceData GetInvoiceDataFromDotNet(string po_Number, bool manualPO)
        {
            _logger.LogInfo("[START] - GetInvoiceDetailByPO");

            var invoiceData = new InvoiceData
            {
                Summary = new POInvoiceResponse(),
                Details = new List<InvoiceDetailResponse>(),
                DataStatus = InvoiceData.InvoiceDataStatusEnum.Incomplete
            };

            var piRequest = new POInvoiceRequest
            {
                PONumber = po_Number,
                AccountPassword = Config.AccountPassword,
                StoreID = Config.StoreID
            };
            string poType = (manualPO) ? "ManualPO" : "AutoPO";
            _logger.LogInfo($"Getting Invoice Data for {poType} {po_Number}");


            var method = "POST";
            var contentType = "text/xml; charset=utf-8";

            //Submit Request for POInvoice
            var requestBody = dotnetscrape_lib.Utilities.SerializeObject(piRequest, false, true);
            var response = WebClient.Submit(Config.DotNetB2BApiUrl,
                method,
                requestBody,
                contentType)?.Result;
            if (!response.HasError)
            {
                var summary = dotnetscrape_lib.Utilities.Deserialize<POInvoiceResponse>(response.Result, false);
                if (summary.StatusCode != 0)
                {
                    _logger.LogDebug($"[END] - GetInvoiceDetailByPO - Response from server: {summary.StatusMessage}");
                    return invoiceData;
                }

                //Set Summary Portion of InvoiceData
                invoiceData.Summary = summary;

                _logger.LogDebug($"{Environment.NewLine}{dotnetscrape_lib.Utilities.SerializeObject(invoiceData.Summary, false, true)}{Environment.NewLine}");

                foreach (var invoice in invoiceData.Summary.POInvoice)
                {
                    _logger.LogInfo($"Getting Invoice Detail for PO: {invoiceData.Summary.PONumber} and InvoiceNumber {invoice.InvoiceNumber}");

                    var invoiceDetailRequest = new InvoiceDetailRequest
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        AccountPassword = Config.AccountPassword,
                        StoreID = Config.StoreID
                    };

                    requestBody = dotnetscrape_lib.Utilities.SerializeObject<InvoiceDetailRequest>(invoiceDetailRequest, false, true);

                    //Submit Request for Invoice Detail
                    response = WebClient.Submit(Config.DotNetB2BApiUrl,
                                method,
                                requestBody,
                                contentType)?.Result;

                    _logger.LogInfo(response.Result);


                    if (!response.HasError)
                    {
                        var detail = dotnetscrape_lib.Utilities.Deserialize<InvoiceDetailResponse>(InvoiceDetailResponse.CleanXML(response.Result), false);

                        if (detail.StatusCode != 0)
                        {
                            _logger.LogDebug($"[END] - GetInvoiceDetailByPO - Response from server: {detail.StatusMessage}");
                            continue;
                        }

                        //Temporary Fix - 7-13-2020
                        detail.InvoiceDetailLine.RemoveAll(x => string.Equals(x.LineAbbrev, "7.0", StringComparison.OrdinalIgnoreCase));

                        invoiceData.Details.Add(detail);

                        _logger.LogDebug($"{Environment.NewLine}{dotnetscrape_lib.Utilities.SerializeObject(detail, false, true)}{Environment.NewLine}");
                    }
                    else
                    {
                        _logger.LogDebug($"{Environment.NewLine}Response Error: {response.Error}{Environment.NewLine}Result:{Environment.NewLine}{response.Result}{Environment.NewLine}");
                    }
                }

                invoiceData.DataStatus = InvoiceData.InvoiceDataStatusEnum.Complete;

            }
            else
            {
                if (LogLevel.DEBUG >= Config.LogLevel)
                {
                    _logger.LogDebug($"{Environment.NewLine}Response Error: {response.Error}{Environment.NewLine}Result:{Environment.NewLine}{response.Result}{Environment.NewLine}");
                }
            }
            _logger.LogInfo("[DONE] - GetInvoiceDetailByPO");
            return invoiceData;
        }

        private static List<PartPurchase> GeneatePartPurchases(List<InvoiceDetailResponse> invoiceDetails)
        {
            var partpurchases = new Dictionary<string, PartPurchase>();
            
            if (invoiceDetails == null || invoiceDetails.Count == 0) return partpurchases.Values.ToList();


            foreach(var invoiceDetail in invoiceDetails)
            { 
                _logger.LogDebug($"[START] - GeneratePartPurchases(PO_Number = {invoiceDetail.PONumber}, InvoiceNumber = {invoiceDetail.InvoiceNumber})");

                DateTime.TryParseExact($"{invoiceDetail.InvoiceDate} {invoiceDetail.InvoiceTime}", "MMddyy HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDateTime);
                foreach (var line in invoiceDetail.InvoiceDetailLine)
                {
                    if (string.Equals(line.LineAbbrev, "M", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string key = $"{invoiceDetail.PONumber}{line.SMPartNumber}";
                    int quantityBilled = (line.QtyBilled.HasValue) ? Convert.ToInt32(line.QtyBilled.Value) : 0;
                    if (partpurchases.ContainsKey(key))
                    {
                        partpurchases[key].OrderCount += quantityBilled;
                    }
                    else
                    {
                        var partPurchase = new PartPurchase()
                        {
                            OrderDateTime = orderDateTime,
                            PO_Number = invoiceDetail.PONumber,
                            PartNumber = line.SMPartNumber,
                            OrderCount = Convert.ToInt32(line.QtyBilled),
                            OrderStatus = PartPurchaseOrderStatus.InTransit,
                            DotNetQtyOnHand = Convert.ToInt32(line.QtyBilled),
                            DotNetListPrice = line.UnitPrice ?? 0,
                            DotNetYourCost =  line.UnitPrice ??  0,
                            DotNetPriceType = "PRI",
                            DotNetTAMSErrorMsg = string.Empty,
                            Comments = "Manual Order Entry"
                        };
                        _logger.LogDebug($"Saving PartPurachase Record: InvoiceNumber: {invoiceDetail.InvoiceNumber}, PO_Number: {partPurchase.PartNumber}, PartNumber: {partPurchase.PartNumber}, Quantity: {partPurchase.OrderCount}, ListPrice: {partPurchase.DotNetListPrice}, Cost: {partPurchase.DotNetYourCost}");
                        partpurchases.Add(key, partPurchase);
                    }

                }
                _logger.LogDebug($"[START] - GeneratePartPurchases(PO_Number = {invoiceDetail.PONumber}, InvoiceNumber = {invoiceDetail.InvoiceNumber})");
            }
            
            return partpurchases.Values.ToList();
        }

        private static void ProcessInvoiceDataForManualPurchaseOrders()
        {
            OrdersBL bl = new OrdersBL(_logger)
            {
                ConnectionString = Config.ConnectionString
            };

            var manualPOs = bl.GetManualPurchasesOrdersByStatus(ManualPurchaseOrderProcessingStatusEnum.RequiresProcessing);
            _logger.LogInfo($"*** Processing Invoice Data for {manualPOs.Count} MANUAL purchase orders.");
            foreach (var manualPO in manualPOs)
            {
                var invoiceData = GetInvoiceDataFromDotNet(manualPO.PO_Number,true);

                //If the InvoiceData is not available or there was some error, then just skip.
                if (invoiceData.DataStatus == InvoiceData.InvoiceDataStatusEnum.Incomplete)
                    continue;

                bl.SavePartPurchases(GeneatePartPurchases(invoiceData.Details));

                var partPurchases = bl.GetPartPurchasesByPONumber(manualPO.PO_Number);

                //Save Invoice Summary and PartReceiving Data
                bl.SaveInvoiceData(partPurchases, invoiceData);

                manualPO.ProcessingStatus = ManualPurchaseOrderProcessingStatusEnum.Processed;

                bl.UpdateManualPurchaseOrderStatus(manualPO);
            }

        }

        private static void ProcessInvoiceDataForAutoPurchasedOrders()
        {
            OrdersBL bl = new OrdersBL(_logger)
            {
                ConnectionString = Config.ConnectionString
            };
            var poNumbers = bl.GetDistinctPurchaseNumbersByStatus(PartPurchaseOrderStatus.OnOrder);
            _logger.LogInfo($"*** Processing Invoice Data for {poNumbers.Count} AUTO purchase orders.");
            foreach(var poNumber in poNumbers)
            {
                var invoiceData = GetInvoiceDataFromDotNet(poNumber, false);
                if (invoiceData.DataStatus == InvoiceData.InvoiceDataStatusEnum.Incomplete) continue;
                var partPurchases = bl.GetPartPurchasesByPONumber(poNumber);
                
                //Save Invoice Summary and PartReceiving Data
                bl.SaveInvoiceData(partPurchases, invoiceData);
            }
        }

        private static void GeneartePartPurchaseOneTime()
        {
            _logger.LogDebug("Running One Time PartPurchase Insert Logic");
            if (!Config.RunOneTimeInsertOfPartPurchase || File.Exists("onetimeFile.txt"))
            {
                _logger.LogDebug("One Time PartPurchase Insert Logic already run.  Exiting Application.");
                return;
            }
            var invoiceXMLs = Invoices.Data.Split("#########").Select(s => s.Trim("\n".ToCharArray()).Trim()).ToList();
            var invoices = new List<InvoiceDetailResponse>();

            
            foreach (var xml in invoiceXMLs)
            {
                var detail = dotnetscrape_lib.Utilities.Deserialize<InvoiceDetailResponse>(InvoiceDetailResponse.CleanXML(xml), false);
                //Temporary Fix - 7-13-2020
                detail.InvoiceDetailLine.RemoveAll(x => string.Equals(x.LineAbbrev, "7.0", StringComparison.OrdinalIgnoreCase));

                invoices.Add(detail);
            }
            _logger.LogDebug($"Inserting PartPurchase Records for {invoices.Count()} invoice detail records.");
            var partpurchases = new Dictionary<string,PartPurchase>();
            OrdersBL bl = new OrdersBL(_logger)
            {
                ConnectionString = Config.ConnectionString
            };
            foreach(var invoice in invoices)
            {
                DateTime.TryParseExact($"{invoice.InvoiceDate} {invoice.InvoiceTime}", "MMddyy HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDateTime);
                foreach (var line in invoice.InvoiceDetailLine)
                {
                    if (string.Equals(line.LineAbbrev,"M",StringComparison.OrdinalIgnoreCase))
                        continue;

                    string key = $"{invoice.PONumber}{line.SMPartNumber}";
                    if (partpurchases.ContainsKey(key))
                    {
                        partpurchases[key].OrderCount += Convert.ToInt32(line.QtyBilled);
                    }
                    else
                    {
                        var partPurchase = new PartPurchase()
                        {
                            OrderDateTime = orderDateTime,
                            PO_Number = invoice.PONumber,
                            PartNumber = line.SMPartNumber,
                            OrderCount = Convert.ToInt32(line.QtyBilled),
                            OrderStatus = PartPurchaseOrderStatus.InTransit,
                            DotNetQtyOnHand = Convert.ToInt32(line.QtyBilled),
                            DotNetListPrice = line.UnitPrice ?? 0,
                            DotNetYourCost = line.UnitPrice ?? 0,
                            DotNetPriceType = "PRI",
                            DotNetTAMSErrorMsg = string.Empty,
                            Comments = "Manual Order Entry"
                        };
                        _logger.LogDebug($"Saving PartPurachase Record: InvoiceNumber: {invoice.InvoiceNumber}, PO_Number: {partPurchase.PartNumber}, PartNumber: {partPurchase.PartNumber}, Quantity: {partPurchase.OrderCount}, ListPrice: {partPurchase.DotNetListPrice}, Cost: {partPurchase.DotNetYourCost}");
                        partpurchases.Add(key,partPurchase);
                    }
                    
                }
            }
            bl.SavePartPurchases(partpurchases.Values.ToList());
            File.Create("onetimeFile.txt");
        }

        private static void CleanupPartPurchases()
        {
            _logger.LogDebug("[START] - CleanupDuplicatePartPurchases()");
            if (!Config.CleanUpPartPurchases)
            {
                _logger.LogDebug("[END]  - CleanupDuplicatePartPurchases() - FEATURE DISABLED (See CleanUpPartPurchases setting in the smp_settings table)");
                return;
            }

            OrdersBL bl = new OrdersBL(_logger)
            {
                ConnectionString = Config.ConnectionString
            };

            bl.CleanupDuplicatePartPurchases();
            _logger.LogDebug("[DONE] - CleanupDuplicatePartPurchases()");
        }
        #endregion
    }
}
