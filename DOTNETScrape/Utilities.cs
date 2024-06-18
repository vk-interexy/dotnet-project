using System;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace DOTNETScrape
{
    public class Utilities
    {
        public static string GenerateCSVString(string text)
        {
            return (string.IsNullOrWhiteSpace(text)) ? "" : $"\"{text.Replace("\"", "\"\"").Replace(Environment.NewLine, string.Empty)}\"";
        }

        public static string SerializeObject<T>(T toSerialize, bool json)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.IndentChars = "\t";
            settings.Indent = true;
            string ret;

            if (json)
            {
                ret = JsonConvert.SerializeObject(toSerialize, Newtonsoft.Json.Formatting.Indented);
            }
            else
            {
                using (StringWriter textWriter = new StringWriter())
                {
                    using (XmlWriter xw = XmlWriter.Create(textWriter, settings))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());
                        xmlSerializer.Serialize(xw, toSerialize);
                    }
                    ret = textWriter.ToString();
                }
            }
            return ret;
        }

        static public string HTMLDecodeWithoutNBSP(string html)
        {
            return WebUtility.HtmlDecode(html).Trim().Replace("\u00A0", " ");
        }
    }
}
