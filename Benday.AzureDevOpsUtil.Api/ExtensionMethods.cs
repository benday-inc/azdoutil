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

    // GetCloneOfArguments() used to be defined here as well as in the framework, with the
    // same signature. Inside this namespace azdoutil's copy won by proximity and nobody
    // noticed, but any other assembly importing both namespaces got an ambiguous call. The
    // framework's copy is also the better one -- it builds the clone with
    // ArgumentCollection.ArgumentNameComparer, so the clone keeps matching argument names
    // without regard to case.

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
