using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public partial class InvoiceDetailResponse
{ 
    public InvoiceDetailResponse()
    {
        InvoiceDetailLine = new List<InvoiceDetailResponseInvoiceDetailLine>();
    }
    public string StoreID { get; set; }
    
    /* CASH, CHG or VOID */
    public string TransactionType { get; set; }

    public string InvoiceNumber { get; set; }

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
    public string ErrorMsg { get; set; }

    public decimal? InvoiceTotal { get; set; }
   
    public string InvoiceDate { get; set; }
    
    public string InvoiceTime { get; set; }
    
    public int CounterPersonID { get; set; }
  
    public int SalesPersonID { get; set; }
    
    public decimal OtherCharges { get; set; }
    
    public decimal NonTaxableTotal { get; set; }
   
    public decimal TaxableTotal { get; set; }

    public decimal Tax1Total { get; set; }

    public decimal Tax2Total { get; set; }

    public decimal AdjustmentTotal { get; set; }

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
    public string Attention { get; set; }
    
    public string PONumber { get; set; }
    
    [XmlElementAttribute("InvoiceDetailLine")]
    public List<InvoiceDetailResponseInvoiceDetailLine> InvoiceDetailLine { get; set; }

    [XmlAttributeAttribute()]
    public int StatusCode { get; set; }

    [XmlAttributeAttribute()]
    public string StatusMessage { get; set; }

    //This is a temporary fix to overcome a syntax issue.
    //Temporary Fix - 7-13-2020
    public static string CleanXML(string xml)
    {
        return xml.Replace("<InvoiceDetailLine><LineAbbrev>7.0</LineAbbrev><PartNumber>0</PartNumber><QtyBilled>1  5.400</QtyBilled><Taxed/><UnitPrice/></InvoiceDetailLine>", string.Empty);
    }
}


public partial class InvoiceDetailResponseInvoiceDetailLine
{
    private string lineAbbrev = string.Empty;
    private string partNumber = string.Empty;
    private string taxed = string.Empty;
    private string unitPriceAsString = "0.00";
    private string qtyBilledAsString = "0.00";

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


    [XmlElement(ElementName = "QtyBilled")]
    public string QtyBilledAsString
    {
        get
        {
            return dotnetscrape_lib.Utilities.EnsureDecimalAsString(qtyBilledAsString);
        }
        set
        {
            qtyBilledAsString = dotnetscrape_lib.Utilities.EnsureDecimalAsString(value);
        }
    }

    [XmlIgnore]
    public decimal? QtyBilled
    {
        get
        {
            return decimal.Parse(qtyBilledAsString);
        }
        set
        {
            qtyBilledAsString = value?.ToString("0.00");
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]

    public string Taxed
    {
        get
        {
            return taxed.Trim();
        }
        set
        {
            taxed = value.Trim();
        }
    }

    [XmlElement(ElementName = "UnitPrice")]
    public string UnitPriceAsString
    {
        get
        {
            return dotnetscrape_lib.Utilities.EnsureDecimalAsString(unitPriceAsString);
        }
        set
        {
            unitPriceAsString = dotnetscrape_lib.Utilities.EnsureDecimalAsString(value);
        }
    }

    [XmlIgnore]
    public decimal? UnitPrice {
        get
        {
            return decimal.Parse(unitPriceAsString);
        }
        set
        {
            unitPriceAsString = value?.ToString("0.00");
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
    public string InvoiceMessageLine { get; set; }

}
