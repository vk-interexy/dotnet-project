using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using dotnetscrape_lib.DataObjects;
using dotnetscrape_lib;
using System.Linq;
using HCPDotNetDAL;

namespace HCPDotNetBLL
{
    public class PartsBL : IDisposable
    {
        private readonly ParallelOptions options;

        private static readonly int MaxUrlLength = 2083;
        private static readonly int MaxTextLength = 65535;

        public PartsBL()
        {
            options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
            };
        }

        public string ConnectionString { get; set; }
        
        public void Dispose()
        {

        }

        private PartsDA CreateDA()
        {
            return new PartsDA { ConnectionString = ConnectionString };
        }

        private static Dictionary<string, object> GenerateKeyValuePairsFromPart(AutoPart part, bool inserting)
        {
            var keyValuePairs = new Dictionary<string, object>
            {
                { "PartName", Utilities.MinString(part.PartName,500) },
                { "ProductLine",  Utilities.MinString(part.ProductLine,100) },
                { "Category",  Utilities.MinString(part.Category,200) },
                { "CategoryId", part.CategoryId },
                { "SubCategory",  Utilities.MinString(part.SubCategory,200) },
                { "SubCategoryId",  Utilities.MinString(part.SubCategoryId,40) },
                { "Unit",  Utilities.MinString(part.Pricing?.Unit,20) },
                { "FeatureAndBenefits",  Utilities.MinString(part.FeatureAndBenefits,MaxTextLength) },
                { "FeatureAndBenefitsUrl",  Utilities.MinString(part.FeatureAndBenefitsUrl,MaxUrlLength) },
                { "FeatureAndBenefitsUrlText",  Utilities.MinString(part.FeatureAndBenefitsUrlText, MaxTextLength) },
                { "Warranty",  Utilities.MinString(part.Warranty, MaxTextLength) },
                { "WarrantyUrl",  Utilities.MinString(part.WarrantyUrl, MaxUrlLength) },
                { "Msds",  Utilities.MinString(part.Msds, MaxTextLength) },
                { "MsdsUrl",  Utilities.MinString(part.MsdsUrl, MaxUrlLength) },
                { "DetailUrl",  Utilities.MinString(part.DetailUrl, MaxUrlLength) },
                { "Manufacturer",  Utilities.MinString(part.Manufacturer, 200) },
                { "ManufacturerPartNumber",  Utilities.MinString(part.ManufacturerPartNumber,50) },
                { "UNSPSC",  Utilities.MinString(part.UNSPSC,50) }
            };
            if (inserting)
            {
                keyValuePairs.Add("PartNumber", Utilities.MinString(part.PartNumber, 50));
            }

            return keyValuePairs;
        }

        private static AutoPartPricing CreatePricing(decimal? list, decimal? cost, decimal? core)
        {
            var pricing = new AutoPartPricing
            {
                List = list ?? 0,
                Cost = cost ?? 0,
                Core = core ?? 0,
                HasCore = core.HasValue,
                PricingDetailProvided = list.HasValue || cost.HasValue || core.HasValue
            };
            
            return pricing;
        }

        private static List<DistributionCenterQuantity> CreateDCQuantities(string name, decimal? quantity, string deliveryTime)
        {
            var dcQtys = new List<DistributionCenterQuantity>
            {
                new DistributionCenterQuantity
                {
                    Name = name,
                    Quantity = quantity ?? 0,
                    DeliveryTime = deliveryTime
                }
            };
            return dcQtys;
        }


        private void UpdatePricingAndQuantityObjectValues(AutoPart part, DataRow row)
        {
            if(part == null || row == null)
            {
                return;
            }

            part.PartsId = (int)row["PartsId"];
            part.Quantity = (decimal)row["Quantity"];
            part.Pricing = CreatePricing((decimal)row["UnitListPrice"],
                                         (decimal)row["UnitCost"],
                                         (decimal)row["UnitCore"]);
            if (part.FindItQuantities != null)
            {
                part.FindItQuantities.DCQty = CreateDCQuantities(row["DCName"] as string ?? string.Empty,
                                                                 (decimal)row["DCQuantity"],
                                                                 row["DeliveryTime"] as string ?? string.Empty);
            }
            var dateTime =(DateTime)row["QuantityPricingUpdatedDate"];
            part.QuantityPricingUpdatedDate = new DateTimeOffset(dateTime);
        }

