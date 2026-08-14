namespace Benday.AzureDevOpsUtil.Api.TfCommandLine;

/// <summary>
/// Where a copy of tf was found, and how.
/// </summary>
public class TfLocation
{
    public string Path { get; set; } = string.Empty;

    /// <summary>What kind of install this copy belongs to.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>True when this copy can be run without giving its full path.</summary>
    public bool IsOnPath { get; set; }
}

/// <summary>
/// Finds the tf command line client.
///
/// tf does not ship on its own and is almost never on the PATH.  It arrives
/// inside Visual Studio, inside an Azure DevOps Server install, or as the
/// cross-platform Team Explorer Everywhere client, and Visual Studio moved it
/// to a long path underneath the install folder in 2017.  So finding it means
/// looking in several places rather than one.
/// </summary>
public class TfExecutableLocator
{
    public const string SourcePath = "PATH";
    public const string SourceVisualStudio = "Visual Studio";
    public const string SourceVisualStudioLegacy = "Visual Studio (2015 or earlier)";
    public const string SourceServer = "Azure DevOps Server";

    /// <summary>
    /// Where Visual Studio has kept tf since 2017, relative to the install
    /// folder.
    /// </summary>
    public const string VisualStudioRelativePath =
        @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe";

    /// <summary>Where Visual Studio kept it before 2017.</summary>
    public const string VisualStudioLegacyRelativePath = @"Common7\IDE\TF.exe";

    /// <summary>
    /// TF.exe on Windows; the Team Explorer Everywhere client is a script
    /// called tf or tf.cmd.
    /// </summary>
    public static readonly string[] ExecutableNames = { "TF.exe", "tf.cmd", "tf" };

    private static readonly string[] ProgramFilesVariables =
    {
        "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432"
    };

    private static readonly string[] ServerInstallFolderPrefixes =
    {
        "Azure DevOps Server", "Microsoft Team Foundation Server"
    };

    private readonly IFileSystemProbe _probe;
    private readonly char _pathSeparator;

