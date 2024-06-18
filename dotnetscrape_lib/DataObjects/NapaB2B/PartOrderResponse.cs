using System;
using System.Xml.Serialization;
using System.Collections.Generic;



public partial class PartOrderResponse {
    
    public PartOrderResponse()
    {
        PartOrderOut = new List<PartOrderResponsePartOrderOut>();
    }
    public string StoreID { get; set; }
    
    [XmlElementAttribute("PartOrderOut")]
    public List<PartOrderResponsePartOrderOut> PartOrderOut { get; set; }


    [XmlAttributeAttribute()]
    public int StatusCode { get; set; }

    [XmlAttributeAttribute()]
    public string StatusMessage { get; set; }
}


public partial class PartOrderResponsePartOrderOut
{    
    public PartOrderResponsePartOrderOut()
    {
        Price = new PartOrderResponsePartOrderOutPrice();
        dotnetNetQtyOnHand = 0;
        lineAbbrev = string.Empty;
        partNumber = string.Empty;
        tamsErrorMsg = string.Empty;

    }

    private Decimal dotnetNetQtyOnHand;
    private string lineAbbrev;
    private string partNumber;
    private string tamsErrorMsg;

    public string LineAbbrev
    {
        get
        {
            return lineAbbrev.Trim();
        }
        set
        {
            lineAbbrev = value.Trim();
        }
    }

    public string PartNumber
    {
        get
        {
            return partNumber.Trim();
        }
        set
        {
            partNumber = value.Trim();
        }
    }
    [XmlIgnore]
    public string SMPartNumber => $"{LineAbbrev} {PartNumber}";

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
    public string TAMSErrorMsg
    {
        get
        {
            return tamsErrorMsg.Trim();
        }
        set
        {
            tamsErrorMsg = value.Trim();
        }
    }

    public decimal? QtyOnHand { get; set; }

    //When writing back to the parts table and this value is negative, this Count Value in parts table will show zero but
    //this value will show the negative quantity
    public decimal? DotNetNetQtyOnHand => dotnetNetQtyOnHand;

    public PartOrderResponsePartOrderOutPrice Price { get; set; }

    public void CalculateDotNetNetQtyOnHand(decimal qtyOrdered)
    {
        if (QtyOnHand.HasValue)
        {
            dotnetNetQtyOnHand = QtyOnHand.Value - qtyOrdered;
        }
    }
}


public partial class PartOrderResponsePartOrderOutPrice {

    public PartOrderResponsePartOrderOutPrice()
    {
        priceType = string.Empty;
    }
    private string priceType;
    public string PriceType
    {
        get
        {
            return priceType.Trim();
        }
        set
        {
            priceType = value.Trim();
        }
    }
    public decimal? ListPrice { get; set; }
    public decimal? YourCost { get; set; }
}
