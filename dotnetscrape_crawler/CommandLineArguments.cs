using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace dotnetscrape_crawler
{
    [Obfuscation(Exclude = true)]
    public class CommandLineArguments
    {
        [CommandLineArg(Name = "cw", Required = false, Description = "Category whitelist, separate by comma: 123,345,456")]
        public string CategoryWhitelist = null;

        [CommandLineArg(Name = "scw", Required = false, Description = "SubCategory whitelist, separate by comma: 123,345,456")]
        public string SubCategoryWhitelist = null;

        [CommandLineArg(Name = "m", Required = false, Description = "Operation mode: CrawlAndProcess, CrawlOnly, ProcessOnly")]
        public OperationMode? OpMode = null;

        [CommandLineArg(Name = "ll", Required = false, Description = "Log level: DEBUG, INFO, ERROR")]
        public LogLevel? LogLevel = null;

        [CommandLineArg(Name = "?", Required = false, Description = "Displays this help information.")]
        public bool ShowHelp = false;


        public override string ToString()
        {
            List<string> ts = (from p in typeof(CommandLineArguments).GetFields(BindingFlags.Instance | BindingFlags.Public)
                               from CommandLineArgAttribute attr in p.GetCustomAttributes(typeof(CommandLineArgAttribute), true)
                               select string.Format("{0}={1}", attr.Name, p.GetValue(this))).ToList();
            return string.Join("\n", ts);
        }
    }
}
