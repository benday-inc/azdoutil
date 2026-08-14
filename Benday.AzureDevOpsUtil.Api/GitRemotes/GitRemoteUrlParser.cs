namespace Benday.AzureDevOpsUtil.Api.GitRemotes;

/// <summary>
/// Reads an Azure DevOps git remote url and works out which collection,
/// project and repository it points at.
///
/// Azure DevOps has accumulated a number of url shapes, but they come down to
/// two rules.  Every https form contains a "_git" segment: what sits before it
/// is the project, what sits after it is the repository, and what sits before
/// that is the collection.  The ssh forms have no "_git" and instead start
/// with "v3" followed by account, project and repository.
///
/// Anything without either shape -- GitHub, GitLab, a plain file path -- is
/// not an Azure DevOps remote and parses to null.
/// </summary>
public static class GitRemoteUrlParser
{
    private const string GitSegment = "_git";
    private const string SshPathPrefix = "v3";
    private const string DevAzureHost = "dev.azure.com";
    private const string VisualStudioHostSuffix = ".visualstudio.com";

    /// <summary>
    /// Returns what the url says, or null when it is not an Azure DevOps
    /// repository url.
    /// </summary>
    public static GitRemoteInfo? Parse(string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl) == true)
        {
            return null;
        }

        var value = remoteUrl.Trim();

        if (TrySplitUrl(value, out var host, out var scheme, out var port, out var path) == false)
        {
            return null;
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToList();

        if (segments.Count == 0)
        {
            return null;
        }

        var sshResult = ParseSshForm(value, host, segments);

        if (sshResult != null)
        {
            return sshResult;
        }

        return ParseHttpForm(value, host, scheme, port, segments);
    }

    /// <summary>
    /// Splits either a real url or the scp-like "user@host:path" form that ssh
    /// remotes use.
    /// </summary>
    private static bool TrySplitUrl(
        string value, out string host, out string scheme, out int port, out string path)
    {
        host = string.Empty;
        scheme = "https";
        port = -1;
        path = string.Empty;

        if (value.Contains("://", StringComparison.Ordinal) == true)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) == false)
            {
                return false;
            }

            host = uri.Host;
            path = uri.AbsolutePath;

            // An ssh url describes how to reach the server, not how the
            // collection is addressed over http, so its scheme and port are
            // not carried into the collection url.
            if (string.Equals(uri.Scheme, "ssh", StringComparison.OrdinalIgnoreCase) == true)
            {
                return host.Length > 0;
            }

            scheme = string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) == true ?
                "http" : "https";

            port = uri.IsDefaultPort == true ? -1 : uri.Port;

            return host.Length > 0;
        }

        // scp-like syntax: [user@]host:path
        var atIndex = value.IndexOf('@');

        var colonIndex = value.IndexOf(':', atIndex + 1);

        if (colonIndex < 0)
        {
            return false;
        }

        host = value.Substring(atIndex + 1, colonIndex - atIndex - 1);
        path = value.Substring(colonIndex + 1);

        return host.Length > 0;
    }

    /// <summary>
    /// The ssh form: v3/{account}/{project}/{repository}.
    /// </summary>
    private static GitRemoteInfo? ParseSshForm(
        string originalUrl, string host, List<string> segments)
    {
        if (string.Equals(segments[0], SshPathPrefix, StringComparison.OrdinalIgnoreCase) == false)
        {
            return null;
        }

        if (segments.Count < 4)
        {
            return null;
        }

        var account = segments[1];

        return new GitRemoteInfo
        {
            OriginalUrl = originalUrl,
            CollectionUrl = BuildCloudCollectionUrl(host, account),
            AccountName = account,
            ProjectName = segments[2],
            RepositoryName = StripGitSuffix(segments[3]),
            IsAzureDevOpsService = true
        };
    }

    private static GitRemoteInfo? ParseHttpForm(
        string originalUrl, string host, string scheme, int port, List<string> segments)
    {
        var gitIndex = segments.FindIndex(x =>
            string.Equals(x, GitSegment, StringComparison.OrdinalIgnoreCase));

        // There has to be a project before the marker and a repository after it.
        if (gitIndex < 1 || gitIndex + 1 >= segments.Count)
        {
            return null;
        }

        var projectName = segments[gitIndex - 1];
        var repositoryName = StripGitSuffix(segments[gitIndex + 1]);

        var collectionSegments = segments.Take(gitIndex - 1).ToList();

        if (IsCloudHost(host) == true)
        {
            var account = GetCloudAccountName(host, collectionSegments);

            if (string.IsNullOrWhiteSpace(account) == true)
            {
                return null;
            }

            return new GitRemoteInfo
            {
                OriginalUrl = originalUrl,
                CollectionUrl = BuildCloudCollectionUrl(host, account, collectionSegments),
                AccountName = account,
                ProjectName = projectName,
                RepositoryName = repositoryName,
                IsAzureDevOpsService = true
            };
        }

        // On-premises.  Everything before the project is the collection, which
        // is what a configuration stores as its url.
        var authority = port > 0 ?
            $"{host}:{port.ToString(System.Globalization.CultureInfo.InvariantCulture)}" :
            host;

        var collectionPath = collectionSegments.Count == 0 ?
            string.Empty : string.Join("/", collectionSegments) + "/";

        return new GitRemoteInfo
        {
            OriginalUrl = originalUrl,
            CollectionUrl = $"{scheme}://{authority}/{collectionPath}",
            AccountName = collectionSegments.Count == 0 ?
                host : collectionSegments[collectionSegments.Count - 1],
            ProjectName = projectName,
            RepositoryName = repositoryName,
            IsAzureDevOpsService = false
        };
    }

    private static bool IsCloudHost(string host)
    {
        if (host.EndsWith(DevAzureHost, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return host.EndsWith(VisualStudioHostSuffix, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// On dev.azure.com the account is the first path segment.  On the older
    /// visualstudio.com hosts it is part of the host name.
    /// </summary>
    private static string GetCloudAccountName(string host, List<string> collectionSegments)
    {
        if (host.EndsWith(VisualStudioHostSuffix, StringComparison.OrdinalIgnoreCase) == true)
        {
            var name = host.Substring(0, host.Length - VisualStudioHostSuffix.Length);

            // vs-ssh.visualstudio.com and account.vs-ssh.visualstudio.com both
            // end in the ssh host name rather than the account.
            var dotIndex = name.IndexOf('.');

            if (dotIndex > 0)
            {
                name = name.Substring(0, dotIndex);
            }

            return name;
        }

        return collectionSegments.Count > 0 ? collectionSegments[0] : string.Empty;
    }

    private static string BuildCloudCollectionUrl(
        string host, string account, List<string>? collectionSegments = null)
    {
        if (host.EndsWith(VisualStudioHostSuffix, StringComparison.OrdinalIgnoreCase) == true)
        {
            // These urls sometimes carry a collection name of their own.
            var extra = string.Empty;

            if (collectionSegments != null && collectionSegments.Count > 0)
            {
                extra = string.Join("/", collectionSegments) + "/";
            }

            return $"https://{account}{VisualStudioHostSuffix}/{extra}";
        }

        return $"https://{DevAzureHost}/{account}/";
    }

    private static string StripGitSuffix(string value)
    {
        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase) == true &&
            value.Length > 4)
        {
            return value.Substring(0, value.Length - 4);
        }

        return value;
    }
}
