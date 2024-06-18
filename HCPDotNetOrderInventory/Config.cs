using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using dotnetscrape_constants;
using dotnetscrape_lib;
using HCPDotNetDAL;

namespace HCPDotNetOrderInventory
{
    public sealed class Config
    {
        public static Config Instance { get; } = new Config();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Config()
        {
        }

        private static Dictionary<string, string> smpSettings;

        private Config()
        {
            var builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("HCPDotNetOrderInventorySettings.json");

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

            try
            {
                using (var da = new SettingsDA { ConnectionString = ConnectionString })
                {
                    smpSettings = da.GetAllSettings();
                }
            }
            catch
            {
                smpSettings = new Dictionary<string, string>();
            }

        }

        private static IConfiguration configuration { get; set; }



        public static string ConnectionString => configuration["connectionString"];
        public static int MaxSalesVelocityDays
        {
            get
            {
                var settingValue = smpSettings.ContainsKey("MaxSalesVelocityDays") ? smpSettings["MaxSalesVelocityDays"] : "60";
                bool parsed = int.TryParse(settingValue, out int maxSalesVelocityDays);
                return (parsed) ? maxSalesVelocityDays : 60;
                
            }
        }

        public static bool CleanUpPartPurchases
        {
            get
            {
                var settingValue = smpSettings.ContainsKey("CleanUpPartPurchases") ? smpSettings["CleanUpPartPurchases"] : "false";
                return "true".Equals(settingValue, StringComparison.OrdinalIgnoreCase) ? true : false;

            }
        }

        public static bool GenerateTestResponseForOrderProcess
        {
            get
            {
                var settingValue = smpSettings.ContainsKey("GenerateTestResponseForOrderProcess") ? smpSettings["GenerateTestResponseForOrderProcess"] : "false";
                return "true".Equals(settingValue, StringComparison.OrdinalIgnoreCase) ? true : false;

            }
        }

        public static string DotNetB2BApiUrl
        {
            get
            {
#if DEBUG
                return Constants.DotNetB2BApiUrl;
#else
                return configuration ["dotnetB2BApiUrl"];
#endif
            }
        }
        public static string StoreID => configuration["storeId"];
        public static string AccountPassword => configuration["accountPassword"];
        public static LogLevel LogLevel => Enum.Parse<LogLevel>(configuration["logLevel"]);
        public static string PrimaryDistributionCetner => configuration["primaryDistributionCenter"];
        public static bool AutoOrderEnabledGlobal
        {
            get
            {
                var settingValue = smpSettings.ContainsKey("AutoOrderEnabledGlobal") ? smpSettings["AutoOrderEnabledGlobal"] : "false";
                return "true".Equals(settingValue, StringComparison.OrdinalIgnoreCase) ? true : false;
            }
        }
        public static int ChunkSize => int.Parse(configuration["chunkSize"]);

        public static bool RunOneTimeInsertOfPartPurchase => "true".Equals(configuration["runOneTimeInsertOfPartPurchase"], StringComparison.OrdinalIgnoreCase) ? true : false;

        public static bool UseSkuOrderQuanityForSalesVelocityCalculations
        {
            get
            {
                var settingValue = smpSettings.ContainsKey("UseSkuOrderQuanityForSalesVelocityCalculations") ? smpSettings["UseSkuOrderQuanityForSalesVelocityCalculations"] : "false";
                return "true".Equals(settingValue, StringComparison.OrdinalIgnoreCase) ? true : false;
            }
        }
        

    }
}
