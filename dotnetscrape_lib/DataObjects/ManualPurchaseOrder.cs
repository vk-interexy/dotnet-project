using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using dotnetscrape_lib;

namespace dotnetscrape_lib.DataObjects
{
    public enum ManualPurchaseOrderProcessingStatusEnum
    {
        RequiresProcessing,
        Processed
    }
    public class ManualPurchaseOrder
    {

        private static readonly DateTime defaultDate = new DateTime(1900, 1, 1);

        private string po_number = string.Empty;


        public ManualPurchaseOrder()
        {
            ProcessingStatus = ManualPurchaseOrderProcessingStatusEnum.RequiresProcessing;
            CreatedDate = DateTime.Now;
            ProcessedDate = defaultDate;
        }

        public ManualPurchaseOrder(DataRow row)
        {
            ManualPurchaseOrderId = DataBaseValue.GetValue<long>(row, "ManualPurchaseOrderId", (long)-1);
            PO_Number = DataBaseValue.GetValue<string>(row, "PO_Number", string.Empty);
            ProcessingStatusAsString = DataBaseValue.GetValue<string>(row, "ProcessingStatus", string.Empty);
            CreatedDate = DataBaseValue.GetValue<DateTime>(row, "CreatedDate", DateTime.Now);
            ProcessedDate = DataBaseValue.GetValue<DateTime>(row, "ProcessedDate", defaultDate);
        }

        public long ManualPurchaseOrderId { get; set; }

        public string PO_Number
        {
            get
            {
                return po_number.Trim();
            }
            set
            {
                po_number = value.Trim();
            }
        }

        public ManualPurchaseOrderProcessingStatusEnum ProcessingStatus;

        public string ProcessingStatusAsString
        {
            get
            {
                return $"{ProcessingStatus}";
            }
            private set
            {
                bool result = Enum.TryParse<ManualPurchaseOrderProcessingStatusEnum>(value.Trim(), out ManualPurchaseOrderProcessingStatusEnum status);
                ProcessingStatus = (result) ? status : ManualPurchaseOrderProcessingStatusEnum.RequiresProcessing;
            }
        }

        public DateTime CreatedDate { get; set; }
        public DateTime ProcessedDate { get; set; }

    }
}
