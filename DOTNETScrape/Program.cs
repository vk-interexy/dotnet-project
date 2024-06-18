using System;
using System.IO;
using DOTNETScrape.DataObjects;
using System.Collections.Generic;
using System.Diagnostics;

namespace DOTNETScrape
{
    class Program
    {
        /* General Flow
         * 1.  Login
         * 2.  Generate List of Part Numbers
         * 3.  Iterate through each Part Number and call API to get Details
         * 4.  Scrape details and populate AutoPart Object
         * 5.  Send AutoPart Object to Database for Storage
        */

        static void Main(string[] args)
        {
            //Create a Stopwatch

            var stopWatch = new Stopwatch();
            var parts = new List<AutoPart>();

            try
            {
                #region Argument Processing Logic
                if (args.Length < 2 || args.Length > 4)
                {
                    Console.Write($"Usage: DotNetScrape <Number of Items> <Data Output Folder> <UserName> <Password> {Environment.NewLine}" +
                                  $"<Number of Items> and <Data Output Folder> are required.  <UserName> and <Password> are optional.");
                    return;
                }

                string exportPath = string.Empty;
                string userName = Config.Username;
                string password = Config.Password;
                int numberOfItems = 0;
                int.TryParse(args[0], out numberOfItems);
                int maxPages = (numberOfItems % Constants.partsPerPage != 0) ? (numberOfItems / Constants.partsPerPage) + 1 : numberOfItems / Constants.partsPerPage;
                exportPath = args[1];
                switch (args.Length)
                {
                    case 3:
                        userName = args[2];
                        break;
                    case 4:
                        userName = args[2];
                        password = args[3];
                        break;
                }
                #endregion

                //Check if Folder Exists
                if (!Directory.Exists(exportPath))
                {
                    try
                    {
                        Directory.CreateDirectory(exportPath);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"Unable to create folder \"{exportPath}\".  Please provide a valid path.{Environment.NewLine}Error: {e.Message}");
                        return;
                    }
                }


                stopWatch.Start();

                Console.WriteLine("Logging In");
                var dotnetClient = new DOTNETClient();
                if (!dotnetClient.Login(userName, password).Result)
                {
                    throw new Exception("Failed to log in.");
                }

                Console.WriteLine("Login Complete");

                Console.WriteLine("Getting All Categories and SubCategories");
                //See Categories
                dotnetClient.PopulateCategories();

                Console.WriteLine("Category and SubCategory Retrieval Complete");

                

                var lockObject = new object();


                //var autoPart = new DOTNETScrape.DataObjects.AutoPart();
                //var detailResponse = dotnetClient.GetProductDetail("NDP6005266").Result;
                //dotnetClient.HTMLToAutoPartDetail(detailResponse, ref autoPart);
                
                

                Console.WriteLine("Setting up Parallel Page Scraping");
                foreach (var part in dotnetClient.GetParts(0, maxPages))
                {
                    lock (lockObject)
                    {
                        parts.Add(part);
                    }
                    if (parts.Count % 10 == 0)
                    {
                        Console.WriteLine($"{parts.Count} parts extracted.");
                    }
                }
                Console.WriteLine("Done Colleecting Parts Data");
                
                Guid guid = Guid.NewGuid();
                string csvFilePath = Path.Combine(exportPath, $"DataExport_{guid}.csv");
                string xmlFilePath = Path.Combine(exportPath, $"DataExport_{guid}.xml");
                string jsonFilePath = Path.Combine(exportPath, $"DataExport_{guid}.json");

                //Geneate JSON File
                Console.WriteLine("Generating JSON File");
                File.WriteAllText(jsonFilePath, Utilities.SerializeObject(parts, true));
                Console.WriteLine($"JSON File writteng to: {jsonFilePath}");

                //Generate XML File
                Console.WriteLine("Generating XML File");
                File.WriteAllText(xmlFilePath, Utilities.SerializeObject(parts, false));
                Console.WriteLine($"XML File writteng to: {xmlFilePath}");

                //Genearte CSV File
                Console.WriteLine("Generating CSV File");
                string csvString = $"{AutoPart.CSVHeader}{Environment.NewLine}";
                foreach (var part in parts)
                {
                    csvString += $"{part.CSV}{Environment.NewLine}";
                }
                File.WriteAllText(csvFilePath, csvString);
                Console.WriteLine($"CSV File writteng to: {csvFilePath}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            finally
            {
                stopWatch.Stop();
                Console.WriteLine("Processing Complete");
                Console.WriteLine($"Time to extract {parts.Count} parts: {stopWatch.Elapsed}");
                
            }
#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