        private Dictionary<string,AutoPart> HydrateDictionary(DataTable table)
        {
            var dictionary = new Dictionary<string, AutoPart>();
            foreach (DataRow row in table.Rows)
            {
                var part = Utilities.Deserialize<AutoPart>(row["ObjectData"].ToString(), true);

                UpdatePricingAndQuantityObjectValues(part, row);

                if (!dictionary.ContainsKey(part.PartNumber))
                {
                    dictionary.Add(part.PartNumber, part);
                }
            }
            return dictionary;
        }

        public AutoPart GetPart(string partNumber)
        {
            using (var da = CreateDA())
            {
                var row = da.GetPart(partNumber);
                if (row == null) return null;

                // TODO: Update to get older Part Attributes Later
                var part = new AutoPart
                {
                    PartNumber = DataBaseValue.GetValue<string>(row, "PartNumber", string.Empty)
                };
                //var part = Utilities.Deserialize<AutoPart>(row["ObjectData"].ToString(), true);
                UpdatePricingAndQuantityObjectValues(part, row);
                return part;
            }
        }

        public List<string> GetAllPartNumbers(bool onlyGetPartNumbersWithUnsetQuantityPricingUpdatedDate = false, bool AscOrDesc = true)
        {
            var partNumbers = new List<string>();
            
            using (var da = CreateDA())
            {
                var table = (onlyGetPartNumbersWithUnsetQuantityPricingUpdatedDate) 
                    ? da.GetAllPartNumbersWithUnsetQuantityPricingUpdatedDate(false,AscOrDesc) : 
                    da.GetAllPartNumbers(false,AscOrDesc);

                string partNumber;

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        partNumber = DataBaseValue.GetValue<string>(row, "PartNumber", string.Empty);
                        if (string.IsNullOrEmpty(partNumber)) continue;
                        partNumbers.Add(partNumber);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return partNumbers;
        }

        public List<string> GetAllPartNumbersWithoutCompatibilityRecords()
        {
            var partNumbers = new List<string>();
            using (var da = CreateDA())
            {
                var table = da.GetAllPartNumbersWithoutCompatibilityRecords(false);

                foreach (DataRow row in table.Rows)
                {
                    partNumbers.Add(row["PartNumber"].ToString());
                }
            }
            return partNumbers;
        }
        
        public Dictionary<string,AutoPart> GetAllParts(bool AscOrDesc = true)
        {
            using (var da = CreateDA())
            {
                return HydrateDictionary(da.GetAllPartData(false,AscOrDesc));
            }
        }

        public List<AutoPart> GetAllPartsAsOrderedList(bool AscOrDesc = true)
        {
            return GetAllParts(AscOrDesc).Values.OrderBy(p => p.PartNumber).ToList();
        }

        public Dictionary<string,AutoPart> GetPartsByCategory(string category)
        {
            using (var da = CreateDA())
            {
                return HydrateDictionary(da.GetPartsByCategory(category));
            }
        }

        public Dictionary<string, AutoPart> GetPartsBySubCategory(string subCategory)
        {
            using (var da = CreateDA())
            {
                return HydrateDictionary(da.GetPartsBySubCategory(subCategory));
            }
        }

        public Dictionary<string, AutoPart> GetPartsByCategoryAndSubCategory(string category, string subCategory)
        {
            using (var da = CreateDA())
            {
                return HydrateDictionary(da.GetPartsByCategorySubCategory(category, subCategory));
            }
        }
        
        public List<string> GetAllJSONData()
        {
            var objectData = new List<string>();
            try
            {
                using (var da = CreateDA())
                {
                    var table = da.GetAllJSONData(false);

                    foreach (DataRow row in table.Rows)
                    {
                        if (!row.IsNull("ObjectData") && !string.IsNullOrWhiteSpace(row["ObjectData"].ToString()))
                        {
                            objectData.Add(row["ObjectData"].ToString());
                        }
                    }
                }
                return objectData;
            }
            catch
            {
            }

            return objectData;
        }

        public int UpdatePartQuantityPricingUpdate(AutoPart part)
        {
            using (var da = CreateDA())
            {
                part.QuantityPricingUpdatedDate = DateTimeOffset.Now;
                return da.UpdatePart(part.PartNumber, part.Category, part.SubCategory, part.QuantityPricingUpdatedDate);
            }
        }

        public int UpdatePartCategoryInfo(AutoPart part)
        {
            using (var da = CreateDA())
            {
                return da.UpdatePartCategoryInfo(part.PartNumber, part.Category, part.CategoryId, part.SubCategory, part.SubCategoryId);
            }
        }

        public int UpdatePart(AutoPart part)
        {
            using (var da = CreateDA())
            {
                return da.UpdatePart(part.PartsId, GenerateKeyValuePairsFromPart(part, false));
            }
        }

        public int InsertPart(AutoPart part)
        {
            if (part == null) return 0;

            using (var da = CreateDA())
            {
                if (da.PartExists(part.PartNumber)) return 0;
                return da.InsertPart(GenerateKeyValuePairsFromPart(part,true));
            }
        }

        public bool PartExists(string partNumber)
        {
            using (var da = CreateDA())
            {
                return da.PartExists(partNumber);
            }
        }
        public int InsertPartInterchangeNumber(AutoPartInterchangeNumber interchangeNumber)
        {
            if (interchangeNumber == null) return 0;

            using (var da = CreateDA())
            {
                if (da.PartInterchangeExists(interchangeNumber.PartNumber, 
                    interchangeNumber.InterchangeNumber,
                    interchangeNumber.InterchangeManufacturer)) return 0;
                return da.InsertPartInterchangeNumber(interchangeNumber.PartNumber,
                    interchangeNumber.InterchangeNumber,
                    interchangeNumber.InterchangeManufacturer);
            }
        }

        public int UpdatePartPricingAndQuantity(AutoPart part, string defaultDCName)
        {
            int count = 0;
            using (var da = CreateDA())
            {
                count += da.UpdatePartPricing(part.PartNumber, part.Pricing?.List, part.Pricing?.Cost, part.Pricing?.Core);
                if (part.FindItQuantities != null && part.FindItQuantities.DCQty != null && part.FindItQuantities.DCQty.Any())
                {
                    var dcQuantity = part.FindItQuantities?.DCQty?.First();
                    count += da.UpdatePartQuantity(part.PartNumber, part.Quantity, dcQuantity?.Name, dcQuantity?.Quantity, dcQuantity.DeliveryTime);
                }
                else
                {
                    count += da.UpdatePartQuantity(part.PartNumber, part.Quantity, defaultDCName, 0, string.Empty);
                }
            }
            return count;
        }

        public int MarkPartNumberCompatibilityComplete(string partNumber)
        {
            using (var da = CreateDA())
            {
                return da.MarkPartNumberCompatibilityComplete(partNumber);
            }
        }

        public int InsertPartCompatibility(AutoPart part)
        {
            if (part == null || part.PartCompatibilities == null || part.PartCompatibilities.Count == 0) return 0;

            int count = 0;
            using (var da = CreateDA())
            {
                foreach (var partCompatibility in part.PartCompatibilities)
                {
                    try
                    {
                        count += da.InsertPartCompatibility(partCompatibility.CompatibilityKey,
                                                        partCompatibility.PartNumber,
                                                        partCompatibility.Make,
                                                        partCompatibility.Model,
                                                        partCompatibility.Engine,
                                                        partCompatibility.StartYear,
                                                        partCompatibility.EndYear);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return count;
        }
        public int InsertPartCompatibility(AutoPartCompatibility partCompatibility)
        {
            if (partCompatibility == null) return 0;

            using (var da = CreateDA())
            {
                return da.InsertPartCompatibility(  partCompatibility.CompatibilityKey,
                                                    partCompatibility.PartNumber,
                                                    partCompatibility.Make, 
                                                    partCompatibility.Model, 
                                                    partCompatibility.Engine,
                                                    partCompatibility.StartYear,
                                                    partCompatibility.EndYear);
            }
        }

        public int InsertPartAttributes(AutoPart part)
        {
            if (part == null || part.Attributes == null || part.Attributes.Count == 0) return 0;

            var attributes = new Dictionary<string, string>();
            foreach(var attrib in part.Attributes)
            {
                if (attributes.ContainsKey(attrib.Name)) continue;
                attributes.Add(attrib.Name, attrib.Value);
            }
            using (var da = CreateDA())
            {
                return da.InsertPartAttributes(part.PartNumber, attributes);
            }
            
        }

        public int InsertPartImageUrls(AutoPart part)
        {
            if (part == null || part.ImageUrls == null || part.ImageUrls.Count == 0) return 0;
            using (var da = CreateDA())
            {
                return da.InsertPartImageUrls(part.PartNumber, part.ImageUrls);
            }

        }

        public int ClearUpdateConfirmed()
        {
            return 0;
        }

    }
}
