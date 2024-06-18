using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace dotnetscrape_crawler
{
    public sealed class Config
    {
        public static Config Instance { get; } = new Config();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Config()
        {
        }

        private Config()
        {
            var builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json");

            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(env))
            {
                env = "Development";
            }

            if (env == "Development")
            {
                builder.AddUserSecrets<Program>();
            }

            configuration = builder.Build();
        }

        private static IConfiguration configuration { get; set; }

        public static string ConnectionString => configuration["connectionString"];
        public static string Username => configuration["username"];
        public static string Password => configuration["password"];
        public static bool UseProxy => "true".Equals(configuration["useProxy"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool CacheResponses => "true".Equals(configuration["cacheResponses"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static string ProxyHost => configuration["proxyHost"];
        public static int ProxyPort => int.Parse(configuration["proxyPort"]);
        public static int MaxPagesPerSubCategory => int.Parse(configuration["maxPagesPerSubCategory"]);
        public static string DataExportPath => configuration["dataExportPath"];
        public static string ProcessingPath => configuration["processingPath"];
        public static int[] CategoryWhiteList => null != CommandLine?.CategoryWhitelist ? GetCategoryWhiteList(CommandLine.CategoryWhitelist) : GetCategoryWhiteList(configuration["categoryWhiteList"]);
        public static int[] SubCategoryWhiteList => null != CommandLine?.SubCategoryWhitelist ? GetCategoryWhiteList(CommandLine.SubCategoryWhitelist) : GetCategoryWhiteList(configuration["subCategoryWhiteList"]);
        public static int MaxPendingRequests => int.Parse(configuration["maxPendingRequests"]);
        public static LogLevel LogLevel => null != CommandLine?.LogLevel ? CommandLine.LogLevel.Value : Enum.Parse<LogLevel>(configuration["logLevel"]);
        public static OperationMode OperationMode => null != CommandLine?.OpMode ? CommandLine.OpMode.Value : Enum.Parse<OperationMode>(configuration["operationMode"]);
        public static bool WriteToProgressFile => "true".Equals(configuration["writeToProgressFile"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static int MaxNumberOfCurrentSearchQueryTask => int.Parse(configuration["maxNumberOfCurrentSearchQueryTask"]);
        public static bool FullSearch => "true".Equals(configuration["fullSearch"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool RetrievePartDetails => "true".Equals(configuration["retrievePartDetails"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool ProcessInReverseOrder => "true".Equals(configuration["processInReverseOrder"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool PerformCacheLookup
        {
            get
            {
                return (OperationMode == OperationMode.CrawlOnlyWithCacheLookup ||
                        OperationMode == OperationMode.CrawlAndProcessWithCacheLookup ||
                        OperationMode == OperationMode.ProcessOnly);
            }
        }

        public static bool ProcessResults
        {
            get
            {
                return (OperationMode == OperationMode.CrawlAndProcessWithCacheLookup ||
                            OperationMode == OperationMode.CrawlAndProcessWithoutCacheLookup ||
                            OperationMode == OperationMode.ProcessOnly);
            }
        }

        public static bool CrawlOnly
        {
            get
            {
                return (OperationMode == OperationMode.CrawlOnlyWithCacheLookup ||
                            OperationMode == OperationMode.CrawlOnlyWithoutCacheLookup);
            }
        }

        public static CommandLineArguments CommandLine { get; set; }

        private static int[] GetCategoryWhiteList(string values)
        {
            return values.Split(new[] { ',' }).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => int.Parse(s)).ToArray();
        }

        public static bool ReadDataFromJSON => "true".Equals(configuration["readDataFromJSON"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool GetPartCounts => "true".Equals(configuration["getPartCounts"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool CloneFindItSearchParms => "true".Equals(configuration["cloneFindItSearchParms"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static string JsonDataPath => configuration["jsonDataPath"];
        public static bool CachePartQuantities => "true".Equals(configuration["cachePartQuantities"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool UseCacheOnly => "true".Equals(configuration["useCacheOnly"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool WriteToDatabaseOnly => "true".Equals(configuration["writeToDatabaseOnly"], StringComparison.OrdinalIgnoreCase) ? true : false;

    }

    public enum LogLevel:int
    {
        DEBUG=0,
        INFO,
        ERROR
    }

    public enum OperationMode
    {
        CrawlAndProcessWithCacheLookup,
        CrawlAndProcessWithoutCacheLookup,
        CrawlOnlyWithCacheLookup,
        CrawlOnlyWithoutCacheLookup,
        ProcessOnly
    }
}
