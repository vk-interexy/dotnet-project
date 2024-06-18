using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace DOTNETScrape.DataObjects
{
    [Serializable]
    public class AutoPartPricing
    {
        public static string CSVHeader = $"List,Core,Cost,Unit";
        private string ConvertToCSV()
        {
            return $"{List},{Core},{Cost},{Utilities.GenerateCSVString(Unit)}";
        }
        public AutoPartPricing()
        {
            List = 0;
            Core = 0;
            Cost = 0;
            Unit = "Each";
        }
        public decimal List { get; set; }
        public decimal Core { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
        public bool HasCore { get; set; }

        public string CSV
        {
            get {  return ConvertToCSV();}
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
