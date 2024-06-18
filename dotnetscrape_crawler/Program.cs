using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HCPDotNetBLL;
using dotnetscrape_lib.DataObjects;

namespace dotnetscrape_crawler
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

        
        static void OldProcess(DOTNETClient dotnetClient, int maxPages)
        {
           
            if (Config.ReadDataFromJSON)
            {
                dotnetClient.IntializeFindIt();
                dotnetClient.GetPartsFromJSON();
                dotnetClient.GeneartePartsFromJSONData();
            }
            else
            {
                Utilities.LogInfo("Getting All Categories and SubCategories");
                //See Categories
                var categories = dotnetClient.PopulateCategories().Result;

                Utilities.LogInfo("Category and SubCategory Retrieval Complete");

                Utilities.LogInfo("Setting up Parallel Page Scraping");
                var task = Task.Run(() =>
                {
                    dotnetClient.GetParts(categories, 0, maxPages);
                });

                task.Wait();


                string butWillNotBeSavedToCSV = (Config.CrawlOnly) ? "However no parts were stored in memory for CSV generation." : string.Empty;
                Utilities.LogInfo($"{DOTNETClient.totalPartCount} parts details extracted. {butWillNotBeSavedToCSV}");

                var controlFile = Path.Combine(Config.ProcessingPath, "control.txt");
                Utilities.LogDebug($"Control File: {controlFile}");
                if (File.Exists(controlFile))
                {
                    Utilities.LogDebug($"Control file contents: {File.ReadAllText(controlFile)}");
                }

                while (!task.IsCompleted)
                {
                    var controlContent = string.Empty;
                    if (File.Exists(controlFile))
                    {
                        controlContent = File.ReadAllText(controlFile).Trim();
                    }

                    if (controlContent.ToLowerInvariant() == "stop")
                    {
                        dotnetClient.Stop = true;
                        Utilities.LogInfo("Stop signal sent, waiting for threads to stop...");
                        break;
                    }

                    Task.Delay(1000).Wait();
                }
                task.Wait();
            }
        }

        static void NewProcess(DOTNETClient dotnetClient, int maxPages)
        {
            Utilities.LogInfo("Getting All Categories and SubCategories");
            //See Categories
            var categories = dotnetClient.PopulateCategories().Result;

            Utilities.LogInfo("Category and SubCategory Retrieval Complete");

            Utilities.LogInfo("Setting up Parallel Page Scraping");
            var task = Task.Run(() =>
            {
                dotnetClient.GetParts(categories, 0, maxPages);
            });

            task.Wait();
        }

        private static PartsBL CreateBL()
        {
            return new PartsBL { ConnectionString = Config.ConnectionString };
        }
        static void Main(string[] args)
        {
            //Create a Stopwatch
            var stopWatch = new Stopwatch();

            try
            {
                var commandLine = CommandLineParser.Parse(args);
                if (null != commandLine)
                {
                    if (commandLine.ShowHelp)
                    {
                        CommandLineParser.ShowHelp();
                        return;
                    }
                    else
                    {
                        Config.CommandLine = commandLine;
                    }
                }

                int maxPages = Config.MaxPagesPerSubCategory;

                //Check if Folder Exists
                if (!Directory.Exists(Config.DataExportPath))
                {
                    try
                    {
                        Directory.CreateDirectory(Config.DataExportPath);
                    }
                    catch (IOException e)
                    {
                        Utilities.LogError($"Unable to create folder \"{Config.DataExportPath}\".  Please provide a valid path.{Environment.NewLine}Error: {e.Message}");
                        return;
                    }
                }

                stopWatch.Start();

                Utilities.LogInfo("Logging In");
                var dotnetClient = new DOTNETClient();
                if (!dotnetClient.Login(Config.Username, Config.Password).Result)
                {
                    throw new Exception("Failed to log in.");
                }
                Utilities.LogInfo("Login Complete");

                NewProcess(dotnetClient, maxPages);

                //OldProcess(dotnetClient, maxPages);

                if (dotnetClient.Stop)
                {
                    Utilities.LogInfo("Processing stopped due to stop signal, exiting now.");
                }
                else
                {
                    Utilities.LogInfo("Done Collecting Parts Data");
                }
            }
            catch (Exception e)
            {
                Utilities.LogError($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            finally
            {
                stopWatch.Stop();
                Utilities.LogInfo("Processing Complete");
                Utilities.LogInfo($"Time to extract {DOTNETClient.totalPartCount} parts: {stopWatch.Elapsed}");
                Utilities.LogInfo($"Total Number of Categories Processed: {DOTNETClient.totalNumberOfCategoriesProcessed} of {DOTNETClient.totalNumberOfCategories}");
                Utilities.LogInfo($"Total Number of SubCategories Processed: {DOTNETClient.totalNumberOfSubCategoriesProcessed} of {DOTNETClient.totalNumberOfSubCategories}");
            }
#if DEBUG
            //Console.ReadLine();
#endif
        }
    }
}
