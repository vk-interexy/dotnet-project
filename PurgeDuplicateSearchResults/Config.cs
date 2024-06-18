using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace PurgeDuplicateSearchResults
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

        
        public static bool PerformDelete => "true".Equals(configuration["perfromDelete"], StringComparison.OrdinalIgnoreCase) ? true : false;
        

        
    }

    public enum LogLevel:int
    {
        DEBUG=0,
        INFO,
        ERROR
    }
}
