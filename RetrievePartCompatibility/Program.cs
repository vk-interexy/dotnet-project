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
using HCPDotNetBLL;
using HtmlAgilityPack;

namespace RetrievePartCompatibility
{
    class Program
    {
        private static ParallelOptions options = new ParallelOptions();
        private static CookieContainer cookies = new CookieContainer();
        private static Random rng = new Random();
        private static object reqLockObj = new object();
        private static int pendingRequestCount = 0;
        private static int updatedPartsRetrievalAttempted = 0;
        private static int updatedPartsRetrieveWithPricing = 0;

        private static PartsBL CreateBL()
        {
            return new PartsBL { ConnectionString = Config.ConnectionString };
        }


        private static async Task<bool> Login(string username, string password)
        {
            var body = $@"__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUKMTE0NjY2NzUwNw9kFgICAQ9kFg4CAQ8WAh4Fc3R5bGUFPWJhY2tncm91bmQtaW1hZ2U6IHVybCgnL2ltYWdlcy9oZWFkZXIvcHJvbGlua2hlYWRlcl90b3AuZ2lmJylkAgMPZBYCAgUPFgIeB1Zpc2libGVnZAIFD2QWAmYPDxYCHgRUZXh0BQtFbiBFc3Bhw7FvbGRkAgcPZBYEAgcPD2QWAh4JYXV0b2ZvY3VzZWQCEQ8PZBYEHgtvbm1vdXNlb3ZlcgUodGhpcy5zcmM9J2ltYWdlcy9idXR0b25zL3N1Ym1pdF9vbi5naWYnOx4Kb25tb3VzZW91dAUldGhpcy5zcmM9J2ltYWdlcy9idXR0b25zL3N1Ym1pdC5naWYnO2QCCw9kFgICBA8PFgIfAgUOMS04MDAtNzQyLTM1NzhkZAINDxYCHwFoZAIPD2QWBGYPZBYCZg8PFgIfAgULRW4gRXNwYcOxb2xkZAIFDw8WAh8CBQwzLjAyLjU2OjE4MEFkZBgBBR5fX0NvbnRyb2xzUmVxdWlyZVBvc3RCYWNrS2V5X18WAgUjbG9naW5Vc2VyQ29udHJvbCRyZW1lbWJlck1lQ2hlY2tCb3gFHWxvZ2luVXNlckNvbnRyb2wkc3VibWl0QnV0dG9uTgU0XMplptas%2B1v7KMnSPTTWl5w%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__SCROLLPOSITIONX=0&__SCROLLPOSITIONY=0&__EVENTVALIDATION=%2FwEdAAdhdHR39gok0%2BNgyDaA9H9Zg%2FeptAxIPFsAU1oAXhpHG67CHLzrouShBrBc%2FU4UiA4Me9PiWJkjWewC4%2Bn63AV6BaGizR6BwHS%2Flshq1smqCT0wIFBvELT0Xzt%2Bi31Ql41xV00Io%2FyiMNHdTLpCbzEbf6SFkcv7SYiLKqDh9ApmMbczBso%3D&loginUserControl%24userLoginTextBox=dotnetautohcp&loginUserControl%24passwordTextBox=hcpauto520&loginUserControl%24rememberMeCheckBox=on&loginUserControl%24submitButton.x=57&loginUserControl%24submitButton.y=12";
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
            //if (args.Length != 2)
            //{
            //    Console.WriteLine("usage: dotnet RetrievePartCompatibility.dll");
            //    return;
            //}
            
            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);
            
            if (!Login(Config.Username, Config.Password).Result)
            {
                throw new Exception("Failed to log in.");
            }

            Process();

        }

        static double TimeElapsed(long milliseconds, int itemCount)
        {
            if (itemCount == 0)
                return 0;

            return (double)milliseconds / (double)1000 / (double)itemCount;

        }

        static List<AutoPartCompatibility> GetAutoPartCompatibility(string partNumber)
        { 
            var ret = new List<AutoPartCompatibility>();
            var partNumberSplit = partNumber.Split(' ');
            if (partNumberSplit.Length != 2) return ret;

            string url = $"https://www.dotnetprolink.com/BGRequestProcessorMI.aspx?LineCode={partNumberSplit[0]}&PartNumber={partNumberSplit[1]}";

            var response = Submit(url);

            if (response != null && !response.Result.HasError)
            {
                ret = HTMLToAutoPartCompatibilityList(partNumber, response.Result.Result);
            }
            return ret;
        }

        static public List<AutoPartCompatibility> HTMLToAutoPartCompatibilityList(string partNumber, string html)
        {
            List<AutoPartCompatibility> list = new List<AutoPartCompatibility>();
            try
            {
                //Protect against exception thrown by HTMLAgility "(Value cannot be null. Parameter name: html)"
                if (string.IsNullOrWhiteSpace(html))
                {
                    return list;
                }

                var detailDoc = new HtmlDocument();
                detailDoc.LoadHtml(html);
                
                foreach(var row in detailDoc.DocumentNode.SelectSingleNode("//table")?.SelectNodes("tr"))
                {
                    var cells = row?.SelectNodes("td");
                    if (cells?.Count != 4 || cells.Where( c => c.FirstChild != null && string.Equals(c.FirstChild.OriginalName,"b",StringComparison.OrdinalIgnoreCase)).Any() )
                        continue;
                    list.Add(
                        new AutoPartCompatibility(partNumber,cells[0].InnerText, cells[1].InnerText, cells[2].InnerText, cells[3].InnerText)
                        );
                } 
            }
            catch
            {

            }

            return list;
        }

        static void Process()
        {
            var lockObject = new object();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("START: Getting all part numbers from database.");
            var partsbl = CreateBL();

            var partNumbers = partsbl.GetAllPartNumbersWithoutCompatibilityRecords();

            Console.WriteLine("END: Getting all part numbers from database.");

            var partCount = 0;
            var partCompatabilityCount = 0;

            int multiple = 0;

            Console.WriteLine($"START: Getting compatabilities for {partNumbers.Count} parts.");
            foreach (var partNumber in partNumbers)
            {
                var partCompatabilities = GetAutoPartCompatibility(partNumber);
                foreach(var partCompatability in partCompatabilities)
                {

                    try
                    {
                        partsbl.InsertPartCompatibility(partCompatability);
                        partCompatabilityCount++;
                    }
                    catch
                    {
                        partCompatabilityCount--;

                    }
                }

                partCount++;

                partsbl.MarkPartNumberCompatibilityComplete(partNumber);


                lock (lockObject)
                {
                    if ((partCount / 1000) > multiple)
                    {
                        Random rnd = new Random();
                        multiple++;
                        stopwatch.Stop();
                        Console.WriteLine($"{partCount} of {partNumbers.Count} part compatabilities processed so far.  Time per part compatability: {TimeElapsed(stopwatch.ElapsedMilliseconds, partCount)}");
                        System.Threading.Thread.Sleep(rnd.Next(0,5000));
                        stopwatch.Start();
                    }
                }
            }

            Console.WriteLine($"DONE: Getting compatabilities for {partNumbers.Count} parts.");


            stopwatch.Stop();
            Console.WriteLine($"SUMMARY: Finished getting {partCompatabilityCount} part compatabilities.  Time per part compatability: {TimeElapsed(stopwatch.ElapsedMilliseconds, partCompatabilityCount)}");
        }
        
        
    }
}
