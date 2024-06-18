using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using dotnetscrape_lib;
using dotnetscrape_lib.DataObjects;
using HCPDotNetDAL;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace PopulateDatabase
{
    class Program
    {
        private static ParallelOptions options = new ParallelOptions();

        static void Main(string[] args)
        {
            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);
            try
            {
                LoadPartsFromJSON();
                Console.ReadLine();
            }
            catch(Exception ex)
            {
                Utilities.LogError($"Unable to Load parts into database.  Reason: {ex.Message}");
            }
        }

        public static void LoadPartsFromJSON()
        {
            string filePath = Config.JsonDataPath;
            if (!Directory.Exists(filePath))
            {
                Utilities.LogError($"File Path {filePath} does not exist");
                return;
            }
            
            Stopwatch serializeJsonDataAndLoadIntoDatabaseStopwatch = new Stopwatch();
            serializeJsonDataAndLoadIntoDatabaseStopwatch.Start();
            Utilities.LogInfo($"START - Loading Data from {filePath}");
            int fileCount = 0;
            object lockFileCount = new object();
            object lockPartInsertionCount = new object();
            Parallel.ForEach(Directory.GetFiles(filePath, "*.json"), options, (file, loopState) =>
            {
                try
                {
                    Utilities.LogInfo($"START - Serializing {file}");
                    var parts = dotnetscrape_lib.Utilities.DeserializeFile<List<AutoPart>>(file, true);
                    Utilities.LogInfo($"DONE - Serializing {file}");
                    lock (lockFileCount)
                    {
                        fileCount++;
                    }

                    Parallel.ForEach(parts, options, (part, loopState2) =>
                    {
                        using (var da = new PartsDA { ConnectionString = Config.ConnectionString })
                        {
                            try
                            {
                                da.InsertPart(part.PartNumber,
                                                part.Category,
                                                part.SubCategory);
                                lock(lockPartInsertionCount)
                                {
                                    Utilities.TotalPartCount++;
                                    if(Utilities.TotalPartCount % 1000 == 0 && Utilities.TotalPartCount != 0)
                                    {
                                        Utilities.LogInfo($"Loaded {Utilities.TotalPartCount} parts so far.");
                                    }
                                }
                            }
                            catch(MySqlException ex)
                            {
                                if(ex.Number == 1062)
                                {
                                    Utilities.LogDebug($"Unable to load duplicate part {part.PartNumber}:  Reason: {ex.Message}");
                                }
                                else
                                {
                                    Utilities.LogError($"Unable to load part {part.PartNumber}:  Reason: {ex.Message}");
                                }

                            }
                            catch(Exception ex)
                            {
                                Utilities.LogError($"Unable to load part {part.PartNumber}:  Reason: {ex.Message}");
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Utilities.LogError($"Unable to serialize {file}:  Reason: {ex.Message}");
                }
            });
            serializeJsonDataAndLoadIntoDatabaseStopwatch.Stop();
            Utilities.LogInfo($"DONE - Loading Data from {filePath}");




            /*Stopwatch getPartQuantitesStopwatchFullTime = new Stopwatch();
            getPartQuantitesStopwatchFullTime.Start();
            //Attempt to Retrieve Part Quantities
            if (Config.GetPartCounts)
            {
                int partIndex = 1;
                var partIndexLock = new object();
                var array = PartsFromJSON.Values.OrderBy(part => part.PartNumber).ToArray();
                Utilities.LogInfo($"START - Attempting to get part quantites for {PartsFromJSON.Count} parts.");
                Parallel.ForEach(array, options, (part, loopState2) =>
                {
                    try
                    {
                        Stopwatch getPartQuantitesStopwatchPerPart = new Stopwatch();
                        Utilities.LogDebug($"START - [{partIndex}] - Getting Part Quantities for Part Number: {part.PartNumber}");
                        getPartQuantitesStopwatchPerPart.Start();
                        part.FindItQuantities = FindPartQuantities(part).Result;
                        getPartQuantitesStopwatchPerPart.Stop();
                        double timeToGetPartCount = (double)getPartQuantitesStopwatchPerPart.ElapsedMilliseconds / 1000;
                        Utilities.LogInfo($"DONE - [{partIndex}] - Getting Part Quantities for Part Number: {part.PartNumber}.  Time: {timeToGetPartCount} seconds.");
                        lock (partIndexLock)
                        {
                            partIndex++;
                        }
                        if (partIndex % 500 == 0)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError($"Unable to get part counts for {part.PartNumber}:  Reason: {ex.Message}");
                    }

                });

                Utilities.LogInfo($"DONE - Attempting to get part quantites for {PartsFromJSON.Count} parts.");
            }
            getPartQuantitesStopwatchFullTime.Stop();



            double partCount = PartsFromJSON.Keys.Count;
            double totalTimeToLoadIntoMemory = (double)serializeJsonDataAndLoadIntoMapStopwatch.ElapsedMilliseconds / 1000;
            double totalTimeToRetrievePartCount = (double)getPartQuantitesStopwatchFullTime.ElapsedMilliseconds / 1000;
            double averageTimeLoadFileIntoMemory = (fileCount == 0) ? -1 : totalTimeToLoadIntoMemory / fileCount;
            double averageTimeLoadPartIntoMemoryFromDisk = (partCount == 0) ? -1 : totalTimeToLoadIntoMemory / partCount;
            double averageTimeToRetievePartCount = (partCount == 0 && Config.GetPartCounts) ? -1 : totalTimeToRetrievePartCount / partCount;
            Utilities.LogInfo($"Total time to deserialize {PartsFromJSON.Count} parts from {fileCount} files is {totalTimeToLoadIntoMemory} seconds.");
            Utilities.LogInfo($"Average time to deserialize each file: {averageTimeLoadFileIntoMemory} seconds");
            Utilities.LogInfo($"Average time to deserialize each part: {averageTimeLoadPartIntoMemoryFromDisk} seconds");
            Utilities.LogInfo($"Average time to retrieve part count for each part: {averageTimeToRetievePartCount} seconds");*/

        }
    }
}
