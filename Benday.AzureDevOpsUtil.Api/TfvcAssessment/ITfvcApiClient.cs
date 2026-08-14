using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

public enum TfvcRecursionLevel
{
    None,
    OneLevel,
    Full
}

/// <summary>
/// The read-only TFVC calls the assessment needs.  Services depend on this
/// rather than on HTTP so they can be exercised against canned payloads.
/// </summary>
public interface ITfvcApiClient
{
    Task<IReadOnlyList<TfvcBranchInfo>> GetBranchesAsync(string projectName);

    Task<IReadOnlyList<TfvcItemInfo>> GetItemsAsync(
        string projectName, string scopePath, TfvcRecursionLevel recursionLevel);

    /// <summary>
    /// Changesets touching <paramref name="itemPath"/>, newest first.  The API
    /// returns no total count, so callers cap the result with
    /// <paramref name="maxResults"/> and treat a full page as "at least this many".
    /// </summary>
    Task<IReadOnlyList<TfvcChangesetInfo>> GetChangesetsAsync(
        string projectName, string itemPath, DateTime? fromDateUtc, int maxResults);

    /// <summary>
    /// The contents of a single file, or null when it could not be read.
    /// </summary>
    Task<string?> GetFileContentAsync(string projectName, string path);
}
