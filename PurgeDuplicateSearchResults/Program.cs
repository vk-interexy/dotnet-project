using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using dotnetscrape_lib;


namespace PurgeDuplicateSearchResults
{
    class Program
    {
        private static ConcurrentDictionary<string, PartNumberDup> map = new ConcurrentDictionary<string, PartNumberDup>();
        private static ConcurrentDictionary<string,int> partMap = new ConcurrentDictionary<string, int>();
        private static ParallelOptions options = new ParallelOptions();

        private static void MergePartNumbers(List<string> partNumbers)
        {
            Parallel.ForEach(partNumbers, Program.options, (partNumber, loopState) =>
            {
                if(!partMap.ContainsKey(partNumber))
                {
                    partMap.TryAdd(partNumber, 1);
                }
                else
                {
                    int count = partMap[partNumber];
                    count++;
                    partMap[partNumber] = count;
                }
            });
        }
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: dotnet PurgeDuplicateSearchResults.dll <resultspath> <repeat-count-threshold>");
                return;
            }
            string path = args[0];

            int threshold = 4;
            int.TryParse(args[1], out threshold);

            options.MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1);

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"{path} is not a valid path.");
                return;
            }

            
            int i = 0;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var lockObject = new object();
            
            
            Parallel.ForEach(Directory.GetFiles(path), options, (file, loopState) =>
            {
                var partNumbers = PartNumberRetriever.GetPartNumbers(file);

                MergePartNumbers(partNumbers);

                var hash = Utilities.GetHashSha256(partNumbers);
                if (map.ContainsKey(hash))
                {
                    var partDup = map[hash];
                    partDup.DupFiles.Add(file);
                }
                else
                {
                    var partDup = new PartNumberDup();
                    partDup.DupFiles.Add(file);
                    map.TryAdd(hash, partDup);
                }

                if (++i % 50 == 0)
                {
                    stopwatch.Stop();
                    Console.WriteLine($"{i} files processed so far.  Time per file: {(double)stopwatch.ElapsedMilliseconds / (double)1000 / (double)i}");
                    stopwatch.Start();
                }
            });

            foreach(var partNumber in partMap.Keys)
            {
                int count = partMap[partNumber];
                if(partMap[partNumber] > 1)
                {
                    Console.WriteLine($"PartNumber: {partNumber} occurs {count} times");
                }
            }

            long numberOfFilesDeleted = 0;
            
                
            foreach (var fileDup in map.Values)
            {
                fileDup.RemoveDups(threshold);
                numberOfFilesDeleted += fileDup.FilesDeleted;
            }
            
            stopwatch.Stop();
            Console.WriteLine($"{i} files processed in Total.{Environment.NewLine}Total files deleted {numberOfFilesDeleted}.{Environment.NewLine}Time per file: {(double)stopwatch.ElapsedMilliseconds / (double)1000 / (double)i}");
        }
    }
}
