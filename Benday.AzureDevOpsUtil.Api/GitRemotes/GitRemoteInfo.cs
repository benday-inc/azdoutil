namespace Benday.AzureDevOpsUtil.Api.GitRemotes;

/// <summary>
/// What an Azure DevOps git remote url says about where the repository lives.
/// </summary>
public class GitRemoteInfo
{
    /// <summary>The url exactly as it appears in the git config.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// The collection url, in the form azdoutil stores in a configuration:
    /// "https://dev.azure.com/account/" for the cloud, or
    /// "https://server:8080/tfs/Collection/" on-premises.  Always ends with a
    /// separator so it compares directly against a stored configuration.
    /// </summary>
    public string CollectionUrl { get; set; } = string.Empty;

    /// <summary>
    /// The organization name in the cloud, or the collection name
    /// on-premises.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;

    public bool IsAzureDevOpsService { get; set; }
}
