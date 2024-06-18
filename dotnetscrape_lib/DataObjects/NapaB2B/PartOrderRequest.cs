using System;
using System.Xml.Serialization;
using System.Collections.Generic;

[Serializable]
public partial class PartOrderRequest {
    
    public PartOrderRequest()
    {
        PartOrderIn = new List<PartOrderRequestPartOrderIn>();
    }
    public string StoreID { get; set; }

    public string AccountPassword { get; set; }

    [XmlElementAttribute("PartOrderIn")]
    public List<PartOrderRequestPartOrderIn> PartOrderIn { get; set; }

    public string PONumber { get; set; }
}


public partial class PartOrderRequestPartOrderIn
{
    public string LineAbbrev { get; set; }
    public string PartNumber { get; set; }
    public decimal? OrderQty { get; set; }

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
    public string PartMessage { get; set; }

    [XmlIgnore]
    public string SMPartNumber => $"{LineAbbrev} {PartNumber}";

    [XmlIgnore]
    public bool OrderCountOverriden { get; set; }
}
