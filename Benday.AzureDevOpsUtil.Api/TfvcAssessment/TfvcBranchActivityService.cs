namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// A branch path to measure, and whether TFVC has it registered as a branch.
/// </summary>
public class BranchCandidate
{
    public BranchCandidate()
    {
    }

    public BranchCandidate(string path, bool isRegisteredBranch)
    {
        Path = path;
        IsRegisteredBranch = isRegisteredBranch;
    }

    public string Path { get; set; } = string.Empty;

    public bool IsRegisteredBranch { get; set; }
}

/// <summary>
/// Counts recent changesets per branch so the report can say which branches are
/// still being worked in.
///
/// The changesets endpoint returns no total count, so counts come from walking
/// the changesets in the window.  The walk is capped, and a capped branch is
/// marked so the report can present its numbers as a floor.
/// </summary>
public class TfvcBranchActivityService
{
    public const int DefaultMaxChangesetsPerBranch = 500;

    private const int ActiveWindowDays = 90;
    private const int MidWindowDays = 180;
    private const int LongWindowDays = 365;

    public async Task<List<BranchActivity>> AnalyzeAsync(
        ITfvcApiClient client,
        string projectName,
        IEnumerable<BranchCandidate> branches,
        DateTime utcNow,
        int maxChangesetsPerBranch = DefaultMaxChangesetsPerBranch)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(branches);

        var results = new List<BranchActivity>();

        foreach (var branch in branches)
        {
            results.Add(await AnalyzeBranchAsync(
                client, projectName, branch, utcNow, maxChangesetsPerBranch));
        }

        return results;
    }

    private async Task<BranchActivity> AnalyzeBranchAsync(
        ITfvcApiClient client,
        string projectName,
        BranchCandidate branch,
        DateTime utcNow,
        int maxChangesetsPerBranch)
    {
        var path = TfvcPath.Normalize(branch.Path);

        var activity = new BranchActivity
        {
            Path = path,
            IsRegisteredBranch = branch.IsRegisteredBranch
        };

        var windowStart = utcNow.AddDays(-LongWindowDays);

        var changesets = await client.GetChangesetsAsync(
            projectName, path, windowStart, maxChangesetsPerBranch);

        if (changesets.Count == 0)
        {
            // Nothing in the last year.  One more call establishes whether the
            // branch has any history at all, so the table can show a real date
            // instead of a blank.
            var lastEver = await client.GetChangesetsAsync(projectName, path, null, 1);

            if (lastEver.Count > 0)
            {
                var newest = lastEver.OrderByDescending(x => x.CreatedDate).First();

                activity.LastChangesetDate = newest.CreatedDate;
                activity.LastChangesetAuthor = newest.AuthorDisplayName;
            }

            activity.Classification = BranchActivityClassification.Dead;

            return activity;
        }

        activity.CountsAreCapped = changesets.Count >= maxChangesetsPerBranch;

        var mostRecent = changesets.OrderByDescending(x => x.CreatedDate).First();

        activity.LastChangesetDate = mostRecent.CreatedDate;
        activity.LastChangesetAuthor = mostRecent.AuthorDisplayName;

        activity.ChangesetsLast90Days =
            changesets.Count(x => x.CreatedDate >= utcNow.AddDays(-ActiveWindowDays));

        activity.ChangesetsLast180Days =
            changesets.Count(x => x.CreatedDate >= utcNow.AddDays(-MidWindowDays));

        activity.ChangesetsLast365Days =
            changesets.Count(x => x.CreatedDate >= windowStart);

        activity.Classification = Classify(activity);

        return activity;
    }

    public static BranchActivityClassification Classify(BranchActivity activity)
    {
        if (activity.ChangesetsLast90Days > 0)
        {
            return BranchActivityClassification.Active;
        }

        if (activity.ChangesetsLast365Days > 0)
        {
            return BranchActivityClassification.Cooling;
        }

        return BranchActivityClassification.Dead;
    }
}
