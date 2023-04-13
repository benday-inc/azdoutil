using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.CommandsFramework;

class Program
{
    static void Main(string[] args)
    {
        var util = new CommandAttributeUtility();

        if (args.Length == 0)
        {
            DisplayUsage(util);
        }
        else
        {
            try
            {
                var names = util.GetAvailableCommandNames(typeof(StringUtility).Assembly);

                if (names.Contains(args[0]) == false)
                {
                    throw new KnownException(
                            $"Invalid command name '{args[0]}'.");
                }

                var command = util.GetCommand(args, typeof(StringUtility).Assembly);

                if (command == null)
                {
                    DisplayUsage(util);
                }
                else
                {
                    var attr = util.GetCommandAttributeForCommandName(typeof(StringUtility).Assembly,
                        command.ExecutionInfo.CommandName);

                    if (attr == null)
                    {
                        throw new InvalidOperationException(
                            $"Could not get command attribute for command name '{command.ExecutionInfo.CommandName}'.");
                    }
                    else
                    {
                        if (attr.IsAsync == false)
                        {
                            var runThis = command as ISynchronousCommand;

                            if (runThis == null)
                            {
                                throw new InvalidOperationException($"Could not convert type to {typeof(ISynchronousCommand)}.");
                            }
                            else
                            {
                                runThis.Execute();
                            }
                        }
                        else
                        {
                            var runThis = command as IAsyncCommand;

                            if (runThis == null)
                            {
                                throw new InvalidOperationException($"Could not convert type to {typeof(IAsyncCommand)}.");
                            }
                            else
                            {
                                var temp = runThis.ExecuteAsync().GetAwaiter();

                                temp.GetResult();
                            }
                        }
                    }
                }
            }
            catch (KnownException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }

    private static void DisplayUsage(CommandAttributeUtility util)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        
        Console.WriteLine($"Azure DevOps Utilities");
        Console.WriteLine($"https://www.benday.com");
        Console.WriteLine();
        Console.WriteLine($"v{versionInfo.FileVersion}");
        Console.WriteLine();
        Console.WriteLine($"Available commands:");

        var assembly = typeof(StringUtility).Assembly;

        var commands = util.GetAvailableCommandAttributes(assembly);

        var longestName = commands.Max(x => x.Name.Length);

        var consoleWidth = Console.WindowWidth;
        var separator = " - ";
        int commandNameColumnWidth = (longestName + separator.Length);

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            Console.Write(LineWrapUtilities.GetValueWithPadding(command.Name, longestName));
            Console.Write(separator);

            Console.WriteLine(
                LineWrapUtilities.WrapValue(commandNameColumnWidth,
                consoleWidth, command.Description));
        }
    }    
}