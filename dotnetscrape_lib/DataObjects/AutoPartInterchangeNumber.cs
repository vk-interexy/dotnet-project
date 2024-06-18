using System;
using System.Collections.Generic;
using System.Text;

namespace dotnetscrape_lib.DataObjects
{
    public class AutoPartInterchangeNumber
    {
        public long PartInterchangeNumberId { get; set; }
        public string PartNumber { get; set; }
        public string InterchangeNumber { get; set; }
        public string InterchangeManufacturer { get; set; }
    }
}
