using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

public class LargeFileInfo
{
    public string Path { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}

/// <summary>
/// A file extension and what it accounts for in the tree.
/// </summary>
public class ExtensionUsage
{
    public string Extension { get; set; } = string.Empty;

    public int FileCount { get; set; }

    public long TotalSizeBytes { get; set; }
}

/// <summary>
/// A folder name that normally holds generated output or downloaded
/// dependencies, and what sits under folders with that name.
/// </summary>
public class GeneratedFolderUsage
{
    public string Name { get; set; } = string.Empty;

    public int FileCount { get; set; }

    public long TotalSizeBytes { get; set; }

    public string ExamplePath { get; set; } = string.Empty;
}

public class TfvcContentScanResult
{
    public int FileCount { get; set; }

    /// <summary>
    /// Total size of the files as they stand at the current version.  This is
    /// not the size of the history.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    public List<LargeFileInfo> LargestFiles { get; set; } = new();

    public int FilesOverWarningSize { get; set; }

    public int FilesOverPushLimit { get; set; }

    public List<ExtensionUsage> ExtensionUsages { get; set; } = new();

    public List<GeneratedFolderUsage> GeneratedFolders { get; set; } = new();

    public int GeneratedFolderFileCount => GeneratedFolders.Sum(x => x.FileCount);

    public long GeneratedFolderSizeBytes => GeneratedFolders.Sum(x => x.TotalSizeBytes);
}

/// <summary>
/// Reads a full item listing and reports what is in the tree: the largest
/// files, the file types that account for the bulk of it, and the folders that
/// normally hold build output or downloaded dependencies.
///
/// This does no I/O.  A single recursive item listing answers all three
/// questions, so the caller fetches once and this walks the result.
/// </summary>
public class TfvcContentScanner
{
    /// <summary>Where GitHub starts warning about file size.</summary>
    public const long WarningSizeBytes = 50L * 1024L * 1024L;

    /// <summary>Where most Git hosts refuse the push outright.</summary>
    public const long PushLimitSizeBytes = 100L * 1024L * 1024L;

    public const int DefaultLargestFileCount = 20;

    /// <summary>
    /// File types worth counting separately: things Git carries forever and
    /// that are usually build output rather than source.
    /// </summary>
    public static readonly string[] ExtensionsOfInterest =
    {
        ".dll", ".exe", ".pdb", ".lib", ".obj", ".msi", ".cab", ".nupkg",
        ".zip", ".7z", ".rar", ".tar", ".gz", ".iso",
        ".bak", ".mdf", ".ldf",
        ".jar", ".war",
        ".mp4", ".mov", ".avi", ".wmv", ".mkv", ".mp3", ".wav"
    };

    /// <summary>
    /// Folder names that normally hold generated output or downloaded
    /// dependencies.  Deliberately excludes ambiguous names such as "build",
    /// "lib", "out", "target", "Debug" and "Release", which are legitimately
    /// source folders often enough that flagging them would make the section
    /// worth ignoring.  Extend this list here; nothing else needs to change.
    /// </summary>
    public static readonly string[] GeneratedFolderNames =
    {
        "bin", "obj", "packages", "node_modules", "bower_components",
        "dist", ".vs", ".nuget", "TestResults", "vendor"
    };

    /// <summary>
    /// Folder names matched on a prefix, for tooling that stamps a suffix onto
    /// the folder it creates.
    /// </summary>
    public static readonly string[] GeneratedFolderPrefixes =
    {
        "_ReSharper"
    };

