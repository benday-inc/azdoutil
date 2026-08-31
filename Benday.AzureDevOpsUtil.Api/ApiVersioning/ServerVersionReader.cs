using System.Text.RegularExpressions;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// What a collection reports about its own build, read off the About page.
/// </summary>
public sealed class ServerVersionInfo
{
    /// <summary>
    /// The service version banner, e.g.
    /// "Dev17.M143.4 (AzureDevOpsServer_20190305.4)".
    /// </summary>
    public string ServiceVersion { get; init; } = string.Empty;

    /// <summary>
    /// The four-part assembly version, e.g. "17.143.28621.4".  Present on-prem;
    /// the hosted service does not carry one.
    /// </summary>
    public string BuildNumber { get; init; } = string.Empty;

    public bool IsEmpty =>
        ServiceVersion.Length == 0 && BuildNumber.Length == 0;
}

/// <summary>
/// Reads the server's build out of the About page.
///
/// There is no API for this.  connectionData carries no version, and no
/// response header does either -- the only first-party guidance is to look at
/// {collection}/_home/About, which is a web page.  Every Azure DevOps page
/// embeds its page context as json, and that context has a "serviceVersion",
/// so the version is fetchable even though it is not an endpoint.
///
/// Being a scrape, this is allowed to come back with nothing; callers report
/// what is missing rather than failing.
/// </summary>
public static class ServerVersionReader
{
    /// <summary>The About page, relative to the collection url.</summary>
    public const string AboutPagePath = "_home/About";

    private static readonly Regex ServiceVersionValue =
        new("\"serviceVersion\"\\s*:\\s*\"(?<value>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// A Team Foundation Server / Azure DevOps Server assembly version.  The
    /// major has run from 14 (TFS 2015) upwards, which is specific enough not
    /// to collide with the asset and package versions also on the page.
    /// </summary>
    private static readonly Regex BuildNumberValue =
        new(@"\b(?<value>(?:1[4-9]|2[0-9])\.\d{1,3}\.\d{4,5}\.\d{1,3})\b",
            RegexOptions.CultureInvariant);

    public static ServerVersionInfo Read(string? aboutPageHtml)
    {
        if (string.IsNullOrWhiteSpace(aboutPageHtml) == true)
        {
            return new ServerVersionInfo();
        }

        var serviceVersion = ServiceVersionValue.Match(aboutPageHtml);

        var buildNumber = BuildNumberValue.Match(aboutPageHtml);

        return new ServerVersionInfo
        {
            ServiceVersion = serviceVersion.Success == true ?
                serviceVersion.Groups["value"].Value.Trim() : string.Empty,
            BuildNumber = buildNumber.Success == true ?
                buildNumber.Groups["value"].Value : string.Empty
        };
    }
}
