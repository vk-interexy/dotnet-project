using System;


namespace dotnetscrape_lib.DataObjects
{
    [Serializable]
    public class AutoPartCompatibility
    {
        private string GetHash()
        {
            return Utilities.GetHashSha256($"{PartNumber}{Make}{Model}{Engine}{StartYear}{EndYear}");
        }
        public int PartCompatibilityID { get; set; }
        public string CompatibilityKey => GetHash();
        public string PartNumber { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Engine { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }

        public AutoPartCompatibility()
        {
            PartCompatibilityID = 0;
            PartNumber = string.Empty;
            Make = string.Empty;
            Model = string.Empty;
            Engine = string.Empty;
            StartYear = 0;
            EndYear = 0;
        }

        public AutoPartCompatibility(string partNumber, string make, string model, string engine, string startendyear)
        {
            PartNumber = partNumber;
            Make = make;
            Model = model;
            Engine = engine;
            if(startendyear.Contains('-'))
            {
                
                var years = startendyear.Split('-');
                if(years.Length == 2)
                {
                    int.TryParse(years[0].Trim(), out int startYear);
                    int.TryParse(years[1].Trim(), out int endYear);
                    StartYear = startYear;
                    EndYear = endYear;
                }
            }
        }

    }
}