    public TfvcContentScanResult Scan(
        IReadOnlyList<TfvcItemInfo>? items,
        string scopePath,
        int largestFileCount = DefaultLargestFileCount)
    {
        var result = new TfvcContentScanResult();

        if (items == null || items.Count == 0)
        {
            return result;
        }

        var scope = TfvcPath.Normalize(scopePath);

        var extensions = new Dictionary<string, ExtensionUsage>(StringComparer.OrdinalIgnoreCase);
        var folders = new Dictionary<string, GeneratedFolderUsage>(StringComparer.OrdinalIgnoreCase);
        var files = new List<LargeFileInfo>();

        foreach (var item in items)
        {
            // Folders carry no size and are not files.  The server leaves
            // isFolder out entirely for files, so this is "not known to be a
            // folder" rather than "known to be a file".
            if (item.IsFolder == true)
            {
                continue;
            }

            var path = TfvcPath.Normalize(item.Path);
            var size = item.Size ?? 0L;

            result.FileCount++;
            result.TotalSizeBytes += size;

            files.Add(new LargeFileInfo { Path = path, SizeBytes = size });

            if (size >= PushLimitSizeBytes)
            {
                result.FilesOverPushLimit++;
            }

            if (size >= WarningSizeBytes)
            {
                result.FilesOverWarningSize++;
            }

            AccumulateExtension(extensions, path, size);
            AccumulateGeneratedFolder(folders, path, scope, size);
        }

        result.LargestFiles = files
            .OrderByDescending(x => x.SizeBytes)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .Take(largestFileCount)
            .ToList();

        result.ExtensionUsages = extensions.Values
            .OrderByDescending(x => x.TotalSizeBytes)
            .ThenByDescending(x => x.FileCount)
            .ThenBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.GeneratedFolders = folders.Values
            .OrderByDescending(x => x.TotalSizeBytes)
            .ThenByDescending(x => x.FileCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result;
    }

    private void AccumulateExtension(
        Dictionary<string, ExtensionUsage> extensions, string path, long size)
    {
        var extension = GetExtension(path);

        if (extension.Length == 0)
        {
            return;
        }

        if (ExtensionsOfInterest.Contains(extension, StringComparer.OrdinalIgnoreCase) == false)
        {
            return;
        }

        if (extensions.TryGetValue(extension, out var usage) == false)
        {
            usage = new ExtensionUsage { Extension = extension };
            extensions[extension] = usage;
        }

        usage.FileCount++;
        usage.TotalSizeBytes += size;
    }

    /// <summary>
    /// A file is counted against the outermost matching folder in its path, so
    /// something under packages/Foo/lib/bin counts once against "packages"
    /// rather than twice.  That keeps the counts from overlapping.
    /// </summary>
    private void AccumulateGeneratedFolder(
        Dictionary<string, GeneratedFolderUsage> folders, string path, string scope, long size)
    {
        foreach (var segment in GetRelativeDirectorySegments(path, scope))
        {
            if (TryGetGeneratedFolderName(segment, out var name) == false)
            {
                continue;
            }

            if (folders.TryGetValue(name, out var usage) == false)
            {
                usage = new GeneratedFolderUsage
                {
                    Name = name,
                    ExamplePath = path
                };

                folders[name] = usage;
            }

            usage.FileCount++;
            usage.TotalSizeBytes += size;

            return;
        }
    }

    /// <summary>
    /// The lower-case extension including the leading dot, or an empty string.
    /// A name that begins with a dot and has no other dot is a dotfile rather
    /// than an extension.
    /// </summary>
    public static string GetExtension(string? path)
    {
        var name = TfvcPath.GetName(path);

        var index = name.LastIndexOf('.');

        if (index <= 0 || index == name.Length - 1)
        {
            return string.Empty;
        }

        return name.Substring(index).ToLowerInvariant();
    }

    /// <summary>
    /// The folder segments between the scope path and the file itself.
    /// </summary>
    public static IReadOnlyList<string> GetRelativeDirectorySegments(string? path, string? scope)
    {
        if (TfvcPath.IsStrictlyUnder(path, scope) == false)
        {
            return Array.Empty<string>();
        }

        var normalizedScope = TfvcPath.Normalize(scope);
        var normalizedPath = TfvcPath.Normalize(path);

        var prefix = normalizedScope.EndsWith('/') ? normalizedScope : normalizedScope + "/";

        var remainder = normalizedPath.Substring(prefix.Length);

        var segments = remainder.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= 1)
        {
            // Only the file name is left, so there are no folders in between.
            return Array.Empty<string>();
        }

        return segments.Take(segments.Length - 1).ToList();
    }

    public static bool TryGetGeneratedFolderName(string? segment, out string name)
    {
        name = string.Empty;

        if (string.IsNullOrWhiteSpace(segment) == true)
        {
            return false;
        }

        var value = segment.Trim();

        foreach (var candidate in GeneratedFolderNames)
        {
            if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase) == true)
            {
                // Report the canonical spelling rather than whatever casing
                // this particular folder happened to use.
                name = candidate;
                return true;
            }
        }

        foreach (var prefix in GeneratedFolderPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                name = prefix + "*";
                return true;
            }
        }

        return false;
    }
}
