using System.Text;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

public static class ExtensionMethods
{
    // RemoveAllArgumentsExcept() used to live here, for a time when running another command
    // meant cloning this command's whole command line and then deleting the arguments that
    // did not belong to the command being run. Commands now name what they pass, so there
    // is nothing to delete -- see AzureDevOpsCommandBase.CreateAzdoCommand().

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
