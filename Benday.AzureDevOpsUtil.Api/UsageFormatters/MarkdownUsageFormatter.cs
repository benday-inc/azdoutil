using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.UsageFormatters;
public class MarkdownUsageFormatter
{
    public string Format(List<CommandInfo> usages)
    {
        var builder = new StringBuilder();

        foreach (var usage in usages)
        {
            AppendUsage(builder, usage);
        }

        return builder.ToString();
    }

    private void AppendUsage(StringBuilder builder, CommandInfo usage)
    {
        builder.AppendLine($"## {usage.Name}");
        builder.AppendLine($"**{usage.Description}**");
        
        builder.AppendLine("### Arguments");

        builder.AppendLine("| Argument | Is Optional | Data Type | Description |");

        foreach (var arg in usage.Arguments)
        {
            builder.Append("| ");
            builder.Append(arg.Name);
            builder.Append(" | ");

            if (arg.IsRequired == true)
            {
                builder.Append("Required");
                builder.Append(" | ");
            }
            else
            {
                builder.Append("Optional");
                builder.Append(" | ");
            }

            builder.Append(arg.DataType);
            builder.Append(" | ");

            if (string.IsNullOrEmpty(arg.Description) == false)
            {
                builder.Append(arg.Description);
            }
            else
            {
                builder.Append(string.Empty);
            }

            builder.Append(" |");

            builder.AppendLine();
        }
    }
}
