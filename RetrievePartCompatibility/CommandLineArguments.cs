using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RetrievePartCompatibility
{
    [Obfuscation(Exclude = true)]
    public class CommandLineArguments
    {
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
