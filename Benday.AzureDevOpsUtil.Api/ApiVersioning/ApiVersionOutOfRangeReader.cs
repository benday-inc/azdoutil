using System.Text.RegularExpressions;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// Recognises the response a collection sends when the api-version it was asked
/// for is newer than anything it has:
///
///   HTTP 400
///   {"message":"The requested REST API version of 7.0 is out of range for this
///    server. The latest REST API version this server supports is 5.1.",
///    "typeKey":"VssVersionOutOfRangeException", ...}
///
/// The rejection names the ceiling, so a failed call teaches the tool what the
/// server can do without a separate probe.
///
/// Note that this is the only status worth reading this way.  A 404 from an
/// older server means the route does not exist there at all -- the endpoint
/// arrived in a later release -- and no api-version will bring it back.
/// </summary>
public static class ApiVersionOutOfRangeReader
{
    private const string TypeKey = "VssVersionOutOfRangeException";

    private const string Phrase = "is out of range for this server";

    private static readonly Regex SupportedVersion =
        new(@"latest\s+REST\s+API\s+version\s+this\s+server\s+supports\s+is\s+(?<version>\d+(?:\.\d+)?(?:-preview(?:\.\d+)?)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsVersionOutOfRange(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody) == true)
        {
            return false;
        }

        return responseBody.Contains(TypeKey, StringComparison.OrdinalIgnoreCase) == true ||
            responseBody.Contains(Phrase, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// The ceiling the rejection names, when it names one.  The message is not
    /// guaranteed to carry it -- it is localized, and older builds word it
    /// differently -- so a caller has to cope with false here.
    /// </summary>
    public static bool TryReadSupportedVersion(string? responseBody, out ApiVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(responseBody) == true)
        {
            return false;
        }

        var match = SupportedVersion.Match(responseBody);

        if (match.Success == false)
        {
            return false;
        }

        // a trailing sentence period is not part of the number
        var raw = match.Groups["version"].Value.TrimEnd('.');

        return ApiVersion.TryParse(raw, out version);
    }
}