    /// <param name="pathSeparator">
    /// What separates entries in the PATH variable.  Defaults to the running
    /// platform's separator; tests state it so they can describe a Windows PATH
    /// while running anywhere.
    /// </param>
    public TfExecutableLocator(IFileSystemProbe probe, char? pathSeparator = null)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _pathSeparator = pathSeparator ?? Path.PathSeparator;
    }

    /// <summary>
    /// Every copy of tf that could be found, the ones already on the PATH
    /// first.
    /// </summary>
    public List<TfLocation> Find()
    {
        var found = new List<TfLocation>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var location in FindOnPath())
        {
            if (seen.Add(GetComparisonKey(location.Path)) == true)
            {
                found.Add(location);
            }
        }

        foreach (var location in FindInInstallFolders())
        {
            if (seen.Add(GetComparisonKey(location.Path)) == true)
            {
                found.Add(location);
            }
        }

        return found
            .OrderByDescending(x => x.IsOnPath)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The directories the PATH variable lists, in order.
    /// </summary>
    public static IReadOnlyList<string> SplitPathVariable(
        string? pathValue, char? separator = null)
    {
        if (string.IsNullOrWhiteSpace(pathValue) == true)
        {
            return Array.Empty<string>();
        }

        return pathValue
            .Split(separator ?? Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().Trim('"'))
            .Where(x => x.Length > 0)
            .ToList();
    }

    private List<TfLocation> FindOnPath()
    {
        var results = new List<TfLocation>();

        var directories = SplitPathVariable(
            _probe.GetEnvironmentVariable("PATH"), _pathSeparator);

        foreach (var directory in directories)
        {
            foreach (var name in ExecutableNames)
            {
                // A PATH entry is a native path, but on Windows that is a
                // backslash path, which is what tf lives behind.
                var candidate = directory.Contains('\\') == true ?
                    CombineWindowsPath(directory, name) :
                    CombineSafely(directory, name);

                if (candidate == null || _probe.FileExists(candidate) == false)
                {
                    continue;
                }

                results.Add(new TfLocation
                {
                    Path = candidate,
                    Source = SourcePath,
                    IsOnPath = true
                });
            }
        }

        return results;
    }

    private List<TfLocation> FindInInstallFolders()
    {
        var results = new List<TfLocation>();

        foreach (var root in GetProgramFilesFolders())
        {
            results.AddRange(FindInVisualStudioFolders(root));
            results.AddRange(FindInServerFolders(root));
        }

        return results;
    }

    private IReadOnlyList<string> GetProgramFilesFolders()
    {
        var folders = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in ProgramFilesVariables)
        {
            var value = _probe.GetEnvironmentVariable(variable);

            if (string.IsNullOrWhiteSpace(value) == true)
            {
                continue;
            }

            if (seen.Add(value) == true)
            {
                folders.Add(value);
            }
        }

        return folders;
    }

    /// <summary>
    /// The years and editions are enumerated rather than listed, so a version
    /// released after this code was written is still found.
    /// </summary>
    private List<TfLocation> FindInVisualStudioFolders(string programFiles)
    {
        var results = new List<TfLocation>();

        var visualStudioRoot = CombineWindowsPath(programFiles, "Microsoft Visual Studio");

        if (_probe.DirectoryExists(visualStudioRoot) == false)
        {
            // Before 2017 each version had its own folder next to this one.
            results.AddRange(FindInLegacyVisualStudioFolders(programFiles));

            return results;
        }

        foreach (var yearFolder in _probe.GetDirectories(visualStudioRoot))
        {
            foreach (var editionFolder in _probe.GetDirectories(yearFolder))
            {
                var candidate = CombineWindowsPath(editionFolder, VisualStudioRelativePath);

                if (_probe.FileExists(candidate) == true)
                {
                    results.Add(new TfLocation
                    {
                        Path = candidate,
                        Source = SourceVisualStudio
                    });
                }
            }
        }

        results.AddRange(FindInLegacyVisualStudioFolders(programFiles));

        return results;
    }

    private List<TfLocation> FindInLegacyVisualStudioFolders(string programFiles)
    {
        var results = new List<TfLocation>();

        if (_probe.DirectoryExists(programFiles) == false)
        {
            return results;
        }

        foreach (var folder in _probe.GetDirectories(programFiles))
        {
            var name = GetFolderName(folder);

            // "Microsoft Visual Studio 14.0" and friends.
            if (name.StartsWith("Microsoft Visual Studio ", StringComparison.OrdinalIgnoreCase) ==
                false)
            {
                continue;
            }

            var candidate = CombineWindowsPath(folder, VisualStudioLegacyRelativePath);

            if (_probe.FileExists(candidate) == true)
            {
                results.Add(new TfLocation
                {
                    Path = candidate,
                    Source = SourceVisualStudioLegacy
                });
            }
        }

        return results;
    }

    private List<TfLocation> FindInServerFolders(string programFiles)
    {
        var results = new List<TfLocation>();

        if (_probe.DirectoryExists(programFiles) == false)
        {
            return results;
        }

        foreach (var folder in _probe.GetDirectories(programFiles))
        {
            var name = GetFolderName(folder);

            var isServerFolder = ServerInstallFolderPrefixes.Any(x =>
                name.StartsWith(x, StringComparison.OrdinalIgnoreCase));

            if (isServerFolder == false)
            {
                continue;
            }

            var candidate = CombineWindowsPath(folder, @"Tools\TF.exe");

            if (_probe.FileExists(candidate) == true)
            {
                results.Add(new TfLocation
                {
                    Path = candidate,
                    Source = SourceServer
                });
            }
        }

        return results;
    }

    private static string GetFolderName(string path)
    {
        var trimmed = path.TrimEnd('\\', '/');

        var index = trimmed.LastIndexOfAny(new[] { '\\', '/' });

        return index < 0 ? trimmed : trimmed.Substring(index + 1);
    }

    /// <summary>
    /// Path.Combine throws on characters the platform will not accept, and a
    /// PATH variable can hold anything at all.
    /// </summary>
    private static string? CombineSafely(string left, string right)
    {
        try
        {
            return Path.Combine(left, right);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Joins with a backslash rather than the platform separator.  These are
    /// Windows install locations whatever the host platform happens to be, so
    /// building them with the platform separator would produce a path that is
    /// wrong everywhere except Windows.
    /// </summary>
    private static string CombineWindowsPath(string left, string right)
    {
        return left.TrimEnd('\\', '/') + "\\" + right;
    }

    /// <summary>
    /// The comparison key for deciding two results are the same file.  Both
    /// separators are treated alike so a copy found on the PATH and the same
    /// copy found in an install folder are not reported twice.
    /// </summary>
    private static string GetComparisonKey(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }
}
