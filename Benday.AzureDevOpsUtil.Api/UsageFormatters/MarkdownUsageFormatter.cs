﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.UsageFormatters;
public class MarkdownUsageFormatter
{
    public string Format(List<CommandInfo> usages, bool skipCommandAnchors)
    {
        var builder = new StringBuilder();

        var categories = usages.Select(x => x.Category).Distinct().Order();

        AppendCommandList(usages.OrderBy(x => x.Category).ThenBy(x => x.Name).ToList(), builder, skipCommandAnchors);

        foreach (var category in categories)
        {
            builder.AppendLine($"# {category}");

            foreach (var usage in usages.Where(x => x.Category == category).OrderBy(x => x.Name))
            {
                AppendUsage(builder, usage, skipCommandAnchors);
            }
        }

        return builder.ToString();
    }

    private void AppendCommandList(List<CommandInfo> usages, StringBuilder builder, bool skipCommandAnchors)
    {
        builder.AppendLine($"## Commands");

        builder.AppendLine("| Category | Command Name | Description |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var usage in usages)
        {
            if (skipCommandAnchors)
            {
                builder.AppendLine($"| {usage.Category} | {usage.Name} | {usage.Description} |");
            }
            else
            {
                builder.AppendLine($"| {usage.Category} | [{usage.Name}](#{usage.Name}) | {usage.Description} |");
            }
        }
    }

    private void AppendUsage(StringBuilder builder, CommandInfo usage, bool skipCommandAnchors)
    {
        if (!skipCommandAnchors)
        {
            builder.AppendLine($"## <a name=\"{usage.Name}\"></a> {usage.Name}");
        }
        else
        {
            builder.AppendLine($"## {usage.Name}");
        }

        builder.AppendLine($"**{usage.Description}**");

        builder.AppendLine("### Arguments");

        builder.AppendLine("| Argument | Is Optional | Data Type | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");

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
