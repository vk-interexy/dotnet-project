using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Text;
using System.IO.Compression;
using dotnetscrape_lib.DataObjects;

namespace GenerateFullOutput
{
    public enum FileGenerateType
    {
        CSVOnly,
        CSVWithDCQty,
        CSVWithFinditQty
    }

    class Program
    {
        private static ConcurrentDictionary<string, AutoPart> partSubCat = new ConcurrentDictionary<string, AutoPart>();
        private static ConcurrentDictionary<string, AutoPart> fullPartList = new ConcurrentDictionary<string, AutoPart>();
        private static ParallelOptions options = new ParallelOptions();
        private static CookieContainer cookies = new CookieContainer();
        private static int filesDeleted = 0;
        private static Random rng = new Random();
        private static object reqLockObj = new object();
        private static int pendingRequestCount = 0;
        private static int updatedPartsRetrievalAttempted = 0;
        private static int updatedPartsRetrieveWithPricing = 0;

        public static string runDateTimePath = Path.Combine(Config.DataExportPath, DateTime.Now.ToString("yyyy-MM-dd_HH+mm+ss"));

        private static async Task<bool> Login(string username, string password)
        {
            //var body = $@"__VIEWSTATE=%2FwEPDwUKLTY4MTAwMjM5Ng9kFgICAQ9kFgwCAQ8WAh4Fc3R5bGUFPWJhY2tncm91bmQtaW1hZ2U6IHVybCgnL2ltYWdlcy9oZWFkZXIvcHJvbGlua2hlYWRlcl90b3AuZ2lmJylkAgMPZBYCAgUPFgIeB1Zpc2libGVnZAIFD2QWBAIHDw9kFgIeCWF1dG9mb2N1c2VkAhEPD2QWBB4Lb25tb3VzZW92ZXIFKHRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXRfb24uZ2lmJzseCm9ubW91c2VvdXQFJXRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXQuZ2lmJztkAgkPZBYCAgQPDxYCHgRUZXh0BQ4xLTgwMC03NDItMzU3OGRkAgsPFgIfAWhkAg0PZBYEZg9kFgJmDw8WAh8FBQtFbiBFc3Bhw7FvbGRkAgUPDxYCHwUFDDMuMDIuMzQ6MTgwQWRkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYCBSNsb2dpblVzZXJDb250cm9sJHJlbWVtYmVyTWVDaGVja0JveAUdbG9naW5Vc2VyQ29udHJvbCRzdWJtaXRCdXR0b24eDdF4GVXx9otVOeqo7QLL%2Bns%2BQw%3D%3D&__EVENTVALIDATION=%2FwEdAAYYQ24zbAXuJdV35wY2d4zCrsIcvOui5KEGsFz9ThSIDgx70%2BJYmSNZ7ALj6frcBXoFoaLNHoHAdL%2BWyGrWyaoJPTAgUG8QtPRfO36LfVCXjXFXTQij%2FKIw0d1MukJvMRvbCsi4wKXKRzgX4kKdE2l%2BTl4sSA%3D%3D&loginUserControl%24userLoginTextBox={username}&loginUserControl%24passwordTextBox={password}&loginUserControl%24submitButton.x=37&loginUserControl%24submitButton.y=6";
            var body = $@"__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUKLTY4MTAwMjM5Ng9kFgICAQ9kFgwCAQ8WAh4Fc3R5bGUFPWJhY2tncm91bmQtaW1hZ2U6IHVybCgnL2ltYWdlcy9oZWFkZXIvcHJvbGlua2hlYWRlcl90b3AuZ2lmJylkAgMPZBYCAgUPFgIeB1Zpc2libGVnZAIFD2QWBAIHDw9kFgIeCWF1dG9mb2N1c2VkAhEPD2QWBB4Lb25tb3VzZW92ZXIFKHRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXRfb24uZ2lmJzseCm9ubW91c2VvdXQFJXRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXQuZ2lmJztkAgkPZBYCAgQPDxYCHgRUZXh0BQ4xLTgwMC03NDItMzU3OGRkAgsPFgIfAWhkAg0PZBYEZg9kFgJmDw8WAh8FBQtFbiBFc3Bhw7FvbGRkAgUPDxYCHwUFCzMuMDIuNDQ6OTlBZGQYAQUeX19Db250cm9sc1JlcXVpcmVQb3N0QmFja0tleV9fFgIFI2xvZ2luVXNlckNvbnRyb2wkcmVtZW1iZXJNZUNoZWNrQm94BR1sb2dpblVzZXJDb250cm9sJHN1Ym1pdEJ1dHRvbmrHxqUJesAY3Pb9fyeAKn9PEkK7&__VIEWSTATEGENERATOR=C2EE9ABB&__SCROLLPOSITIONX=0&__SCROLLPOSITIONY=0&__EVENTVALIDATION=%2FwEdAAYy7gsQardj5rX8GlfvIpQgrsIcvOui5KEGsFz9ThSIDgx70%2BJYmSNZ7ALj6frcBXoFoaLNHoHAdL%2BWyGrWyaoJPTAgUG8QtPRfO36LfVCXjXFXTQij%2FKIw0d1MukJvMRvpOYtxluSxPDfZ8aBxegxeYrrUig%3D%3D&loginUserControl%24userLoginTextBox={System.Net.WebUtility.UrlEncode(username)}&loginUserControl%24passwordTextBox={System.Net.WebUtility.UrlEncode(password)}&loginUserControl%24rememberMeCheckBox=on&loginUserControl%24submitButton.x=64&loginUserControl%24submitButton.y=17";
            var url = "https://www.dotnetprolink.com/login.aspx";
            var method = "POST";
            var contentType = "application/x-www-form-urlencoded";

            var response = await Submit(url, method, body, contentType, false);
            return response.Result.Contains("ctl00$logoutLinkButton") && response.Result.Contains("ctl00_poMyAccount");
        }

