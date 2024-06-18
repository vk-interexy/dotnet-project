using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.ComponentModel;

namespace dotnetscrape_lib.DataObjects.DotNetB2B
{
    [SerializableAttribute()]
    [DebuggerStepThroughAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class PriceAvailabilityRequest
    {

        public PriceAvailabilityRequest()
        {
            Part = new List<PriceAvailabilityRequestPart>();
        }

        public string StoreID { get; set; }

        public string AccountPassword { get; set; }

        [XmlElementAttribute("Part")]
        public List<PriceAvailabilityRequestPart> Part { get; set; }

        [XmlAttributeAttribute()]
        public string RequestID { get; set; }
    }
}



