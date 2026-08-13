using System.Text.RegularExpressions;

using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Looks for folders that are being used as branches without being registered
/// as branches in TFVC.  Two things have to be true before a set of folders is
/// reported: the names look like branch names, and the folders contain much the
/// same things as each other.
/// </summary>
public class TfvcFolderHeuristicScanner
{
    public const int DefaultMaxDepth = 3;

    /// <summary>
    /// Share of the smaller folder's child names that must also appear in the
    /// other folder before the two are treated as copies of one another.
    /// </summary>
    public const double SimilarityThreshold = 0.5d;

    /// <summary>
    /// Folder names that are branch names on their own.  Extend this list here;
    /// nothing else needs to change.
    /// </summary>
    public static readonly string[] BranchNames =
    {
        "main", "trunk", "dev", "development", "qa", "test",
        "stage", "staging", "prod", "production"
    };

    /// <summary>
    /// Folder names that are branch names when they start this way, such as
    /// "Release2019" or "Feature-Login".
    /// </summary>
    public static readonly string[] BranchNamePrefixes =
    {
        "release", "hotfix", "feature", "branch"
    };

    private static readonly Regex VersionLikeName = new(
        @"^[vr]?\d+(\.\d+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool LooksLikeBranchName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) == true)
        {
            return false;
        }

        var value = name.Trim();

        foreach (var branchName in BranchNames)
        {
            if (string.Equals(value, branchName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        foreach (var prefix in BranchNamePrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return VersionLikeName.IsMatch(value);
    }

    /// <summary>
    /// Walks down from the scope path looking for sibling folders that look like
    /// unregistered branch copies.
    /// </summary>
    /// <param name="registeredBranchPaths">
    /// Branch paths already known from the branches API.  These are skipped so
    /// the two sections do not report the same folders.
    /// </param>
    /// <param name="maxDepth">
    /// How many levels below the scope path to walk.  The cost that matters is
    /// response size on wide trees, not the number of calls.
    /// </param>
    public async Task<List<UnregisteredBranchGroup>> ScanAsync(
        ITfvcApiClient client,
        string projectName,
        string scopePath,
        IEnumerable<string>? registeredBranchPaths,
        int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(client);

        var results = new List<UnregisteredBranchGroup>();

        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (registeredBranchPaths != null)
        {
            foreach (var path in registeredBranchPaths)
            {
                registered.Add(TfvcPath.Normalize(path));
            }
        }

        var scope = TfvcPath.Normalize(scopePath);

        // The similarity check and the walk itself ask for the same listings, so
        // each folder is fetched at most once per scan.
        var listings = new Dictionary<string, IReadOnlyList<TfvcItemInfo>>(
            StringComparer.OrdinalIgnoreCase);

        var pending = new Queue<string>();
        pending.Enqueue(scope);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { scope };

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();

            var depth = TfvcPath.GetDepthBelow(scope, current);

            if (depth < 0 || depth >= maxDepth)
            {
                continue;
            }

            var childFolders = await GetChildFoldersAsync(client, projectName, current, listings);

            if (childFolders.Count == 0)
            {
                continue;
            }

            var candidates = childFolders
                .Where(x => x.IsBranch == false)
                .Where(x => registered.Contains(TfvcPath.Normalize(x.Path)) == false)
                .Where(x => LooksLikeBranchName(TfvcPath.GetName(x.Path)) == true)
                .Select(x => TfvcPath.Normalize(x.Path))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // A lone folder called "Main" is not evidence of branching.  It takes
            // siblings that look like copies of each other.
            if (candidates.Count > 1)
            {
                var groups = await GroupBySimilarityAsync(
                    client, projectName, current, candidates, listings);

                results.AddRange(groups);
            }

            foreach (var folder in childFolders)
            {
                var childPath = TfvcPath.Normalize(folder.Path);

                if (visited.Add(childPath) == true)
                {
                    pending.Enqueue(childPath);
                }
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<TfvcItemInfo>> GetListingAsync(
        ITfvcApiClient client,
        string projectName,
        string path,
        Dictionary<string, IReadOnlyList<TfvcItemInfo>> listings)
    {
        if (listings.TryGetValue(path, out var cached) == true)
        {
            return cached;
        }

        var items = await client.GetItemsAsync(projectName, path, TfvcRecursionLevel.OneLevel);

        listings[path] = items;

        return items;
    }

    private async Task<List<TfvcItemInfo>> GetChildFoldersAsync(
        ITfvcApiClient client,
        string projectName,
        string path,
        Dictionary<string, IReadOnlyList<TfvcItemInfo>> listings)
    {
        var items = await GetListingAsync(client, projectName, path, listings);

        // A one-level listing includes the folder that was asked for.
        return items
            .Where(x => x.IsFolder == true)
            .Where(x => TfvcPath.AreEqual(x.Path, path) == false)
            .ToList();
    }

    private async Task<List<UnregisteredBranchGroup>> GroupBySimilarityAsync(
        ITfvcApiClient client,
        string projectName,
        string parentPath,
        List<string> candidates,
        Dictionary<string, IReadOnlyList<TfvcItemInfo>> listings)
    {
        var contentsByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            contentsByPath[candidate] =
                await GetChildNamesAsync(client, projectName, candidate, listings);
        }

        var results = new List<UnregisteredBranchGroup>();
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (assigned.Contains(candidate) == true)
            {
                continue;
            }

            var group = new List<string> { candidate };
            assigned.Add(candidate);

            foreach (var other in candidates)
            {
                if (assigned.Contains(other) == true)
                {
                    continue;
                }

                if (IsSimilar(contentsByPath[candidate], contentsByPath[other]) == true)
                {
                    group.Add(other);
                    assigned.Add(other);
                }
            }

            if (group.Count > 1)
            {
                results.Add(new UnregisteredBranchGroup
                {
                    ParentPath = parentPath,
                    FolderPaths = group
                });
            }
        }

        return results;
    }

    private async Task<HashSet<string>> GetChildNamesAsync(
        ITfvcApiClient client,
        string projectName,
        string path,
        Dictionary<string, IReadOnlyList<TfvcItemInfo>> listings)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var items = await GetListingAsync(client, projectName, path, listings);

        foreach (var item in items)
        {
            if (TfvcPath.AreEqual(item.Path, path) == true)
            {
                continue;
            }

            names.Add(TfvcPath.GetName(item.Path));
        }

        return names;
    }

    /// <summary>
    /// Overlap is measured against the smaller of the two folders so that a
    /// branch which has grown since it was copied still matches its origin.
    /// </summary>
    public static bool IsSimilar(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return false;
        }

        var overlap = left.Count(x => right.Contains(x));

        var smaller = Math.Min(left.Count, right.Count);

        return (double)overlap / smaller > SimilarityThreshold;
    }
}
