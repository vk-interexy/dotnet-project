using System;
using System.Collections.Generic;

namespace dotnetscrape_lib.DataObjects.DotNetB2B
{
    [Serializable]
    public partial class PriceAvailabilityResponsePartPriceAvailability
    {
        public PriceAvailabilityResponsePartPriceAvailability()
        {
            Price = new List<PriceAvailabilityResponsePartPriceAvailabilityPrice>();
        }

        public string LineAbbrev { get; set; }

        public string PartNumber { get; set; }

        public string TAMSErrorMsg { get; set; }

        public decimal? QtyOnHand { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("Price")]
        public List<PriceAvailabilityResponsePartPriceAvailabilityPrice> Price { get; set; }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public decimal? DCBalanceQty { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public string DeliveryTime { get; set; }

        [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
        public string PartDescription { get; set; }
    }
}
