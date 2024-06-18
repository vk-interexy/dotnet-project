using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetscrape_lib.DataObjects
{
    [Serializable]
    public class AutoPartPricing
    {
        public static string CSVHeader = $"List,Core,Cost,Unit";

        private bool _pricingDetailProvided = false;
        public string CSV()
        {
            return $"{List},{Core},{Cost},{Utilities.GenerateCSVString(Unit)}";
        }
        public AutoPartPricing()
        {
            List = 0;
            Core = 0;
            Cost = 0;
            Unit = "Each";
            PricingDetailProvided = false;
        }
        public decimal List { get; set; }
        public decimal Core { get; set; }
        public decimal Cost { get; set; }

        /// <summary>
        /// Not In Database
        /// </summary>
        public string Unit { get; set; }

        public bool HasCore { get; set; }
        public bool PricingDetailProvided
        {
            get { return _pricingDetailProvided || List != 0 || Core != 0 || Cost != 0; }
            set { _pricingDetailProvided = value; }
        }
        
        public override string ToString()
        {
            return $"List: {List}{Environment.NewLine}" +
                    ((HasCore) ? $"Core: {Core}{Environment.NewLine}" : string.Empty) +
                    $"Cost: {Cost}{Environment.NewLine}" +
                    $"Unit: {Unit}{Environment.NewLine}";
        }

    }
}
