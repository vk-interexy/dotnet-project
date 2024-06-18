using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Linq;
using dotnetscrape_constants;
using dotnetscrape_lib.DataObjects;
using dotnetscrape_lib.DataObjects.DotNetB2B;
using HCPDotNetBLL;

namespace HCPDotNetGetPricingAndQuantity
{
   
    class Program
    {
        private readonly static ParallelOptions options = new ParallelOptions();
        private readonly static Random rng = new Random();

        #region Utility Methods
        private static PartsBL CreateBL()
        {
            return new PartsBL { ConnectionString = Config.ConnectionString };
        }

        private static PriceAvailabilityRequest GenerateRequest(List<string> partNumbers)
        {
            var request = new PriceAvailabilityRequest
            {
                RequestID = Constants.MiscellaneousChargesString,
                StoreID = Config.StoreID,
                AccountPassword = Config.AccountPassword,
            };
            foreach(var partNumber in partNumbers)
            {
                string[] partNumberTokens = partNumber.Split(" ".ToCharArray());
                if (partNumberTokens.Length != 2 ||
                    string.IsNullOrWhiteSpace(partNumberTokens[0]) ||
                    string.IsNullOrWhiteSpace(partNumberTokens[1])
                )
                {
                    continue;
                }

                request.Part.Add(
                    new PriceAvailabilityRequestPart
                    {
                        LineAbbrev = partNumberTokens[0],
                        PartNumber = partNumberTokens[1],
                        PartMessage = Constants.DCQTYPartMessage

                    });
            }
            return request;

        }

        private static PriceAvailabilityResponse GetPricingAndAvaiablilty(PriceAvailabilityRequest request)
        {
            PriceAvailabilityResponse priceAvailabilityResponse = null;
            var method = "POST";
            var contentType = "text/xml; charset=utf-8";
            var requestBody = dotnetscrape_lib.Utilities.SerializeObject(request, false, true);
            var response = Submit(Config.DotNetB2BApiUrl,
                method,
                requestBody,
                contentType)?.Result;
            if (!response.HasError)
            {
                priceAvailabilityResponse = dotnetscrape_lib.Utilities.Deserialize<PriceAvailabilityResponse>(response.Result, false);
                if (LogLevel.DEBUG >= Config.LogLevel)
                {
                    Utilities.LogDebug($"{Environment.NewLine}{dotnetscrape_lib.Utilities.SerializeObject(priceAvailabilityResponse, false, true)}{Environment.NewLine}");
                }
            }
            else
            {
                if (LogLevel.DEBUG >= Config.LogLevel)
                {
                    Utilities.LogDebug($"{Environment.NewLine}Response Error: {response.Error}{Environment.NewLine}Result:{Environment.NewLine}{response.Result}{Environment.NewLine}");
                }
            }
            return priceAvailabilityResponse;
        }

