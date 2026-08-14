using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.BranchHealth;

/// <summary>
/// Turns a repository's branch statistics into the numbers that describe how
/// much work is in flight at once.
///
/// This does no I/O: one call to the branch stats endpoint covers every branch,
/// so the caller fetches and this does the arithmetic.
/// </summary>
public class BranchHealthAnalyzer
{
    public const int DefaultActivityWindowDays = 7;

    public const int SecondaryWindowDays = 30;

    public const int DeadAfterDays = 365;

    /// <summary>
    /// How recently somebody has to have committed for the report to count them
    /// as working on a branch right now.
    /// </summary>
    public const int CommitterWindowDays = 14;

    public BranchHealthResult Analyze(
        IReadOnlyList<GitBranchStatsInfo>? stats,
        string projectName,
        string repositoryName,
        DateTime utcNow,
        int activityWindowDays = DefaultActivityWindowDays)
    {
        var result = new BranchHealthResult
        {
            ProjectName = projectName,
            RepositoryName = repositoryName,
            GeneratedUtc = utcNow,
            ActivityWindowDays = activityWindowDays
        };

        if (stats == null || stats.Count == 0)
        {
            result.Notes.Add(
                "No branches were returned for this repository. An empty repository has none.");

            return result;
        }

        foreach (var item in stats)
        {
            result.Branches.Add(ToBranchInfo(item, utcNow));
        }

        result.ActiveBranchCount = CountActive(result.Branches, activityWindowDays);
        result.ActiveBranchCountLast30Days = CountActive(result.Branches, SecondaryWindowDays);

        result.DeadBranchCount = result.Branches.Count(x =>
            x.AgeInDays.HasValue == true && x.AgeInDays.Value > DeadAfterDays);

        var unmerged = result.Branches
            .Where(x => x.IsDefaultBranch == false)
            .Where(x => x.IsUnmerged == true)
            .ToList();

        result.MedianUnmergedBranchAgeInDays = GetMedianAge(unmerged);

        result.OldestUnmergedBranch = unmerged
            .Where(x => x.AgeInDays.HasValue == true)
            .OrderByDescending(x => x.AgeInDays!.Value)
            .FirstOrDefault();

        result.Committers = GetCommitterActivity(result.Branches, utcNow);

        return result;
    }

    private BranchInfo ToBranchInfo(GitBranchStatsInfo stats, DateTime utcNow)
    {
        // The committer date is what says when the work landed on this branch.
        var date = stats.Commit?.Committer?.Date ?? stats.Commit?.Author?.Date;

        var info = new BranchInfo
        {
            Name = stats.Name,
            IsDefaultBranch = stats.IsBaseVersion,
            AheadCount = stats.AheadCount,
            BehindCount = stats.BehindCount,
            LastCommitDate = date,
            LastCommitBy = stats.Commit?.Committer?.Name ??
                stats.Commit?.Author?.Name ??
                string.Empty
        };

        if (date.HasValue == true)
        {
            info.AgeInDays = (utcNow - date.Value.ToUniversalTime()).TotalDays;
        }

        return info;
    }

    private int CountActive(List<BranchInfo> branches, int windowDays)
    {
        return branches.Count(x =>
            x.AgeInDays.HasValue == true && x.AgeInDays.Value <= windowDays);
    }

    /// <summary>
    /// The median rather than the mean, because one abandoned branch from three
    /// years ago drags an average somewhere no branch actually is.
    /// </summary>
    public static double? GetMedianAge(IReadOnlyList<BranchInfo> branches)
    {
        var ages = branches
            .Where(x => x.AgeInDays.HasValue == true)
            .Select(x => x.AgeInDays!.Value)
            .OrderBy(x => x)
            .ToList();

        if (ages.Count == 0)
        {
            return null;
        }

        var middle = ages.Count / 2;

        if (ages.Count % 2 == 1)
        {
            return ages[middle];
        }

        return (ages[middle - 1] + ages[middle]) / 2d;
    }

    private List<CommitterActivity> GetCommitterActivity(
        List<BranchInfo> branches, DateTime utcNow)
    {
        var byCommitter = new Dictionary<string, CommitterActivity>(StringComparer.OrdinalIgnoreCase);

        foreach (var branch in branches)
        {
            if (branch.IsDefaultBranch == true)
            {
                continue;
            }

            if (branch.AgeInDays.HasValue == false ||
                branch.AgeInDays.Value > CommitterWindowDays)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(branch.LastCommitBy) == true)
            {
                continue;
            }

            if (byCommitter.TryGetValue(branch.LastCommitBy, out var committer) == false)
            {
                committer = new CommitterActivity { Name = branch.LastCommitBy };
                byCommitter[branch.LastCommitBy] = committer;
            }

            committer.BranchNames.Add(branch.Name);
        }

        return byCommitter.Values
            .OrderByDescending(x => x.BranchCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
