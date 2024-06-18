using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using dotnetscrape_constants;

namespace dotnetscrape_lib.DataObjects
{
    public class PartInventoryTarget
    {
        public static double DefaultValue = 0;

        public PartInventoryTarget()
        {
            Initialize();
        }
        public PartInventoryTarget(string partNumber)
        {
            Initialize();
            PartNumber = partNumber;
        }

        public PartInventoryTarget(DataTable table)
        {
            if (table.Rows.Count > 0 && table.Columns.Contains("PartInventoryTargetId"))
            {
                Load(table.Rows[0]);
            }
            else
            {
                Initialize();
            }
        }

        public PartInventoryTarget(string partNumber, int daysInStock)
        {
            Initialize();
            PartNumber = partNumber;
            DaysInStock = daysInStock;
        }
        public PartInventoryTarget(string partNumber, int daysInStock, int salesVelocityInDays)
        {
            Initialize();
            PartNumber = partNumber;
            DaysInStock = daysInStock;
            SalesVelocityInDays = salesVelocityInDays;
        }
        public PartInventoryTarget(DataRow row)
        {
            Load(row);
        }

        private void Load(DataRow row)
        {
            Initialize();
            PartInventoryTargetId = DataBaseValue.GetValue<long>(row, "PartInventoryTargetId", (long)-1);
            PartNumber = DataBaseValue.GetValue<string>(row, "PartNumber", string.Empty);
            LeadTimeInDays = DataBaseValue.GetValue<double>(row, "LeadTimeInDays",  SKUInventoryContants.LeadTimeInDaysDefault);
            DaysInStock = DataBaseValue.GetValue<double>(row, "DaysInStock", SKUInventoryContants.DaysInStockDefault);
            SalesVelocityInDays = DataBaseValue.GetValue<double>(row, "SalesVelocityInDays", SKUInventoryContants.SalesVelocityInDaysDefault);
            ReorderBufferInDays = DataBaseValue.GetValue<double>(row, "ReorderBufferInDays", SKUInventoryContants.ReorderBufferInDaysDefault);
            ForecastedGrowthPercentage = DataBaseValue.GetValue<double>(row, "ForecastedGrowthPercentage", SKUInventoryContants.ForecastedGrowthPercentageDefault);
            OrderCountOverride = DataBaseValue.GetValue<int>(row, "OrderCountOverride", 0);
            AutoOrderEnabled = (DataBaseValue.GetValue<long>(row, "AutoOrderEnabled", 0) != 0);

        }

        private void Initialize()
        {
            PartInventoryTargetId = -1;
            PartNumber = string.Empty;
            LeadTimeInDays = SKUInventoryContants.LeadTimeInDaysDefault;
            DaysInStock = SKUInventoryContants.DaysInStockDefault;
            SalesVelocityInDays = SKUInventoryContants.SalesVelocityInDaysDefault;
            ReorderBufferInDays = SKUInventoryContants.ReorderBufferInDaysDefault;
            ForecastedGrowthPercentage = SKUInventoryContants.ForecastedGrowthPercentageDefault;
            OrderCountOverride = 0;
            AutoOrderEnabled = false;
        }

        public long PartInventoryTargetId { get; set; }
        public string PartNumber { get; set; }
        public double LeadTimeInDays { get; set; }
        public double DaysInStock { get; set; }
        public double SalesVelocityInDays { get; set; }
        public double ReorderBufferInDays { get; set; }
        public double ForecastedGrowthPercentage { get; set; }
        public int OrderCountOverride { get; set; }
        public bool AutoOrderEnabled { get; set; }



    }
}
