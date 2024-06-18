using System;
using System.Configuration;

namespace DOTNETScrape
{
    public static class Config
    {
        public static string Username => ConfigurationManager.AppSettings["username"];
        public static string Password => ConfigurationManager.AppSettings["password"];
        public static bool UseProxy => "true".Equals(ConfigurationManager.AppSettings["useProxy"],StringComparison.OrdinalIgnoreCase) ? true : false;
    }
}
