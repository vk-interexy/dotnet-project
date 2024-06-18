using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace HCPDotNetGetPricingAndQuantity
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
                                .AddJsonFile("HCPDotNetGetPricingAndQuantitySettings.json");

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
        public static string DotNetB2BApiUrl => configuration["dotnetB2BApiUrl"];
        public static string StoreID => configuration["storeId"];
        public static string AccountPassword => configuration["accountPassword"];
        public static LogLevel LogLevel => Enum.Parse<LogLevel>(configuration["logLevel"]);
        public static string PrimaryDistributionCetner => configuration["primaryDistributionCenter"];
        public static int ChunkSize => int.Parse(configuration["chunkSize"]);
        public static bool QuantityPricingUpdatedDateFieldOnly => "true".Equals(configuration["quantityPricingUpdatedDateFieldOnly"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool RetrievePricingForOnlyUnsetQuantityPricingUpdatedDate => "true".Equals(configuration["retrievePricingForOnlyUnsetQuantityPricingUpdatedDate"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool SortAscOrDesc => "true".Equals(configuration["sortAscOrDesc"], StringComparison.OrdinalIgnoreCase) ? true : false;
        //public static bool WriteToProgressFile => "true".Equals(configuration["writeToProgressFile"], StringComparison.OrdinalIgnoreCase) ? true : false;
        //public static int MaxNumberOfCurrentSearchQueryTask => int.Parse(configuration["maxNumberOfCurrentSearchQueryTask"]);
    }

    public enum LogLevel : int
    {
        DEBUG = 0,
        INFO,
        ERROR
    }
}
