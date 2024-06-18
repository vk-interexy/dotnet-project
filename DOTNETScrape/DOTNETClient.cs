using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using DOTNETScrape.DataObjects;
using System.IO.Compression;
using Newtonsoft.Json.Linq;


namespace DOTNETScrape
{
    public class DOTNETClient
    {
        private CookieContainer cookies = new CookieContainer();
        private WebProxy proxy = new WebProxy("localhost", 8080);
        private List<Category> Categories = new List<Category>();

        public async Task<bool> Login(string username, string password)
        {
            string postData = $@"__VIEWSTATE=%2FwEPDwUKLTY4MTAwMjM5Ng9kFgICAQ9kFgwCAQ8WAh4Fc3R5bGUFPWJhY2tncm91bmQtaW1hZ2U6IHVybCgnL2ltYWdlcy9oZWFkZXIvcHJvbGlua2hlYWRlcl90b3AuZ2lmJylkAgMPZBYCAgUPFgIeB1Zpc2libGVnZAIFD2QWBAIHDw9kFgIeCWF1dG9mb2N1c2VkAhEPD2QWBB4Lb25tb3VzZW92ZXIFKHRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXRfb24uZ2lmJzseCm9ubW91c2VvdXQFJXRoaXMuc3JjPSdpbWFnZXMvYnV0dG9ucy9zdWJtaXQuZ2lmJztkAgkPZBYCAgQPDxYCHgRUZXh0BQ4xLTgwMC03NDItMzU3OGRkAgsPFgIfAWhkAg0PZBYEZg9kFgJmDw8WAh8FBQtFbiBFc3Bhw7FvbGRkAgUPDxYCHwUFDDMuMDIuMzQ6MTgwQWRkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYCBSNsb2dpblVzZXJDb250cm9sJHJlbWVtYmVyTWVDaGVja0JveAUdbG9naW5Vc2VyQ29udHJvbCRzdWJtaXRCdXR0b24eDdF4GVXx9otVOeqo7QLL%2Bns%2BQw%3D%3D&__EVENTVALIDATION=%2FwEdAAYYQ24zbAXuJdV35wY2d4zCrsIcvOui5KEGsFz9ThSIDgx70%2BJYmSNZ7ALj6frcBXoFoaLNHoHAdL%2BWyGrWyaoJPTAgUG8QtPRfO36LfVCXjXFXTQij%2FKIw0d1MukJvMRvbCsi4wKXKRzgX4kKdE2l%2BTl4sSA%3D%3D&loginUserControl%24userLoginTextBox={username}&loginUserControl%24passwordTextBox={password}&loginUserControl%24submitButton.x=37&loginUserControl%24submitButton.y=6";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            var req = WebRequest.Create("https://www.dotnetprolink.com/login.aspx");
            req.Method = "POST";
            req.Proxy = (Config.UseProxy) ? proxy : null;

            var http = (req as HttpWebRequest);
            http.AllowAutoRedirect = false;
            http.CookieContainer = cookies;
            req.ContentLength = byteArray.Length;
            req.ContentType = "application/x-www-form-urlencoded";

            using (var dataStream = http.GetRequestStream())
            {
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            }

            var resp = (http.GetResponse() as HttpWebResponse);
            using (var rs = resp.GetResponseStream())
            using (var sr = new StreamReader(rs))
            {
                var respBody = await sr.ReadToEndAsync();
            }

            return (resp.StatusCode == HttpStatusCode.Found) && (resp.Headers[HttpResponseHeader.Location] == "/default.aspx");
        }

        private async Task<List<SubCategory>> GetSubCategories(Category category)
        {
            string postData = $@"{{categoryId: ""{category.Id}""}}";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            var req = WebRequest.Create("https://www.dotnetprolink.com/services/cataloglistsservice.asmx/GetSubCategoriesNonVeh");
            req.Method = "POST";
            req.Proxy = (Config.UseProxy) ? proxy : null;

            var http = (req as HttpWebRequest);
            http.AllowAutoRedirect = false;
            http.CookieContainer = cookies;
            http.Accept = "application/json; charset=utf-8";
            http.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            http.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
            http.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
            http.Headers.Add("X-Requested-With", "XMLHttpRequest");

            req.ContentLength = byteArray.Length;
            req.ContentType = "application/json; charset=utf-8";

            using (var dataStream = http.GetRequestStream())
            {
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            }

            var resp = (http.GetResponse() as HttpWebResponse);
            string responseJson = "";
            using (var rs = resp.GetResponseStream())
            using (var sr = new StreamReader(rs))
            {
                responseJson = await sr.ReadToEndAsync();
            }

            SubCategoryResult result = Newtonsoft.Json.JsonConvert.DeserializeObject<SubCategoryResult>(responseJson);

            var subcats = from subcat in result.d
                          where subcat.Value != -1
                          select subcat;
            var list = subcats.ToList<SubCategory>();
            foreach(var subcat in list)
            {
                subcat.ParentCategory = category;
            }
            return list;
        }

