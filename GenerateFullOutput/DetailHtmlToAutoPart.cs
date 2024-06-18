using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using dotnetscrape_lib.DataObjects;
using HtmlAgilityPack;

namespace GenerateFullOutput
{
    public class DetailHtmlToAutoPart
    {
        static public AutoPart HTMLToAutoPartDetail(string html)
        {
            AutoPart autoPart = null;
            try
            {
                //Protect against exception thrown by HTMLAgility "(Value cannot be null. Parameter name: html)"
                if (string.IsNullOrWhiteSpace(html))
                {
                    return autoPart;
                }

                autoPart = new AutoPart();
                var detailDoc = new HtmlDocument();
                detailDoc.LoadHtml(html);

                //PartNumber
                var partNumberEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='pagewrap']/table/tr/td/table[1]/tr[2]/td[3]/table[2]/tr[3]/td[3]/table[1]/tr[1]/td[2]");
                if (null != partNumberEl)
                {
                    autoPart.PartNumber = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(partNumberEl.InnerText);
                }
                else
                {
                    return null;
                }

                //Part Detail URL
                var formEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='aspnetForm']");
                if (null != formEl)
                {
                    //Set Part Name
                    string actionUrl = formEl.Attributes["action"]?.Value.Replace("./", string.Empty);
                    if (!string.IsNullOrWhiteSpace(actionUrl))
                    {
                        autoPart.DetailUrl = $"https://www.dotnetprolink.com/{actionUrl.Trim()}";
                    }
                }

                //Set ProductLine
                var productLineEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='pagewrap']/table/tr/td/table[1]/tr[2]/td[3]/table[2]/tr[3]/td[3]/table[1]/tr[2]/td[2]");
                if (null != productLineEl)
                {
                    autoPart.ProductLine = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(productLineEl.InnerText);
                }

                //PartName
                var titleEl = detailDoc.DocumentNode.SelectSingleNode("//*[@id='pagewrap']/table/tr/td/table[1]/tr[2]/td[3]/table[2]/tr[2]/td[2]/div[1]/span");
                if (null != titleEl)
                {
                    autoPart.PartName = dotnetscrape_lib.Utilities.HTMLDecodeWithoutNBSP(titleEl.InnerText);
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
                var priceEl = detailDoc.DocumentNode.SelectNodes("(//span[@id='ctl00_mainContentPlaceHolder_ProlinkPricingControl1']/table/tr/td)[3]/text()");
                if (null != priceEl)
                {
                    bool coreIncluded = priceEl.Count == 4;
                    int listIndex = 0;
                    int coreIndex = 1;
                    int costIndex = (coreIncluded) ? 2 : 1;
                    int unitIndex = (coreIncluded) ? 3 : 2;
                    decimal price = 0;
                    if (priceEl.Count > listIndex && decimal.TryParse(priceEl.ElementAt(listIndex).InnerText, out price))
                    {
                        autoPart.Pricing.List = price;
                        autoPart.Pricing.PricingDetailProvided = true;
                    }

                    if (coreIncluded)
                    {
                        if (priceEl.Count > coreIndex && decimal.TryParse(priceEl.ElementAt(coreIndex).InnerText, out price))
                        {
                            autoPart.Pricing.Core = price;
                            autoPart.Pricing.PricingDetailProvided = true;
                        }
                    }

                    if (priceEl.Count > costIndex && decimal.TryParse(priceEl.ElementAt(costIndex).InnerText, out price))
                    {
                        autoPart.Pricing.Cost = price;
                        autoPart.Pricing.PricingDetailProvided = true;
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
                if (null != quantityEl)
                {
                    string[] quantityStrs = quantityEl.InnerText.Split(":".ToCharArray());
                    if (quantityStrs.Length == 2)
                    {
                        decimal quantity = 0m;
                        decimal.TryParse(quantityStrs[1].Trim(), out quantity);
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
                    foreach (var attr in attributesEl)
                    {
                        var parts = attr.InnerText.Trim().Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2) { continue; }
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
            catch
            {

            }

            return autoPart;
        }
    }
}
