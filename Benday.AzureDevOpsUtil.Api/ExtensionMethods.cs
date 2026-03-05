using System.Text;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

public static class ExtensionMethods
{
    /// <summary>
    /// Remove all arguments from the CommandExecutionInfo except for the common arguments (quiet mode and configuration name) and any additional arguments specified in the argumentNamesToKeep parameter.
    /// </summary>
    /// <param name="execInfo"></param>
    /// <param name="preserveCommonArguments"></param>
    /// <param name="argumentNamesToKeep"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void RemoveAllArgumentsExcept(
        this CommandExecutionInfo execInfo,
        bool preserveCommonArguments,
        params string[] argumentNamesToKeep)
    {
        if (execInfo is null || execInfo.Arguments is null)
        {
            throw new ArgumentNullException(nameof(execInfo));
        }

        var commonArguments = new List<string>()
        {
            Constants.ArgumentNameQuietMode, 
            Constants.ArgumentNameConfigurationName 
        };

        var keysToRemove = new List<string>();

        foreach (var key in execInfo.Arguments.Keys)
        {
            if (preserveCommonArguments == true &&
                commonArguments.Contains(key,
                StringComparer.CurrentCultureIgnoreCase))
            {
                continue;
            }
            else if (argumentNamesToKeep != null &&
                argumentNamesToKeep.Contains(key, StringComparer.CurrentCultureIgnoreCase))
            {
                continue;
            }
            else
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            execInfo.RemoveArgumentValue(key);
        }
    }

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
        returnValue.Arguments = argsClone;
        returnValue.CommandName = commandName;

        return returnValue;
    }

    public static void AppendLabeledValue(this StringBuilder builder, string label, string value)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(value);
    }

    public static void AppendLabeledValue(this StringBuilder builder, string label, int value)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
        builder.AppendLine();
    }

    public static void AppendLabeledValue(this StringBuilder builder, string label, DateTime value)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
        builder.AppendLine();
    }

    public static void AppendCsv(this StringBuilder builder, string label, string value)
    {
        builder.Append(value);
        builder.Append(',');
    }

    public static void AppendCsvHeader(this StringBuilder builder, string label)
    {
        builder.Append(label);
        builder.Append(',');
    }

    public static void AppendCsv(this StringBuilder builder, string label, int value)
    {
        builder.Append(value);
        builder.Append(',');
    }

    public static void AppendCsv(this StringBuilder builder, string label, DateTime value)
    {
        builder.Append(value);
        builder.Append(',');
    }
}
