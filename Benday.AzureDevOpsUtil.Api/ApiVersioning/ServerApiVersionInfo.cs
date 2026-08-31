namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// What is known about one collection's api-version support.
///
/// The good case is a full catalog from the OPTIONS probe.  The fallback is the
/// single number a server quotes when it turns a request down ("The latest REST
/// API version this server supports is 5.1"), which is coarser -- it is the
/// preview ceiling rather than what shipped -- but it comes free with the
/// rejection and beats knowing nothing.
/// </summary>
public sealed class ServerApiVersionInfo
{
    private ServerApiVersionInfo(
        ApiVersionCatalog? catalog, ApiVersion reportedMaximum, bool isPinned)
    {
        Catalog = catalog;
        ReportedMaximum = reportedMaximum;
        IsPinned = isPinned;
    }

    public ApiVersionCatalog? Catalog { get; }

    public ApiVersion ReportedMaximum { get; }

    /// <summary>
    /// Set when the ceiling came from the stored configuration rather than from
    /// the collection.  A person who pinned a version has overruled discovery,
    /// so nothing discovered later may quietly replace it.
    /// </summary>
    public bool IsPinned { get; }

    public static ServerApiVersionInfo FromCatalog(ApiVersionCatalog catalog) =>
        new(catalog, catalog.MaxVersion, false);

    public static ServerApiVersionInfo FromReportedMaximum(ApiVersion reportedMaximum) =>
        new(null, reportedMaximum, false);

    public static ServerApiVersionInfo Pinned(ApiVersion maximum) =>
        new(null, maximum, true);

    /// <summary>
    /// The version identifying this collection's product wave, for display.
    /// </summary>
    public ApiVersion Ceiling => Catalog?.MaxReleasedVersion ?? ReportedMaximum;

    public bool TryResolve(string? requestPath, ApiVersion requested, out ApiVersion resolved)
    {
        if (Catalog != null)
        {
            return Catalog.TryResolve(requestPath, requested, out resolved);
        }

        resolved = requested;

        if (ReportedMaximum.IsEmpty == true || requested <= ReportedMaximum)
        {
            return false;
        }

        resolved = requested.WithNumberOf(ReportedMaximum);

        return resolved != requested;
    }
}
