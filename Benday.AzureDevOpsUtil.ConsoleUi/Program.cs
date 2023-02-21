﻿using System.Text;

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
        Console.WriteLine($"Azure DevOps Utilities");
        Console.WriteLine($"https://www.benday.com");
        Console.WriteLine();
        Console.WriteLine($"Available commands:");

        var assembly = typeof(StringUtility).Assembly;

        var commands = util.GetAvailableCommandAttributes(assembly);

        var longestName = commands.Max(x => x.Name.Length);

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            Console.Write(GetNameWithPadding(command.Name, longestName));
            Console.WriteLine($" - {command.Description}");
        }
    }

    private static string GetNameWithPadding(string name, int padToLength)
    {
        var builder = new StringBuilder();

        builder.Append(name);
        builder.Append(' ', padToLength - name.Length);

        return builder.ToString();
    }
}