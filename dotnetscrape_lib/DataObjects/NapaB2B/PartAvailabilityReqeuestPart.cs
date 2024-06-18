using System;
using System.Collections.Generic;
using System.Text;

namespace dotnetscrape_lib.DataObjects.DotNetB2B
{
    [Serializable]
    public partial class PriceAvailabilityRequestPart
    {
        public string LineAbbrev { get; set; }
        public string PartNumber { get; set; }
        public string PartMessage { get; set; }
    }
}
