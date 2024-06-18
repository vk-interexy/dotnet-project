using System;
using System.Collections.Generic;
using System.Text;

namespace dotnetscrape_lib.DataObjects.DotNetB2B
{
    /// <remarks/>
    [Serializable]
    public partial class PriceAvailabilityResponsePartPriceAvailabilityPrice
    {
        public string PriceType { get; set; }        
        public decimal? ListPrice { get; set; }
        public decimal? YourCost { get; set; }
    }
}
