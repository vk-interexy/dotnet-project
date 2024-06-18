using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace GenerateFullOutput
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

            /*if (env == "Development")
            {
                builder.AddUserSecrets<Program>();
            }*/

            configuration = builder.Build();
        }

        private static IConfiguration configuration { get; set; }

        public static CommandLineArguments CommandLine { get; set; }


        public static string Username => configuration["username"];
        public static string Password => configuration["password"];
        public static bool UseJSONData => "true".Equals(configuration["useJSONData"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static bool RetrieveDetailsWithNoPricing => "true".Equals(configuration["retrieveDetailsWithNoPricing"], StringComparison.OrdinalIgnoreCase) ? true : false;
        public static string DataExportPath => configuration["dataExportPath"];
        public static string ProcessingPath => configuration["processingPath"];
        public static int MaxPendingRequests => int.Parse(configuration["maxPendingRequests"]);

    }

    public enum LogLevel:int
    {
        DEBUG=0,
        INFO,
        ERROR
    }
}
