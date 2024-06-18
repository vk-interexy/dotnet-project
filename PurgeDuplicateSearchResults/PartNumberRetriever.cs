using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using dotnetscrape_lib;

namespace PurgeDuplicateSearchResults
{
    public class PartNumberRetriever
    {
        private static IEnumerable<HtmlNode> GetElementsWithClass(HtmlNode node, String className)
        {

            Regex regex = new Regex("\\b" + Regex.Escape(className) + "\\b", RegexOptions.Compiled);
            return node.Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Element)
                .Where(e => e.Name == "td" && regex.IsMatch(e.GetAttributeValue("class", string.Empty)));
        }

        static public List<string> GetPartNumbers(string fileName)
        {
            var list = new List<string>();
            if(!File.Exists(fileName))
            {
                return list;
            }
            var doc = new HtmlDocument();
            doc.Load(fileName);

            //*[@id="ctl00_mainContentPlaceHolder_ProlinkResultList1"]/table/tbody/tr[2]/td[4]

            var parts = (doc.DocumentNode.SelectNodes("//span[@id='ctl00_mainContentPlaceHolder_ProlinkResultList1']/table/tr") == null) ?
                new List<string>() :
            doc.DocumentNode.SelectNodes("//span[@id='ctl00_mainContentPlaceHolder_ProlinkResultList1']/table/tr/td[4]").Select(td =>
            {
                if (td == null) return string.Empty;
                var partnum = Utilities.HTMLDecodeWithoutNBSP(td?.InnerText);
                return string.Equals(dotnetscrape_constants.Constants.QuantityHeaderString,partnum,StringComparison.OrdinalIgnoreCase) ? string.Empty : partnum;
                
            })
            .Where(partNumber => !string.IsNullOrWhiteSpace(partNumber)).ToList();
            parts.Sort();
            return parts;
        }
    }
}
