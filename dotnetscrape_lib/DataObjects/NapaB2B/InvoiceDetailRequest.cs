using System;


[Serializable]
public partial class InvoiceDetailRequest
{
    public string StoreID { get; set; }
    public string AccountPassword { get; set; }
    public string InvoiceNumber { get; set; }
}
