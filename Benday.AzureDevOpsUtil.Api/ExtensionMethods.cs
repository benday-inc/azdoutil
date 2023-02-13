using Benday.CommandsFramework;
using OfficeOpenXml.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;
public static class ExtensionMethods
{
    public static CommandExecutionInfo GetCloneOfArguments(
        this CommandExecutionInfo execInfo, string commandName, bool quietMode)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        var argsClone = execInfo.Arguments.ToDictionary(entry => entry.Key, entry => entry.Value);
                
        if (quietMode == true)
        {
            argsClone.TryAdd(Constants.ArgumentNameQuietMode, "true");
        }

        var returnValue = new CommandExecutionInfo();
        returnValue.Arguments= argsClone;
        returnValue.CommandName = commandName;

        return returnValue;
    }

    public static void AddArgumentValue(
        this CommandExecutionInfo execInfo, string argName, string argValue)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        if (execInfo.Arguments.ContainsKey(argName) == true)
        {
            execInfo.Arguments.Remove(argName);
        }

        execInfo.Arguments.Add(argName, argValue);
    }

    public static void RemoveArgumentValue(
        this CommandExecutionInfo execInfo, string argName)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        if (execInfo.Arguments.ContainsKey(argName) == true)
        {
            execInfo.Arguments.Remove(argName);
        }
    }

    public static DateTime GetDateTimeValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return DateTime.MinValue;
        }
        else
        {
            var argAsDateTime = args[argumentName] as DateTimeArgument;

            if (argAsDateTime == null)
            {
                throw new InvalidOperationException($"Cannot get as datetime arg '{argumentName}'.");
            }
            else
            {
                if (argAsDateTime.HasValue == false)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return argAsDateTime.ValueAsDateTime;
                }
            }
        }
    }

    public static string GetStringValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return string.Empty;
        }
        else
        {
            return args[argumentName].Value;
        }
    }

    public static bool GetBooleanValue(
        this ArgumentCollection args, string argumentName)
    {
        if (args.ContainsKey(argumentName) == false)
        {
            return false;
        }
        else
        {
            var argAsBool = args[argumentName] as BooleanArgument;

            if (argAsBool == null)
            {
                throw new InvalidOperationException($"Cannot call ArgumentBooleanValue() on non-boolean arg '{argumentName}'.");
            }
            else
            {
                if (argAsBool.HasValue == false)
                {
                    return false;
                }
                else
                {
                    return argAsBool.ValueAsBoolean;
                }
            }
        }
    }
}
