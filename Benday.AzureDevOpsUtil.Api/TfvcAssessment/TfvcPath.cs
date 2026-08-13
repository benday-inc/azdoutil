namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Comparison and manipulation of TFVC server paths ("$/Project/Folder").
///
/// Containment is compared on directory boundaries rather than raw string
/// prefixes, so "$/App/MainFrame" is not treated as living inside "$/App/Main".
/// TFVC paths are case-insensitive.
/// </summary>
public static class TfvcPath
{
    public const string Root = "$/";

    private const char Separator = '/';

    private static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Trims whitespace, converts backslashes to forward slashes, and removes a
    /// trailing separator.  Null, empty, and whitespace values become "$/".
    /// </summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) == true)
        {
            return Root;
        }

        var value = path.Trim().Replace('\\', Separator);

        while (value.Length > Root.Length && value[value.Length - 1] == Separator)
        {
            value = value.Substring(0, value.Length - 1);
        }

        return value;
    }

    public static bool AreEqual(string? left, string? right)
    {
        return string.Equals(Normalize(left), Normalize(right), Comparison);
    }

    /// <summary>
    /// True when <paramref name="candidate"/> is <paramref name="root"/> itself
    /// or sits anywhere beneath it.
    /// </summary>
    public static bool IsSameOrUnder(string? candidate, string? root)
    {
        var candidatePath = Normalize(candidate);
        var rootPath = Normalize(root);

        if (string.Equals(candidatePath, rootPath, Comparison) == true)
        {
            return true;
        }

        var prefix = rootPath[rootPath.Length - 1] == Separator ?
            rootPath : rootPath + Separator;

        return candidatePath.StartsWith(prefix, Comparison);
    }

    /// <summary>
    /// True when <paramref name="candidate"/> sits beneath <paramref name="root"/>
    /// but is not <paramref name="root"/> itself.
    /// </summary>
    public static bool IsStrictlyUnder(string? candidate, string? root)
    {
        return AreEqual(candidate, root) == false && IsSameOrUnder(candidate, root) == true;
    }

    /// <summary>
    /// The last segment of the path.  "$/App/Main" returns "Main".
    /// </summary>
    public static string GetName(string? path)
    {
        var value = Normalize(path);

        var index = value.LastIndexOf(Separator);

        if (index < 0 || index == value.Length - 1)
        {
            return value;
        }

        return value.Substring(index + 1);
    }

    /// <summary>
    /// The containing folder, or null when the path is the "$/" root.
    /// </summary>
    public static string? GetParent(string? path)
    {
        var value = Normalize(path);

        if (string.Equals(value, Root, StringComparison.Ordinal) == true)
        {
            return null;
        }

        var index = value.LastIndexOf(Separator);

        if (index <= 1)
        {
            return Root;
        }

        return value.Substring(0, index);
    }

    /// <summary>
    /// How many segments separate <paramref name="path"/> from
    /// <paramref name="root"/>.  Returns 0 when they are the same path and -1
    /// when the path does not sit beneath the root.
    /// </summary>
    public static int GetDepthBelow(string? root, string? path)
    {
        if (IsSameOrUnder(path, root) == false)
        {
            return -1;
        }

        var rootPath = Normalize(root);
        var candidatePath = Normalize(path);

        if (string.Equals(rootPath, candidatePath, Comparison) == true)
        {
            return 0;
        }

        var prefix = rootPath[rootPath.Length - 1] == Separator ?
            rootPath : rootPath + Separator;

        var remainder = candidatePath.Substring(prefix.Length);

        return remainder.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Of the supplied candidates, the one that most closely encloses
    /// <paramref name="path"/>.  Returns null when nothing encloses it.
    /// </summary>
    public static string? FindNearestEnclosing(string path, IEnumerable<string> candidates)
    {
        string? nearest = null;
        var nearestLength = -1;

        foreach (var candidate in candidates)
        {
            if (IsStrictlyUnder(path, candidate) == false)
            {
                continue;
            }

            var length = Normalize(candidate).Length;

            if (length > nearestLength)
            {
                nearest = Normalize(candidate);
                nearestLength = length;
            }
        }

        return nearest;
    }
}
