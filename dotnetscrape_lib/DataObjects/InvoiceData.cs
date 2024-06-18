using System;
using System.Collections.Generic;
using System.Text;

namespace dotnetscrape_lib.DataObjects
{
    public class InvoiceData
    {
        public enum InvoiceDataStatusEnum
        {
            Incomplete,
            Complete
        };

        public InvoiceData ()
        {
            Summary = new POInvoiceResponse();
            Details = new List<InvoiceDetailResponse>();
            DataStatus = InvoiceDataStatusEnum.Incomplete;
        }

        public POInvoiceResponse Summary { get; set; }
        public List<InvoiceDetailResponse> Details { get; set; }
        
        public InvoiceDataStatusEnum DataStatus { get; set; }
    }
}
