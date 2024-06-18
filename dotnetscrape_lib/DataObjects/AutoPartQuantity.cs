using System;
using System.Linq;
using System.Collections.Generic;

namespace dotnetscrape_lib.DataObjects
{
    [Serializable]
    public class AutoPartQuantity
    {
        public IEnumerable<DistributionCenterQuantity> DCQty { get; set; }
        public IEnumerable<SupplierQuantity> SupplierQty { get; set; }
        public IEnumerable<StoreQuantity> StoreQty { get; set; }

        public string CSV()
        {
            string[] csv = new string[3];

            if (this != null)
            {
                csv[0] = (StoreQty != null) ?  Utilities.GenerateCSVString(string.Join("|", StoreQty.OrderBy(qty => qty.Name).Select((qty) => qty.CSV()))) : string.Empty;
                csv[1] = (DCQty != null) ? Utilities.GenerateCSVString(string.Join("|", DCQty.OrderBy(qty => qty.Name).Select((qty) => qty.CSV()))) : string.Empty;
                csv[2] = (SupplierQty != null) ? Utilities.GenerateCSVString(string.Join("|", SupplierQty.OrderBy(qty => qty.Name).Select((qty) => qty.CSV()))) : string.Empty;
            }
            return string.Join(",",csv);
            
        }

        public string CSVDCQtyOnly()
        {
            return (DCQty != null) ? Utilities.GenerateCSVString(string.Join("|", DCQty.OrderBy(qty => qty.Name).Select((qty) => qty.CSV()))) : string.Empty;
        }
    }

    [Serializable]
    public class DistributionCenterQuantity
    {
        public string Name { get; set; }

        private decimal _quantity = 0;

        public decimal Quantity
        {
            get
            {
                return (_quantity >= 0) ? _quantity : 0;
            }
            set
            {
                _quantity = (value >= 0) ? value : 0;
            }
        }
        public string DeliveryTime { get; set; }
        public string CSV()
        {
            return $"{Name}~{Quantity}~{DeliveryTime}";
        }

        public static IEnumerable<DistributionCenterQuantity> FromJSON(string json)
        {
            var dcList = new List<DistributionCenterQuantity>();
            if (string.IsNullOrWhiteSpace(json)) return dcList;
            try
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var blob = obj["d"];
                var parts = blob.Split(new[] { '|' });
                foreach (var part in parts)
                {
                    try
                    {
                        var subparts = part.Split(new[] { ':' });
                        if (subparts.Length != 2) { continue; }
                        var dc = new DistributionCenterQuantity
                        {
                            Name = subparts[0].Trim().TrimStart(']'),
                            Quantity = decimal.Parse(subparts[1].Trim())
                        };
                        dcList.Add(dc);
                    }
                    catch { }
                }
            }
            catch { }
            return dcList;
        }
    }

    [Serializable]
    public class SupplierQuantity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal Quantity { get; set; }

        public string CSV()
        {
            return $"{Name}~{Quantity}";
        }

        public static IEnumerable<SupplierQuantity> FromJSON(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<SupplierQuantity>();
            try
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SQDTO>(json);
                return obj.d.WarehouseStock.Select(w => new SupplierQuantity
                {
                    Name = w.Message.Trim(),
                    Code = w.Code.Trim(),
                    Quantity = w.Qty
                });
            }
            catch { }
            return null;
        }

        private class SQDTO
        {
            public InventoryDTO d { get; set; }
        }

        private class InventoryDTO
        {
            public string type { get; set; }
            public IEnumerable<WarehouseStockDTO> WarehouseStock { get; set; }
        }

        private class WarehouseStockDTO
        {
            public decimal Qty { get; set; }
            public string Message { get; set; }
            public string Code { get; set; }
        }
    }

    [Serializable]
    public class StoreQuantity
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public string CSV()
        {
            return $"{Name}~{Quantity}";
        }

        public static StoreQuantity FromJSON(string json)
        {
            StoreQuantity storeQty = null;
            try
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var blob = obj["d"];
                var parts = blob.Split(new[] { '|' });
                if(parts.Length == 2) {
                    storeQty = new StoreQuantity
                    {
                        Quantity = decimal.Parse(parts[0].Trim())
                    };
                }
            }
            catch { }
            return storeQty;
        }
    }
}