        private async Task PopulateSubCategories()
        {
            object lockObj = new object();
            List<string> responses = new List<string>();

            await Task.Run(() =>
            {
                Parallel.For(0, Categories.Count, item =>
                {
                    var response = GetSubCategories(Categories[item]).Result;
                    lock (lockObj)
                    {
                        Categories[item].SubCategories = response;
                    }
                });
            });
        }
        
        public void PopulateCategories()
        {
            string url = $"https://www.dotnetprolink.com/default.aspx";
            var response = SubmitGET(url).Result;
            if(string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            foreach(var element in doc.DocumentNode.SelectNodes("//*[@id='ctl00_mainContentPlaceHolder_categoryMainDropDownList']/option"))
            {
                if (element.HasAttributes &&
                   !string.IsNullOrWhiteSpace(element.Attributes[0].Value))
                {
                    int id = 0;
                    int.TryParse(Utilities.HTMLDecodeWithoutNBSP(element.Attributes[0].Value), out id);
                    Categories.Add(new Category {
                        Name = Utilities.HTMLDecodeWithoutNBSP(element.InnerText),
                        Id = id });
                }
            }
            var tasks = new List<Task>
            {
                PopulateSubCategories()
            };
            Task.WaitAll(tasks.ToArray());
        }
        
        public Task<string> GenerateSearchResult(string queryString)
        {
            string url = $"https://www.dotnetprolink.com/resultsmi.aspx?{queryString}";

            return SubmitGET(url);
        }

        public IEnumerable<AutoPart> GetParts(int startPage = 0, int maxPages = 1)
        {
            List<AutoPart> list = new List<AutoPart>();
#if DEBUG
            int i = 0;
#endif
            foreach (var category in Categories)
            {
#if DEBUG
                if (i++ == 1) break;
#endif
                foreach (var subCat in category.SubCategories)
                {
                    Console.WriteLine($"START - Extracting data for parts in Category: <{category.Name} - {category.Id}> SubCategory <{subCat.Text} - {subCat.Value}>");
                    list.AddRange(GetPartsBySubCategory(subCat, startPage, maxPages).Result);
                    Console.WriteLine($"DONE - Extracting data for parts in Category: <{category.Name}> SubCategory <{subCat.Text}>");
                }
            }
            return list;
        }

        public async Task<IEnumerable<AutoPart>> GetPartsBySubCategory(SubCategory subCat, int startPage=0, int maxPages=1)
        {

            object lockObj = new object();
            List<string> responses = new List<string>();

            await Task.Run(() =>
            {
                Parallel.For(startPage, startPage + maxPages, page =>
                {
                    var searchString = (page == 0) ? $"Ntt=&N={subCat.Value}&SearchType=1" : $"N={subCat.Value}&Ntt=&No={Constants.partsPerPage * page}";
                    Console.WriteLine($"Search data for parts in Category: <{subCat.ParentCategory.Id}> SubCategory <{subCat.Value}>");
                    var response = GenerateSearchResult(searchString).Result;
                    lock (lockObj)
                    {
                        responses.Add(response);
                    }
                });
            });
            var result = responses.SelectMany(response => { return HTMLToAutoParts(response, subCat); }).AsParallel();
            return result;
        }

        public Task<string> GetProductDetail(string productId)
        {
            string url = $"https://www.dotnetprolink.com/Detailmi.aspx?R={productId}";

            return SubmitGET(url);
        }

        public IEnumerable<AutoPart> HTMLToAutoParts(string html, SubCategory subCat, bool fetchDetails=true)
        {
            Console.WriteLine($"START - Extract parts detail in Category: <{subCat.ParentCategory.Id}> SubCategory <{subCat.Value}>");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return (doc.DocumentNode.SelectNodes("//span[@id='ctl00_mainContentPlaceHolder_ProlinkResultList1']/table/tr") == null) ? 
                new List<AutoPart>() : 
            doc.DocumentNode.SelectNodes("//span[@id='ctl00_mainContentPlaceHolder_ProlinkResultList1']/table/tr").Select(tr =>
            {
                var autoPart = new AutoPart();

                var partnum = GetElementsWithClass(tr, "resultpartnum").FirstOrDefault()?.InnerText;

                //Set Part Number
                if (!string.IsNullOrWhiteSpace(partnum))
                {
                    autoPart.PartNumber = Utilities.HTMLDecodeWithoutNBSP(partnum);
                }
                if (subCat != null)
                {
                    autoPart.Category = subCat.ParentCategory.Name;
                    autoPart.CategoryId = subCat.ParentCategory.Id;
                    autoPart.SubCategory = subCat.Text;
                    autoPart.SubCategoryId = subCat.Value;
                }

                var partnameEl = GetElementsWithClass(tr, "resultdesc").FirstOrDefault();
                if (null != partnameEl)
                {
                    //Set ProductLine
                    var imgEl = partnameEl.SelectSingleNode(".//img");
                    if (null != imgEl && null != imgEl.NextSibling)
                    {
                        autoPart.ProductLine = Utilities.HTMLDecodeWithoutNBSP(imgEl.NextSibling.InnerText);
                    }

                    var aEl = partnameEl.SelectSingleNode(".//a");
                    if (null != aEl)
                    {
                        //Set Part Name
                        autoPart.PartName = Utilities.HTMLDecodeWithoutNBSP(aEl.InnerText);
                        var href = aEl.GetAttributeValue("href", null);
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            string detailUrl = $"https://www.dotnetprolink.com/{href.Trim()}";
                            autoPart.DetailUrl = detailUrl;
                            if (fetchDetails)
                            {
                                var detailResponse = SubmitGET(detailUrl).Result;
                                HTMLToAutoPartDetail(detailResponse, ref autoPart);
                            }
                        }
                    }

                    
                }

                return autoPart;
            })
            .Where(part => !string.IsNullOrWhiteSpace(part.PartNumber));
        }

