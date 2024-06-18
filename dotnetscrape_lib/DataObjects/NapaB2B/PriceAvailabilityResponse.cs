using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace dotnetscrape_lib.DataObjects.DotNetB2B
{
    /// <remarks/>
    [Serializable]
    
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class PriceAvailabilityResponse
    {
        public PriceAvailabilityResponse()
        {
            PartPriceAvailability = new List<PriceAvailabilityResponsePartPriceAvailability>();
        }

        [XmlElementAttribute(IsNullable = true)]
        public string StoreID { get; set; }

        [XmlElementAttribute("PartPriceAvailability")]
        public List<PriceAvailabilityResponsePartPriceAvailability> PartPriceAvailability { get; set; }

        [XmlAttributeAttribute()]
        public int StatusCode { get; set; }

        [XmlAttributeAttribute()]
        public string StatusMessage { get; set; }
    }
}
