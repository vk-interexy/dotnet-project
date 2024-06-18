using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GenerateFullOutput
{
    internal class CommandLineArgAttribute : Attribute
    {
        public string Name;
        public bool Required;
        public string Description;
    }

    internal class CommandLinePropAttr
    {
        public FieldInfo Property;
        public CommandLineArgAttribute Attribute;
    }

    internal class CommandLineParser
    {
        private static IEnumerable<CommandLinePropAttr> getCommandLineOptions()
        {
            return from p in typeof(CommandLineArguments).GetFields(BindingFlags.Instance | BindingFlags.Public)
                   let attr = p.GetCustomAttributes(typeof(CommandLineArgAttribute), true)
                   where attr.Length != 0
                   select new CommandLinePropAttr { Property = p, Attribute = attr.First() as CommandLineArgAttribute };
        }

        public static void ShowHelp()
        {
            CommandLineArguments defaultArgs = new CommandLineArguments();
            var options = getCommandLineOptions();
            List<string> argDescriptions = options.Select(option =>
            {
                bool isFlag = option.Property.FieldType.Name == "Boolean";
                if (isFlag)
                {
                    string name = !string.IsNullOrWhiteSpace(option.Attribute.Name) ? $"/{option.Attribute.Name}" : null;
                    return string.Format("\t{0} ({2})\n\t\t{1}\n", name, option.Attribute.Description.Replace("\n", "\n\t\t"), option.Attribute.Required ? "required" : "optional");
                }
                else
                {
                    string name = !string.IsNullOrWhiteSpace(option.Attribute.Name) ? $"-{option.Attribute.Name}" : null;
                    return string.Format("\t{0} ({3}) [{2}]\n\t\t{1}\n", name, option.Attribute.Description.Replace("\n", "\n\t\t"), option.Property.GetValue(defaultArgs), option.Attribute.Required ? "required" : "optional");
                }
            }).ToList();

            Console.Out.WriteLine(string.Format("Useage:\n{0}", string.Join("\n", argDescriptions)));
        }

        public static CommandLineArguments Parse(string[] args)
        {
            var options = getCommandLineOptions();

            CommandLineArguments commandLineArgs = new CommandLineArguments();
            if (args.Length == 0)
            {
                if (options.Any(o => o.Attribute.Required))
                {
                    return null;
                }
                else
                {
                    return commandLineArgs;
                }
            }

            Dictionary<string, bool> requiredArgsSet = options.Where(o => o.Attribute.Required).ToDictionary(kv => kv.Attribute.Name, kv => false);

            for (int i = 0; i < args.Length; i++)
            {
                bool valid = false;
                if (args[i] == "?" || args[i] == "-?" || args[i] == "/?")
                {
                    valid = true;
                    commandLineArgs = new CommandLineArguments();
                    commandLineArgs.ShowHelp = true;
                    return commandLineArgs;
                }
                else if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    string name = args[i].Remove(0, 1); //remove the - or /
                    ++i;
                    if (i >= args.Length) { continue; }
                    string value = args[i];

                    foreach (var prop in options)
                    {
                        if ((prop.Attribute.Name != name))
                        {
                            continue;
                        }
                        TypeConverter tc = TypeDescriptor.GetConverter(prop.Property.FieldType);
                        if (tc != null)
                        {
                            try
                            {
                                if (args[i].StartsWith("/"))
                                {
                                    prop.Property.SetValue(commandLineArgs, true);
                                }
                                else
                                {
                                    prop.Property.SetValue(commandLineArgs, tc.ConvertFromString(value));
                                }

                                if (requiredArgsSet.ContainsKey(prop.Attribute.Name))
                                {
                                    requiredArgsSet[prop.Attribute.Name] = true;
                                }
                                valid = true;
                            }
                            catch (Exception ex)
                            {
                                Console.Out.WriteLine(string.Format("Error while parsing command line argument {0}={1}: {2}", args[i], args[i + 1], ex.Message));
                            }
                        }
                        break;
                    }
                }

                if (valid)
                {
                    continue;
                }

                Console.Out.WriteLine(string.Format("Unable to parse command line at argument {0}.", args[i]));
                return null;
            }

            if (requiredArgsSet.Any(o => !o.Value))
            {
                Console.Out.WriteLine("Missing required parameter");
                return null;
            }

            return commandLineArgs;
        }
    }
}