        private static string RequestHashString(string url, string method, string body, string contentType)
        {
            return ($":U_{url}_U:M_{method}_M:B_{body ?? string.Empty}_B:CT_{contentType ?? string.Empty}_CT");
        }

        private static string GetCachedFileLocation(string url, string method, string body, string contentType)
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

        private static Stream GetResponseStream(HttpWebResponse response)
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

        private static async Task<SubmitResult> Submit(string url, string method = "GET", string body = null, string contentType = null, bool checkCache = true)
        {
            var result = new SubmitResult();
            var cachedPath = GetCachedFileLocation(url, method, body, contentType);

            if (checkCache && File.Exists(cachedPath))
            {
                try
                {
                    var contents = File.ReadAllText(cachedPath);
                    if (!string.IsNullOrWhiteSpace(contents))
                    {
                        result.CacheHit = true;
                        result.Result = contents;
                        return result;
                    }
                }
                catch (Exception e)
                {
                    result.Error = e.Message;
                    return result;
                }

            }

            while (pendingRequestCount >= Config.MaxPendingRequests)
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

                    var req = WebRequest.Create(url);
                    req.Method = method;
                    req.Proxy = null;
                    req.Timeout = 1000000;
                    var http = (req as HttpWebRequest);
                    http.KeepAlive = true;
                    http.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    http.CookieContainer = cookies;
                    http.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
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
                        req.ContentType = contentType;
                        http.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    }

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

                    }
                    catch (WebException ex)
                    {
                        response = (ex.Response as HttpWebResponse);
                    }
                    catch
                    {
                        await Task.Delay(rng.Next(1000, 3000));
                    }

