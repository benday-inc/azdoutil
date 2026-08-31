using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// What one Azure DevOps collection says it can serve, as returned by OPTIONS
/// on its _apis root.
///
/// This is the only version-negotiation mechanism that works without already
/// knowing the product version: the OPTIONS call itself takes no api-version,
/// so it answers on a server of any age.  Neither connectionData nor any
/// response header carries the server's version, which is why the catalog is
/// worth the round trip.
/// </summary>
public sealed class ApiVersionCatalog
{
    private readonly List<ApiResourceLocation> _locations;
    private readonly List<(Regex Matcher, ApiResourceLocation Location)> _matchers;

    private ApiVersionCatalog(List<ApiResourceLocation> locations)
    {
        _locations = locations;

        _matchers = locations
            .Select(location => (BuildMatcher(location), location))
            .ToList();

        MaxReleasedVersion = Highest(locations.Select(x => x.ReleasedVersion));
        MaxVersion = Highest(locations.Select(x => x.MaxVersion));
    }

    public IReadOnlyList<ApiResourceLocation> Locations => _locations;

    /// <summary>
    /// The highest non-preview api-version anywhere in the catalog.  This is
    /// the number that identifies the product wave -- 5.0 on Azure DevOps
    /// Server 2019, 7.1 on Azure DevOps Services.
    /// </summary>
    public ApiVersion MaxReleasedVersion { get; }

    /// <summary>
    /// The highest api-version anywhere in the catalog, previews included.  This
    /// is the number the server quotes back when it rejects a request as out of
    /// range.
    /// </summary>
    public ApiVersion MaxVersion { get; }

    private static ApiVersion Highest(IEnumerable<ApiVersion> versions)
    {
        var highest = new ApiVersion(0, 0);

        foreach (var version in versions)
        {
            if (version > highest)
            {
                highest = version;
            }
        }

        return highest;
    }