        public void HTMLToAutoPartDetail(string html, ref AutoPart autoPart)
        {
            var detailDoc = new HtmlDocument();
            detailDoc.LoadHtml(html);

            var titleEl = GetElementsWithClass(detailDoc.DocumentNode, "widepageTitle");
            if (null != titleEl && titleEl.Count() > 0)
            {
                autoPart.PartName = titleEl.First<HtmlNode>().InnerText;
            }
            //Image URL
            var imgUrlEl = detailDoc.DocumentNode.SelectSingleNode("//img[@id='ImageControlctl00_mainContentPlaceHolder_MultiImageControl1']");
            if (null != imgUrlEl)
            {
                if (imgUrlEl.ParentNode != null && 
                    imgUrlEl.ParentNode.Attributes != null &&
                    imgUrlEl.ParentNode.Attributes.Count > 0)
                {
                    var imgUrl = imgUrlEl.ParentNode.Attributes[0].DeEntitizeValue.Replace("MultiImageOnClick('", string.Empty).Replace("')", string.Empty);
                    var imgParts = imgUrl.Substring(imgUrl.IndexOf("asset=")+5).Split(new[] { ',' })
                                            .Select(s =>
                                            {
                                                var partNum = Regex.Match(s.Split(new[] { '/' })[1], "[0-9]+").Value;
                                                return $"https://s7d9.scene7.com/is/image/GenuinePartsCompany/{partNum}?.jpg&fit=constrain,1&wid=2000&hei=2000";
                                            }).ToList();
                    autoPart.ImageUrls.AddRange(imgParts);
                }
            }

            //Pricing Information
            var priceEl = detailDoc.DocumentNode.SelectNodes("(//span[@id='ctl00_mainContentPlaceHolder_ProlinkPricingControl1']/table/tr/td)[3]/text()");
            if(null != priceEl)
            {
                bool coreIncluded = priceEl.Count == 4;
                int listIndex = 0;
                int coreIndex = 1;
                int costIndex = (coreIncluded) ? 2 : 1;
                int unitIndex = (coreIncluded) ? 3 : 2;
                decimal price = 0;
                if(priceEl.Count > listIndex && decimal.TryParse(priceEl.ElementAt(listIndex).InnerText, out price ))
                {
                    autoPart.Pricing.List = price;
                }

                if(coreIncluded)
                {
                    if (priceEl.Count > coreIndex && decimal.TryParse(priceEl.ElementAt(coreIndex).InnerText, out price))
                    {
                        autoPart.Pricing.Core = price;
                    }
                }

                if (priceEl.Count > costIndex && decimal.TryParse(priceEl.ElementAt(costIndex).InnerText, out price))
                {
                    autoPart.Pricing.Cost = price;
                }

                if (priceEl.Count > unitIndex)
                { 
                    autoPart.Pricing.Unit = priceEl.ElementAt(unitIndex).InnerText;
                }
                
               
            }

            //Feature and Benefits
            var featuresEl = detailDoc.DocumentNode.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_FeaturesBenefitsDataLinkControl1']");
            if (null != featuresEl)
            {
                autoPart.FeatureAndBenefits = featuresEl.InnerText;
                var featureAndBenefitsUrlEl = featuresEl.SelectSingleNode(".//a");
                if (null != featureAndBenefitsUrlEl)
                {
                    autoPart.FeatureAndBenefitsUrl = featureAndBenefitsUrlEl.GetAttributeValue("href", null);
                    autoPart.FeatureAndBenefitsUrlText = featureAndBenefitsUrlEl.InnerText;
                }
            }


            //Quantity Available
            var quantityEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='ctl00_mainContentPlaceHolder_qtyAvailLabel']");
            if(null != quantityEl)
            {
                string[] quantityStrs = quantityEl.InnerText.Split(":".ToCharArray());
                if(quantityStrs.Length == 2)
                {
                    double quantity = 0;
                    double.TryParse(quantityStrs[1].Trim(), out quantity);
                    autoPart.Quantity = quantity;
                }
            }

