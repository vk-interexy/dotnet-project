using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using dotnetscrape_constants;

namespace HCPDotNetDAL
{
    public static class DataBaseValueForDAL
    {
        public static T GetValue<T>(DataRow row, string columnName, object defaultValue)
        {
            if (row == null)
                throw new NullReferenceException("row cannot be null");
            if (row.Table == null)
                throw new NullReferenceException("row.Table cannot be null");
            if (!row.Table.Columns.Contains(columnName)) return (T)defaultValue;
            if (row[columnName].GetType() == typeof(T) && !row.IsNull(columnName))
            {
                object val = row[columnName];
                if (val.GetType() == typeof(string))
                {
                    val = val.ToString().Trim();
                    return (T)val;
                }
                else
                {
                    return (T)row[columnName];
                }
            }
            return (T)defaultValue;
        }
    }

    public class PartReceivingIdAndStatus
    {
        public string ReceivingStatus { get; set; }
        public long PartReceivingId { get; set; }
    }


    public class OrdersDA : IDisposable
    {
        private Database CreateDatabase()
        {
            return new Database() { ConnectionString = ConnectionString };
        }

        public string ConnectionString { get; set; }

        public void Dispose()
        {

        }

        public long GetNextPO_Number()
        {
            Database db = CreateDatabase();

            var sql = "get_Next_PO_Number";
            DataTable table = db.GetDataTableSp(sql);
            return (long)table.Rows[0]["PO_Number"];

        }

        public DataTable GetOnOrderInTransitCountByForParts()
        {
            Database db = CreateDatabase();
            var sql = OrderDASqlStrings.OnOrderInTransitCountSql
                .Replace(SKUInventoryContants.OrderStatusOnOrderPlaceholder, SKUInventoryContants.OrderStatusOnOrder)
                .Replace(SKUInventoryContants.OrderStatusInTransitPlaceholder, SKUInventoryContants.OrderStatusInTransit);
            return db.GetDataTable(sql);
        }

        public double GetSkusSold(string sku, int salesVelocityInDays, bool useQuantitySql = false)
        {
            var sql = (useQuantitySql) ? OrderDASqlStrings.OrderQuantityBySkuAndSalesVelocityInDaysSql :
                OrderDASqlStrings.OrderCountBySkuAndSalesVelocityInDaysSql;
                
            //Expected Format for orderDate is 2020-07-23T00:00:00.0000000 because it is now a string value in ss_orders 
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                },
                new MySqlParameter
                {
                    ParameterName = "@cutoffDate",
                    Value = DateTime.Now.Date.Subtract(new TimeSpan(salesVelocityInDays,0,0,0)).ToString("yyyy-MM-ddT00:00:00.0000000")
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return (double)0.0;
            }
            else
            {
                if (useQuantitySql)
                {
                    decimal val = (decimal)table.Rows[0]["Quantity"];
                    return Decimal.ToDouble(val);
                }
                else
                {
                    long val = (long)table.Rows[0]["Quantity"];
                    return (double)val;
                }

            }
        }