    public static ApiVersionCatalog? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) == true)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.TryGetProperty("value", out var value) == false ||
                value.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var locations = new List<ApiResourceLocation>();

            foreach (var item in value.EnumerateArray())
            {
                var location = ReadLocation(item);

                if (location != null)
                {
                    locations.Add(location);
                }
            }

            return locations.Count == 0 ? null : new ApiVersionCatalog(locations);
        }
        catch (JsonException)
        {
            // an on-prem server that wants a sign-in answers with html rather
            // than json, and that is a failed probe, not a crash
            return null;
        }
    }

    private static ApiResourceLocation? ReadLocation(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var routeTemplate = ReadString(item, "routeTemplate");

        if (string.IsNullOrWhiteSpace(routeTemplate) == true)
        {
            return null;
        }

        return new ApiResourceLocation
        {
            Area = ReadString(item, "area"),
            ResourceName = ReadString(item, "resourceName"),
            RouteTemplate = routeTemplate,
            MinVersion = ReadVersion(item, "minVersion"),
            MaxVersion = ReadVersion(item, "maxVersion"),
            ReleasedVersion = ReadVersion(item, "releasedVersion"),
            Specificity = CountFixedSegments(routeTemplate)
        };
    }

    private static string ReadString(JsonElement item, string propertyName)
    {
        if (item.TryGetProperty(propertyName, out var property) == false)
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.ToString(),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Version properties arrive as strings ("5.0") from some collections and as
    /// json numbers (6.0) from others, so both are read as text before parsing.
    /// </summary>
    private static ApiVersion ReadVersion(JsonElement item, string propertyName)
    {
        var raw = ReadString(item, propertyName);

        return ApiVersion.TryParse(raw, out var version) == true ? version : new ApiVersion(0, 0);
    }

    /// <summary>
    /// The {area} and {resource} tokens count as fixed, because they are filled
    /// in from the location and match as literal text.
    /// </summary>
    private static int CountFixedSegments(string routeTemplate)
    {
        return routeTemplate
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Count(segment =>
                IsToken(segment) == false ||
                string.Equals(segment, "{area}", StringComparison.OrdinalIgnoreCase) == true ||
                string.Equals(segment, "{resource}", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool IsToken(string segment) =>
        segment.StartsWith('{') == true && segment.EndsWith('}') == true;

    /// <summary>
    /// Turns a route template into something a request path can be tested
    /// against.  The {area} and {resource} tokens are filled in from the
    /// location itself, so they match as literal text; every other token is
    /// optional, because the templates spell out ids and project names that a
    /// given url may or may not carry.
    /// </summary>
    private static Regex BuildMatcher(ApiResourceLocation location)
    {
        var pattern = new StringBuilder("^");

        foreach (var segment in location.RouteTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsToken(segment) == false)
            {
                pattern.Append('/').Append(Regex.Escape(segment));

                continue;
            }

            var tokenName = segment[1..^1];

            if (string.Equals(tokenName, "area", StringComparison.OrdinalIgnoreCase) == true &&
                location.Area.Length > 0)
            {
                pattern.Append('/').Append(Regex.Escape(location.Area));
            }
            else if (string.Equals(tokenName, "resource", StringComparison.OrdinalIgnoreCase) == true &&
                location.ResourceName.Length > 0)
            {
                pattern.Append('/').Append(Regex.Escape(location.ResourceName));
            }
            else if (tokenName.StartsWith('*') == true)
            {
                pattern.Append("(?:/.*)?");
            }
            else
            {
                pattern.Append("(?:/[^/]+)?");
            }
        }

        pattern.Append('$');

        return new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// The location that best describes <paramref name="requestPath"/>, or null
    /// when the catalog has nothing that looks like it.
    /// </summary>
    public ApiResourceLocation? FindLocation(string? requestPath)
    {
        var normalized = NormalizePath(requestPath);

        if (normalized == null)
        {
            return null;
        }

        ApiResourceLocation? best = null;

        foreach (var (matcher, location) in _matchers)
        {
            if (matcher.IsMatch(normalized) == false)
            {
                continue;
            }

            if (best == null || location.Specificity > best.Specificity)
            {
                best = location;
            }
        }

        return best;
    }

    /// <summary>
    /// Route templates are written relative to the collection, so the path is
    /// cut back to the _apis segment.  Anything ahead of it is the collection
    /// and, on a project-scoped url, the project -- both of which the templates
    /// treat as optional tokens.
    /// </summary>
    internal static string? NormalizePath(string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath) == true)
        {
            return null;
        }

        var path = requestPath;

        var queryIndex = path.IndexOfAny(['?', '#']);

        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        if (path.StartsWith('/') == false)
        {
            path = "/" + path;
        }

        var apisIndex = path.IndexOf("/_apis/", StringComparison.OrdinalIgnoreCase);

        if (apisIndex < 0)
        {
            return path.EndsWith("/_apis", StringComparison.OrdinalIgnoreCase) == true ? "/_apis" : null;
        }

        return path[apisIndex..].TrimEnd('/');
    }

    /// <summary>
    /// The version this collection should be asked for, given what the caller
    /// wanted.  Returns false when the request is already within range and
    /// should be left exactly as it is -- this only ever lowers a version, so a
    /// collection that is newer than the caller sees no change at all.
    /// </summary>
    public bool TryResolve(string? requestPath, ApiVersion requested, out ApiVersion resolved)
    {
        resolved = requested;

        var location = FindLocation(requestPath);

        // a preview request wants the preview form of the resource, so it is
        // measured against the preview ceiling; a released request is held to
        // what actually shipped
        var ceiling = requested.IsPreview == true
            ? location?.MaxVersion ?? MaxVersion
            : location?.ReleasedVersion ?? MaxReleasedVersion;

        // a resource that never left preview reports a released version of 0.0,
        // and there is no released form of it to fall back to
        if (ceiling.IsEmpty == true)
        {
            ceiling = location?.MaxVersion ?? MaxVersion;
        }

        if (ceiling.IsEmpty == true || requested <= ceiling)
        {
            return false;
        }

        resolved = requested.WithNumberOf(ceiling);

        return resolved != requested;
    }
}
