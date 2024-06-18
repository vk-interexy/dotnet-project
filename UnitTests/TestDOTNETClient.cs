using Microsoft.VisualStudio.TestTools.UnitTesting;
using DOTNETScrape;
using DOTNETScrape.DataObjects;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UnitTests
{
    [TestClass]
    public class TestDOTNETClient
    {
        [TestMethod]
        public void TestLogin()
        {
            var dotnetClient = new DOTNETClient();
            Assert.IsTrue(dotnetClient.Login(Config.Username, Config.Password).Result);

            Assert.IsFalse(dotnetClient.Login("baduser", "badpass").Result);
        }

        [TestMethod]
        public void TestGetSearch()
        {
            var dotnetClient = new DOTNETClient();
            Assert.IsTrue(dotnetClient.Login(Config.Username, Config.Password).Result);
            var searchResults = dotnetClient.GetParts(0);
            Assert.AreEqual(Constants.partsPerPage, searchResults.Count());
        }

        [TestMethod]
        public void TestParseSearchPage()
        {
            var dotnetClient = new DOTNETClient();
            var searchResults = dotnetClient.HTMLToAutoParts(ReadTestData("searchpageresponse.html"), null, false);
            Assert.AreEqual(Constants.partsPerPage, searchResults.Count());
            Assert.IsFalse(searchResults.Any(part => string.IsNullOrWhiteSpace(part.PartNumber)));
            var testPart = searchResults.ElementAt(0);
            Assert.AreEqual("MPE CS791SB", testPart.PartNumber);
            Assert.AreEqual("Contact Set (Points) & Condenser", testPart.PartName);

            testPart = searchResults.ElementAt(23);
            Assert.AreEqual("MPE CS359SB", testPart.PartNumber);
            Assert.AreEqual("Contact Set (Points)", testPart.PartName);
        }

        [TestMethod]
        public void TestParsePartDetailsPage()
        {
            var dotnetClient = new DOTNETClient();
            var testPart = new AutoPart();
            dotnetClient.HTMLToAutoPartDetail(ReadTestData("detailpageresponse.html"), ref testPart);
            Assert.AreEqual(2, testPart.ImageUrls.Count);
            Assert.AreEqual("https://s7d9.scene7.com/is/image/GenuinePartsCompany/50474?.jpg&fit=constrain,1&wid=2000&hei=2000", testPart.ImageUrls[0]);
            Assert.AreEqual("https://s7d9.scene7.com/is/image/GenuinePartsCompany/500808?.jpg&fit=constrain,1&wid=2000&hei=2000", testPart.ImageUrls[1]);
            Assert.AreEqual(@"Mileage Plus Value Priced Products Meet Original Equipment Quality & Performance; They Are Designed To Be A Cost Effective Replacement Part For The Vehicle.", testPart.FeatureAndBenefits);
            Assert.AreEqual("", testPart.MsdsUrl);
            Assert.AreEqual(13.96m, testPart.Pricing.Cost);
            Assert.AreEqual(27.92m, testPart.Pricing.List);
            Assert.AreEqual("Each", testPart.Pricing.Unit);
            Assert.AreEqual(0ul, testPart.Quantity);
            Assert.AreEqual("https://s7d9.scene7.com/is/content/GenuinePartsCompany/1278185pdf?$PDF$", testPart.WarrantyUrl);
            Assert.AreEqual(1, testPart.Attributes.Count);
            Assert.AreEqual("UNSPSC", testPart.Attributes.Where(a => a.Name == "UNSPSC").FirstOrDefault().Name);
            Assert.AreEqual("26101718", testPart.Attributes.Where(a => a.Name == "UNSPSC").FirstOrDefault().Value);
        }

        private static string ReadTestData(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"UnitTests.TestData.{name}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }
    }
}