                    if (null != response)
                    {
                        

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
                                try
                                {
                                    File.WriteAllText(cachedPath, respBody);
                                }
                                catch (Exception ex)
                                {
                                    result.Error = ex.Message;
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

        static void Main(string[] args)
        {

            //var commandLine = CommandLineParser.Parse(args);
            //if (null != commandLine)
            //{
            //    if (commandLine.ShowHelp)
            //    {
            //        CommandLineParser.ShowHelp();
            //        return;
            //    }
            //    else
            //    {
            //        Config.CommandLine = commandLine;
            //    }
            //}

            
            Config.CommandLine = null;

            if (args.Length != 2)
            {
                Console.WriteLine("usage: dotnet GenerateFullOutput.dll <partCatSubCatFilePath> <fullDetailPath>");
                return;
            }

            string partCatSubCatFilePath = args[0];
            string fullDetailPath = args[1];
            //string outputFolder = args[2];
            
            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);

            if (!Directory.Exists(partCatSubCatFilePath))
            {
                Console.WriteLine($"<partCatSubCatFilePath> {partCatSubCatFilePath} is not a valid path.");
                return;
            }

            if (!Directory.Exists(fullDetailPath))
            {
                Console.WriteLine($"<fullDetailPath> {fullDetailPath} is not a valid path.");
                return;
            }

            //if (!Directory.Exists(outputFolder))
            //{
            //    try
            //    {
            //        Directory.CreateDirectory(outputFolder);
            //    }
            //    catch(Exception)
            //    {
            //        Console.WriteLine($"Unable to create <outputFolder> {outputFolder}");
            //    }
                
            //    return;
            //}

            if (!Login(Config.Username, Config.Password).Result)
            {
                throw new Exception("Failed to log in.");
            }

            LoadPartsWithCategorySubCategoryMapping(partCatSubCatFilePath);

            GeneateFullList(fullDetailPath);

            WriteDataFiles(fullPartList.Values.Where(r => null != r).ToArray());
        }

        static double TimeElapsed(long milliseconds, int itemCount)
        {
            if (itemCount == 0)
                return 0;

            return (double)milliseconds / (double)1000 / (double)itemCount;

        }

        static void LoadPartsWithCategorySubCategoryMapping(string path)
        {
            var lockObject = new object();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Start loading part numbers with their corresponding category/sub-category mapppings.");
            Parallel.ForEach(Directory.GetFiles(path), options, (file, loopState) =>
            {
                Console.WriteLine($"Serializing {file}");
                var parts = dotnetscrape_lib.Utilities.DeserializeFile<List<AutoPart>>(file, true);

                Console.WriteLine($"Mapping {parts.Count} parts from {file}");

                Parallel.ForEach(parts, options, (part, loopState2) =>
                  { 
                      if (!Config.UseJSONData)
                      {
                          part.ResetAllButPartNumAndCatData();
                      }

                      string urlHash = part.DetailUrlHash;

                      if (!partSubCat.ContainsKey(urlHash))
                      {
                          partSubCat.TryAdd(urlHash, part);
                          if(Config.UseJSONData)
                          {
                              if (!fullPartList.ContainsKey(urlHash))
                              {
                                  fullPartList.TryAdd(urlHash, part);
                              }
                          }
                      }

                      lock (lockObject)
                      {
                          if (partSubCat.Count % 50000 == 0)
                          {
                              stopwatch.Stop();
                              Console.WriteLine($"{partSubCat.Count} part mappings processed so far.  Time per part mapping: {TimeElapsed(stopwatch.ElapsedMilliseconds, partSubCat.Count)}");
                              stopwatch.Start();
                          }
                      }
                  }); 
            });

            stopwatch.Stop();
            Console.WriteLine($"Finished loading {partSubCat.Count} part mappings.  Time per part mapping: {TimeElapsed(stopwatch.ElapsedMilliseconds, partSubCat.Count)}");
        }
        
        static void WriteDataToFile(string path, List<string> csvStrings)
        {
            Console.WriteLine($"Start writing {csvStrings.Count - 1} parts to CSV file located at: {path}.");
            using (StreamWriter writer = new StreamWriter(path, true,Encoding.UTF8))
            {
                int i = 0;
                for (i = 0; i < csvStrings.Count - 1; i++)
                {
                    writer.WriteLine(csvStrings[i]);
                }
                writer.Write(csvStrings[i]);
            }
            Console.WriteLine($"Finished writing {csvStrings.Count - 1} parts to CSV file located at: {path}.");
        }

        static void WriteDataFiles(AutoPart[] parts)
        {
            
            string jsonFolder = Path.Combine(runDateTimePath, "json");

            string csvFolder = Path.Combine(runDateTimePath, "csv");

            if (!Directory.Exists(jsonFolder)) Directory.CreateDirectory(jsonFolder);

            if (!Directory.Exists(csvFolder)) Directory.CreateDirectory(csvFolder);

            string fullSearchFileNamePrefix = $"FullSearch_COUNT_{parts.Length}";

            string jsonFilePath = Path.Combine(jsonFolder, $"{fullSearchFileNamePrefix}.json");

            string csvFilePath = Path.Combine(csvFolder, $"{fullSearchFileNamePrefix}.csv");

            //Generate JSON File
            dotnetscrape_lib.Utilities.SerializeObject(parts, jsonFilePath);

            //Generate CSV File
            GenerateCSV(csvFilePath, FileGenerateType.CSVWithDCQty);
        }
           
        static void DeleteDetail(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    filesDeleted++;
                }
                catch
                {
                    Console.WriteLine($"Unable to delete {file}.");
                }
            }
        }

        static AutoPart RetrieveUpdatedDetail(string partDetailUrl)
        {
            Console.WriteLine($"Retrieving Update of {partDetailUrl}");
            var result = Submit(partDetailUrl, "GET", null, null, false)?.Result?.Result;
            updatedPartsRetrievalAttempted++;
            var part = (string.IsNullOrWhiteSpace(result) ? null : DetailHtmlToAutoPart.HTMLToAutoPartDetail(result));
            if (part != null && part.Pricing.PricingDetailProvided) updatedPartsRetrieveWithPricing++;
            Console.WriteLine($"Total Updates Retrivals Attempted: {updatedPartsRetrievalAttempted}.  Total Updates Retrieved with Pricing: {updatedPartsRetrieveWithPricing}.  Difference {updatedPartsRetrievalAttempted - updatedPartsRetrieveWithPricing}");
            return part;
        }

        static void GeneateFullList(string fullDetailPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Parallel.ForEach(Directory.GetFiles(fullDetailPath), options, (file, loopState) =>
            {
                var fullDetailPart = DetailHtmlToAutoPart.HTMLToAutoPartDetail(File.ReadAllText(file));
                bool updatedPartRetrieved = false;
                if (fullDetailPart == null)
                {
                    DeleteDetail(file);
                    return;
                }

                if (Config.RetrieveDetailsWithNoPricing && !fullDetailPart.Pricing.PricingDetailProvided)
                {
                    fullDetailPart = RetrieveUpdatedDetail(fullDetailPart.DetailUrl);
                    if (fullDetailPart == null)
                        return;

                    updatedPartRetrieved = true;
                }

                string urlHash = fullDetailPart.DetailUrlHash;

                if (partSubCat.ContainsKey(urlHash))
                {
                    if (Config.UseJSONData && !updatedPartRetrieved)
                    {
                        fullDetailPart = partSubCat[urlHash];
                    }
                    else
                    {
                        var part = partSubCat[urlHash];
                        fullDetailPart.Category = part.Category;
                        fullDetailPart.CategoryId = part.CategoryId;
                        fullDetailPart.SubCategory = part.SubCategory;
                        fullDetailPart.SubCategoryId = part.SubCategoryId;
                    }
                }

                if (!fullPartList.ContainsKey(urlHash))
                {
                    fullPartList.TryAdd(urlHash, fullDetailPart);
                }

            });
        }

        static void GenerateCSV(string csvFilePath, FileGenerateType fileGenerateType)
        {
            var lockList = new object();

            Console.WriteLine($"Start transforming AutoParts to CSV.");

            var csvStrings = new List<string>();
            csvStrings.Add(AutoPart.CSVHeader);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Parallel.ForEach(fullPartList.Values, options, (fullDetailPart, loopState) =>
            {
                if (fullDetailPart == null) return;

                lock (lockList)
                {
                    switch(fileGenerateType)
                    {
                        case FileGenerateType.CSVOnly:
                            csvStrings.Add(fullDetailPart.CSV());
                            break;
                        case FileGenerateType.CSVWithDCQty:
                            csvStrings.Add(fullDetailPart.CSVWithDCQuantityOnly());
                            break;
                        case FileGenerateType.CSVWithFinditQty:
                            csvStrings.Add(fullDetailPart.CSVWithFinditQuantites());
                            break;

                    }
                    
                }
                if (csvStrings.Count % 500 == 0)
                {
                    stopwatch.Stop();
                    Console.WriteLine($"{csvStrings.Count} AutoParts transformed so far.  Time per file transformation: {TimeElapsed(stopwatch.ElapsedMilliseconds, csvStrings.Count)}");
                    stopwatch.Start();
                }
            });

            stopwatch.Stop();
            Console.WriteLine($"Finished transforming {csvStrings.Count} raw HTML files into CSV data. Time per file transformation: {TimeElapsed(stopwatch.ElapsedMilliseconds, csvStrings.Count)}");

            WriteDataToFile(csvFilePath, csvStrings);
        }
        
        static void LoadPartsFromCSV(string csvPath)
        {
            string[] csvFileData = File.ReadAllLines(csvPath);
            Parallel.ForEach(csvFileData, options, (line, loopState) =>
            {
                var part = ConvertCSVToPart(line);
                if (part != null)
                {
                    string urlHash = part.DetailUrlHash;

                    if (!fullPartList.ContainsKey(urlHash))
                    {
                        fullPartList.TryAdd(urlHash, part);
                    }
                }
            });

        }

        static AutoPart ConvertCSVToPart(string line)
        {
            var part = new AutoPart();

            Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            var fields = CSVParser.Split(line);
            for(int i =0; i< fields.Length;i++)
            {
                fields[i] = fields[i].Trim('\"');
            }

            return part;

        }

    }
}
