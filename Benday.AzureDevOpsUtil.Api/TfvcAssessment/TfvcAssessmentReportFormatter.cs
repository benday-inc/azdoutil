using System.Globalization;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Renders an assessment as a markdown report.  The formatter states facts and
/// consequences; it does not rank them or suggest what to do about them.
/// </summary>
public class TfvcAssessmentReportFormatter
{
    public const string FooterLine =
        "Learn more about TFVC-to-Git migration: " +
        "https://www.benday.com/blog/migrating-tfvc-to-git-its-harder-than-you-think";

    private const string DateFormat = "yyyy-MM-dd";

    public string FormatReport(TfvcAssessmentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var sb = new StringBuilder();

        FormatHeader(sb, result);
        FormatSummary(sb, result);
        FormatBranchHierarchy(sb, result);
        FormatNestedBranches(sb, result);
        FormatUnregisteredBranches(sb, result);
        FormatBranchActivity(sb, result);
        FormatBuildDefinitions(sb, result);
        FormatFindings(sb, result);
        FormatNotes(sb, result);
        FormatFooter(sb);

        return sb.ToString();
    }

    private void FormatHeader(StringBuilder sb, TfvcAssessmentResult result)
    {
        sb.AppendLine("# TFVC Migration Assessment");
        sb.AppendLine();
        sb.AppendLine($"- Team project: {result.ProjectName}");
        sb.AppendLine($"- Path: {result.ScopePath}");
        sb.AppendLine(
            $"- Generated: {result.GeneratedUtc.ToString(DateFormat, CultureInfo.InvariantCulture)} UTC");
    }

    private void FormatSummary(StringBuilder sb, TfvcAssessmentResult result)
    {
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Registered branches | {result.RegisteredBranchPaths.Count} |");
        sb.AppendLine($"| Nested branches | {result.NestedBranches.Count} |");
        sb.AppendLine(
            $"| Folder groups that look like unregistered branches | {result.UnregisteredBranchGroups.Count} |");
        sb.AppendLine($"| Branches with changes in the last 90 days | {result.ActiveBranchCount} |");
        sb.AppendLine($"| Branches with changes only in the last 365 days | {result.CoolingBranchCount} |");
        sb.AppendLine($"| Branches with no changes in 365 days | {result.DeadBranchCount} |");
        sb.AppendLine(
            $"| Build definitions pulling from TFVC | {result.TfvcBuildDefinitions.Count} |");
        sb.AppendLine(
            $"| Build definitions mapping more than one path | {result.ComplexBuildDefinitionCount} |");
        sb.AppendLine(
            "| Paths mapped by more than one build | " +
            $"{result.MappedPathUsages.Count(x => x.DefinitionCount > 1)} |");
    }

    private void FormatBranchHierarchy(StringBuilder sb, TfvcAssessmentResult result)
    {
        sb.AppendLine();
        sb.AppendLine("## Registered branches");
        sb.AppendLine();

        if (result.RegisteredBranchRoots.Count == 0)
        {
            sb.AppendLine(
                $"No TFVC folders under {result.ScopePath} are registered as branches.");

            return;
        }

        sb.AppendLine("```");

        foreach (var root in result.RegisteredBranchRoots)
        {
            AppendTreeLines(sb, root, 0);
        }

        sb.AppendLine("```");

        sb.AppendLine();
        sb.AppendLine(FormatMermaidDiagram(result.RegisteredBranchRoots));
    }

    private void AppendTreeLines(StringBuilder sb, TfvcBranchNode node, int depth)
    {
        var indent = new string(' ', depth * 2);

        sb.AppendLine($"{indent}{node.Path}");

        foreach (var child in node.Children)
        {
            AppendTreeLines(sb, child, depth + 1);
        }
    }

    /// <summary>
    /// Renders branch lineage as a Mermaid diagram.  Node ids are generated
    /// rather than derived from paths, because TFVC paths contain characters
    /// that Mermaid treats as syntax.
    /// </summary>
    public string FormatMermaidDiagram(List<TfvcBranchNode> roots)
    {
        var sb = new StringBuilder();

        sb.AppendLine("```mermaid");
        sb.AppendLine("graph TD");

        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nextId = 0;

        foreach (var root in roots)
        {
            AppendMermaidNodes(sb, root, ids, ref nextId);
        }

        foreach (var root in roots)
        {
            AppendMermaidEdges(sb, root, ids);
        }

        sb.Append("```");

        return sb.ToString();
    }

    private void AppendMermaidNodes(
        StringBuilder sb, TfvcBranchNode node, Dictionary<string, string> ids, ref int nextId)
    {
        if (ids.ContainsKey(node.Path) == false)
        {
            var id = "B" + nextId.ToString(CultureInfo.InvariantCulture);
            nextId++;

            ids[node.Path] = id;

            sb.AppendLine($"    {id}[\"{node.Path}\"]");
        }

        foreach (var child in node.Children)
        {
            AppendMermaidNodes(sb, child, ids, ref nextId);
        }
    }

    private void AppendMermaidEdges(
        StringBuilder sb, TfvcBranchNode node, Dictionary<string, string> ids)
    {
        foreach (var child in node.Children)
        {
            sb.AppendLine($"    {ids[node.Path]} --> {ids[child.Path]}");

            AppendMermaidEdges(sb, child, ids);
        }
    }

    private void FormatNestedBranches(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.NestedBranches.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## Nested branches");
        sb.AppendLine();
        sb.AppendLine("| Branch | Rooted inside |");
        sb.AppendLine("|---|---|");

        foreach (var pair in result.NestedBranches)
        {
            sb.AppendLine($"| {pair.ChildPath} | {pair.ParentPath} |");
        }
    }

