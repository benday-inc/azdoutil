using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// In-memory stand-in for the TFVC API so the assessment services can be
/// exercised against known payloads.
/// </summary>
public class FakeTfvcApiClient : ITfvcApiClient
{
    public List<TfvcBranchInfo> Branches { get; } = new();

    public Dictionary<string, List<TfvcItemInfo>> ItemsByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Recursive listings, kept separate from the one-level ones so a test that
    /// only sets up a folder walk does not accidentally also feed the content
    /// scan.
    /// </summary>
    public Dictionary<string, List<TfvcItemInfo>> FullItemsByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, List<TfvcChangesetInfo>> ChangesetsByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every scope path that was asked for, in order.</summary>
    public List<string> ItemRequests { get; } = new();

    public List<string> ChangesetRequests { get; } = new();

    public static TfvcItemInfo FolderItem(string path, bool isBranch = false)
    {
        return new TfvcItemInfo
        {
            Path = path,
            IsFolder = true,
            IsBranch = isBranch
        };
    }

    public static TfvcItemInfo FileItem(string path, long size = 0)
    {
        return new TfvcItemInfo
        {
            Path = path,
            IsFolder = false,
            Size = size
        };
    }

    /// <summary>
    /// Registers the one-level listing for a folder.  The real API includes the
    /// folder that was asked for in its own listing, so this does too.
    /// </summary>
    public void SetChildren(string parentPath, params TfvcItemInfo[] children)
    {
        var items = new List<TfvcItemInfo>
        {
            FolderItem(parentPath)
        };

        items.AddRange(children);

        ItemsByPath[parentPath] = items;
    }

    /// <summary>
    /// Convenience for the common case: a folder whose children are all folders.
    /// </summary>
    public void SetChildFolders(string parentPath, params string[] childPaths)
    {
        SetChildren(parentPath, childPaths.Select(x => FolderItem(x)).ToArray());
    }

    /// <summary>
    /// Registers what a recursive listing returns for a scope path.
    /// </summary>
    public void SetFullListing(string scopePath, params TfvcItemInfo[] items)
    {
        FullItemsByPath[scopePath] = items.ToList();
    }

    public Dictionary<string, string> FileContentByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<string> FileContentRequests { get; } = new();

    public void SetFileContent(string path, string content)
    {
        FileContentByPath[path] = content;
    }

    public Task<string?> GetFileContentAsync(string projectName, string path)
    {
        FileContentRequests.Add(path);

        if (FileContentByPath.TryGetValue(path, out var content) == true)
        {
            return Task.FromResult<string?>(content);
        }

        return Task.FromResult<string?>(null);
    }

    public void SetChangesets(string itemPath, params TfvcChangesetInfo[] changesets)
    {
        ChangesetsByPath[itemPath] = changesets.ToList();
    }

    public static TfvcChangesetInfo Changeset(int id, DateTime createdDate, string author = "Ann Dev")
    {
        return new TfvcChangesetInfo
        {
            ChangesetId = id,
            CreatedDate = createdDate,
            Author = new TfvcIdentityRef { DisplayName = author }
        };
    }

    public Task<IReadOnlyList<TfvcBranchInfo>> GetBranchesAsync(string projectName)
    {
        return Task.FromResult<IReadOnlyList<TfvcBranchInfo>>(Branches);
    }

    public Task<IReadOnlyList<TfvcItemInfo>> GetItemsAsync(
        string projectName, string scopePath, TfvcRecursionLevel recursionLevel)
    {
        ItemRequests.Add(scopePath);

        var source = recursionLevel == TfvcRecursionLevel.Full ? FullItemsByPath : ItemsByPath;

        if (source.TryGetValue(scopePath, out var items) == true)
        {
            return Task.FromResult<IReadOnlyList<TfvcItemInfo>>(items);
        }

        return Task.FromResult<IReadOnlyList<TfvcItemInfo>>(Array.Empty<TfvcItemInfo>());
    }

    public Task<IReadOnlyList<TfvcChangesetInfo>> GetChangesetsAsync(
        string projectName, string itemPath, DateTime? fromDateUtc, int maxResults)
    {
        ChangesetRequests.Add(itemPath);

        if (ChangesetsByPath.TryGetValue(itemPath, out var changesets) == false)
        {
            return Task.FromResult<IReadOnlyList<TfvcChangesetInfo>>(
                Array.Empty<TfvcChangesetInfo>());
        }

        var matches = changesets.AsEnumerable();

        if (fromDateUtc.HasValue == true)
        {
            matches = matches.Where(x => x.CreatedDate >= fromDateUtc.Value);
        }

        var results = matches
            .OrderByDescending(x => x.CreatedDate)
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<TfvcChangesetInfo>>(results);
    }
}