        private static Stream GetResponseStream(HttpWebResponse response)
        {
            Stream dataStream;
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

        private static async Task<SubmitResult> Submit(string url, string method = "GET", string body = null, string contentType = null)
        {
            var result = new SubmitResult();
            
           
            int maxTries = 2;
            bool retry = true;
            while (--maxTries >= 0)
            {
                var req = WebRequest.Create(url);
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

                Utilities.LogDebug($"{Environment.NewLine}Sending request: {method} {url}...{Environment.NewLine}{body}{Environment.NewLine}");

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
                catch (Exception ex)
                {
                    Utilities.LogError($"Error in Submit: {ex.Message}");
                    await Task.Delay(rng.Next(1000, 3000));
                }

                string respBody;
                if (null != response)
                {
                    using (var rs = GetResponseStream(response))
                    using (var sr = new StreamReader(rs))
                    {
                        respBody = await sr.ReadToEndAsync();
                        result.Result = respBody;
                        retry = false;
                    }
                    Utilities.LogDebug($"Status: {method} {url} {response.StatusCode}");
                }
                if (retry == false)
                    break;
            }
            
            return result;
        }

        private static AutoPartPricing CreatePricing(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            var pricing = new AutoPartPricing();
            //Get Primary List Pricing
            var listPricing = (priceAvail.Price.Any(a => a.PriceType == "PRI")) ?
                priceAvail.Price.First(a => a.PriceType == "PRI") 
                : null;

            //Get Core Pricing if it exists
            var corePricing =
            (priceAvail.Price.Any(a => a.PriceType == "COR")) ? 
                priceAvail.Price.First(a => a.PriceType == "COR")
                : null;

            pricing.HasCore = false;
            pricing.PricingDetailProvided = false;

            //If list Pricing set, then set from 
            if (listPricing != null)
            {
                pricing.List = (listPricing.ListPrice != null) ? listPricing.ListPrice.Value : 0;
                pricing.Cost = (listPricing.YourCost != null) ? listPricing.YourCost.Value : 0;
                pricing.PricingDetailProvided = true;
            }

            if (corePricing != null)
            {
                pricing.Core = (corePricing.YourCost != null) ? corePricing.YourCost.Value : 0;
                pricing.HasCore = true;
                pricing.PricingDetailProvided = true;
            }
            return pricing;
        }

        private static List<DistributionCenterQuantity> CreateDCQuantities(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            var dcQtys = new List<DistributionCenterQuantity>();
            if(priceAvail.DCBalanceQty.HasValue)
            {
                dcQtys.Add(
                    new DistributionCenterQuantity
                    {
                        Name = Config.PrimaryDistributionCetner,
                        Quantity = priceAvail.DCBalanceQty.Value,
                        DeliveryTime = priceAvail.DeliveryTime
                    });
            }
            return dcQtys;
        }

        private static int UpdateAllQuantityPricingUpdatedDate(string partNumber)
        {
            int ret = 0;
            using (var bl = CreateBL())
            {
                var part = bl.GetPart(partNumber);
                if (part == null) return ret;

                if (part.QuantityPricingUpdatedDate == null)
                {
                    part.QuantityPricingUpdatedDate = new DateTimeOffset(1980, 1, 1, 0, 0, 0, new TimeSpan(-5, 0, 0));
                }

                Utilities.LogDebug($"START - Updating QuantityPricingUpdatedDate for part number: {part.PartNumber}");
                ret = bl.UpdatePartQuantityPricingUpdate(part);
                if (ret > 0)
                {
                    Utilities.TotalPartCountWithBadUnit++;
                }
                Utilities.LogDebug($"DONE - Updating QuantityPricingUpdatedDate for part number: {part.PartNumber}");
                
                return ret;
            }
        }

        private static int UpdatePartInDatabase(string partNumber)
        {
            int ret = 0;
            using (var bl = CreateBL())
            {
                bool updatePart = false;
                var part = bl.GetPart(partNumber);
                if (part == null) return ret;

                if (part.QuantityPricingUpdatedDate == null)
                {
                    part.QuantityPricingUpdatedDate = new DateTimeOffset(1980, 1, 1, 0, 0, 0, new TimeSpan(-5, 0, 0));
                    updatePart = true;
                }

                //Check if the Unit is numeric because some issues were showing in part database.
                if (part.Pricing != null && double.TryParse(part.Pricing.Unit, out double dTemp))
                {
                    part.Pricing.Core = part.Pricing.Cost;
                    part.Pricing.Cost = (decimal)dTemp;
                    part.Pricing.Unit = Constants.EachString;
                    updatePart = true;
                }

                if (updatePart)
                {
                    Utilities.LogInfo($"START - Updating Pricing due to bad unit or null QuantityPricingUpdatedDate for part number: {part.PartNumber}");
                    ret = bl.UpdatePartPricingAndQuantity(part, Config.PrimaryDistributionCetner);
                    if (ret > 0)
                    {
                        Utilities.TotalPartCountWithBadUnit++;
                    }
                    Utilities.LogInfo($"DONE - Updating Pricing due to bad unit or null QuantityPricingUpdatedDate for part number: {part.PartNumber}");
                }
                return ret;
            }
        }

        private static int UpdatePartInDatabase(PriceAvailabilityResponsePartPriceAvailability priceAvail)
        {
            int ret = 0;
            using (var bl = CreateBL())
            {
                var part = bl.GetPart($"{priceAvail.LineAbbrev.Trim()} {priceAvail.PartNumber.Trim()}");
                if (part == null) return 0;

                Utilities.LogDebug($"START - Updating Pricing and Availability for part number: {part.PartNumber}");

                part.Pricing = CreatePricing(priceAvail);
                part.Quantity = priceAvail.QtyOnHand ?? 0;
                part.FindItQuantities = new AutoPartQuantity
                {
                    DCQty = CreateDCQuantities(priceAvail)
                };

                ret = bl.UpdatePartPricingAndQuantity(part,Config.PrimaryDistributionCetner);
                if(ret > 0)
                {
                    Utilities.TotalPartCountProcessed++;
                }
                Utilities.LogDebug($"DONE - Updating Pricing and Availability for part number: {part.PartNumber}");
            }
            return ret;
        }
        #endregion


        private static void Main(string[] args)
        {
            try
            {
                Utilities.LogInfo($"Starting the collection of pricing and availability information.");
                options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);
                using (var bl = CreateBL())
                {

                    var partNumbers = bl.GetAllPartNumbers(Config.RetrievePricingForOnlyUnsetQuantityPricingUpdatedDate, Config.SortAscOrDesc);

                    //if (Config.QuantityPricingUpdatedDateFieldOnly)
                    //{
                    //    Utilities.LogInfo($"------------ START - Only Setting QuantityPricingUpdatedDate Field");
                    //}

                    //Parallel.ForEach(partNumbers, options, (partNumber, loopState) =>
                    //{
                    //    if (Config.QuantityPricingUpdatedDateFieldOnly)
                    //    {
                    //        UpdateAllQuantityPricingUpdatedDate(partNumber);
                    //    }
                    //    else
                    //    {
                    //        UpdatePartInDatabase(partNumber);
                    //    }
                    //});

                    ////Short Circuting Process
                    //if (Config.QuantityPricingUpdatedDateFieldOnly)
                    //{
                    //    Utilities.LogInfo($"------------ END - Only Setting QuantityPricingUpdatedDate Field.  Exiting.");
                    //    return;
                    //}

                    //Set Total Number of Parts in Database
                    Utilities.TotalPartsInDatabase = partNumbers.Count;

                    var chunks = dotnetscrape_lib.Utilities.ChunkBy<string>(partNumbers,Config.ChunkSize);

                    //Release memory of List 
                    partNumbers = null;
                    

                    for(int i=0; i < chunks.Count; i++)
                    {
                        var chunk = chunks[i];
                        try
                        {
                            PriceAvailabilityResponse response = null;
                            bool success = false;
                            
                            for(short retry=1; retry <= 3; retry++)
                            {
                                Utilities.LogInfo($"********* START ATTEMPT #{retry} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                                response = GetPricingAndAvaiablilty(GenerateRequest(chunk));
                                if(response != null)
                                {
                                    //If the response code is 2 then that means the "Store is not answer" and we should retry
                                    if (response.StatusCode == 2)
                                    {
                                        Utilities.LogInfo($"********* RETRYING ATTEMPT #{retry} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                                        continue;
                                    }
                                    else
                                    {
                                        success = response.StatusCode == 0;
                                        break;
                                    }
                                }
                            }

                            string status = (success) ? "DONE" : $"FAILED WITH STATUS CODE ({response.StatusCode})";

                            Utilities.LogInfo($"********* {status} - Getting Pricing and Availability for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");

                            if (success)
                            {
                                Utilities.LogInfo($"########## START - Updating database for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                                if (response != null)
                                {
                                    Parallel.ForEach(response.PartPriceAvailability, options, (priceAvailability, loopState) =>
                                    {
                                        UpdatePartInDatabase(priceAvailability);
                                    });
                                }
                                Utilities.LogInfo($"########## DONE - Updating database for chunk-of-{Config.ChunkSize} [Chunck # {i + 1} of {chunks.Count}]");
                            }
                        }
                        catch(Exception ex)
                        {
                            Utilities.LogError(ex.Message);
                        }
                        
                    }
                }
                Utilities.LogInfo($"Finished the collection of pricing and availability information.");
                Utilities.LogInfo($"Total Parts Processed: {Utilities.TotalPartCountProcessed}");
                Utilities.LogInfo($"Total Parts with bad Units Updated: {Utilities.TotalPartCountWithBadUnit}");
            }
            catch(Exception ex)
            {
                Utilities.LogError(ex.Message);
            }
            
        }
    }
}
