namespace Benday.AzureDevOpsUtil.Api.GitRemotes;

/// <summary>
/// Finds the git repository a directory belongs to and reads a remote url out
/// of its config.
///
/// This reads the files directly rather than running git, so it does not
/// depend on git being installed or on PATH.
/// </summary>
public static class GitRepositoryLocator
{
    public const string DefaultRemoteName = "origin";

    /// <summary>
    /// Walks up from the directory looking for the repository's git directory.
    /// Returns null when the directory is not inside a repository.
    /// </summary>
    public static string? FindGitDirectory(string? startingDirectory)
    {
        if (string.IsNullOrWhiteSpace(startingDirectory) == true)
        {
            return null;
        }

        DirectoryInfo? directory;

        try
        {
            directory = new DirectoryInfo(startingDirectory);
        }
        catch (ArgumentException)
        {
            return null;
        }

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".git");

            if (Directory.Exists(candidate) == true)
            {
                return candidate;
            }

            if (File.Exists(candidate) == true)
            {
                // Worktrees and submodules leave a file holding the real
                // location rather than a directory.
                return ResolveGitDirectoryFile(candidate);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? ResolveGitDirectoryFile(string gitFilePath)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(gitFilePath);
        }
        catch (IOException)
        {
            return null;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            var target = trimmed.Substring("gitdir:".Length).Trim();

            if (target.Length == 0)
            {
                return null;
            }

            if (Path.IsPathRooted(target) == true)
            {
                return target;
            }

            var baseDirectory = Path.GetDirectoryName(gitFilePath) ?? string.Empty;

            return Path.GetFullPath(Path.Combine(baseDirectory, target));
        }

        return null;
    }

    /// <summary>
    /// The config file for a git directory.  A worktree keeps its config in the
    /// main repository, named by its commondir file.
    /// </summary>
    public static string? FindConfigFilePath(string? gitDirectory)
    {
        if (string.IsNullOrWhiteSpace(gitDirectory) == true)
        {
            return null;
        }

        var commonDirFile = Path.Combine(gitDirectory, "commondir");

        if (File.Exists(commonDirFile) == true)
        {
            var target = File.ReadAllText(commonDirFile).Trim();

            if (target.Length > 0)
            {
                var resolved = Path.IsPathRooted(target) == true ?
                    target :
                    Path.GetFullPath(Path.Combine(gitDirectory, target));

                var sharedConfig = Path.Combine(resolved, "config");

                if (File.Exists(sharedConfig) == true)
                {
                    return sharedConfig;
                }
            }
        }

        var configPath = Path.Combine(gitDirectory, "config");

        return File.Exists(configPath) == true ? configPath : null;
    }

    /// <summary>
    /// Reads the url of a remote out of git config content.  The format is
    /// sections in square brackets followed by key/value pairs.
    /// </summary>
    public static string? ParseRemoteUrl(
        string? configContent, string remoteName = DefaultRemoteName)
    {
        if (string.IsNullOrWhiteSpace(configContent) == true)
        {
            return null;
        }

        var wantedSection = $"remote \"{remoteName}\"";

        var isInSection = false;

        using var reader = new StringReader(configContent);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 ||
                trimmed.StartsWith("#", StringComparison.Ordinal) == true ||
                trimmed.StartsWith(";", StringComparison.Ordinal) == true)
            {
                continue;
            }

            if (trimmed.StartsWith("[", StringComparison.Ordinal) == true &&
                trimmed.EndsWith("]", StringComparison.Ordinal) == true)
            {
                var section = trimmed.Substring(1, trimmed.Length - 2).Trim();

                isInSection = string.Equals(
                    section, wantedSection, StringComparison.OrdinalIgnoreCase);

                continue;
            }

            if (isInSection == false)
            {
                continue;
            }

            var equalsIndex = trimmed.IndexOf('=');

            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = trimmed.Substring(0, equalsIndex).Trim();

            if (string.Equals(key, "url", StringComparison.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            var url = trimmed.Substring(equalsIndex + 1).Trim();

            if (url.Length > 0)
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>
    /// The url of a remote for the repository containing the directory, or null
    /// when there is no repository or no such remote.
    /// </summary>
    public static string? FindRemoteUrl(
        string? startingDirectory, string remoteName = DefaultRemoteName)
    {
        var gitDirectory = FindGitDirectory(startingDirectory);

        if (gitDirectory == null)
        {
            return null;
        }

        var configPath = FindConfigFilePath(gitDirectory);

        if (configPath == null)
        {
            return null;
        }

        try
        {
            return ParseRemoteUrl(File.ReadAllText(configPath), remoteName);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
