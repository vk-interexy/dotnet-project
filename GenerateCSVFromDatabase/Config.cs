using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace GenerateCSVFromDatabase
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
                                .AddJsonFile("GenerateCSVFromDatabaseSettings.json");

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
        public static string CsvExportPath => configuration["csvExportPath"];
        public static LogLevel LogLevel => Enum.Parse<LogLevel>(configuration["logLevel"]);
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
