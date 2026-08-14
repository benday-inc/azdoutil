using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.Api.TfCommandLine;

/// <summary>
/// Where a local directory sits in TFVC.
/// </summary>
public class TfvcLocationInfo
{
    public string CollectionUrl { get; set; } = string.Empty;

    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>The server path the local directory corresponds to.</summary>
    public string ServerPath { get; set; } = string.Empty;

    /// <summary>The team project, which is the first segment of the server path.</summary>
    public string TeamProjectName { get; set; } = string.Empty;

    /// <summary>The mapping the directory was resolved through.</summary>
    public string MappedServerPath { get; set; } = string.Empty;

    public string MappedLocalPath { get; set; } = string.Empty;
}

/// <summary>
/// Turns a workspace's mappings and a local directory into the TFVC server
/// path that directory holds.
///
/// This does no I/O.  "tf workfold" lists every mapping in the workspace rather
/// than only the relevant one, so choosing between them is arithmetic on the
/// paths.
/// </summary>
public static class TfWorkspaceResolver
{
    /// <summary>
    /// Resolves a local directory against a workspace, or returns null when the
    /// directory is not inside any of its mappings.
    /// </summary>
    public static TfvcLocationInfo? Resolve(TfWorkfoldResult? workspace, string? localDirectory)
    {
        if (workspace == null || string.IsNullOrWhiteSpace(localDirectory) == true)
        {
            return null;
        }

        var directory = NormalizeLocalPath(localDirectory);

        TfWorkspaceMapping? best = null;

        foreach (var mapping in workspace.Mappings)
        {
            if (mapping.IsCloaked == true || mapping.LocalPath.Length == 0)
            {
                continue;
            }

            if (IsInsideDirectory(directory, NormalizeLocalPath(mapping.LocalPath)) == false)
            {
                continue;
            }

            // A workspace can map a folder and then map something beneath it,
            // so the most specific mapping is the one that applies.
            if (best == null || mapping.LocalPath.Length > best.LocalPath.Length)
            {
                best = mapping;
            }
        }

        if (best == null)
        {
            return null;
        }

        var mappedLocal = NormalizeLocalPath(best.LocalPath);

        var remainder = directory.Length == mappedLocal.Length ?
            string.Empty :
            directory.Substring(mappedLocal.Length).Trim('\\', '/');

        var serverPath = TfvcPath.Normalize(best.ServerPath);

        if (remainder.Length > 0)
        {
            serverPath = serverPath + "/" + remainder.Replace('\\', '/');
        }

        serverPath = TfvcPath.Normalize(serverPath);

        return new TfvcLocationInfo
        {
            CollectionUrl = workspace.CollectionUrl,
            WorkspaceName = workspace.WorkspaceName,
            ServerPath = serverPath,
            TeamProjectName = GetTeamProjectName(serverPath),
            MappedServerPath = TfvcPath.Normalize(best.ServerPath),
            MappedLocalPath = best.LocalPath
        };
    }

    /// <summary>
    /// The first segment of a server path is the team project.
    /// </summary>
    public static string GetTeamProjectName(string? serverPath)
    {
        var normalized = TfvcPath.Normalize(serverPath);

        if (normalized.Length <= TfvcPath.Root.Length)
        {
            return string.Empty;
        }

        var remainder = normalized.Substring(TfvcPath.Root.Length);

        var separatorIndex = remainder.IndexOf('/');

        return separatorIndex < 0 ? remainder : remainder.Substring(0, separatorIndex);
    }

    private static string NormalizeLocalPath(string path)
    {
        return path.Trim().TrimEnd('\\', '/');
    }

    /// <summary>
    /// Compared on a directory boundary, so "C:\code\AppTests" is not treated
    /// as living inside "C:\code\App".  Local paths here are Windows paths and
    /// are compared without regard to case.
    /// </summary>
    private static bool IsInsideDirectory(string path, string directory)
    {
        if (directory.Length == 0)
        {
            return false;
        }

        if (string.Equals(path, directory, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return path.StartsWith(directory + "\\", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase);
    }
}
