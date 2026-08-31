using System.Text;

namespace Benday.AzureDevOpsUtil.Api;
public class AzureDevOpsConfiguration
{
    private string _CollectionUrl = string.Empty;

    public string Name { get; set; } = Constants.DefaultConfigurationName;
    public string CollectionUrl
    {
        get
        {
            if (_CollectionUrl.Length > 0)
            {
                if (_CollectionUrl.Trim().EndsWith("/") == false)
                {
                    _CollectionUrl = $"{_CollectionUrl}/";
                }
            }

            return _CollectionUrl;
        }

        set => _CollectionUrl = value;
    }
    public string Token { get; set; } = string.Empty;
    public bool IsWindowsAuth { get; set; }

    /// <summary>
    /// Highest REST api-version this collection will be asked for, overruling
    /// what it would otherwise be discovered to support.
    ///
    /// Discovery needs the collection to answer OPTIONS on its _apis root.  A
    /// server that will not -- a proxy or an IIS configuration that blocks the
    /// verb -- leaves nothing to go on, and pinning the version here is what
    /// makes such a collection usable.  Empty means discover it.
    /// </summary>
    public string MaxApiVersion { get; set; } = string.Empty;
    public bool IsAzureDevOpsService
    {
        get
        {
            return CollectionUrl.Contains("dev.azure.com");
        }
    }

    public string AccountNameOrCollectionName
    {
        get
        {
            if (Uri.TryCreate(CollectionUrl, UriKind.Absolute, out var uri) == false)
            {
                return string.Empty;
            }
            else
            {
                var segments = uri.Segments;

                if (segments.Length < 2)
                {
                    return string.Empty;
                }
                else
                {
                    return segments[1].Replace("/", "");
                }
            }
        }
    }

    public string AnalyticsUrl
    {
        get
        {
            if (IsAzureDevOpsService == false)
            {
                return CollectionUrl;
            }
            else
            {
                return $"https://analytics.dev.azure.com/{AccountNameOrCollectionName}/";
            }
        }
    }

    public string GetTokenBase64Encoded()
    {
        var tokenBase64 = Convert.ToBase64String(
            ASCIIEncoding.ASCII.GetBytes(":" + Token));

        return tokenBase64;
    }
}