            //Warranty
            var warrantyEl = detailDoc.DocumentNode.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_WarrantyDataLinkControl1']/a");
            if (null != warrantyEl)
            {
                autoPart.WarrantyUrl = warrantyEl.GetAttributeValue("href", null);
            }

            //Attributes
            var attributesEl = detailDoc.DocumentNode.SelectNodes("(//tr/td[@class='DetailBodyHeader' and contains(text(),'Attributes')]/ancestor::tr/following-sibling::tr)/td/text()");
            if (null != attributesEl)
            {
                foreach (var attr in attributesEl) {
                    var parts = attr.InnerText.Trim().Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if(parts.Length != 2) { continue; }
                    autoPart.Attributes.Add(new AutoPartAttribute { Name = parts[0].Trim(), Value = parts[1].Trim() });
                }
            }

            //MSDS
            var mdsEl = detailDoc.DocumentNode.SelectSingleNode("//span[@id='ctl00_mainContentPlaceHolder_MsdsDataLinkControl1']/a");
            if (null != mdsEl)
            {
                autoPart.MsdsUrl = mdsEl.GetAttributeValue("href", null);
            }
        }

        private Stream GetResponseStream(HttpWebResponse response)
        {
            Stream dataStream = null;
            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                dataStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else
            {
                dataStream = response.GetResponseStream();
            }
            return dataStream;
        }

        private static Random rng = new Random();
        private int pendingRequestCount = 0;
        private const int maxPendingRequests = 20;
        private static object reqLockObj = new object();
        private async Task<string> SubmitGET(string url)
        {
            while(pendingRequestCount >= maxPendingRequests)
            {
                await Task.Delay(rng.Next(1000, 3000));
            }

            lock (reqLockObj)
            {
                ++pendingRequestCount;
            }

            try
            {
                var req = WebRequest.Create(url);
                req.Method = "GET";
                req.Proxy = (Config.UseProxy) ? proxy : null;

                var http = (req as HttpWebRequest);
                http.CookieContainer = cookies;
                //http.Headers.Clear();
                http.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                //http.Accept = "application/json, text/javascript, */*; q=0.01";
                http.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                http.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                http.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";

                int maxTries = 2;
                while (--maxTries > 0)
                {
                    try
                    {
                        var resp = (http.GetResponse() as HttpWebResponse);
                        using (var rs = GetResponseStream(resp))
                        {
                            using (var sr = new StreamReader(rs))
                            {
                                var respBody = await sr.ReadToEndAsync();
                                return respBody;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in SubmitGet: {ex.Message}");
                        await Task.Delay(rng.Next(1000, 3000));
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

            return "";
        }

        private static IEnumerable<HtmlNode> GetElementsWithClass(HtmlNode node, String className)
        {

            Regex regex = new Regex("\\b" + Regex.Escape(className) + "\\b", RegexOptions.Compiled);
            return node.Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Element)
                .Where(e => e.Name == "td" && regex.IsMatch(e.GetAttributeValue("class", "")));
        }
    }
}
