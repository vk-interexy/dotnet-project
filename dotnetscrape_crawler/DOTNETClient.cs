using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Compression;
using System.Diagnostics;
using dotnetscrape_constants;
using dotnetscrape_lib.DataObjects;
using System.Security.Cryptography;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using HCPDotNetBLL;

namespace dotnetscrape_crawler
{

    public class ExecuteTaskParams
    {
        public SubCategory SubCat { get; set; }
        public int EndOfPaginatedResults { get; set; }
        public string progressFile { get; set; }
        public double categoryProgress { get; set; }
        public Guid catExtractGuid { get; set; }
        public List<string> searchResultPages { get; set; }
        public string prefix { get; set; }
        public int progressCount { get; set; }
        public int startPage; int maxPages { get; set; }
        public object searchTaskLockObject { get; set; }
    }

    public class DOTNETClient
    {
        private CookieContainer cookies = new CookieContainer();
        private WebProxy proxy;
        private ParallelOptions options = new ParallelOptions();
        public static ulong totalPartCount = 0;
        public static ulong totalPartsCached = 0;
        public static ulong totalPartsAdded = 0;
        public static ulong totalPartsUpdated = 0;
        public static string runDateTimePath = Path.Combine(Config.DataExportPath, DateTime.Now.ToString("yyyy-MM-dd_HH+mm+ss"));
        public static long totalNumberOfSubCategories = 0;
        public static long totalNumberOfCategories = 0;
        public static long totalNumberOfCategoriesProcessed = 0;
        public static long totalNumberOfSubCategoriesProcessed = 0;
        private static Dictionary<string, object> ProgressLocks = new Dictionary<string, object>();
        private static ConcurrentDictionary<string, AutoPart> PartsFromJSON = new ConcurrentDictionary<string, AutoPart>();
        private static FindIt findIt = null;
        private static object findItCloneLock = new object();
        



        public bool Stop { get; set; }

        public DOTNETClient()
        {
            if (Config.UseProxy)
            {
                proxy = new WebProxy(Config.ProxyHost, Config.ProxyPort);
            }

            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount-1,1);
        }

        private static PartsBL CreateBL()
        {
            return new PartsBL { ConnectionString = Config.ConnectionString };
        }

        public async Task<bool> Login(string username, string password)
        {
            //var body = $@"__VIEWSTATE=%2FwEPDwUKLTY4MTAwMjM5Ng9kFgICAQ9kFgwCAQ8WAh4Fc3R5bGUFPWJhY2tncm91bmQtaW1hZ2U6IHVybCgnL2ltYWdlcy9oZWFkZXIvcHJvbGlua2hlYWRlcl90b3AuZ2lmJylkAgMPZBYCAgUPFgIeB1Zpc2libGVnZAIFD2QWBAIHDw9kFgIeCWF1dG9mb2N1c2VkAhEPD2QWBB4Lb25tb3VzZW92ZXIFKHRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXRfb24uZ2lmJzseCm9ubW91c2VvdXQFJXRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXQuZ2lmJztkAgkPZBYCAgQPDxYCHgRUZXh0BQ4xLTgwMC03NDItMzU3OGRkAgsPFgIfAWhkAg0PZBYEZg9kFgJmDw8WAh8FBQtFbiBFc3Bhw7FvbGRkAgUPDxYCHwUFDDMuMDIuMzQ6MTgwQWRkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYCBSNsb2dpblVzZXJDb250cm9sJHJlbWVtYmVyTWVDaGVja0JveAUdbG9naW5Vc2VyQ29udHJvbCRzdWJtaXRCdXR0b24eDdF4GVXx9otVOeqo7QLL%2Bns%2BQw%3D%3D&__EVENTVALIDATION=%2FwEdAAYYQ24zbAXuJdV35wY2d4zCrsIcvOui5KEGsFz9ThSIDgx70%2BJYmSNZ7ALj6frcBXoFoaLNHoHAdL%2BWyGrWyaoJPTAgUG8QtPRfO36LfVCXjXFXTQij%2FKIw0d1MukJvMRvbCsi4wKXKRzgX4kKdE2l%2BTl4sSA%3D%3D&loginUserControl%24userLoginTextBox={username}&loginUserControl%24passwordTextBox={password}&loginUserControl%24submitButton.x=37&loginUserControl%24submitButton.y=6";

            var url = "https://www.dotnetprolink.com/login.aspx";
            var method = "GET";
            var contentType = "application/x-www-form-urlencoded";
            var response = await Submit(url, method, null, contentType, false, false, true);

            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(response.Result));

            var doc = document.DocumentElement;

            var viewStateElement = doc.ChildNodes.GetElementById("__VIEWSTATE") as IElement;
            var viewStateGeneratorElement = doc.ChildNodes.GetElementById("__VIEWSTATEGENERATOR");

            string viewStateValue = System.Net.WebUtility.UrlEncode(viewStateElement.Attributes["value"].Value);
            string viewStateGeneratorVAlue = System.Net.WebUtility.UrlEncode(viewStateGeneratorElement.Attributes["value"].Value);

            var body = $@"__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE={viewStateValue}&__VIEWSTATEGENERATOR={viewStateGeneratorVAlue}&loginUserControl%24userLoginTextBox={System.Net.WebUtility.UrlEncode(username)}&loginUserControl%24passwordTextBox={System.Net.WebUtility.UrlEncode(password)}&loginUserControl%24rememberMeCheckBox=on&loginUserControl%24submitButton.x=64&loginUserControl%24submitButton.y=17";

            method = "POST";
            response = await Submit(url, method, body, contentType, false, false, true);
            if (response.StatusCode != (int)HttpStatusCode.Found && response.StatusCode != (int)HttpStatusCode.OK) return false;

