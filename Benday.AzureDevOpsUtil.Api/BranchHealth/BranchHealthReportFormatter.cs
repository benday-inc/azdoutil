using System.Globalization;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api.BranchHealth;

/// <summary>
/// Renders a branch survey as markdown.  States facts and consequences; does
/// not rank them or suggest what to do about them.
/// </summary>
public class BranchHealthReportFormatter
{
    public const string FooterLine =
        "Learn more: https://www.benday.com/blog/case-study-04-ninety-three-things";

    private const string DateFormat = "yyyy-MM-dd";

    public string FormatReport(BranchHealthResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var sb = new StringBuilder();

        sb.AppendLine("# Branch Health");
        sb.AppendLine();
        sb.AppendLine($"- Team project: {result.ProjectName}");
        sb.AppendLine($"- Repository: {result.RepositoryName}");
        sb.AppendLine(
            $"- Generated: {result.GeneratedUtc.ToString(DateFormat, CultureInfo.InvariantCulture)} UTC");

        FormatSummary(sb, result);
        FormatHeadline(sb, result);
        FormatBranches(sb, result);
        FormatCommitters(sb, result);
        FormatNotes(sb, result);

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(FooterLine);

        return sb.ToString();
    }

    private void FormatSummary(StringBuilder sb, BranchHealthResult result)
    {
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Branches | {result.BranchCount} |");
        sb.AppendLine(
            $"| Received commits in the last {result.ActivityWindowDays} days " +
            $"| {result.ActiveBranchCount} |");
        sb.AppendLine(
            "| Received commits in the last 30 days " +
            $"| {result.ActiveBranchCountLast30Days} |");
        sb.AppendLine($"| Not merged to the default branch | {result.UnmergedBranchCount} |");
        sb.AppendLine(
            "| Median age of the unmerged branches | " +
            $"{FormatDays(result.MedianUnmergedBranchAgeInDays)} |");
        sb.AppendLine(
            "| Oldest unmerged branch | " +
            $"{FormatOldest(result.OldestUnmergedBranch)} |");
        sb.AppendLine($"| No commits in a year | {result.DeadBranchCount} |");
    }

    private void FormatHeadline(StringBuilder sb, BranchHealthResult result)
    {
        if (result.BranchCount == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine(
            $"In the last {result.ActivityWindowDays} days, {result.ActiveBranchCount} " +
            $"branch(es) received commits in {result.RepositoryName}. Each active branch is a " +
            "separate piece of work in progress.");

        if (result.DeadBranchCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"{result.DeadBranchCount} branch(es) have had no commits in a year. Dead " +
                "branches sit alongside the branches that matter in every list of branches.");
        }
    }

    private void FormatBranches(StringBuilder sb, BranchHealthResult result)
    {
        if (result.Branches.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("## Branches");
        sb.AppendLine();
        sb.AppendLine("| Branch | Last commit | By | Age | Ahead | Behind |");
        sb.AppendLine("|---|---|---|---|---|---|");

        foreach (var branch in result.Branches
            .OrderByDescending(x => x.IsDefaultBranch)
            .ThenBy(x => x.AgeInDays ?? double.MaxValue))
        {
            var name = branch.IsDefaultBranch == true ? $"{branch.Name} (default)" : branch.Name;

            var lastCommit = branch.LastCommitDate.HasValue == true ?
                branch.LastCommitDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture) :
                "unknown";

            sb.AppendLine(
                $"| {name} | {lastCommit} | {branch.LastCommitBy} " +
                $"| {FormatDays(branch.AgeInDays)} | {branch.AheadCount} | {branch.BehindCount} |");
        }
    }

    private void FormatCommitters(StringBuilder sb, BranchHealthResult result)
    {
        var concentrated = result.Committers.Where(x => x.BranchCount > 1).ToList();

        if (concentrated.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine(
            $"## People with more than one active branch in the last " +
            $"{BranchHealthAnalyzer.CommitterWindowDays} days");
        sb.AppendLine();
        sb.AppendLine("| Person | Branches | Which |");
        sb.AppendLine("|---|---|---|");

        foreach (var committer in concentrated)
        {
            sb.AppendLine(
                $"| {committer.Name} | {committer.BranchCount} " +
                $"| {string.Join(", ", committer.BranchNames)} |");
        }

        sb.AppendLine();
        sb.AppendLine("These people are working on multiple things at once.");
    }

    private void FormatNotes(StringBuilder sb, BranchHealthResult result)
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

    private string FormatDays(double? days)
    {
        if (days.HasValue == false)
        {
            return "unknown";
        }

        return days.Value.ToString("0.#", CultureInfo.InvariantCulture) + " days";
    }

    private string FormatOldest(BranchInfo? branch)
    {
        if (branch == null)
        {
            return "none";
        }

        return $"{branch.Name} at {FormatDays(branch.AgeInDays)}";
    }

    /// <summary>
    /// One row per branch, for a spreadsheet.
    /// </summary>
    public string FormatCsv(BranchHealthResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var csvWriter = new Benday.CommandsFramework.DataFormatting.CsvWriter();

        csvWriter.AddColumns(
            "Project", "Repository", "Branch", "Is Default", "Last Commit", "Last Commit By",
            "Age In Days", "Ahead", "Behind", "Unmerged");

        foreach (var branch in result.Branches)
        {
            csvWriter.AddRow(
                result.ProjectName,
                result.RepositoryName,
                branch.Name,
                branch.IsDefaultBranch.ToString(),
                branch.LastCommitDate.HasValue == true ?
                    branch.LastCommitDate.Value.ToString(
                        DateFormat, CultureInfo.InvariantCulture) :
                    string.Empty,
                branch.LastCommitBy,
                branch.AgeInDays.HasValue == true ?
                    branch.AgeInDays.Value.ToString("0.#", CultureInfo.InvariantCulture) :
                    string.Empty,
                branch.AheadCount.ToString(CultureInfo.InvariantCulture),
                branch.BehindCount.ToString(CultureInfo.InvariantCulture),
                branch.IsUnmerged.ToString());
        }

        return csvWriter.ToCsvString();
    }
}
