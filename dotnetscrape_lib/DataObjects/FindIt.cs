using System.Collections.Generic;

namespace dotnetscrape_lib.DataObjects
{
    public class FindIt
    {
        public Dictionary<string, string> CheckDC { get; set; }
        public Dictionary<string, string> CheckSupplier { get; set; }
        public IEnumerable<Dictionary<string, string>> GetStoreQtys { get; set; }

        public static string GetLineCode(AutoPart part)
        {
            int i = part.PartNumber.IndexOf(' ');
            return part.PartNumber.Substring(0, i).Trim();
        }

        public static string GetPartNumber(AutoPart part)
        {
            int i = part.PartNumber.IndexOf(' ');
            return part.PartNumber.Substring(i).Trim();
        }
        
        public FindIt Clone()
        {
            FindIt finditClone = new FindIt();
            finditClone.CheckDC = Utilities.CloneDictionary(CheckDC);
            finditClone.CheckSupplier = Utilities.CloneDictionary(CheckSupplier);
            var list = new List<Dictionary<string, string>>();
            foreach(var dict in GetStoreQtys)
            {
                list.Add(Utilities.CloneDictionary(dict));
            }
            finditClone.GetStoreQtys = list;
            return finditClone;
        }
    }
}