            url = "https://www.dotnetprolink.com/default.aspx";
            response = await Submit(url, method, body, contentType, false, false, true);
            //Utilities.LogError(response.Result);
            return response.Result.Contains("ctl00$logoutLinkButton") && response.Result.Contains("ctl00_poMyAccount");
            //return false;
        }

        private async Task<IEnumerable<Category>> FetchCategories()
        {
            var categories = GenerateFullSearchCategoryList();
            //UNCOMMENT THIS CODE

            if (!Config.FullSearch)
            {
                string url = $"https://www.dotnetprolink.com/default.aspx";

                bool cacheLookup = Config.PerformCacheLookup;

                var response = await Submit(url, "GET", null, null, cacheLookup, Config.CacheResponses);
                if (string.IsNullOrWhiteSpace(response.Result))
                {
                    Utilities.LogError($"In FetchCategories, got empty response for {url}");
                    return Enumerable.Empty<Category>();
                }

                //Use the default configuration for AngleSharp
                var config = Configuration.Default;

                //Create a new context for evaluating webpages with the given config
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(response.Result));
                var doc = document.DocumentElement;

               

                categories = new List<Category>();
                //var nods = doc.SelectNodes("//*[@id='ctl00_mainContentPlaceHolder_categoryMainDropDownList']/option");

                foreach (var node in doc.SelectNodes("//*[@id='ctl00_mainContentPlaceHolder_categoryMainDropDownList']/option"))
                {
                    var element = node as IElement;

                    if (element.Attributes.Any() &&
                       !string.IsNullOrWhiteSpace(element.Attributes[0].Value))
                    {
                        int id = 0;
                        int.TryParse(dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(element.Attributes[0].Value), out id);
                        categories.Add(new Category
                        {
                            Name = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(element.Text()),
                            Id = id
                        });
                    }
                }
            }

            ProgressLocks.Clear();
            foreach (var c in categories)
            {
                if (ProgressLocks.ContainsKey(c.Name)) continue;
                ProgressLocks.Add(c.Name, new object());

            }

            return categories;
        }

        private async Task FetchSubCategoriesForCategory(Category category)
        {
            category.ClearSubCategories();
            bool cacheLookup = Config.PerformCacheLookup;

            //var body = $@"{{categoryId: ""{category.Id}""}}";
            var url = $"https://www.dotnetprolink.com/api/catalog/category/{category.Id}/subcategory";
            //var url = "https://www.dotnetprolink.com/services/cataloglistsservice.asmx/GetSubCategoriesNonVeh";
            var method = "GET";
            var contentType = "application/json; charset=utf-8";
            var response = await Submit(url, method, null, contentType, cacheLookup, Config.CacheResponses);

            SubCategoryResult result = Newtonsoft.Json.JsonConvert.DeserializeObject<SubCategoryResult>(response.Result);

            var subcats = from subcat in result
                          where subcat.Value != "-1"
                          select subcat;
            var subCategories = subcats.ToList<SubCategory>();
            foreach (var subcat in subCategories)
            {
                if (!Config.SubCategoryWhiteList.Any() || Config.SubCategoryWhiteList.Contains(subcat.ValueAsInt))
                {
                    category.AddSubCategory(subcat);
                }
            }
        }

        private async Task FetchSubCategories(IList<Category> categories)
        {
            await Task.Run(() =>
            {
                //for(int item = 0; item < categories.Count; ++item)
                Parallel.For(0, categories.Count, options, item =>
                {
                    if (Stop) { return; }
                    var category = categories[item];
                    FetchSubCategoriesForCategory(category).Wait();
                });
            });
        }

        List<Category> GenerateFullSearchCategoryList()
        {
            var categories = new List<Category>();
            var category = new Category
            {
                Id = 0,
                Name = Constants.FullSearchCategoryName
            };
            var subCat = new SubCategory { Value = "0", Text = Constants.FullSearchSubCategoryName };
            category.AddSubCategory(subCat);
            categories.Add(category);
            return categories;
        }

        public async Task<IEnumerable<Category>> PopulateCategories()
        {
            var categories = (await FetchCategories()).Where(c => Config.FullSearch || Config.CategoryWhiteList.Length == 0 || Config.CategoryWhiteList.Contains(c.Id)).ToList();
                
            if (!Config.FullSearch)
            {   
                await FetchSubCategories(categories);    
            }
            
            totalNumberOfCategories = categories.Count;
            foreach (var cat in categories)
            {
                totalNumberOfSubCategories += cat.SubCategories.Count;
            }
            return categories;
        }

        public void GetParts(IEnumerable<Category> categories, int startPage = 0, int maxPages = 1)
        {
            int categoryCount = categories.Count();

            foreach(var category in categories)
            { 
            //Parallel.For(0, categoryCount, c =>
            //{
                if (Stop) { return; }
                
                //var category = categories.ElementAt(c);
                var catExtractGuid = Guid.NewGuid();
                //var catOpId = $"catOpId:{catExtractGuid}::";
                var catOpId = $"&&&{category.Id}&&&";

                Utilities.LogInfo($"{catOpId}START - Extracting data for parts in Category: <{category.Name} - {category.Id}>");

                var progressFile = Path.Combine(Config.ProcessingPath, "progress", category.Name);
                if (!Directory.Exists(Path.GetDirectoryName(progressFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(progressFile));
                }

                WriteProgress(progressFile, category, 0, 0);
                for(int i = 0; i < category.SubCategories.Count; ++i)
                {
                    //var subcatOpId = $"{catOpId}SubCatOpId:{Guid.NewGuid()}:::";
                    
                    if (Stop) { return; }

                    var subCat = category.SubCategories[i];
                    var subcatOpId = $"&&&{category.Id}&&&{subCat.Value}&&&";
                    Utilities.LogInfo($"{subcatOpId}START - Extracting data for parts in Category: <{category.Name} - {category.Id}> SubCategory <{subCat.Text} - {subCat.Value}>");

                    GetPartsBySubCategory(subCat, startPage, maxPages, progressFile, (double)i / (double)category.SubCategories.Count, subcatOpId).Wait();

                    if (Stop) { return; }
                    
                    WriteProgress(progressFile, category, ((double)(i+1)/ (double)category.SubCategories.Count), 1.0);
                    Utilities.LogInfo($"{subcatOpId}DONE - Extracting data for parts in Category: <{category.Name}> SubCategory <{subCat.Text}>");
                    totalNumberOfSubCategoriesProcessed++;
                }

                Utilities.LogInfo($"{catOpId}DONE - Extracting data for parts in Category: <{category.Name} - {category.Id}>");

                if (!Stop)
                {
                    WriteProgress(progressFile, category, 1.0, 1.0);
                }
            }
            //);

            totalNumberOfCategoriesProcessed++;

            if (Stop) { return ; }

            return; 
        }

        private async Task<List<AutoPartCompatibility>> GetAutoPartCompatibility(string partNumber, string operationId)
        {
            Utilities.LogInfo($"{operationId}START - Retrieving Part Compatability Info for {partNumber}");
            var ret = new List<AutoPartCompatibility>();
            var partNumberSplit = partNumber.Split(' ');
            if (partNumberSplit.Length != 2)
            {
                Utilities.LogInfo($"{operationId}DONE - Retrieving Part Compatability Info for {partNumber} - Invalid PartNumber");
                return ret;
            }

            string url = $"https://www.dotnetprolink.com/BGRequestProcessorMI.aspx?LineCode={partNumberSplit[0]}&PartNumber={partNumberSplit[1]}";

            var response = Submit(url,"GET",null,null,Config.PerformCacheLookup,Config.CacheResponses,false,operationId);

            if (response != null && !response.Result.HasError)
            {
                Utilities.LogDebug($"{operationId}START - Parsing Part Compatability info for {partNumber}");
                ret = await HTMLToAutoPartCompatibilityList(partNumber, response.Result.Result);
                Utilities.LogDebug($"{operationId}DONE - Parsing Part Compatability info for {partNumber}");
            }
            Utilities.LogInfo($"{operationId}DONE - Retrieving Part Compatability Info for {partNumber}");
            return ret;
        }

        private async Task<List<AutoPartCompatibility>> HTMLToAutoPartCompatibilityList(string partNumber, string html)
        {
            List<AutoPartCompatibility> list = new List<AutoPartCompatibility>();
            try
            {
                //Protect against exception thrown by HTMLAgility "(Value cannot be null. Parameter name: html)"
                if (string.IsNullOrWhiteSpace(html))
                {
                    return list;
                }

                //Use the default configuration for AngleSharp
                var config = Configuration.Default;

                //Create a new context for evaluating webpages with the given config
                var context = BrowsingContext.New(config);
                var doc = await context.OpenAsync(req => req.Content(html));

                var detailDoc = doc.DocumentElement;

                foreach (var node in detailDoc.SelectNodes("/html/body/table/tbody/tr"))
                {
                    var row = node as IElement;
                    var cells = row.SelectNodes("td");
                    if (cells?.Count != 4 || cells.Where(c => c.FirstChild != null && string.Equals(c.FirstChild.NodeName, "b", StringComparison.OrdinalIgnoreCase)).Any())
                        continue;
                    list.Add(
                        new AutoPartCompatibility(partNumber, cells[0].TextContent, cells[1].TextContent, cells[2].TextContent, cells[3].TextContent)
                        );
                }
            }
            catch
            {

            }

            return list;
        }

        private void WriteDataFilesPartsFromJSON()
        {
            var parts = PartsFromJSON.Values.ToArray();
            

            string logInfoSuffix = "Generating Files from PartsFromJSON Array";

            Utilities.LogInfo($"START - {logInfoSuffix}");

            string csvFolder = Path.Combine(runDateTimePath, $"csv");
            string jsonFolder = Path.Combine(runDateTimePath, "json");
            //string xmlFolder = Path.Combine(runDateTimePath, "xml");

            if (!Directory.Exists(csvFolder)) Directory.CreateDirectory(csvFolder);
            if (!Directory.Exists(jsonFolder)) Directory.CreateDirectory(jsonFolder);
            //if (!Directory.Exists(xmlFolder)) Directory.CreateDirectory(xmlFolder);

            string fullSearchFileNamePrefix = $"FullDataExport_COUNT_{parts.Length}";


            string csvFilePath = Path.Combine(csvFolder, $"{fullSearchFileNamePrefix}.csv");
            //string xmlFilePath = Path.Combine(xmlFolder, $"{fullSearchFileNamePrefix}.xml");
            string jsonFilePath = Path.Combine(jsonFolder, $"{fullSearchFileNamePrefix}.json");


            try
            {
                //Genearte CSV File
                string csvString = $"{AutoPart.CSVHeader}{Environment.NewLine}";
                int index = 0;
                foreach(var part in parts)
                {
                    csvString += $"{part.CSV()}{Environment.NewLine}";
                    if(index++ > 0 && index % 2000 == 0)
                    {
                        File.AppendAllText(csvFilePath, csvString);
                        csvString = string.Empty;
                    }

                }
                if (csvString.Length > 0)
                { File.AppendAllText(csvFilePath, csvString); }
                Utilities.LogInfo($"CSV File written to: {csvFilePath}");
            }
            catch(Exception ex)
            {
                Utilities.LogError($"Unable to Generate CSV File.  Reason: {ex.Message}");
            }

            try
            {
                var partsTemp = new List<AutoPart>();
                string partInJSON = string.Empty;
                foreach (var part in parts)
                {
                    try
                    {
                        partInJSON = dotnetscrape_lib.Utilities.SerializeObject(part, true);
                        partsTemp.Add(part);
                    }
                    catch(Exception ex)
                    {
                        Utilities.LogError($"Unable to serialize part {part.DetailUrl}.  Reason: {ex.Message}");
                    }
                }

                //Generate JSON File
                dotnetscrape_lib.Utilities.SerializeObject(partsTemp.ToArray(),jsonFilePath);
                Utilities.LogInfo($"JSON File written to: {jsonFilePath}");
            }
            catch(Exception ex)
            {
                Utilities.LogError($"Unable to Genereate JSON File.  Reason: {ex.Message}");
            }

            //Generate XML File
            //File.WriteAllText(xmlFilePath, dotnetscrape_lib.Utilities.SerializeObject(parts, false));
            //Utilities.LogInfo($"XML File written to: {xmlFilePath}");

           

            Utilities.LogInfo($"DONE - {logInfoSuffix}");
        }

        private void SavePartDataToDatabase(IEnumerable<AutoPart> parts,string operationId)
        {
            var bl = CreateBL();

            Parallel.ForEach(parts, options, part =>
            //foreach(var part in parts)
            {

                if (part == null) return;
                var count = 0;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Utilities.LogInfo($"{operationId}START - Attempting to Insert Part Data for <{part.PartNumber}>");
                        count = bl.InsertPart(part);
                        Utilities.LogInfo($"{operationId}DONE - Attempting to Save Part Data for <{part.PartNumber}> Count({count})");

                        Utilities.LogInfo($"{operationId}Start - Attempting to Save Part Attributes Data for <{part.PartNumber}>");
                        bl.InsertPartAttributes(part);
                        Utilities.LogInfo($"{operationId}DONE - Attempting to Save Part Attributes Data for <{part.PartNumber}> Count({count})");

                        Utilities.LogInfo($"{operationId}Start - Attempting to Save Part ImageUrls Data for <{part.PartNumber}>");
                        bl.InsertPartImageUrls(part);
                        Utilities.LogInfo($"{operationId}DONE - Attempting to Save Part ImageUrls Data for <{part.PartNumber}> Count({count})");

                        Utilities.LogInfo($"{operationId}Start - Attempting to Save Part Compatability Data for <{part.PartNumber}>");
                        bl.InsertPartCompatibility(part);
                        Utilities.LogInfo($"{operationId}DONE - Attempting to Save Part Compatability Data for <{part.PartNumber}> Count({count})");
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError($"{operationId} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        if (i < 5) continue; else return;
                    }
                    lock (totalPartsAddedLockObject)
                    {
                        totalPartsAdded++;
                    }
                    break;
                }
            });
            
        }

        private void SavePartInterchangeDataToDatabase(IEnumerable<AutoPartInterchangeNumber> interchangeNumbers)
        {
            var bl = CreateBL();

            Parallel.ForEach(interchangeNumbers, options, interchangeNumber =>
            {
                if (interchangeNumber == null) return;
                var count = 0;
                Utilities.LogInfo($"START - Attempting to Save Part Data for <{interchangeNumber.PartNumber}>");
                count = bl.InsertPartInterchangeNumber(interchangeNumber);                
                Utilities.LogInfo($"DONE - Attempting to Save Part Data for <{interchangeNumber.PartNumber}> Count({count})");
            });
        }

        private void SavePartData(IEnumerable<AutoPart> partsParam, SubCategory sub, PageBatch pageBatch)
        {
            //if (Config.CrawlOnly) return;

            //SavePartDataToDatabase(partsParam);

            //if (Config.WriteToDatabaseOnly)
            //{
            //    return;
            //}

            ////Remove any null entries from the array before processing
            
            //var parts = partsParam.Where(r => null != r).ToArray();
            //Guid guid = Guid.NewGuid();
            //string subCatId = sub.Value;
            //string subCat = sub.Text;
            //string catId = sub.ParentCategory.Id.ToString();
            //string cat = sub.ParentCategory.Name;

            //string logInfoSuffix = sub.IsFullSearch() ? $"Generating Data Files for FullSearch PageStart_{pageBatch.StartPage.ToString("00000")} PageEnd_{pageBatch.EndPage.ToString("00000")} with {parts.Count()} parts." : 
            //    $"Generating Data Files for Category: <{cat}> - SubCategory: <{subCat}> with {parts.Count()} parts.";

            //Utilities.LogInfo($"START - {logInfoSuffix}");

            //string csvFolder = Path.Combine(runDateTimePath, $"csv");
            //string jsonFolder = Path.Combine(runDateTimePath, "json");
            //string xmlFolder = Path.Combine(runDateTimePath, "xml");

            //if (!Directory.Exists(csvFolder)) Directory.CreateDirectory(csvFolder);
            //if (!Directory.Exists(jsonFolder)) Directory.CreateDirectory(jsonFolder);
            //if (!Directory.Exists(xmlFolder)) Directory.CreateDirectory(xmlFolder);

            //string fullSearchFileNamePrefix = $"FullSearch_PageStart_{pageBatch.StartPage.ToString("00000")}_PageEnd_{pageBatch.EndPage.ToString("00000")}_COUNT_{parts.Length}";
            //string catSubCatFilePrefix = $"CAT_{catId}_SUBCAT_{subCatId}_COUNT_{parts.Length}";

            //string csvFilePath = Path.Combine(csvFolder, sub.IsFullSearch() ? $"{fullSearchFileNamePrefix}.csv" : $"{catSubCatFilePrefix}.csv");
            //string xmlFilePath = Path.Combine(xmlFolder, sub.IsFullSearch() ? $"{fullSearchFileNamePrefix}.xml" : $"{catSubCatFilePrefix}.xml");
            //string jsonFilePath = Path.Combine(jsonFolder, sub.IsFullSearch() ? $"{fullSearchFileNamePrefix}.json" : $"{catSubCatFilePrefix}.json");

            ////Generate JSON File

            //File.WriteAllText(jsonFilePath, dotnetscrape_lib.Utilities.SerializeObject(parts, true));
            //Utilities.LogInfo($"JSON File written to: {jsonFilePath}");

            ////Generate XML File
            //File.WriteAllText(xmlFilePath, dotnetscrape_lib.Utilities.SerializeObject(parts, false));
            //Utilities.LogInfo($"XML File written to: {xmlFilePath}");

            ////Genearte CSV File
            //string csvString = $"{AutoPart.CSVHeader}{Environment.NewLine}";
            //foreach (var part in parts)
            //{
            //    csvString += $"{part.CSV()}{Environment.NewLine}";
            //}
            //File.WriteAllText(csvFilePath, csvString);
            //Utilities.LogInfo($"CSV File written to: {csvFilePath}");

            //Utilities.LogInfo($"DONE - {logInfoSuffix}");
        }

        private static Regex maxResultsRegex = new Regex("Displaying\\s.+?\\sto\\s.+?\\sof\\s(.+?)\\smatches");
        private static Regex digitsOnlyRegex = new Regex(@"[^\d]");

        private List<PageBatch> GetPageBatches(int maxPages)
        {
            var batches = new List<PageBatch>();
            //Handle case where there are no pages to process
            if(maxPages == 0)
            {
                return batches;
            }
            int totalBatches = maxPages / (Constants.pagesPerBatch);
            int remainderPages = maxPages % (Constants.pagesPerBatch);
            
            //Handle case where the number of pages to process is less than the total number of pages
            //is less than the total number of pages per batch
            if(totalBatches == 0)
            {
                batches.Add(
                    new PageBatch
                    {
                        StartPage = 0,
                        EndPage = remainderPages
                    });
                return batches;
            }

            int i = 0;
            for(i=0;i<totalBatches;i++)
            {
                batches.Add(
                    new PageBatch
                    {
                        StartPage = i * Constants.pagesPerBatch,
                        EndPage = (i + 1) * Constants.pagesPerBatch
                    }
               );
            }

            if(remainderPages > 0)
            {
                batches.Add(
                    new PageBatch
                    {
                        StartPage = i * Constants.pagesPerBatch,
                        EndPage = i * Constants.pagesPerBatch + remainderPages
                    });
            }
           
            return batches;
        }

        public void GetPartsFromJSON()
        {
            string filePath = Config.JsonDataPath;
            if (!Directory.Exists(filePath))
            {
                Utilities.LogError($"File Path {filePath} does not exist");
            }

            Stopwatch serializeJsonDataAndLoadIntoMapStopwatch = new Stopwatch();
            serializeJsonDataAndLoadIntoMapStopwatch.Start();
            Utilities.LogInfo($"START - Loading Data from {filePath}");
            int fileCount = 0;
            object lockFileCount = new object();
            Parallel.ForEach(Directory.GetFiles(filePath, "*.json"), options, (file, loopState) =>
             {
                 try
                 {
                     Utilities.LogInfo($"START - Serializing {file}");
                     var parts = dotnetscrape_lib.Utilities.DeserializeFile<List<AutoPart>>(file, true);
                     Utilities.LogInfo($"DONE - Serializing {file}");
                     lock (lockFileCount)
                     {
                         fileCount++;
                     }

                     Parallel.ForEach(parts, options, (part, loopState2) =>
                     {
                         PartsFromJSON.TryAdd(part.PartNumber, part);
                     });
                 }
                 catch (Exception ex)
                 {
                     Utilities.LogError($"Unable to serialize {file}:  Reason: {ex.Message}");
                 }
             });
            serializeJsonDataAndLoadIntoMapStopwatch.Stop();
            Utilities.LogInfo($"DONE - Loading Data from {filePath}");


            

            Stopwatch getPartQuantitesStopwatchFullTime = new Stopwatch();
            getPartQuantitesStopwatchFullTime.Start();
            //Attempt to Retrieve Part Quantities
            if (Config.GetPartCounts)
            {
                int partIndex = 1;
                var partIndexLock = new object();
                var array = PartsFromJSON.Values.OrderBy(part => part.PartNumber).ToArray();
                Utilities.LogInfo($"START - Attempting to get part quantites for {PartsFromJSON.Count} parts.");
                Parallel.ForEach(array, options, (part, loopState2) =>
                {
                    try
                    {
                        Stopwatch getPartQuantitesStopwatchPerPart = new Stopwatch();
                        Utilities.LogDebug($"START - [{partIndex}] - Getting Part Quantities for Part Number: {part.PartNumber}");
                        getPartQuantitesStopwatchPerPart.Start();
                        part.FindItQuantities = FindPartQuantities(part).Result;
                        getPartQuantitesStopwatchPerPart.Stop();
                        double timeToGetPartCount = (double)getPartQuantitesStopwatchPerPart.ElapsedMilliseconds / 1000;
                        Utilities.LogInfo($"DONE - [{partIndex}] - Getting Part Quantities for Part Number: {part.PartNumber}.  Time: {timeToGetPartCount} seconds.");
                        lock(partIndexLock)
                        {
                            partIndex++;
                        }
                        if(partIndex % 500 == 0)
                        {
                            System.Threading.Thread.Sleep(1000); 
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError($"Unable to get part counts for {part.PartNumber}:  Reason: {ex.Message}");
                    }

                });

                Utilities.LogInfo($"DONE - Attempting to get part quantites for {PartsFromJSON.Count} parts.");
            }
            getPartQuantitesStopwatchFullTime.Stop();

            

            double partCount = PartsFromJSON.Keys.Count;
            double totalTimeToLoadIntoMemory = (double)serializeJsonDataAndLoadIntoMapStopwatch.ElapsedMilliseconds / 1000;
            double totalTimeToRetrievePartCount = (double)getPartQuantitesStopwatchFullTime.ElapsedMilliseconds / 1000;
            double averageTimeLoadFileIntoMemory = (fileCount == 0) ? -1 : totalTimeToLoadIntoMemory / fileCount;
            double averageTimeLoadPartIntoMemoryFromDisk = (partCount == 0) ? -1 : totalTimeToLoadIntoMemory / partCount;
            double averageTimeToRetievePartCount = (partCount == 0 && Config.GetPartCounts) ? -1 : totalTimeToRetrievePartCount / partCount;
            Utilities.LogInfo($"Total time to deserialize {PartsFromJSON.Count} parts from {fileCount} files is {totalTimeToLoadIntoMemory} seconds.");
            Utilities.LogInfo($"Average time to deserialize each file: {averageTimeLoadFileIntoMemory} seconds");
            Utilities.LogInfo($"Average time to deserialize each part: {averageTimeLoadPartIntoMemoryFromDisk} seconds");
            Utilities.LogInfo($"Average time to retrieve part count for each part: {averageTimeToRetievePartCount} seconds");

        }

        public void GeneartePartsFromJSONData()
        {
            WriteDataFilesPartsFromJSON();
        }

        private async Task GetPartsBySubCategory(SubCategory subCat, int startPage, int maxPages, string progressFile, double categoryProgress, string operationId)
        {
            bool cacheLookup = (Config.PerformCacheLookup);

            object searchTaskLockObject = new object();
            string prefix = $"{operationId}GetPartsBySubCategory::";
            string initialResponse = string.Empty;
            try
            {
                int endOfPaginatedResults = int.MaxValue;
                {
                    //figure out how many result pages there actually are
                    //we have to hit this page or else search results will come back invalid
                    string searchUrl = $"https://www.dotnetprolink.com/resultsmi.aspx?Ntt=&N={subCat.Value}&SearchType=1";
                    ClearCache(searchUrl);
                    var response = await Submit(searchUrl, "GET", null, null, cacheLookup, Config.CacheResponses,false, prefix);

                    //Always clear the cache for this first search to ensure that we initialize the search engine each time we run.
                    

                    var match = maxResultsRegex.Match(response.Result);
                    if (match.Success)
                    {
                        var maxStr = digitsOnlyRegex.Replace(match.Groups[1].Value, string.Empty);
                        int maxItemsParsed = -1;
                        if (int.TryParse(maxStr, out maxItemsParsed))
                        {
                            int maxPagesParsed = (maxItemsParsed % Constants.partsPerPage != 0) ? (maxItemsParsed / Constants.partsPerPage) + 1 : maxItemsParsed / Constants.partsPerPage;
                            if (maxPagesParsed < maxPages)
                            {
                                maxPages = maxPagesParsed;
                                endOfPaginatedResults = startPage + maxPages;
                            }
                        }
                        initialResponse = response.Result;
                    }
                    else
                    {
                        Utilities.LogError($"{prefix}Unable to retrieve number of parts in Category: <{subCat.ParentCategory.Id}> SubCategory <{subCat.Value}>");
                        return;
                    }
                }

                object progressLockObj = new object();
                int progressCount = 0;
                var searchResultPages = new List<string>();


                Utilities.LogInfo($"{prefix}START - SubCat: {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
                //var searchTasks = new List<Task>();

                var pageBatches = GetPageBatches(maxPages);
                
                //Reverse order processing of pages
                if(Config.ProcessInReverseOrder)
                    pageBatches = pageBatches.OrderByDescending(p => p.StartPage).ToList();
                else
                    pageBatches = pageBatches.OrderBy(p => p.StartPage).ToList();

                foreach (var pageBatch in pageBatches)
                {
                    searchResultPages.Clear();
                    //searchTasks.Clear();
                    if (pageBatch.StartPage == 0)
                    {
                        pageBatch.StartPage++;
                        searchResultPages.Add(initialResponse);
                    }
                    //Collect Search Data
                    for (int pageToWorkOn = pageBatch.StartPage; pageToWorkOn < pageBatch.EndPage; pageToWorkOn++)
                    {
                        if (Stop) { break; }
                        if (pageToWorkOn >= endOfPaginatedResults) { break; }
                        
                        //Create a new parallel action
                        //Action<object> action = (currentPageObj) =>
                        //{
                            string webRequestId = prefix + Utilities.GenerateWebRequestId();
                            int currentPage = (int)pageToWorkOn;
                            int currentPageForOutput = currentPage + 1;
                            var searchString = $"N={subCat.Value}&Ntt=&Nao={Constants.partsPerPage * currentPage}";
                            Utilities.LogInfo($"{webRequestId}START - Search data for parts in Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages})");

                            string searchUrl = $"https://www.dotnetprolink.com/resultsmi.aspx?{searchString}";

                            int maxTries = 10;
                            int nullResponseRetry = 0;
                            while (--maxTries >= 0)
                            {
                                if (Stop) { break; }
                                var response = Submit(searchUrl, "GET", null, null, cacheLookup, Config.CacheResponses,false, webRequestId).Result;
                                if (response.Result.Contains("No search results found"))
                                {
                                    if (currentPage < endOfPaginatedResults)
                                    {
                                    //this can happen if you skip the step above (hit the initial search page)
                                    Utilities.LogError($"{webRequestId}Search came back with no results. Category: <{subCat.ParentCategory.Name}> SubCategory <{subCat.Text}> Page <{currentPageForOutput}> EndOfPaginatedResults <{endOfPaginatedResults}>");
                                    }
                                    ClearCache(searchUrl);
                                    Thread.Sleep(rng.Next(1000, 3000));
                                    //ask.Delay(rng.Next(1000, 3000));
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(response.Result))
                                {
                                    lock (searchTaskLockObject)
                                    {
                                        searchResultPages.Add(response.Result);
                                    }
                                    WriteProgress(progressFile, subCat.ParentCategory, categoryProgress, ((double)++progressCount / (double)searchResultPages.Count));
                                    break;
                                }
                                else
                                {
                                    nullResponseRetry++;
                                    string nullResponseRetryFailedStr = (nullResponseRetry == 4) ? " ### NULL RESPONSE RETRY FAILED ### " : string.Empty;
                                    Utilities.LogError($"{webRequestId}Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages}) - null response from {searchUrl} Retry Count: {nullResponseRetry}{nullResponseRetryFailedStr}");
                                    Thread.Sleep(rng.Next(1000, 3000));
                                    //Task.Delay(rng.Next(1000, 3000));

                                }
                            }

                            Utilities.LogInfo($"{webRequestId}DONE - Search data for parts in Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages})");
                        //};

                        //searchTasks.Add(new Task(action, pageToWorkOn));
                    }

                    //Execute Search Tasks in Parallel and wait unti all searches are complete
                    //Utilities.StartAndWaitAllThrottled(searchTasks, Config.MaxNumberOfCurrentSearchQueryTask);

                    if (Stop) { return; }

                    progressCount = 0;
                    var parts = new AutoPart[searchResultPages.Count * Constants.partsPerPage];
                    
                    //Process Search Results
                    if (!Config.RetrievePartDetails)
                    {
                        Utilities.LogInfo($"{prefix}DONE - SubCat ##### {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
                        continue;
                    }

                    int pageCount = searchResultPages.Count;
#if DEBUG
                    pageCount = 1;
#endif

                    Parallel.For(0, pageCount, options, parallelPage =>
                    {
                        if (Stop) { return; }
                        var partsPartial = HTMLToAutoParts(searchResultPages[parallelPage], subCat, progressFile, categoryProgress,operationId).Result.ToArray();
                        WriteProgress(progressFile, subCat.ParentCategory, categoryProgress, ((double)++progressCount / (double)parts.Length));

                        if (partsPartial.Length > 0)
                        {
                            Array.Copy(partsPartial, 0, parts, parallelPage * Constants.partsPerPage, partsPartial.Length);
                        }
                        partsPartial = null;
                    });

                    SavePartDataToDatabase(parts,operationId);

                    //SavePartData(result, subCat, pageBatch);
                    parts = null;

                    if (Stop) { return; }

                    Utilities.LogInfo($"{prefix}DONE - SubCat ##### {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
                }

            }
            catch(Exception ex)
            {
                Utilities.LogError($"{prefix} ERROR - SubCat {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages}){Environment.NewLine}{ex.Message}");
            }
        }


        private static void WriteProgress(string filename, Category category, double categoryProgress/*0.0-1.0*/, double subcategoryProgress/*0.0-1.0*/)
        {
            if (!Config.WriteToProgressFile) return;

            const int maxProgressColumns = 20;
            var sb = new StringBuilder();

            int progressMarks = (int)(subcategoryProgress * maxProgressColumns);
            sb.Append($"[");
            for (int i = 0; i < maxProgressColumns; ++i)
            {
                if (i <= progressMarks)
                {
                    sb.Append("#");
                }
                else
                {
                    sb.Append(" ");
                }
            }
            sb.Append("]");
            sb.Append(Environment.NewLine);

            progressMarks = (int)(categoryProgress * maxProgressColumns);
            sb.Append($"[");
            for (int i = 0; i < maxProgressColumns; ++i)
            {
                if (i <= progressMarks)
                {
                    sb.Append("#");
                }
                else
                {
                    sb.Append(" ");
                }
            }
            sb.Append("]");
            sb.Append(Environment.NewLine);

            object lockObject = ProgressLocks[category.Name];
            if (lockObject != null)
            {
                lock (lockObject)
                {
                    File.WriteAllText(filename, sb.ToString());
                }
            }
        }

        private static object totalCountLockObject = new object();
        private static object totalPartsAddedLockObject = new object();
        private static object totalPartsUpdatedLockObject = new object();
        private static object totalPartsCachedLockObject = new object();

        public async Task<IEnumerable<AutoPart>> HTMLToAutoParts(string html, SubCategory subCat, string progressFile, double categoryProgress, string operationId)
        {
            //Protect against exception thrown by HTMLAgility "(Value cannot be null. Parameter name: html)"
            if (string.IsNullOrWhiteSpace(html))
            {
                return new List<AutoPart>();
            }

            var bl = CreateBL();
           

            Utilities.LogDebug($"{operationId}START - Extract parts detail in Category: <{subCat.ParentCategory.Id}> SubCategory <{subCat.Value}>");
            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(req => req.Content(html));
           
            
            
            //doc.LoadHtml(html);

            var parts = new List<AutoPart>();

            //var nodes = doc.DocumentElement.SelectNodes("//*[@id='ctl00_mainContentPlaceHolder_ProlinkResultList1']/table/tbody/tr");
            var nodes = doc.DocumentElement.QuerySelectorAll("tr.result-item-row");
            if (nodes != null && nodes.Any())
            {
                parts = nodes.Select(tr =>
                {
                    
                   //var tr = trAsNode as IElement;

                    var autoPart = new AutoPart();
                    if (!tr.ClassList.Contains("result-item-row"))
                        return autoPart;


                    var partnum = GetElementsWithClass(tr, "td.resultpartnum").FirstOrDefault()?.Text();

                    //Set Part Number
                    if (!string.IsNullOrWhiteSpace(partnum))
                    {
                        autoPart.PartNumber = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(partnum);
                    }
                    if (subCat != null)
                    {
                        autoPart.Category = (string.Equals(subCat.ParentCategory.Name, Constants.FullSearchCategoryName, StringComparison.OrdinalIgnoreCase)) ? string.Empty : subCat.ParentCategory.Name;
                        autoPart.CategoryId = subCat.ParentCategory.Id;
                        autoPart.SubCategory = (string.Equals(subCat.Text, Constants.FullSearchSubCategoryName, StringComparison.OrdinalIgnoreCase)) ? string.Empty : subCat.Text;
                        autoPart.SubCategoryId = subCat.Value;
                    }

                    //If part already exist in database, then just update
                    autoPart.Exists = bl.PartExists(autoPart.PartNumber);
                    

                    var partnameEl = GetElementsWithClass(tr, "td.resultdesc").FirstOrDefault();
                    if (null != partnameEl)
                    {
                    //Set ProductLine
                    var imgEl = partnameEl.SelectSingleNode(".//img");
                        if (null != imgEl && null != imgEl.NextSibling)
                        {
                            autoPart.ProductLine = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(imgEl.NextSibling.Text());
                        }

                        var aEl = (IElement)partnameEl.SelectSingleNode(".//a");
                        if (null != aEl)
                        {
                        //Set Part Name
                        autoPart.PartName = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(aEl.Text());
                            var href = aEl.Attributes.Where(attrib => attrib.Name == "href").FirstOrDefault().Value;
                            if (!string.IsNullOrWhiteSpace(href))
                            {
                                string detailUrl = $"https://www.dotnetprolink.com/{href.Trim()}";
                                autoPart.DetailUrl = detailUrl;
                            }
                        }
                    }
                    return autoPart;
              })
              .Where(part => part != null && !string.IsNullOrWhiteSpace(part.PartNumber)).ToList();
            }

            var partsThatExist = parts.Where(p => p.Exists);
            var partsThatDoNotExists = parts.Where(p => !p.Exists);

            //Simply Update the category information for each Part already in the database.
            Parallel.ForEach(partsThatExist, options, (part, loopState) =>
            {
                Utilities.LogInfo($"{operationId}START - Attempting to Update Part Category and SubCategory for <{part.PartNumber}>");
                bl.UpdatePartCategoryInfo(part);
                lock(totalPartsUpdatedLockObject)
                {
                    totalPartsUpdated++;
                }
                Utilities.LogInfo($"{operationId}DONE - Attempting to Update Part Category and SubCategory for <{part.PartNumber}>");
            });



            double progressCount = 0;

            _ = Parallel.ForEach(partsThatDoNotExists, options, (part, loopState) =>
              {
                  if (Stop) { return; }
                  string webRequestId = operationId + Utilities.GenerateWebRequestId();
                  Utilities.LogInfo($"{webRequestId}START - Getting PartDetails for {part.PartNumber}");
                  var detailResponse = Submit(part.DetailUrl,"GET",null,null,Config.PerformCacheLookup,true,false, webRequestId).Result;
                  Utilities.LogInfo($"{webRequestId}DONE - Getting PartDetails for {part.PartNumber}");
                  if (!string.IsNullOrWhiteSpace(detailResponse.Result))
                  {
                      lock (totalCountLockObject)
                      {
                          totalPartCount++;
                      }

                      if (!detailResponse.CacheHit)
                      {
                          lock (totalPartsCachedLockObject)
                          {
                              totalPartsCached++;
                          }
                      }

                      Utilities.LogInfo($"{webRequestId}START - Parsing PartDetails for {part.PartNumber}");
                      part = HTMLToAutoPartDetail(detailResponse.Result, part).Result;
                      Utilities.LogInfo($"{webRequestId}DONE - Parsing PartDetails for {part.PartNumber}");

                      
                      part.PartCompatibilities = GetAutoPartCompatibility(part.PartNumber, webRequestId).Result;
                      

                      WriteProgress(progressFile, subCat.ParentCategory, categoryProgress, ((double)++progressCount / (double)parts.Count));
                  }
              });

            //If we are stopping are we are configured to no collect parts in memory then just return empty list.
            if (Stop)
            {
                return Enumerable.Empty<AutoPart>();
            }
            
            return (!Config.CrawlOnly) ? partsThatDoNotExists : new List<AutoPart>();
            
        }

        public async Task<AutoPart> HTMLToAutoPartDetail(string html, AutoPart autoPart)
        {
            if (Config.CrawlOnly)
            {
                return autoPart;
            }

            //Protect against exception thrown by HTMLAgility "(Value cannot be null. Parameter name: html)"
            if (string.IsNullOrWhiteSpace(html))
            {
                return autoPart;
            }
           
            //detailDoc.LoadHtml(html);

            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(req => req.Content(html));

            var detailDoc = doc.DocumentElement;

            var titleEl = GetElementsWithClass(detailDoc, "span.widepageTitle");
            if (null != titleEl && titleEl.Count() > 0)
            {
                autoPart.PartName = titleEl.FirstOrDefault().Text();
            }
            //Image URL
            if(detailDoc.SelectSingleNode("//img[@id='ImageControlctl00_mainContentPlaceHolder_MultiImageControl1']") is IElement imgUrlEl)
            {
                IElement parent = imgUrlEl.ParentElement;
                if (parent != null &&
                    parent.Attributes != null &&
                    parent.Attributes.Count() > 0)
                {
                    var imgUrl = parent.Attributes[0].Value.Replace("MultiImageOnClick('", string.Empty).Replace("')", string.Empty);
                    var imgParts = imgUrl.Substring(imgUrl.IndexOf("asset=") + 5).Split(new[] { ',' })
                                            .Select(s =>
                                            {
                                                var partNum = Regex.Match(s.Split(new[] { '/' })[1], "[0-9]+").Value;
                                                return $"https://s7d9.scene7.com/is/image/GenuinePartsCompany/{partNum}?.jpg&fit=constrain,1&wid=2000&hei=2000";
                                            }).ToList();
                    autoPart.ImageUrls.AddRange(imgParts);
                }
            }

            //Pricing Information
            // //*[@id="ctl00_mainContentPlaceHolder_ProlinkPricingControl1"]/table/tbody/tr/td[2]
            if (detailDoc.SelectSingleNode("//*[@id='ctl00_mainContentPlaceHolder_ProlinkPricingControl1']/table/tbody/tr/td[2]") is IElement priceEl)
            {
                var values = priceEl.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                bool coreIncluded = values.Count == 4;
                int listIndex = 0;
                int coreIndex = 1;
                int costIndex = (coreIncluded) ? 2 : 1;
                int unitIndex = (coreIncluded) ? 3 : 2;
                
                if (values.Count > listIndex && decimal.TryParse(values[listIndex], out decimal price))
                {
                    autoPart.Pricing.List = price;
                }

                if (coreIncluded)
                {
                    if (values.Count > coreIndex && decimal.TryParse(values[coreIndex], out price))
                    {
                        autoPart.Pricing.Core = price;
                    }
                }

                if (values.Count > costIndex && decimal.TryParse(values[costIndex], out price))
                {
                    autoPart.Pricing.Cost = price;
                }

                if (values.Count > unitIndex)
                {
                    autoPart.Pricing.Unit = values[unitIndex];
                }
            }

            //Feature and Benefits
            if(detailDoc.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_FeaturesBenefitsDataLinkControl1']") is IElement featuresEl)
            {
                autoPart.FeatureAndBenefits = featuresEl.Text();
                var featureAndBenefitsUrlEl = (IElement)featuresEl.SelectSingleNode(".//a");
                if (null != featureAndBenefitsUrlEl)
                {
                    autoPart.FeatureAndBenefitsUrl = featureAndBenefitsUrlEl.Attributes["href"]?.Value;
                    autoPart.FeatureAndBenefitsUrlText = featureAndBenefitsUrlEl.Text();
                }
            }


            ////Quantity Available
            //var quantityEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='ctl00_mainContentPlaceHolder_qtyAvailLabel']");
            //if (null != quantityEl)
            //{
            //    string[] quantityStrs = quantityEl.InnerText.Split(":".ToCharArray());
            //    if (quantityStrs.Length == 2)
            //    {
            //        decimal quantity = 0m;
            //        decimal.TryParse(quantityStrs[1].Trim(), out quantity);
            //        autoPart.Quantity = quantity;
            //    }
            //}

            //Warranty
            if(detailDoc.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_WarrantyDataLinkControl1']/a") is IElement warrantyEl)
            {
                autoPart.WarrantyUrl = warrantyEl.Attributes["href"]?.Value;
            }

            //Attributes
            //*[@id="pagewrap"]/table/tbody/tr/td/table[1]/tbody/tr[2]/td[3]/table[2]/tbody/tr[1]
            //(//tr/td[@class='DetailBodyHeader' and contains(text(),'Attributes')]/ancestor::tr/following-sibling::tr)/td/text()
            if(detailDoc.SelectNodes("(//tr/td[@class='DetailBodyHeader' and contains(text(),'Attributes')]/ancestor::tr/following-sibling::tr)/td/text()") is List<INode> attributes)
            {
                foreach (var attr in attributes)
                {
                    var attribute = attr.TextContent.Trim();
                    if (attribute == "") break;

                    var attributeTokens = attribute.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (attributeTokens.Length != 2) { continue; }
                    autoPart.Attributes.Add(new AutoPartAttribute { Name = attributeTokens[0].Trim(), Value = attributeTokens[1].Trim() });
                }
            }

            ////MSDS
            if (detailDoc.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_MsdsDataLinkControl1']/a") is IElement mdsEl)
            {
                autoPart.MsdsUrl = mdsEl.Attributes["href"]?.Value;
            }

            return autoPart;
        }

        public void IntializeFindIt()
        {
            if (Config.CloneFindItSearchParms)
            {
                findIt = GetFindIt(false).Result;
            }
        }
        public async Task<FindIt> GetFindIt(bool useCache)
        {
            if(findIt != null && Config.CloneFindItSearchParms)
            {
                lock (findItCloneLock)
                {
                    return findIt.Clone();
                }
            }

            string searchUrl = $"https://www.dotnetprolink.com/resultsmi.aspx?Ntt=&N=0&SearchType=1";
            var response = await Submit(searchUrl, "GET",null, null, useCache, Config.CacheResponses, false);
            var html = response.Result;

            if (string.IsNullOrWhiteSpace(html) || !html.Contains("var partnerGrp1"))
            {
                return null;
            }

            int i = html.IndexOf("var partnerGrp1");
            html = html.Substring(i);
            i = html.IndexOf("function");
            html = html.Substring(0, i).Trim();
            var varParts = html.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var parsedVariables = new Dictionary<string, string>();
            foreach (var varPart in varParts)
            {
                i = varPart.IndexOf("=");
                if (-1 == i || i == varPart.Length) { continue; }
                var name = varPart.Substring(0, i).Replace("var ", "").Trim();
                var val = varPart.Substring(i + 1).Trim().TrimStart('\'').TrimEnd('\'');
                if (val.StartsWith("new Array")) { continue; }
                parsedVariables[name] = val;
            }

            //POST https://www.dotnetprolink.com/services/partlocator.asmx/CheckDC
            var CheckDC = new Dictionary<string, string>
            {
                { "divNumber", parsedVariables["npa_div"] },
                { "dcNumber", parsedVariables["npa_dc"] },
                { "dcAbbrev", parsedVariables["npa_dcAbbrev"] },
                { "customerNumber", parsedVariables["npa_cus"] },
                { "lineCode", "" },
                { "partNumber", "" },
                { "useBizTalk", parsedVariables["useBizTalk"] },
                { "associatedDC1", parsedVariables["npa_aDC1Abbrev"] },
                { "associatedDC2", parsedVariables["npa_aDC2Abbrev"] },
                { "sid", parsedVariables["npa_sid"] },
                { "spenc", parsedVariables["npa_spenc"] },
                { "useEjoei", parsedVariables["npa_useEjoei"] }
            };

            //POST https://www.dotnetprolink.com/services/partlocator.asmx/CheckSupplier
            var CheckSupplier = new Dictionary<string, string>
            {
                { "divNumber", parsedVariables["npa_div"] },
                { "dcNumber", parsedVariables["npa_dc"] },
                { "dcAbbrev", parsedVariables["npa_dcAbbrev"] },
                { "customerNumber", parsedVariables["npa_cus"] },
                { "lineCode","" },
                { "partNumber", "" },
                { "sid", parsedVariables["npa_sid"] },
                { "useEjoei", parsedVariables["npa_useEjoei"] }
            };

            var GetStoreQtys = new List<Dictionary<string, string>>();
            var partnerGrp1s = parsedVariables.Where(kv => kv.Key.StartsWith("partnerGrp1["));
            var partnerGroupTP1s = parsedVariables.Where(kv => kv.Key.StartsWith("partnerGroupTP1["));
            var partnerGroupAlias1s = parsedVariables.Where(kv => kv.Key.StartsWith("partnerGroupAlias1["));
            foreach (var partnerGrp1 in partnerGrp1s)
            {
                i = partnerGrp1.Key.IndexOf('[');
                var index = partnerGrp1.Key.Substring(i);
                var accountId = partnerGroupTP1s.Where(kv => kv.Key.EndsWith(index)).Select(kv => kv.Value).FirstOrDefault();
                var storeName = partnerGroupAlias1s.Where(kv => kv.Key.EndsWith(index)).Select(kv => kv.Value).FirstOrDefault();
                //POST https://www.dotnetprolink.com/services/partlocator.asmx/GetStoreQty
                var GetStoreQty = new Dictionary<string, string>
                {
                    { "tamsStoreId", partnerGrp1.Value },
                    { "lineCode", "" },
                    { "partNumber", "" },
                    { "accountId", accountId },
                    { "storeName", storeName }
                };
                GetStoreQtys.Add(GetStoreQty);
            }

            var ret = new FindIt
            {
                CheckDC = CheckDC,
                CheckSupplier = CheckSupplier,
                GetStoreQtys = GetStoreQtys
            };
            return ret;
        }

        public async Task<AutoPartQuantity> FindPartQuantities(AutoPart part)
        {
            var findit = await GetFindIt(true);
            var cacheLookup = Config.PerformCacheLookup;
            var persistQuantityInCache = Config.CachePartQuantities;

            var method = "POST";
            var contentType = "application/json; charset=utf-8";

            findit.CheckDC["lineCode"] = FindIt.GetLineCode(part);
            findit.CheckDC["partNumber"] = FindIt.GetPartNumber(part);
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(findit.CheckDC);
            var url = "https://www.dotnetprolink.com/services/partlocator.asmx/CheckDC";
            var response = await Submit(url, method, body, contentType, cacheLookup, persistQuantityInCache);
            var dcQty = DistributionCenterQuantity.FromJSON(response.Result);

            findit.CheckSupplier["lineCode"] = FindIt.GetLineCode(part);
            findit.CheckSupplier["partNumber"] = FindIt.GetPartNumber(part);
            body = Newtonsoft.Json.JsonConvert.SerializeObject(findit.CheckSupplier);
            url = "https://www.dotnetprolink.com/services/partlocator.asmx/CheckSupplier";
            response = await Submit(url, method, body, contentType, cacheLookup, persistQuantityInCache);
            var supplierQty = SupplierQuantity.FromJSON(response.Result);

            var storeQty = new List<StoreQuantity>();
            var storeQtyLock = new object();
            Parallel.ForEach(findit.GetStoreQtys, options, (getStoreQty, loopState) =>
            {
                getStoreQty["lineCode"] = FindIt.GetLineCode(part);
                getStoreQty["partNumber"] = FindIt.GetPartNumber(part);

                body = Newtonsoft.Json.JsonConvert.SerializeObject(getStoreQty);
                url = "https://www.dotnetprolink.com/services/partlocator.asmx/GetStoreQty";
                response = Submit(url, method, body, contentType, cacheLookup, persistQuantityInCache).Result;
                var sqty = StoreQuantity.FromJSON(response.Result);
                if (sqty != null)
                {
                    sqty.Name = getStoreQty["storeName"];
                    lock (storeQtyLock)
                    {
                        storeQty.Add(sqty);
                    }
                }
            });

            var ret = new AutoPartQuantity
            {
                DCQty = dcQty,
                SupplierQty = supplierQty,
                StoreQty = storeQty
            };
            return ret;
        }

        private Stream GetResponseStream(HttpWebResponse response)
        {
            Stream dataStream = null;
            if (!string.IsNullOrWhiteSpace(response.ContentEncoding) && response.ContentEncoding.ToLower().Contains("gzip"))
            {
                dataStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else
            {
                dataStream = response.GetResponseStream();
            }
            return dataStream;
        }

        private string RequestHashString(string url, string method, string body, string contentType)
        {
            return ($":U_{url}_U:M_{method}_M:B_{body ?? string.Empty}_B:CT_{contentType ?? string.Empty}_CT");
        }

        private string GetCachedFileLocation(string url, string method, string body, string contentType)
        {
            var cachedPath = Config.ProcessingPath;
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in pathParts)
            {
                cachedPath = Path.Combine(cachedPath, part);
            }
            var requestHash = dotnetscrape_lib.Utilities.GetHashSha256(RequestHashString(url, method, body, contentType));
            cachedPath = Path.Combine(cachedPath, requestHash);
            return cachedPath;
        }

        private void ClearCache(string url, string method = "GET", string body = null, string contentType = null)
        {
            var cachedPath = GetCachedFileLocation(url, method, body, contentType);

            if (File.Exists(cachedPath))
            {
                try
                {
                    File.Delete(cachedPath);
                }
                catch(Exception ex)
                {
                    Utilities.LogError($"In ClearCache: {cachedPath} {ex.Message}");
                }
            }
        }

        private static Random rng = new Random();
        private int pendingRequestCount = 0;
        private static object reqLockObj = new object();


        private async Task<SubmitResult> Submit(string url, string method="GET", string body=null, string contentType=null, bool checkCache = true, bool cacheResult = true, bool saveCookies = false, string operationId = "" )
        {
            var result = new SubmitResult();
            var cachedPath = GetCachedFileLocation(url, method, body, contentType);
            bool cacheFileFound = File.Exists(cachedPath);

            if (checkCache || Config.UseCacheOnly)
            {
                if(cacheFileFound)
                {
                    Utilities.LogDebug($"{operationId}Cache hit: {cachedPath} File hash: {RequestHashString(url, method, body, contentType)}");
                    try
                    {
                        var contents = File.ReadAllText(cachedPath);
                        if (!string.IsNullOrWhiteSpace(contents))
                        {
                            result.CacheHit = true;
                            result.Result = contents;
                        }
                    }
                    catch (Exception e)
                    {
                        result.Error = e.Message;
                    }
                }
               
                if(cacheFileFound || Config.UseCacheOnly)
                    return result;
            }

            while(pendingRequestCount >= Config.MaxPendingRequests)
            {
                await Task.Delay(rng.Next(1000, 3000));
            }

            lock (reqLockObj)
            {
                ++pendingRequestCount;
            }

            try
            {
                int maxTries = 2;
                while (--maxTries >= 0)
                {
                    if (Stop) { return result; }

                    var req = WebRequest.Create(url);
                    req.Method = method;
                    req.Proxy = (Config.UseProxy) ? proxy : null;
                    req.Timeout = 1000000;
                    var http = (req as HttpWebRequest);
                    http.KeepAlive = true;
                    http.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    http.CookieContainer = cookies;
                    //http.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                    http.Accept = "*/*";
                    http.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                    http.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                    http.Headers.Add("Upgrade-Insecure-Requests", "1");
                    http.Headers.Add("Pragma", "no-cache");
                    http.Headers.Add("Cache-Control", "no-cache");
                    http.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
                    http.Timeout = 120 * 1000;
                    byte[] byteArray = null;
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        byteArray = Encoding.UTF8.GetBytes(body);
                        req.ContentLength = byteArray.Length;
                        
                    }
                    req.ContentType = contentType;
                    http.Headers.Add("X-Requested-With", "XMLHttpRequest");

                    Utilities.LogDebug($"{operationId}Sending request: {method} {url}...");

                    HttpWebResponse response = null;
                    try
                    {
                        if (null != byteArray)
                        {
                            using (var dataStream = http.GetRequestStream())
                            {
                                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                            }
                        }

                        
                        response = (http.GetResponse() as HttpWebResponse);

                        if (saveCookies)
                        {
                            foreach (Cookie cookie in response.Cookies)
                            {
                                cookies.Add(cookie);
                            }
                        }

                    }
                    catch (WebException ex)
                    {
                        response = (ex.Response as HttpWebResponse);
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError($"{operationId}Error in Submit: {ex.Message}");
                        await Task.Delay(rng.Next(1000, 3000));
                    }

                    if(null != response)
                    {
                        result.StatusCode = (int)response.StatusCode;
                        Utilities.LogDebug($"{operationId}Status: {method} {url} {response.StatusCode}");

                        using (var rs = GetResponseStream(response))
                        using (var sr = new StreamReader(rs))
                        {
                            var respBody = await sr.ReadToEndAsync();
                            if (!Directory.Exists(Path.GetDirectoryName(cachedPath)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(cachedPath));
                            }
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                if (cacheResult)
                                {
                                    try
                                    {
                                        File.WriteAllText(cachedPath, respBody);
                                    }
                                    catch (Exception ex)
                                    {
                                        result.Error = ex.Message;
                                    }
                                }
                            }
                            result.Result = respBody;
                            return result;
                        }
                    }
                }
            }
            finally
            {
                lock (reqLockObj)
                {
                    --pendingRequestCount;
                    if (pendingRequestCount < 0)
                    {
                        pendingRequestCount = 0;
                    }
                }
            }

            return result;
        }
        
        private static IEnumerable<IElement> GetElementsWithClass(IElement element, string query)
        {
            return element.QuerySelectorAll(query);
        }

        //private static IEnumerable<HtmlNode> GetElementsWithClass(HtmlNode node, String className)
        //{

        //    Regex regex = new Regex("\\b" + Regex.Escape(className) + "\\b", RegexOptions.Compiled);
        //    return node.Descendants()
        //        .Where(n => n.NodeType == HtmlNodeType.Element)
        //        .Where(e => e.Name == "td" && regex.IsMatch(e.GetAttributeValue("class", string.Empty)));
        //}



//        private async Task GetPartsForInterchangePartNumbers(int startPage, int maxPages, string progressFile)
//        {
//            bool cacheLookup = (Config.PerformCacheLookup);

//            object searchTaskLockObject = new object();
//            string prefix = $"(GetPartsForInterchangePartNumbers Call ({Guid.NewGuid()}) - ";
//            string initialResponse = string.Empty;
//            try
//            {
//                int endOfPaginatedResults = int.MaxValue;
//                {
//                    //figure out how many result pages there actually are
//                    //we have to hit this page or else search results will come back invalid
//                    string searchUrl = $"https://dotnetprolink.com/resultsmi.aspx?Ntt=&Ntk=Interchange%20Number&Nty=1&N=0&SearchType=3";
//                    ClearCache(searchUrl);
//                    var response = await Submit(searchUrl, "GET", null, null, cacheLookup, Config.CacheResponses);

//                    //Always clear the cache for this first search to ensure that we initialize the search engine each time we run.


//                    var match = maxResultsRegex.Match(response.Result);
//                    if (match.Success)
//                    {
//                        var maxStr = digitsOnlyRegex.Replace(match.Groups[1].Value, string.Empty);
//                        int maxItemsParsed = -1;
//                        if (int.TryParse(maxStr, out maxItemsParsed))
//                        {
//                            int maxPagesParsed = (maxItemsParsed % Constants.partsPerPage != 0) ? (maxItemsParsed / Constants.partsPerPage) + 1 : maxItemsParsed / Constants.partsPerPage;
//                            if (maxPagesParsed < maxPages)
//                            {
//                                maxPages = maxPagesParsed;
//                                endOfPaginatedResults = startPage + maxPages;
//                            }
//                        }
//                        initialResponse = response.Result;
//                    }
//                    else
//                    {
//                        Utilities.LogError($"{prefix}Unable to retrieve number of parts in Category: <{subCat.ParentCategory.Id}> SubCategory <{subCat.Value}>");
//                        return;
//                    }
//                }

//                object progressLockObj = new object();
//                int progressCount = 0;
//                var searchResultPages = new List<string>();


//                Utilities.LogInfo($"{prefix} START - SubCat: {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
//                var searchTasks = new List<Task>();

//                var pageBatches = GetPageBatches(maxPages);

//                //Reverse order processing of pages
//                if (Config.ProcessInReverseOrder)
//                    pageBatches = pageBatches.OrderByDescending(p => p.StartPage).ToList();

//                foreach (var pageBatch in pageBatches)
//                {

//                    searchResultPages.Clear();
//                    searchTasks.Clear();
//                    if (pageBatch.StartPage == 0)
//                    {
//                        pageBatch.StartPage++;
//                        searchResultPages.Add(initialResponse);
//                    }
//                    //Collect Search Data
//                    for (int pageToWorkOn = pageBatch.StartPage; pageToWorkOn < pageBatch.EndPage; pageToWorkOn++)
//                    {
//                        if (Stop) { break; }
//                        if (pageToWorkOn >= endOfPaginatedResults) { break; }

//                        //Create a new parallel action
//                        Action<object> action = (currentPageObj) =>
//                        {
//                            int currentPage = (int)currentPageObj;
//                            int currentPageForOutput = currentPage + 1;
//                            var searchString = $"N={subCat.Value}&Ntt=&Nao={Constants.partsPerPage * currentPage}";
//                            Utilities.LogInfo($"{prefix}START - Search data for parts in Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages})");

//                            string searchUrl = $"https://www.dotnetprolink.com/resultsmi.aspx?{searchString}";

//                            int maxTries = 10;
//                            int nullResponseRetry = 0;
//                            while (--maxTries >= 0)
//                            {
//                                if (Stop) { break; }
//                                var response = Submit(searchUrl, "GET", null, null, cacheLookup, Config.CacheResponses).Result;
//                                if (response.Result.Contains("No search results found"))
//                                {
//                                    if (currentPage < endOfPaginatedResults)
//                                    {
//                                        //this can happen if you skip the step above (hit the initial search page)
//                                        Utilities.LogError($"{prefix}Search came back with no results. Category: <{subCat.ParentCategory.Name}> SubCategory <{subCat.Text}> Page <{currentPageForOutput}> EndOfPaginatedResults <{endOfPaginatedResults}>");
//                                    }
//                                    ClearCache(searchUrl);
//                                    Task.Delay(rng.Next(1000, 3000));
//                                    continue;
//                                }

//                                if (!string.IsNullOrWhiteSpace(response.Result))
//                                {
//                                    lock (searchTaskLockObject)
//                                    {
//                                        searchResultPages.Add(response.Result);
//                                    }
//                                    WriteProgress(progressFile, subCat.ParentCategory, categoryProgress, ((double)++progressCount / (double)searchResultPages.Count));
//                                    break;
//                                }
//                                else
//                                {
//                                    nullResponseRetry++;
//                                    string nullResponseRetryFailedStr = (nullResponseRetry == 4) ? " ### NULL RESPONSE RETRY FAILED ### " : string.Empty;
//                                    Utilities.LogError($"{prefix}Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages}) - null response from {searchUrl} Retry Count: {nullResponseRetry}{nullResponseRetryFailedStr}");
//                                    Task.Delay(rng.Next(1000, 3000));

//                                }
//                            }

//                            Utilities.LogInfo($"{prefix}DONE - Search data for parts in Category: <{subCat.ParentCategory.Name}> - SubCategory <{subCat.Text}> Page <{currentPageForOutput}> (startPage={startPage}, maxPages={maxPages})");
//                        };

//                        searchTasks.Add(new Task(action, pageToWorkOn));
//                    }

//                    //Execute Search Tasks in Parallel and wait unti all searches are complete
//                    Utilities.StartAndWaitAllThrottled(searchTasks, Config.MaxNumberOfCurrentSearchQueryTask);

//                    if (Stop) { return; }

//                    progressCount = 0;
//                    var result = new AutoPart[searchResultPages.Count * Constants.partsPerPage];

//                    //Process Search Results
//                    if (!Config.RetrievePartDetails)
//                    {
//                        Utilities.LogInfo($"{prefix}DONE - SubCat ##### {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
//                        continue;
//                    }

//                    int pageCount = searchResultPages.Count;
//#if DEBUG
//                    pageCount = 1;
//#endif

//                    Parallel.For(0, pageCount, options, parallelPage =>
//                    {
//                        if (Stop) { return; }
//                        var parts = HTMLToAutoParts(searchResultPages[parallelPage], subCat, progressFile, categoryProgress).Result.ToArray();
//                        WriteProgress(progressFile, subCat.ParentCategory, categoryProgress, ((double)++progressCount / (double)result.Length));

//                        if (parts.Length > 0)
//                        {
//                            Array.Copy(parts, 0, result, parallelPage * Constants.partsPerPage, parts.Length);
//                        }
//                        parts = null;
//                    });

//                    SavePartData(result, subCat, pageBatch);
//                    result = null;

//                    if (Stop) { return; }

//                    Utilities.LogInfo($"{prefix}DONE - SubCat ##### {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages})");
//                }

//            }
//            catch (Exception ex)
//            {
//                Utilities.LogError($"{prefix} ERROR - SubCat {subCat.Text} - {subCat.Value} (startPage={startPage}, maxPages={maxPages}){Environment.NewLine}{ex.Message}");
//            }
//        }

    }
}
