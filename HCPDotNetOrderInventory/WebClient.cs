using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using dotnetscrape_lib.DataObjects;
using dotnetscrape_lib;

namespace HCPDotNetOrderInventory
{
    public static class WebClient
    {
        private static ParallelOptions options = new ParallelOptions();
        private static Random rng = new Random();

        public static ILogger Logger { get; set; }

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

        public static async Task<SubmitResult> Submit(string url, string method = "GET", string body = null, string contentType = null)
        {
            var result = new SubmitResult();


            int maxTries = 2;
            bool retry = true;
            while (--maxTries >= 0)
            {
                var req = WebRequest.Create(url);

                //Getting IP Addresses of the web host
                var ips = Dns.GetHostEntry(new Uri(url).Host).AddressList;
                var ipsToLog = "Resolved IP Addresses: ";
                foreach(var ip in ips)
                {
                    ipsToLog += $"{ip}, ";
                }
                ipsToLog.TrimEnd(",".ToCharArray());


                req.Method = method;
                req.Timeout = 1000000;
                var http = (req as HttpWebRequest);
                http.KeepAlive = true;
                http.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
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

                Logger.LogDebug($"{Environment.NewLine}Sending request: {method} {url}...{Environment.NewLine}{body}{Environment.NewLine}");
                //Log the IP's collected as well
                Logger.LogDebug($"{ipsToLog}{Environment.NewLine}");

                HttpWebResponse response = null;
                try
                {
                     
                    if (null != byteArray)
                    {
                        Logger.LogDebug($"Calling http.GetRequestStream()");
                        using (var dataStream = http.GetRequestStream())
                        {
                            Logger.LogDebug($"Calling http.GetRequestStream() - DONE");
                            Logger.LogDebug($"Calling dataStream.WriteAsync() with Content Length = <{req.ContentLength}> <{byteArray.Length}>");
                            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                            Logger.LogDebug($"Calling dataStream.WriteAsync() with Content Length = <{req.ContentLength}> <{byteArray.Length}> - DONE");
                        }
                    }

                    Logger.LogDebug($"Calling http.GetResponse()");
                    response = (http.GetResponse() as HttpWebResponse);
                   
                    Logger.LogDebug($"Calling http.GetResponse() - DONE");

                }
                catch (WebException ex)
                {
                    response = (ex.Response as HttpWebResponse);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in Submit: {ex.Message}");
                    await Task.Delay(rng.Next(1000, 3000));
                }

                string respBody = string.Empty;
                if (null != response)
                {
                    Logger.LogDebug($"Calling GetResponseStream()");
                    using (var rs = GetResponseStream(response))
                    using (var sr = new StreamReader(rs))
                    {
                        Logger.LogDebug($"Calling GetResponseStream() - DONE");
                        Logger.LogDebug($"Calling sr.ReadToEndAsync()");
                        respBody = await sr.ReadToEndAsync();
                        Logger.LogDebug($"Calling sr.ReadToEndAsync() - DONE");
                        result.Result = respBody;
                        retry = false;
                    }
                    Logger.LogDebug($"Status: {method} {url} {response.StatusCode}");
                }
                if (retry == false)
                    break;
            }

            return result;
        }
    }
}
