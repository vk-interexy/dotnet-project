using System;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DOTNETScrape.DataObjects
{
    [Serializable]
    public class AutoPart
    {

        public static string CSVHeader
            = $"PartNumber,PartName,ProductLine,Category,SubCategory,{AutoPartPricing.CSVHeader},Quantity," +
              $"Features And Benefits,Feature And Benefits Url,Feature And Benefits URL Text," + 
              $"Warranty,Waranty URL,MSDS,MSDS Url,Image URL(Hi-Res),Part Detail URL, Manufacturer, Manufacturer Part Number";
        private string ConvertToCSV()
        {
            return
                $"{Utilities.GenerateCSVString(PartNumber)}," +
                $"{Utilities.GenerateCSVString(PartName)}," +
                $"{Utilities.GenerateCSVString(ProductLine)}," +
                $"{Utilities.GenerateCSVString(Category)}," +
                $"{Utilities.GenerateCSVString(SubCategory)}," +
                $"{Pricing.CSV}," +
                $"{Quantity.ToString("0.00")}," +
                $"{Utilities.GenerateCSVString(FeatureAndBenefits)}," +
                $"{Utilities.GenerateCSVString(FeatureAndBenefitsUrl)}," +
                $"{Utilities.GenerateCSVString(FeatureAndBenefitsUrlText)}," +
                $"{Utilities.GenerateCSVString(Warranty)}," +
                $"{Utilities.GenerateCSVString(WarrantyUrl)}," +
                $"{Utilities.GenerateCSVString(Msds)}," +
                $"{Utilities.GenerateCSVString(MsdsUrl)}," +
                $"{Utilities.GenerateCSVString(string.Join("; ", ImageUrls))}," +
                $"{Utilities.GenerateCSVString(DetailUrl)}," +
                $"{Utilities.GenerateCSVString(Manufacturer)}," +
                $"{Utilities.GenerateCSVString(ManufacturerPartNumber)}";
        }
        public AutoPart()
        {
            ProductURL = string.Empty;
            PartNumber = string.Empty;
            PartName = string.Empty;
            ProductLine = string.Empty;
            Pricing = new AutoPartPricing();
            Quantity = 0;
            FeatureAndBenefits = string.Empty;
            Attributes = new List<AutoPartAttribute>();
            ImageUrls = new List<string>();
            Warranty = string.Empty;
            Msds = string.Empty;
            WarrantyUrl = string.Empty;
            MsdsUrl = string.Empty;
            DetailUrl = string.Empty;
            FeatureAndBenefitsUrl = string.Empty;
            FeatureAndBenefitsUrlText = string.Empty;
        }

        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public string ProductLine { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
        public string SubCategory { get; set; }
        public int SubCategoryId { get; set; }
        public AutoPartPricing Pricing { get; set; }
        public double Quantity { get; set; }
        public string FeatureAndBenefits { get; set; }
        public List<AutoPartAttribute> Attributes { get; set; }
        public string Warranty { get; set; }
        public string Msds { get; set; }
        #region URL Properties
        public string ProductURL { get; set; }
        public List<string> ImageUrls { get; set; }
        public string WarrantyUrl { get; set; }
        public string MsdsUrl { get; set; }
        public string DetailUrl { get; set; }
        public string FeatureAndBenefitsUrl { get; set; }
        public string FeatureAndBenefitsUrlText { get; set; }
        public string Manufacturer
        {
            get
            {
                string ret = string.Empty;
                try
                {

                    var attr = Attributes.Where(a => a.Name.Equals(Constants.Manufacturer, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    ret = (attr != null) ? attr.Value : string.Empty;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                return ret;

            }
        }
        public string ManufacturerPartNumber
        {
            get
            {
                string ret = string.Empty;
                try
                {

                    var attr = Attributes.Where(a => a.Name.Equals(Constants.ManufacturerPartNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    ret = (attr != null) ? attr.Value : string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                return ret;
            }
        }
        #endregion

        [XmlIgnore]
        public string CSV { get { return ConvertToCSV(); } }

        public override string ToString()
        {
            var attributesStr = string.Empty;
            foreach(var attrb in Attributes)
            {
                attributesStr += $"{attrb}{Environment.NewLine}";
            }
            return $"------------------------------------------------------------------------------------{Environment.NewLine}" +
                $"PartName: {PartName}{Environment.NewLine}" +
                   $"PartNumber: {PartNumber}{Environment.NewLine}" +
                   $"ProductLine: {ProductLine}{Environment.NewLine}" +
                   $"Pricing Info:{Environment.NewLine}{Pricing}{Environment.NewLine}" +
                   $"Quantity Available: {Quantity}{Environment.NewLine}" +
                   $"Features & Benefits: {FeatureAndBenefits}{Environment.NewLine}" +
                   $"Features and Benefits URL: {FeatureAndBenefitsUrl}{Environment.NewLine}" +
                   $"Features and Benefits URL Text: {FeatureAndBenefitsUrlText}{Environment.NewLine}" +
                   $"Attributes:{Environment.NewLine}\t{attributesStr}{Environment.NewLine}" +
                   $"WarrantyText: {Warranty}{Environment.NewLine}" +
                   $"Warranty URL: {WarrantyUrl}{Environment.NewLine}" +
                   $"Material Safty Data Sheet: {Msds}{Environment.NewLine}" +
                   $"MSDS URL: {MsdsUrl}{Environment.NewLine}" +
                   $"Product Image URL: {string.Join("; ", ImageUrls)}{Environment.NewLine}" +
                   $"Product URL: {ProductURL}{Environment.NewLine}" + 
                   $"Part Detail URL: {DetailUrl}{Environment.NewLine}" +
                   $"------------------------------------------------------------------------------------{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}";
        }
    }
}
