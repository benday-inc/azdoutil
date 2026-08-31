using System.Text.RegularExpressions;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// Rewrites the api-version query parameter of a request url down to what the
/// collection can serve.
/// </summary>
public static class ApiVersionRequestRewriter
{
    /// <summary>
    /// Anchored on a query separator so that an "api-version" appearing
    /// somewhere in the path is left alone.
    /// </summary>
    private static readonly Regex ApiVersionParameter =
        new(@"(?<=[?&])api-version=(?<value>[^&#]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Whether the url names an api-version at all.  A failure on a url that
    /// names none cannot be a version problem, so there is nothing to look into.
    /// </summary>
    public static bool ContainsApiVersion(string? url)
    {
        return string.IsNullOrWhiteSpace(url) == false && ApiVersionParameter.IsMatch(url);
    }

    /// <summary>
    /// The url with its api-version lowered, or null when it should be sent
    /// exactly as it stands -- no api-version on it, an unparseable one, or one
    /// the collection already supports.
    /// </summary>
    public static string? Rewrite(string? url, ServerApiVersionInfo info)
    {
        if (string.IsNullOrWhiteSpace(url) == true)
        {
            return null;
        }

        var match = ApiVersionParameter.Match(url);

        if (match.Success == false)
        {
            return null;
        }

        var group = match.Groups["value"];

        if (ApiVersion.TryParse(Uri.UnescapeDataString(group.Value), out var requested) == false)
        {
            return null;
        }

        if (info.TryResolve(url, requested, out var resolved) == false)
        {
            return null;
        }

        return string.Concat(
            url.AsSpan(0, group.Index),
            resolved.ToString(),
            url.AsSpan(group.Index + group.Length));
    }
}