    private void FormatUnregisteredBranches(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.UnregisteredBranchGroups.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## Folders that look like unregistered branches");
        sb.AppendLine();

        foreach (var group in result.UnregisteredBranchGroups)
        {
            sb.AppendLine($"Under {group.ParentPath}:");
            sb.AppendLine();

            foreach (var path in group.FolderPaths)
            {
                sb.AppendLine($"- {path}");
            }

            sb.AppendLine();
        }
    }

    private void FormatBranchActivity(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.BranchActivity.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## Branch activity");
        sb.AppendLine();
        sb.AppendLine("| Branch | Registered | Last change | Last changed by | 90d | 180d | 365d | State |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (var item in result.BranchActivity
            .OrderBy(x => x.Classification)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
        {
            var lastChange = item.LastChangesetDate.HasValue == true ?
                item.LastChangesetDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture) :
                "none found";

            sb.AppendLine(
                $"| {item.Path} " +
                $"| {(item.IsRegisteredBranch == true ? "yes" : "no")} " +
                $"| {lastChange} " +
                $"| {item.LastChangesetAuthor} " +
                $"| {FormatCount(item.ChangesetsLast90Days, item.CountsAreCapped)} " +
                $"| {FormatCount(item.ChangesetsLast180Days, item.CountsAreCapped)} " +
                $"| {FormatCount(item.ChangesetsLast365Days, item.CountsAreCapped)} " +
                $"| {item.Classification} |");
        }
    }

    private void FormatBuildDefinitions(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.TfvcBuildDefinitions.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## Build definitions that pull from TFVC");
        sb.AppendLine();
        sb.AppendLine("| Definition | Mapped paths | Workspace | Last run |");
        sb.AppendLine("|---|---|---|---|");

        foreach (var definition in result.TfvcBuildDefinitions
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var lastRun = definition.LastRunDate.HasValue == true ?
                definition.LastRunDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture) :
                "never run";

            if (definition.IsInactive == true && definition.LastRunDate.HasValue == true)
            {
                lastRun += " (inactive)";
            }

            var paths = definition.MappedPaths.Count == 0 ?
                "none recorded" :
                string.Join("<br>", definition.MappedPaths);

            var shape = definition.IsComplexMapping == true ? "complex" : "simple";

            sb.AppendLine(
                $"| {definition.Name} | {paths} | {shape} | {lastRun} |");
        }

        FormatComplexMappings(sb, result);
        FormatMappedPathFrequency(sb, result);
    }

    private void FormatComplexMappings(StringBuilder sb, TfvcAssessmentResult result)
    {
        var complex = result.TfvcBuildDefinitions
            .Where(x => x.IsComplexMapping == true)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (complex.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("### Workspaces built from more than one path");
        sb.AppendLine();

        foreach (var definition in complex)
        {
            sb.AppendLine($"{definition.Name}:");
            sb.AppendLine();

            foreach (var mapping in definition.Mappings)
            {
                var kind = mapping.IsCloak == true ? "cloak" : "map";

                sb.AppendLine($"- {kind}: {TfvcPath.Normalize(mapping.ServerPath)}");
            }

            sb.AppendLine();
        }
    }

    private void FormatMappedPathFrequency(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.MappedPathUsages.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("### How many builds map each path");
        sb.AppendLine();
        sb.AppendLine("| Path | Builds | Definitions |");
        sb.AppendLine("|---|---|---|");

        foreach (var usage in result.MappedPathUsages)
        {
            sb.AppendLine(
                $"| {usage.Path} | {usage.DefinitionCount} " +
                $"| {string.Join(", ", usage.DefinitionNames)} |");
        }
    }

    private string FormatCount(int count, bool isCapped)
    {
        var value = count.ToString(CultureInfo.InvariantCulture);

        return isCapped == true ? value + "+" : value;
    }

    private void FormatFindings(StringBuilder sb, TfvcAssessmentResult result)
    {
        sb.AppendLine();
        sb.AppendLine("## Findings");
        sb.AppendLine();

        if (result.Findings.Count == 0)
        {
            sb.AppendLine("Nothing to report for the sections that were run.");

            return;
        }

        foreach (var finding in result.Findings)
        {
            sb.AppendLine($"### {finding.Category}");
            sb.AppendLine();
            sb.AppendLine(finding.Fact);
            sb.AppendLine();
            sb.AppendLine(finding.Consequence);

            if (string.IsNullOrWhiteSpace(finding.Detail) == false)
            {
                sb.AppendLine();
                sb.AppendLine($"Detail: {finding.Detail}");
            }

            sb.AppendLine();
        }
    }

    private void FormatNotes(StringBuilder sb, TfvcAssessmentResult result)
    {
        if (result.Notes.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## What this scan did not cover");
        sb.AppendLine();

        foreach (var note in result.Notes)
        {
            sb.AppendLine($"- {note}");
        }
    }

    private void FormatFooter(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(FooterLine);
    }

    /// <summary>
    /// One row per finding, for a spreadsheet.
    /// </summary>
    public string FormatFindingsCsv(TfvcAssessmentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var csvWriter = new Benday.CommandsFramework.DataFormatting.CsvWriter();

        csvWriter.AddColumns("Category", "Fact", "Consequence", "Detail");

        foreach (var finding in result.Findings)
        {
            csvWriter.AddRow(
                finding.Category,
                finding.Fact,
                finding.Consequence,
                finding.Detail);
        }

        return csvWriter.ToCsvString();
    }
}