        public double GetActualOnHand(string sku)
        {
            var sql = OrderDASqlStrings.ActualOnHandCountSql;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return (double)0.0;
            }
            else
            {
                decimal val = (decimal)table.Rows[0]["Quantity"];
                return Decimal.ToDouble(val);

            }
        }

        public List<string> GetValidSkusOrderedOverAllTime()
        {
            var list = new List<string>();
            //var sql = $"SELECT DISTINCT result.SKU FROM{Environment.NewLine}" +
            //                            $"({Environment.NewLine}" +
            //                            $"SELECT DISTINCT item1Sku as SKU FROM `ss_orders` where IFNULL(item1Sku, '') <> ''{Environment.NewLine}" +
            //                            $"UNION{Environment.NewLine}" +
            //                            $"SELECT DISTINCT item2Sku as SKU FROM `ss_orders` where IFNULL(item2Sku, '') <> ''{Environment.NewLine}" +
            //                            $"UNION{Environment.NewLine}" +
            //                            $"SELECT DISTINCT item3Sku as SKU FROM `ss_orders` where IFNULL(item3Sku, '') <> ''{Environment.NewLine}" +
            //                            $"UNION{Environment.NewLine}" +
            //                            $"SELECT DISTINCT item4Sku as SKU FROM `ss_orders` where IFNULL(item4Sku, '') <> ''{Environment.NewLine}" +
            //                            $") result{Environment.NewLine} INNER JOIN `partskumaster` psm ON result.SKU = psm.SKU {Environment.NewLine}" +
            //                            $"WHERE result.SKU NOT IN (SELECT DISTINCT SKU from `partskuinventorymetrics`)";
            var sql = "sp_GetInitialSKUOrderMetrics";

            Database db = CreateDatabase();
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@maxSalesVelocityDays",
                    Value = 50000
                }
            };
            var table = db.GetDataTableSp(sql, parameters);

            foreach (DataRow row in table.Rows)
            {
                list.Add(row["SKU"] as string);
            }
            return list;
        }

        public DataTable GetPartSku(string sku)
        {
            var sql = "Select * from `dotnet`.`partskumaster` where SKU = @sku";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };

            Database db = CreateDatabase();
            return db.GetDataTable(sql, parameters);
        }

        public DataTable GetPartSkuInventoryTarget(string sku)
        {
            var sql = "Select * from `dotnet`.`partskuinventorytarget` where SKU = @sku";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };

            Database db = CreateDatabase();
            return db.GetDataTable(sql, parameters);
        }

        public DataTable GetPartInventoryTargetWithOrderCountOverride()
        {
            Database db = CreateDatabase();
            return db.GetDataTable(OrderDASqlStrings.GetPartInventoryTargetWithOrderCountOverrideSql, null);
        }

        public void ResetPartInventoryTargetOrderCountOverride(string partNumber)
        {
            var sql = OrderDASqlStrings.ResetPartInventoryTargetOrderCountOverrideSql;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                }
            };

            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);
        }
        public long InventorySkuMetricsIndex(string sku)
        {
            var sql = "Select PartSkuInventoryMetricId from `dotnet`.`partskuinventorymetrics` where SKU = @sku LIMIT 1;";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(sql, parameters);
            return (table.Rows.Count == 0) ? -1 : (long)table.Rows[0]["PartSkuInventoryMetricId"];
        }
        public bool InventorySkuMetricExists(string sku)
        {
            return (InventorySkuMetricsIndex(sku) > -1);
        }
        public long InsertSkuInventoryMetric(string sku)
        {
            long index = InventorySkuMetricsIndex(sku);
            if (index > -1) return index;

            var sql = "INSERT INTO `dotnet`.`partskuinventorymetrics` (SKU) VALUES (@sku)";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };

            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);

            return InventorySkuMetricsIndex(sku);
        }

        private DataTable GetSkuInventoryMetrics(string whereClause, List<MySqlParameter> parameters)
        {
            var sql = OrderDASqlStrings.GetPartInventorySkuMetricQueryPredicate + " " + whereClause;
            Database db = CreateDatabase();
            return db.GetDataTable(sql, parameters);
        }

        public DataTable GetSkuInventoryMetricBySku(string sku)
        {
            var whereClause = $"WHERE psim.SKU = @sku";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@sku",
                    Value = sku
                }
            };
            return GetSkuInventoryMetrics(whereClause, parameters);
        }

        public DataTable GetSkuInventoryMetricByOrderingStatus(string orderingStatus)
        {
            var whereClause = $"WHERE psim.OrderingStatus = @orderingStatus";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@orderingStatus",
                    Value = orderingStatus
                }
            };
            return GetSkuInventoryMetrics(whereClause, parameters);
        }

        public void InsertPartPurchase(string po_Number, string partNumber,
                                        int orderCount, DateTime orderDateTime,
                                        string orderStatus, int dotnetQtyOnHand, int dotnetNetQtyOnHand,
                                        string dotnetPriceType, decimal dotnetListPrice, decimal dotnetYourCost,
                                        string dotnetTAMSErrorMsg,
                                        string comments,
                                        bool orderCountOverriden
                                        )
        {
            var sql = OrderDASqlStrings.InsertPartPurchaseSql;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@orderCount",
                    Value = orderCount
                },
                new MySqlParameter
                {
                    ParameterName = "@orderDateTime",
                    Value = orderDateTime
                },
                new MySqlParameter
                {
                    ParameterName = "@orderStatus",
                    Value = orderStatus
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetQtyOnHand",
                    Value = dotnetQtyOnHand
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetNetQtyOnHand",
                    Value = dotnetNetQtyOnHand
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetPriceType",
                    Value = dotnetPriceType
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetListPrice",
                    Value = dotnetListPrice
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetYourCost",
                    Value = dotnetYourCost
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetTAMSErrorMsg",
                    Value = dotnetTAMSErrorMsg
                },
                new MySqlParameter
                {
                    ParameterName = "@comments",
                    Value = comments
                },
                 new MySqlParameter
                {
                    ParameterName = "@orderCountOverriden",
                    Value = (orderCountOverriden) ? 1 : 0
                }
            };
            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);
        }


        public void UpdateSkuInventoryMetricOrderingStatusByPartNumber(string partNumber, string orderingStatus)
        {
            var sql = OrderDASqlStrings.UpdatePartSkuInventoryMetricOrderingStatusByPartSql;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@orderingStatus",
                    Value = orderingStatus
                }
            };

            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);
        }

        public DataTable GetAllSkuInventoryMetrics()
        {
            return GetSkuInventoryMetrics(string.Empty, null);
        }

        public DataTable GetInitialSKUInventoryMetrics(int maxSalesVelocityDays)
        {
            var sql = "sp_GetInitialSKUOrderMetrics";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@maxSalesVelocityDays",
                    Value = maxSalesVelocityDays
                }
            };

            Database db = CreateDatabase();
            return db.GetDataTableSp(sql, parameters);
        }

        public DataTable GetPartPurchasesByPONumber(string po_Number)
        {
            var sql = OrderDASqlStrings.GetPartPurchasesByPONumber;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                }
            };

            Database db = CreateDatabase();
            return db.GetDataTable(sql, parameters);
        }

        public void DeletePartPurchase(long partPurchaseId)
        {
            var sql = OrderDASqlStrings.DeletePartPurchaseSql;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partPurchaseId",
                    Value = partPurchaseId
                }
            };

            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);
        }

        public int DeleteAllPartPurchasesWithOrderCountZero()
        {
            Database db = CreateDatabase();
            return db.UpdateDatabase(OrderDASqlStrings.DeleteAllPartPurchasesWithOrderCountZeroSql, null);
        }

        public void UpdatePartPurchaseCounts(long partPurchaseId, int orderCount, int dotnetQtyOnHand, int dotnetNetQtyOnHand)
        {
            var sql = OrderDASqlStrings.UpdatePartPurchaseCounts;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partPurchaseId",
                    Value = partPurchaseId
                },
                new MySqlParameter
                {
                    ParameterName = "@orderCount",
                    Value = orderCount
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetQtyOnHand",
                    Value = dotnetQtyOnHand
                },
                new MySqlParameter
                {
                    ParameterName = "@dotnetNetQtyOnHand",
                    Value = dotnetNetQtyOnHand
                }
            };

            Database db = CreateDatabase();
            db.UpdateDatabase(sql, parameters);
        }

        public DataTable GetPartAllPurchases()
        {
            Database db = CreateDatabase();
            return db.GetDataTable(OrderDASqlStrings.GetAllPartPurchasesSql, null);
        }
        public void UpdateSkuInventoryMetric(long partSkuInventoryMetricId,
                                                double actualOnHand_SKU,
                                                double actualOnHand_Part,
                                                double leadTimeInDays,
                                                double onOrder_SKU,
                                                double onOrder_Part,
                                                double inTransit_SKU,
                                                double inTransit_Part,
                                                double incomingUnits_SKU,
                                                double incomingUnits_Part,
                                                double daysInStock,
                                                double quantitySoldInLastXDays_SKU,
                                                double quantitySoldInLastXDays_Part,
                                                double salesVelocityInDays,
                                                double forecastedGrowthPercentage,
                                                double salesVelocity_SKU,
                                                double salesVelocity_Part,
                                                double reorderBufferInDays,
                                                double quantityToOrder_SKU,
                                                double quantityToOrder_Part,
                                                string orderingStatus,
                                                DateTime orderDate,
                                                string calculationMethod)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partSkuInventoryMetricId",
                    Value = partSkuInventoryMetricId
                },
                new MySqlParameter
                {
                    ParameterName = "@actualOnHand_SKU",
                    Value = actualOnHand_SKU
                },
                 new MySqlParameter
                {
                    ParameterName = "@actualOnHand_Part",
                    Value = actualOnHand_Part
                },
                new MySqlParameter
                {
                    ParameterName = "@leadTimeInDays",
                    Value = leadTimeInDays
                },
                new MySqlParameter
                {
                    ParameterName = "@onOrder_SKU",
                    Value = onOrder_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@onOrder_Part",
                    Value = onOrder_Part
                },
                new MySqlParameter
                {
                    ParameterName = "@inTransit_SKU",
                    Value = inTransit_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@inTransit_Part",
                    Value = inTransit_Part
                },
                 new MySqlParameter
                {
                    ParameterName = "@incomingUnits_SKU",
                    Value = incomingUnits_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@incomingUnits_Part",
                    Value = incomingUnits_Part
                },

                new MySqlParameter
                {
                    ParameterName = "@daysInStock",
                    Value = daysInStock
                },
                new MySqlParameter
                {
                    ParameterName = "@quantitySoldInLastXDays_SKU",
                    Value = quantitySoldInLastXDays_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@quantitySoldInLastXDays_Part",
                    Value = quantitySoldInLastXDays_Part
                },
                 new MySqlParameter
                {
                    ParameterName = "@salesVelocityInDays",
                    Value = salesVelocityInDays
                },
                new MySqlParameter
                {
                    ParameterName = "@forecastedGrowthPercentage",
                    Value = forecastedGrowthPercentage
                },
                new MySqlParameter
                {
                    ParameterName = "@salesVelocity_SKU",
                    Value = salesVelocity_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@salesVelocity_Part",
                    Value = salesVelocity_Part
                },
                new MySqlParameter
                {
                    ParameterName = "@reorderBufferInDays",
                    Value = reorderBufferInDays
                },
                new MySqlParameter
                {
                    ParameterName = "@quantityToOrder_SKU",
                    Value = quantityToOrder_SKU
                },
                new MySqlParameter
                {
                    ParameterName = "@quantityToOrder_Part",
                    Value = quantityToOrder_Part
                },
                new MySqlParameter
                {
                    ParameterName = "@orderingStatus",
                    Value = orderingStatus
                },
                new MySqlParameter
                {
                    ParameterName = "@orderDate",
                    Value = orderDate
                },
                new MySqlParameter
                {
                    ParameterName = "@calculationMethod",
                    Value = calculationMethod
                }
            };
            #endregion

            Database db = CreateDatabase();
            db.UpdateDatabase(OrderDASqlStrings.UpdatePartSkuInventoryMetricSql, parameters);
        }

        public void UpdatePartPurchaseComments(long partPurchaseId, string comments)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partPurchaseId",
                    Value = partPurchaseId
                },
                new MySqlParameter
                {
                    ParameterName = "@comments",
                    Value = comments
                },

            };
            #endregion

            Database db = CreateDatabase();
            db.UpdateDatabase(OrderDASqlStrings.UpdatePartPurchaseComments, parameters);
        }

        public void UpdatePartPurchaseOrderStatus(long partPurchaseId, string orderStatus)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partPurchaseId",
                    Value = partPurchaseId
                },
                new MySqlParameter
                {
                    ParameterName = "@orderStatus",
                    Value = orderStatus
                },

            };
            #endregion

            Database db = CreateDatabase();
            db.UpdateDatabase(OrderDASqlStrings.UpdatePartPurchaseOrderStatusSql, parameters);
        }

        public DataTable GetManualPurchaseOrderByStatus(string processingStatus)
        {
            var sql = OrderDASqlStrings.GetManualPurchaseOrdersByStatus;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@processingStatus",
                    Value = processingStatus
                }
            };

            Database db = CreateDatabase();
            return db.GetDataTable(sql, parameters);
        }

        public void UpdateManualPurchaseOrderStatus(long manualPurchaseOrderId, string processingStatus, DateTime processedDate)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@manualPurchaseOrderId",
                    Value = manualPurchaseOrderId
                },
                new MySqlParameter
                {
                    ParameterName = "@processingStatus",
                    Value = processingStatus
                },
                 new MySqlParameter
                {
                    ParameterName = "@processedDate",
                    Value = processedDate
                }
            };
            #endregion
            /* UPDATE `dotnet`.`smp_manualpurchaseorders` SET ProcessingStatus = @processingStatus, ProcessedDate = @processedDate WHERE ManualPurchaseOrderId = @manualPurchaseOrderId*/
            Database db = CreateDatabase();
            db.UpdateDatabase(OrderDASqlStrings.UpdateManualPurchaseOrderStatus, parameters);
        }

        public List<string> GetDistinctPurchaseNumbersByStatus(string orderStatus)
        {
            var list = new List<string>();
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@orderStatus",
                    Value = orderStatus
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(OrderDASqlStrings.GetDistinctPONumbersByOrderStatusSql, parameters);
            foreach(DataRow row in table.Rows)
            {
                list.Add(row["PO_Number"] as string);
            }
            return list;
        }

        private long GetInvoiceSummaryIdWithBlankInvoice(string po_Number)
        {
            long ret = -1;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(OrderDASqlStrings.GetInvoiceSummaryIdWithBlankInvoiceByPoNumberSql, parameters);



            if (table == null || table.Rows == null || table.Rows.Count == 0)
                return ret;

            ret = DataBaseValueForDAL.GetValue<long>(table.Rows[0], "InvoiceSummaryId", (long)-1);


            return ret;
        }

        private long GetInvoiceSummaryId(string po_Number, string invoiceNumber)
        {
            long ret = -1;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceNumber",
                    Value = invoiceNumber
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(OrderDASqlStrings.GetInvoiceSummaryIdByPoNumberAndInvoiceNumberSql, parameters);



            if (table == null || table.Rows == null || table.Rows.Count == 0)
                return ret;

            ret = DataBaseValueForDAL.GetValue<long>(table.Rows[0], "InvoiceSummaryId", (long)-1);


            return ret;
        }

        public void SaveInvoiceSummary(string storeID, string po_Number, string invoiceNumber,
                                       string transactionType, string errorMsg, decimal invoiceTotal,
                                       DateTime invoiceDate, int counterPersonID, int salesPersonID,
                                       decimal otherCharges, decimal nonTaxableTotal, decimal taxableTotal,
                                       decimal tax1Total, decimal tax2Total, decimal adjustmentTotal, string attention, bool applyingBlankInvoice)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@storeID",
                    Value = storeID
                },
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceNumber",
                    Value = invoiceNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@transactionType",
                    Value = transactionType
                },
                new MySqlParameter
                {
                    ParameterName = "@errorMsg",
                    Value = (errorMsg == null) ? string.Empty : errorMsg
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceTotal",
                    Value = invoiceTotal
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceDate",
                    Value = invoiceDate
                },
                new MySqlParameter
                {
                    ParameterName = "@counterPersonID",
                    Value = counterPersonID
                },
                new MySqlParameter
                {
                    ParameterName = "@salesPersonID",
                    Value = salesPersonID
                },
                new MySqlParameter
                {
                    ParameterName = "@otherCharges",
                    Value = otherCharges
                },
                new MySqlParameter
                {
                    ParameterName = "@nonTaxableTotal",
                    Value = nonTaxableTotal
                },
                new MySqlParameter
                {
                    ParameterName = "@taxableTotal",
                    Value = taxableTotal
                },
                new MySqlParameter
                {
                    ParameterName = "@tax1Total",
                    Value = tax1Total
                },
                new MySqlParameter
                {
                    ParameterName = "@tax2Total",
                    Value = tax2Total
                },
                new MySqlParameter
                {
                    ParameterName = "@adjustmentTotal",
                    Value = adjustmentTotal
                }
                ,
                new MySqlParameter
                {
                    ParameterName = "@attention",
                    Value = (attention == null) ? string.Empty : attention
                }
            };
            #endregion

            long id = GetInvoiceSummaryIdWithBlankInvoice(po_Number);
            if (applyingBlankInvoice)
            {
                SaveInvoiceSummaryToDatabase(id, parameters);
            }
            else
            {
                if (id == -1)
                {
                    id = GetInvoiceSummaryId(po_Number, invoiceNumber);
                }
                SaveInvoiceSummaryToDatabase(id, parameters);
            }

        }

        private void SaveInvoiceSummaryToDatabase(long invoiceSummaryId, List<MySqlParameter> parameters)
        {
            Database db = CreateDatabase();
            if (invoiceSummaryId == -1)
            {
                db.UpdateDatabase(OrderDASqlStrings.InsertInvoiceSummarySql, parameters);
            }
            else
            {
                parameters.Add(
                new MySqlParameter
                {

                    ParameterName = "@invoiceSummaryId",
                    DbType = DbType.Int64,
                    Value = invoiceSummaryId

                });
                db.UpdateDatabase(OrderDASqlStrings.UpdateInvoiceSummarySql, parameters);
            }
        }

        private long GetPartReceivingIdWithBlankInvoice(string po_Number, string partNumber)
        {
            long ret = -1;
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(OrderDASqlStrings.GetPartReceivingIdWithBlankInvoiceByPoNumberAndPartNumberSql, parameters);
            


            if (table == null || table.Rows == null || table.Rows.Count == 0)
                return ret;

            ret = DataBaseValueForDAL.GetValue<long>(table.Rows[0],"PartRecevingId", (long)-1);


            return ret;
        }

        private PartReceivingIdAndStatus GetPartReceivingId(string po_Number, string partNumber, string invoiceNumber)
        {
            var ret = new PartReceivingIdAndStatus
            {
                ReceivingStatus = SKUInventoryContants.OrderStatusDefault,
                PartReceivingId = -1
            };

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                }
                ,
                new MySqlParameter
                {
                    ParameterName = "@invoiceNumber",
                    Value = invoiceNumber
                }
            };

            Database db = CreateDatabase();
            var table = db.GetDataTable(OrderDASqlStrings.GetPartReceivingIdByPoNumberAndPartNumberAndInoviceNumberSql, parameters);



            if (table == null || table.Rows == null || table.Rows.Count == 0)
                return ret;

            ret.PartReceivingId = DataBaseValueForDAL.GetValue<long>(table.Rows[0], "PartRecevingId", (long)-1);
            ret.ReceivingStatus = DataBaseValueForDAL.GetValue<string>(table.Rows[0], "ReceivingStatus", SKUInventoryContants.OrderStatusDefault);


            return ret;
        }

        private void SavePartReceivingToDatabase(long partReceivingId, List<MySqlParameter> parameters)
        {
            Database db = CreateDatabase();
            if (partReceivingId == -1)
            {
                db.UpdateDatabase(OrderDASqlStrings.InsertPartReceivingSql, parameters);
            }
            else
            { 
                parameters.Add(
                new MySqlParameter
                {

                    ParameterName = "@partReceivingId",
                    DbType = DbType.Int64,
                    Value = partReceivingId

                });
                db.UpdateDatabase(OrderDASqlStrings.UpdatePartReceivingSql, parameters);
            }
        }

        public void SavePartReceiving(string po_Number, string partNumber, int orderCount, string invoiceNumber,
                                       int reportedShippedcount, int actualReceivedCount, int reportVsActualDiff,
                                       DateTime receivedDateTime, string receivingStatus, string receivingNotes,
                                       decimal unitPrice, string taxed, string invoiceMsgLine, bool applyingBlankLineItem)
        {
            #region Parameter Creation
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@po_Number",
                    Value = po_Number
                },
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@orderCount",
                    Value = orderCount
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceNumber",
                    Value = invoiceNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@reportedShippedcount",
                    Value = reportedShippedcount
                },
                new MySqlParameter
                {
                    ParameterName = "@actualReceivedCount",
                    Value = actualReceivedCount
                },
                new MySqlParameter
                {
                    ParameterName = "@reportVsActualDiff",
                    Value = reportVsActualDiff
                },
                new MySqlParameter
                {
                    ParameterName = "@receivedDateTime",
                    Value = receivedDateTime
                },
                new MySqlParameter
                {
                    ParameterName = "@receivingStatus",
                    Value = receivingStatus
                },
                new MySqlParameter
                {
                    ParameterName = "@receivingNotes",
                    Value = receivingNotes
                },
                new MySqlParameter
                {
                    ParameterName = "@unitPrice",
                    Value = unitPrice
                },
                new MySqlParameter
                {
                    ParameterName = "@taxed",
                    Value = taxed
                },
                new MySqlParameter
                {
                    ParameterName = "@invoiceMsgLine",
                    Value = (invoiceMsgLine == null) ? string.Empty : invoiceMsgLine
                }
            };
            #endregion

            long id = GetPartReceivingIdWithBlankInvoice(po_Number, partNumber);
            if (applyingBlankLineItem)
            {
                SavePartReceivingToDatabase(id, parameters);
            }
            else
            {
                bool performUpdate = true;
                if (id == -1)
                {
                    var ret = GetPartReceivingId(po_Number, partNumber, invoiceNumber);
                    id = ret.PartReceivingId;
                    //Do not update status unless it is set to (None, OnOrder, Invoiced, or InTransit) - Preserve status so it does not get overwritten.
                    if(id != -1 && 
                        !string.Equals(ret.ReceivingStatus, SKUInventoryContants.OrderStatusOnOrder,StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ret.ReceivingStatus, SKUInventoryContants.OrderStatusDefault, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ret.ReceivingStatus, SKUInventoryContants.OrderStatusInvoiced, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ret.ReceivingStatus, SKUInventoryContants.OrderStatusInTransit, StringComparison.OrdinalIgnoreCase))
                    {
                        performUpdate = false;  // parameters.Where(p => p.ParameterName == "@receivingStatus").First().Value = ret.ReceivingStatus;
                    }
                }
                if (performUpdate)
                {
                    SavePartReceivingToDatabase(id, parameters);
                }
            }
        }
    }
}
