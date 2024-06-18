using System;
namespace dotnetscrape_constants
{
    public class Constants
    {
        public const string ManufacturerPartNumber = @"Manufacturer Part Number";
        public const string Manufacturer = @"Manufacturer";
        public const string UNSPSC = @"UNSPSC";
        public const int partsPerPage = 50;
        public const int pagesPerBatch = 20;
        public const string FullSearchCategoryName = @"FullSearchCategory";
        public const string FullSearchSubCategoryName = @"FullSearchSubCategory";
        public const string QuantityHeaderString = @"Quantity";
        public const string MiscellaneousChargesString = @"MISCHG";
        public const string DCQTYPartMessage = @"DCQTY";
        public const string EachString = @"Each";
        public const string DotNetB2BApiUrl = @"http://gateway.dotnetprolink.com/b2bBridgetest?request=xml&service=POSServiceI&SDK=HCP_Custom";
        public const string NoStore = @"NOSTORE";
        public const string NoInvoice = @"NOINVOICE";
        public const string NoneReceived = @"NoneReceived";
        public static DateTime DefaultDate = new DateTime(2777, 1, 1);
        public static decimal ZeroAsDecimal = (decimal)0.00;

    }

    public class SKUInventoryContants
    {
        public const double DaysInStockDefault = 7;
        public const double SalesVelocityInDaysDefault = 30;
        public const double LeadTimeInDaysDefault = 5;
        public const double ForecastedGrowthPercentageDefault = 0;
        public const double ReorderBufferInDaysDefault = 0;
        public const int PackageQuantityDefault = 1;
        public const string OrderStatusDefault = "None";
        public const double Zero = 0.0;
        public const string OrderStatusOnOrder = "OnOrder";
        public const string OrderStatusOnOrderPlaceholder = "##OnOrder##";
        public const string OrderStatusInTransit = "InTransit";
        public const string OrderStatusInvoiced = "Invoiced";
        public const string OrderStatusInTransitPlaceholder = "##InTransit##";
        public const string CalculationMethodDefault = "UsingOrderCount";
    }
    
}
