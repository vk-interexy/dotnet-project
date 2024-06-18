using System;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using HCPDotNetBLL;
using dotnetscrape_lib.DataObjects;

namespace GenerateCSVFromDatabase
{
    class Program
    {
        private static void CompressFile(string fileName)
        {
            string compressedFileName = fileName.Replace(".csv", ".zip");
            string newFileNameEntryInArchive = Path.GetFileName(fileName);
            using (FileStream fs = new FileStream(compressedFileName, FileMode.Create))
            {
                using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    arch.CreateEntryFromFile(fileName, newFileNameEntryInArchive,CompressionLevel.Optimal);
                }
            }
            File.Delete(fileName);
        }
        static void Main(string[] args)
        {
            try
            {
                string csvFolder = Config.CsvExportPath;

                if(!Directory.Exists(csvFolder))
                {
                    Directory.CreateDirectory(csvFolder);
                }

                string fullSearchFileNamePrefix = $"FullDataExport_{DateTime.Now.ToString("yyyy-MM-dd_HH+mm+ss")}.csv";
                
                string csvFilePath = Path.Combine(csvFolder, fullSearchFileNamePrefix);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                Utilities.LogInfo("Starting creation of CSV file.  Please wait...");
                int partCount = 0;
                using (var bl = new PartsBL() { ConnectionString = Config.ConnectionString })
                {
                    var parts = bl.GetAllPartsAsOrderedList();
                    partCount = parts.Count;
                    using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
                    {
                        writer.WriteLine(AutoPart.CSVHeader);
                        foreach (var part in parts)
                        {
                            writer.WriteLine(part.CSVWithDCQuantityOnly());
                        }
                    }
                }
                CompressFile(csvFilePath);

                watch.Stop();
                Utilities.LogInfo($"Created CSV file with {partCount} records.{Environment.NewLine}File Location: {csvFilePath}{Environment.NewLine}Total time to create file: {watch.Elapsed}");
                Console.ReadLine();
            }
            catch(Exception ex)
            {
                Utilities.LogError(ex.Message);
            }

        }
    }
}
