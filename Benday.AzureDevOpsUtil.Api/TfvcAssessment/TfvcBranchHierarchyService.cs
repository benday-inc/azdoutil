using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

public class BranchHierarchyResult
{
    public List<TfvcBranchNode> Roots { get; set; } = new();

    /// <summary>Every branch path in the scope, sorted.</summary>
    public List<string> AllPaths { get; set; } = new();

    public List<NestedBranchPair> NestedBranches { get; set; } = new();
}

/// <summary>
/// Turns the branches API payload into a lineage tree scoped to a path, and
/// reports branches whose root folder sits inside another branch's root folder.
/// </summary>
public class TfvcBranchHierarchyService
{
    /// <summary>
    /// A record of one branch as it came back from the API, flattened out of the
    /// nested children structure.
    /// </summary>
    private sealed class FlatBranch
    {
        public string Path { get; set; } = string.Empty;

        public string? ParentPath { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime? CreatedDate { get; set; }
    }

    public BranchHierarchyResult Build(IReadOnlyList<TfvcBranchInfo>? branches, string scopePath)
    {
        var result = new BranchHierarchyResult();

        var scope = TfvcPath.Normalize(scopePath);

        var flattened = new List<FlatBranch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (branches != null)
        {
            foreach (var branch in branches)
            {
                Flatten(branch, null, flattened, seen);
            }
        }

        var inScope = flattened
            .Where(x => TfvcPath.IsSameOrUnder(x.Path, scope) == true)
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.AllPaths = inScope.Select(x => x.Path).ToList();

        result.Roots = BuildTree(inScope);

        result.NestedBranches = FindNestedBranches(result.AllPaths);

        return result;
    }

    private void Flatten(
        TfvcBranchInfo branch, string? parentPath, List<FlatBranch> sink, HashSet<string> seen)
    {
        if (branch == null || string.IsNullOrWhiteSpace(branch.Path) == true)
        {
            return;
        }

        var path = TfvcPath.Normalize(branch.Path);

        // Deleted branches are not part of what a migration would carry forward,
        // and the API only returns them when explicitly asked.
        if (branch.IsDeleted == true)
        {
            return;
        }

        // The same branch can appear more than once when the payload includes
        // both parent and child links.  First occurrence wins.
        if (seen.Add(path) == true)
        {
            sink.Add(new FlatBranch
            {
                Path = path,
                ParentPath = parentPath,
                Description = branch.Description ?? string.Empty,
                CreatedDate = branch.CreatedDate
            });
        }

        foreach (var child in branch.Children)
        {
            Flatten(child, path, sink, seen);
        }
    }

    private List<TfvcBranchNode> BuildTree(List<FlatBranch> inScope)
    {
        var nodesByPath = new Dictionary<string, TfvcBranchNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in inScope)
        {
            nodesByPath[item.Path] = new TfvcBranchNode
            {
                Path = item.Path,
                Description = item.Description,
                CreatedDate = item.CreatedDate
            };
        }

        var roots = new List<TfvcBranchNode>();

        foreach (var item in inScope)
        {
            var node = nodesByPath[item.Path];

            // A branch whose lineage parent fell outside the scope is shown as a
            // root of the scoped tree.
            if (item.ParentPath != null &&
                nodesByPath.TryGetValue(item.ParentPath, out var parentNode) == true)
            {
                parentNode.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    /// <summary>
    /// A branch is nested when its root folder lives inside another branch's
    /// root folder.  Only the nearest enclosing branch is reported so that a
    /// three-deep nest produces two findings rather than three.
    /// </summary>
    private List<NestedBranchPair> FindNestedBranches(List<string> allPaths)
    {
        var results = new List<NestedBranchPair>();

        foreach (var path in allPaths)
        {
            var enclosing = TfvcPath.FindNearestEnclosing(path, allPaths);

            if (enclosing == null)
            {
                continue;
            }

            results.Add(new NestedBranchPair
            {
                ChildPath = path,
                ParentPath = enclosing
            });
        }

        return results;
    }
}
