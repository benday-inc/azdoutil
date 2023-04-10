using System.Text;

using Benday.CommandsFramework;

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
        builder.Append(",");
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
