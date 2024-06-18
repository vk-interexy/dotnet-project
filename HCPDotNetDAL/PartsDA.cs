using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace HCPDotNetDAL
{
    public class PartsDA : IDisposable
    {
        private Database CreateDatabase()
        {
            return new Database() { ConnectionString = ConnectionString };
        }


        #region Parameter Creation Private Methods
        private List<MySqlParameter> CreatePartNumberParams(string partNumber)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                }
            };
            return parameters;
        }

        private List<MySqlParameter> CreatePartInterchangeNumberParams(string partNumber, string interchangePartNumber, string interchangeManufacturer)
        {
            var parameters = CreatePartNumberParams(partNumber);
            parameters.Add(
                 new MySqlParameter
                 {
                     ParameterName = "@interchangePartNumber",
                     Value = interchangePartNumber
                 });
            parameters.Add(
                 new MySqlParameter
                 {
                     ParameterName = "@interchangeManufacturer",
                     Value = interchangeManufacturer
                 });
            return parameters;

        }
        private List<MySqlParameter> CreateCategoryParams(string category)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@category",
                    Value = category
                },
                
            };
            return parameters;
        }

        private List<MySqlParameter> CreateSubCategoryParams(string subCategory)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@subCategory",
                    Value = subCategory
                }
            };
            return parameters;
        }

        private List<MySqlParameter> CreateCategorySubCategoryParams(string category, string subCategory)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@category",
                    Value = category
                },
                new MySqlParameter
                {
                    ParameterName = "@subCategory",
                    Value = subCategory
                }
            };
            return parameters;
        }

        private List<MySqlParameter> CreateFullTableParams(string partNumber, string category, string subCategory, DateTimeOffset quantityPricingUpdatedDate)
        {
            return new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@category",
                    Value = category
                },
                new MySqlParameter
                {
                    ParameterName = "@subCategory",
                    Value = subCategory
                },
                new MySqlParameter
                {
                    ParameterName = "@quantityPricingUpdatedDate",
                    Value = quantityPricingUpdatedDate.DateTime
                }

            };
        }

        private List<MySqlParameter> CreateFullTableParams2(string partNumber, string category, long categoryId, string subCategory, string subCategoryId, DateTimeOffset quantityPricingUpdatedDate)
        {
            var list = CreateFullTableParams(partNumber, category, subCategory, quantityPricingUpdatedDate);

            list.Add(new MySqlParameter
            {
                ParameterName = "@categoryId",
                Value = categoryId
            });

            list.Add(new MySqlParameter
            {
                ParameterName = "@subCategoryId",
                Value = subCategoryId
            });
            return list;
        }

        private List<MySqlParameter> CreatePartCompatibilityGetRecordParams(string compatibilityKey)
        {
            return new List<MySqlParameter>
            {
                 new MySqlParameter
                {
                    ParameterName = "@compatibilityKey",
                    Value = compatibilityKey
                }
            };
        }

        private List<MySqlParameter> CreatePartCompatibilityTableParams(string compatibilityKey, string partNumber, string make, string model, string engine, int startyear, int endyear)
        {
            return new List<MySqlParameter>
            {
                 new MySqlParameter
                {
                    ParameterName = "@compatibilityKey",
                    Value = compatibilityKey
                },
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@make",
                    Value = make
                },
                new MySqlParameter
                {
                    ParameterName = "@model",
                    Value = model
                },
                new MySqlParameter
                {
                    ParameterName = "@engine",
                    Value = engine
                },
                new MySqlParameter
                {
                    ParameterName = "@startyear",
                    Value = startyear
                },
                new MySqlParameter
                {
                    ParameterName = "@endyear",
                    Value = endyear
                }

            };
        }

        private string GetSortOrder(bool AscOrDesc = true)
        {
            return (AscOrDesc) ? "ASC" : "DESC";
        }

        private List<MySqlParameter> CreatePricingParams(string partNumber, decimal? listPrice, decimal? cost, decimal? core, DateTimeOffset quantityPricingUpdatedDate)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@listPrice",
                    Value = listPrice
                },
                new MySqlParameter
                {
                    ParameterName = "@cost",
                    Value = cost
                },
                new MySqlParameter
                {
                    ParameterName = "@core",
                    Value = core
                },
                new MySqlParameter
                {
                    ParameterName = "@quantityPricingUpdatedDate",
                    Value = quantityPricingUpdatedDate.DateTime
                }
            };
            return parameters;
        }

        private List<MySqlParameter> CreateQuantityParams(string partNumber, decimal? quantity, string dcName, decimal? dcQuantity, string deliveryTime, DateTimeOffset quantityPricingUpdatedDate)
        {
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@partNumber",
                    Value = partNumber
                },
                new MySqlParameter
                {
                    ParameterName = "@quantity",
                    Value = quantity
                },
                new MySqlParameter
                {
                    ParameterName = "@dcName",
                    Value = dcName
                },
                new MySqlParameter
                {
                    ParameterName = "@dcQuantity",
                    Value = dcQuantity
                },
                new MySqlParameter
                {
                    ParameterName = "@deliveryTime",
                    Value = deliveryTime
                },
                new MySqlParameter
                {
                    ParameterName = "@quantityPricingUpdatedDate",
                    Value = quantityPricingUpdatedDate.DateTime
                }
            };
            return parameters;
        }

        #endregion


        public void Dispose()
        {

        }

        public string ConnectionString { get; set; }
        
        public DataTable GetAllJSONData(bool test, bool AscOrDesc = true)
        {
            string filter = (test) ? "WHERE p.PartNumber = 'AC 1'" : string.Empty;
            Database db = CreateDatabase();
            return db.GetDataTable($"SELECT `ObjectData` FROM `dotnet`.`partobjectdata` {filter} ORDER BY `PartNumber` {GetSortOrder(AscOrDesc)}");
        }

        public DataTable GetAllPartData(bool test,bool AscOrDesc = true)
        {
            string filter = (test) ? "WHERE p.PartNumber = 'AC 1'" : string.Empty;
            Database db = CreateDatabase();
            return db.GetDataTable($"SELECT p.*,pod.ObjectData FROM `dotnet`.`parts` p INNER JOIN `dotnet`.`partobjectdata` pod ON p.PartNumber = pod.PartNumber {filter} ORDER BY p.`PartNumber` {GetSortOrder(AscOrDesc)}");
        }

        public DataTable GetAllPartNumbers(bool test, bool AscOrDesc = true)
        {
            string filter = (test) ? "WHERE PartNumber = 'AC 1'" : string.Empty;
            Database db = CreateDatabase();
            return db.GetDataTable($"SELECT PartNumber FROM `dotnet`.`parts` {filter} ORDER BY PartNumber {GetSortOrder(AscOrDesc)}");
        }

        public DataTable GetAllPartNumbersWithUnsetQuantityPricingUpdatedDate(bool test, bool AscOrDesc = true)
        {
            string filter = (test) ? "AND PartNumber = 'AC 1'" : string.Empty;
            Database db = CreateDatabase();
            return db.GetDataTable($"SELECT PartNumber FROM `dotnet`.`parts` WHERE (`QuantityPricingUpdatedDate` = '2001-01-01' or `QuantityPricingUpdatedDate` is null) {filter} ORDER BY PartNumber {GetSortOrder(AscOrDesc)}");
        }

        public DataTable GetAllPartNumbersWithoutCompatibilityRecords(bool test, bool AscOrDesc = true)
        {
            string filter = (test) ? "AND PartNumber = 'AC 1'" : string.Empty;
            Database db = CreateDatabase();
            return db.GetDataTable($"SELECT PartNumber FROM `dotnet`.`parts` WHERE `PartCompatibilityComplete` = 0 {filter} ORDER BY PartNumber {GetSortOrder(AscOrDesc)}");
        }

        public DataRow GetPart(string partNumber)
        {
            Database db = CreateDatabase();
            var parameters = CreatePartNumberParams(partNumber);
            var sql = "SELECT p.* FROM `dotnet`.`parts` p WHERE p.`PartNumber` = @partNumber";
            return db.GetDataRow(sql, parameters);
        }

        public bool PartExists(string partNumber)
        {
            Database db = CreateDatabase();
            var parameters = CreatePartNumberParams(partNumber);
            var sql = "SELECT PartNumber FROM `dotnet`.`parts` WHERE `PartNumber` = @partNumber";
            return db.GetDataTable(sql, parameters).Rows.Count > 0;
        }
        
        public bool PartInterchangeExists(string partNumber, string interchangeNumber, string interchangeManufacturer)
        {
            Database db = CreateDatabase();
            var parameters = CreatePartInterchangeNumberParams(partNumber, interchangeNumber, interchangeManufacturer);
            var sql = "SELECT 1 FROM `dotnet`.`part_interchange_numbers` WHERE `PartNumber` = @partNumber" +
                        "AND InterchangeNumber = @interchangeNumber " +
                        "AND InterchangeManufacturer = @interchangeManufacturer";
            return db.GetDataTable(sql, parameters).Rows.Count > 0;
        }

        public DataTable GetPartsByCategory(string category)
        {
            Database db = CreateDatabase();
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@category",
                    Value = category
                }
            };
            var sql = "SELECT * FROM `dotnet`.`parts` WHERE Category = @category";
            return db.GetDataTable(sql, parameters);
        }
        public DataTable GetPartNumbersByCategory(string category)
        {
            Database db = CreateDatabase();
            var parameters = CreateCategoryParams(category);
            var sql = "SELECT PartNumber FROM `dotnet`.`parts` WHERE `Category` = @category;";
            return db.GetDataTable(sql, parameters);
        }

        public DataTable GetPartsBySubCategory(string subCategory)
        {
            Database db = CreateDatabase();
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter
                {
                    ParameterName = "@subCategory;",
                    Value = subCategory
                }
            };
            var sql = "SELECT * FROM `dotnet`.`parts` WHERE `SubCategory` = @subCategory;";
            return db.GetDataTable(sql, parameters);
        }
        public DataTable GetPartNumbersBySubCategory(string subCategory)
        {
            Database db = CreateDatabase();
            var parameters = CreateSubCategoryParams(subCategory);
            var sql = "SELECT PartNumber FROM `dotnet`.`parts` WHERE `SubCategory` = @subCategory;";
            return db.GetDataTable(sql, parameters);
        }

        
        public DataTable GetPartsByCategorySubCategory(string category, string subCategory)
        {
            Database db = CreateDatabase();
            var parameters = CreateCategorySubCategoryParams(category, subCategory);
            var sql = "SELECT * FROM `dotnet`.`parts` WHERE `Category` = @category AND `SubCategory` = @subCategory;";
            return db.GetDataTable(sql, parameters);
        }

        public DataTable GetPartNumbersByCategorySubCategory(string category, string subCategory)
        {
            Database db = CreateDatabase();
            var parameters = CreateCategorySubCategoryParams(category, subCategory);
            var sql = "SELECT PartNumber FROM `dotnet`.`parts` WHERE `Category` = @category AND `SubCategory` = @subCategory;";
            return db.GetDataTable(sql, parameters);
        }

        public int UpdatePart(string partNumber, string category, string subCategory, DateTimeOffset quantityPricingUpdatedDate)
        {
            var parameters = CreateFullTableParams(partNumber, category, subCategory, quantityPricingUpdatedDate);
            Database db = CreateDatabase();
            var sql = "UPDATE `dotnet`.`parts` SET `Category` = @category, `SubCategory` = @subCategory, `QuantityPricingUpdatedDate` = @quantityPricingUpdatedDate WHERE `PartNumber` = @partNumber;";
            return db.UpdateDatabase(sql, parameters);
        }

        public int UpdatePartCategoryInfo(string partNumber, string category, long categoryId, string subCategory, string subCategoryId)
        {
            var parameters = CreateFullTableParams2(partNumber, category, categoryId, subCategory, subCategoryId, DateTime.Now);
            Database db = CreateDatabase();
            var sql = "UPDATE `dotnet`.`parts` SET `Category` = @category, `CategoryId` = @categoryId, `SubCategory` = @subCategory, `SubCategoryId` = @subCategoryId WHERE `PartNumber` = @partNumber;";
            return db.UpdateDatabase(sql, parameters);
        }

        private List<MySqlParameter> CreateFullTableParams(Dictionary<string,object> keyValuePairs)
        {
            var list = new List<MySqlParameter>();
            foreach(string key in keyValuePairs.Keys)
            {
                list.Add(
                    new MySqlParameter
                    {
                        ParameterName = $"@{key}",
                        Value = keyValuePairs[key]
                    }
                    );
            }
            return list;
        }

        private string CreateUpdateParamSql(List<string> fields)
        {
            string sql = @"UPDATE `dotnet`.`parts` SET ";
            foreach(var fieldName in fields)
            {
                sql += $"`{fieldName}` = @{fieldName}, ";
            }

            sql = sql.TrimEnd(", ".ToCharArray());

            sql += $" WHERE `PartsID` = @PartsID";

            return sql;
        }

        private string CreateInsertParamSql(List<string> fields)
        {
            string sql = @"INSERT INTO `dotnet`.`parts` (";
            foreach (var fieldName in fields)
            {
                sql += $"`{fieldName}`, ";
            }

            sql = $"{sql.TrimEnd(", ".ToCharArray())}) VALUES (";

            foreach (var fieldName in fields)
            {
                sql += $"@{fieldName}, ";
            }
            sql = $"{sql.TrimEnd(", ".ToCharArray())})";

            return sql;
        }

        public int UpdatePart(int partId, Dictionary<string,object> keyValuePairs)
        {
            string sql = CreateUpdateParamSql(keyValuePairs.Keys.ToList());
            var parameters = CreateFullTableParams(keyValuePairs);
            parameters.Add(new MySqlParameter
            {
                ParameterName = "@PartsID",
                Value = partId
            });
            Database db = CreateDatabase();
            return db.UpdateDatabase(sql, parameters);
        }

        public int InsertPart(Dictionary<string, object> keyValuePairs)
        {
            string sql = CreateInsertParamSql(keyValuePairs.Keys.ToList());
            var parameters = CreateFullTableParams(keyValuePairs);
            Database db = CreateDatabase();
            return db.UpdateDatabase(sql, parameters);
        }

        public int UpdatePartPricing(string partNumber, decimal? listPrice, decimal? cost, decimal? core)
        {
            var parameters = CreatePricingParams(partNumber, listPrice, cost, core, DateTimeOffset.Now);
            Database db = CreateDatabase();
            var sql = "UPDATE `dotnet`.`parts` SET `UnitListPrice` = @listPrice, `UnitCost` = @cost, `UnitCore` = @core, `QuantityPricingUpdatedDate` = @quantityPricingUpdatedDate WHERE `PartNumber` = @partNumber;";
            return db.UpdateDatabase(sql, parameters);
        }

        public int UpdatePartQuantity(string partNumber, decimal? quantity, string dcName, decimal? dcQuantity, string deliveryTime)
        {
            var parameters = CreateQuantityParams(partNumber, quantity, dcName, dcQuantity, deliveryTime, DateTimeOffset.Now);
            Database db = CreateDatabase();
            var sql = "UPDATE `dotnet`.`parts` SET `Quantity` = @quantity, `DCName` = @dcName, `DCQuantity` = @dcQuantity,  `DeliveryTime` = @deliveryTime, `QuantityPricingUpdatedDate` = @quantityPricingUpdatedDate  WHERE `PartNumber` = @partNumber;";
            return db.UpdateDatabase(sql, parameters);
        }

        public int InsertPart(string partNumber, string category, string subCategory)
        {
            var parameters = CreateFullTableParams(partNumber, category, subCategory,DateTimeOffset.Now);
            Database db = CreateDatabase();
            var sql = "INSERT INTO `dotnet`.`parts` (`PartNumber`, `Category`, `SubCategory`, `QuantityPricingUpdatedDate`) VALUES (@partNumber, @category, @subCategory, @quantityPricingUpdatedDate);" + Environment.NewLine +
                      "INSERT INTO `dotnet`.`partobjectdata` (`PartNumber`, `ObjectData`) VALUES (@partNumber, @json);";


            return db.UpdateDatabase(sql, parameters);
        }

        public int InsertPartCompatibility(string compatibilityKey, string partNumber, string make, string model, string engine, int startyear, int endyear)
        {
            Database db = CreateDatabase();
            var row = 
                db.GetDataRow("SELECT `CompatibilityKey` FROM `dotnet`.`partcompatibility` WHERE `CompatibilityKey` = @compatibilityKey", 
                CreatePartCompatibilityGetRecordParams(compatibilityKey));

            if (row != null)
                return 0;

            var parameters = CreatePartCompatibilityTableParams(compatibilityKey, partNumber, make, model, engine, startyear, endyear);
            
            var sql = $"INSERT INTO `dotnet`.`partcompatibility` (`CompatibilityKey`, `PartNumber`, `Make`, `Model`, `Engine`, `StartYear`, `EndYear`) " +
                      $"VALUES (@compatibilityKey, @partNumber, @make, @model, @engine, @startYear, @endYear);";
            return db.UpdateDatabase(sql, parameters);
        }


        public int InsertPartAttributes(string partNumber, Dictionary<string,string> attributes )
        {
            Database db = CreateDatabase();
            var attributesToInsert = new Dictionary<string, string>();
            var sqlInsert = $"INSERT INTO `dotnet`.`partattributes` (`PartNumber`, `PartAttributeName`, `PartAttributeValue`) " +
                            $"VALUES (@partNumber, @partAttributeName, @partAttributeValue);";
            var sqlLookup = "SELECT 1 from `dotnet`.`partattributes` WHERE `PartNumber` = @partNumber AND `PartAttributeName` = @partAttributeName LIMIT 1";
            var cmd = new MySqlCommand();

            int count = 0;

            using (var conn = new MySqlConnection(db.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;

                //Lookup each attribute and only add those that are not in the database.
                foreach (var key in attributes.Keys)
                {
                    cmd.CommandText = sqlLookup;
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(
                       new MySqlParameter
                       {
                           ParameterName = "@partNumber",
                           Value = partNumber
                       });
                    cmd.Parameters.Add(
                        new MySqlParameter
                        {
                            ParameterName = "@partAttributeName",
                            Value = key
                        });
                    var table = new DataTable();
                    table.Load(cmd.ExecuteReader());
                    if (table.Rows.Count == 0)
                    {
                        attributesToInsert.Add(key, attributes[key]);
                    }
                }

                //Insert Attributes Into the databae
                foreach (var key in attributesToInsert.Keys)
                {
                    cmd.CommandText = sqlInsert;
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(
                       new MySqlParameter
                       {
                           ParameterName = "@partNumber",
                           Value = partNumber
                       });
                    cmd.Parameters.Add(
                        new MySqlParameter
                        {
                            ParameterName = "@partAttributeName",
                            Value = key
                        });
                    cmd.Parameters.Add(
                        new MySqlParameter
                        {
                            ParameterName = "@partAttributeValue",
                            Value = attributesToInsert[key]
                        }
                    );
                    count += cmd.ExecuteNonQuery();
                }
                conn.Close();   
            }
            return count;
        }

        public int InsertPartImageUrls(string partNumber, List<string> imageUrls)
        {
            Database db = CreateDatabase();
            var sqlInsert = $"INSERT INTO `dotnet`.`partimageurls` (`PartNumber`, `PartImageUrl`) " +
                      $"VALUES (@partNumber, @partImageUrl);";
            var sqlSelect = "SELECT 1 from `dotnet`.`partimageurls` WHERE `PartNumber` = @partNumber AND `PartImageUrl` = @partImageUrl LIMIT 1";
            var cmd = new MySqlCommand();

            int count = 0;

            using (var conn = new MySqlConnection(db.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                foreach (var imageUrl in imageUrls)
                {
                    cmd.CommandText = sqlSelect;
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(
                       new MySqlParameter
                       {
                           ParameterName = "@partNumber",
                           Value = partNumber
                       });
                    cmd.Parameters.Add(
                        new MySqlParameter
                        {
                            ParameterName = "@partImageUrl",
                            Value = imageUrl
                        });
                    
                    //Look up if record exists
                    var table = new DataTable();
                    table.Load(cmd.ExecuteReader());
                    
                    //If record does not exist then insert
                    if (table.Rows.Count == 0)
                    {
                        cmd.CommandText = sqlInsert;
                        count += cmd.ExecuteNonQuery();
                    }
                }
                conn.Close();
            }
            return count;
        }

        public int InsertPartInterchangeNumber(string partNumber, string interchangeNumber, string interchangeManufacturer)
        {
            Database db = CreateDatabase();
            var parameters = CreatePartInterchangeNumberParams(partNumber, interchangeNumber, interchangeManufacturer);
            var sql = "INSERT INTO `dotnet`.`part_interchange_numbers` " +
                      "(`PartNumber`, `InterchangeNumber,InterchangeManufacturer`)  " +
                      "VALUES (@partNumber, @interchangeNumber, @interchangeManufacturer) ";
            return db.UpdateDatabase(sql, parameters);
        }

        public int MarkPartNumberCompatibilityComplete(string partNumber)
        {
            Database db = CreateDatabase();
            var parameters = CreatePartNumberParams(partNumber);
            return db.UpdateDatabase($"UPDATE `dotnet`.`parts` Set `PartCompatibilityComplete` = 1 WHERE `PartNumber` = @partNumber",parameters);
        }

    }
}
