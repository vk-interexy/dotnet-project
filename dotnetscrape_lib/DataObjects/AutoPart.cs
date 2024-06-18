using System;
using System.Linq;
using System.Collections.Generic;
using dotnetscrape_constants;

namespace dotnetscrape_lib.DataObjects
{
    [Serializable]
    public class AutoPart
    {

        public static string CSVHeader
            = $"PartNumber,PartName,ProductLine,Category,SubCategory,{AutoPartPricing.CSVHeader},Quantity," +
              $"Features And Benefits,Feature And Benefits Url,Feature And Benefits URL Text," + 
              $"Warranty,Waranty URL,MSDS,MSDS Url,Image URL(Hi-Res),Part Detail URL, Manufacturer, Manufacturer Part Number, Store Quantities, DC Quantities, Supplier Quantities,QuantityPricingUpdatedDate";
        public string CSV()
        {
            return
                $"{Utilities.GenerateCSVString(PartNumber)}," +
                $"{Utilities.GenerateCSVString(PartName)}," +
                $"{Utilities.GenerateCSVString(ProductLine)}," +
                $"{Utilities.GenerateCSVString(Category)}," +
                $"{Utilities.GenerateCSVString(SubCategory)}," +
                $"{Pricing.CSV()}," +
                $"{Quantity:0.00}," +
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
                $"{Utilities.GenerateCSVString(ManufacturerPartNumber)},";
        }

        public string EndingFields()
        {
            return $"{Utilities.GenerateCSVString(QuantityPricingUpdatedDate.ToString())}";
            //return $"{Utilities.GenerateCSVString(QuantityPricingUpdatedDate.ToString())},{Utilities.GenerateCSVString(UpdateConfirmed.ToString())}";
        }

        public string CSVWithDCQuantityOnly()
        {
            return $"{CSV()},{FindItQuantities?.CSVDCQtyOnly()},,{EndingFields()}";
        }

        public string CSVWithFinditQuantites()
        {
            return $"{CSV()}{FindItQuantities?.CSV()},{EndingFields()}";
        }
        public AutoPart()
        {
            PartNumber = string.Empty;
            PartName = string.Empty;
            ProductLine = string.Empty;
            Pricing = new AutoPartPricing();
            Quantity = 0;
            Category = string.Empty;
            CategoryId = 0;
            SubCategory = string.Empty;
            SubCategoryId = string.Empty;
            FeatureAndBenefits = string.Empty;
            Attributes = new List<AutoPartAttribute>();
            PartCompatibilities = new List<AutoPartCompatibility>();
            ImageUrls = new List<string>();
            Warranty = string.Empty;
            Msds = string.Empty;
            WarrantyUrl = string.Empty;
            MsdsUrl = string.Empty;
            DetailUrl = string.Empty;
            FeatureAndBenefitsUrl = string.Empty;
            FeatureAndBenefitsUrlText = string.Empty;
        }

        public void ResetAllButPartNumAndCatData()
        {
            PartName = string.Empty;
            ProductLine = string.Empty;
            Pricing = null;
            Quantity = 0;
            FeatureAndBenefits = string.Empty;
            Attributes = null;
            PartCompatibilities = null;
            ImageUrls = null;
            Warranty = string.Empty;
            Msds = string.Empty;
            WarrantyUrl = string.Empty;
            MsdsUrl = string.Empty;
            DetailUrl = string.Empty;
            FeatureAndBenefitsUrl = string.Empty;
            FeatureAndBenefitsUrlText = string.Empty;
        }

        #region General Properties

        public int PartsId { get; set; }

        public string PartNumber { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string PartName { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string ProductLine { get; set; }
        
        public string Category { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public long CategoryId { get; set; }

       
        public string SubCategory { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string SubCategoryId { get; set; }


        /// <summary>
        /// Paritially in Database
        /// </summary>
        public AutoPartPricing Pricing { get; set; }

        private decimal _quantity = 0;
        public decimal Quantity
        {
            get
            {
                return (_quantity >= 0) ? _quantity : 0;
            }
            set
            {
                _quantity = (value >= 0) ? value : 0;
            }
        }

        /// <summary>
        /// Partially in Database
        /// </summary>
        public AutoPartQuantity FindItQuantities { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string FeatureAndBenefits { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public List<AutoPartAttribute> Attributes { get; set; }

        public List<AutoPartCompatibility> PartCompatibilities { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string Warranty { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string Msds { get; set; }

        public DateTimeOffset QuantityPricingUpdatedDate { get; set; }
        

        public bool UpdateConfirmed { get; set; }
        #endregion

        #region URL Properties
        /// <summary>
        /// Not In Database
        /// </summary>
        public List<string> ImageUrls { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string WarrantyUrl { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string MsdsUrl { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string DetailUrl { get; set; }
        public string DetailUrlHash { get { return string.IsNullOrWhiteSpace(DetailUrl) ? string.Empty : Utilities.GetHashSha256(DetailUrl); } }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string FeatureAndBenefitsUrl { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string FeatureAndBenefitsUrlText { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
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

        /// <summary>
        /// Not In Database
        /// </summary>
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


        /// <summary>
        /// Not In Database
        /// </summary>
        public string UNSPSC
        {
            get
            {
                string ret = string.Empty;
                try
                {

                    var attr = Attributes.Where(a => a.Name.Equals(Constants.UNSPSC, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    ret = (attr != null) ? attr.Value : string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                return ret;

            }
        }

        public bool Exists { get; set; }

        #endregion



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
                   $"Category: {Category}{Environment.NewLine}" +
                   $"CategoryId: {CategoryId}{Environment.NewLine}" +
                   $"SubCategory: {SubCategory}{Environment.NewLine}" +
                   $"SubCategoryId: {SubCategoryId}{Environment.NewLine}" +
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
                   $"Part Detail URL: {DetailUrl}{Environment.NewLine}" +
                   $"Part Detail URL Hash: {DetailUrlHash}{Environment.NewLine}" +
                   $"------------------------------------------------------------------------------------{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}";
        }
    }
}
