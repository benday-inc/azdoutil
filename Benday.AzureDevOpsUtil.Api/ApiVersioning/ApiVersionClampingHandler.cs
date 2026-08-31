using System.Net;

namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// Lowers the api-version on outbound requests to what the collection can
/// actually serve.
///
/// Commands name the api-version they were written against, and those numbers
/// are spread across dozens of url strings.  Against Azure DevOps Services they
/// are all fine; against an older on-prem server -- Azure DevOps Server 2019
/// tops out at 5.x -- every one of them is too new.  Clamping here rather than
/// at each call site means the numbers in the commands stay as a statement of
/// what the command wants, and one place decides what it gets.
///
/// Nothing is probed up front.  The first request goes out as written, and only
/// a rejection triggers discovery, so a current collection pays nothing at all.
/// The version is only ever lowered, never raised.
/// </summary>
public sealed class ApiVersionClampingHandler : DelegatingHandler
{
    private readonly string _CollectionUrl;
    private readonly Func<CancellationToken, Task<string?>> _ProbeCatalog;

    /// <param name="collectionUrl">
    /// Identifies the server for caching.  Requests to the release-management
    /// host of the same account share it, because they share a deployment.
    /// </param>
    /// <param name="probeCatalog">
    /// Fetches the OPTIONS catalog.  Passed in rather than done here, because
    /// the credentials live on the client above this handler.
    /// </param>
    public ApiVersionClampingHandler(
        HttpMessageHandler innerHandler,
        string collectionUrl,
        Func<CancellationToken, Task<string?>> probeCatalog) : base(innerHandler)
    {
        _CollectionUrl = collectionUrl;
        _ProbeCatalog = probeCatalog;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var known = ServerApiVersionCache.Get(_CollectionUrl);

        if (known != null)
        {
            TryRewrite(request, known);

            return await base.SendAsync(request, cancellationToken);
        }

        // nothing is known yet, so this request may have to be sent twice; a
        // body that can only be read once would not survive that
        await MakeContentRepeatable(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        var body = await GetVersionFailureBody(
            response, request.RequestUri?.ToString(), cancellationToken);

        if (body == null)
        {
            return response;
        }

        var info = await ServerApiVersionCache.GetOrDiscoverAsync(
            _CollectionUrl, () => Discover(body, cancellationToken));

        var retry = await Clone(request, cancellationToken);

        if (TryRewrite(retry, info) == false)
        {
            // either discovery came back with nothing usable, or it says the
            // version was fine all along and this failure is about something
            // else -- a route that does not exist on this server, say.  The
            // original response is the more honest answer.
            retry.Dispose();

            return response;
        }

        response.Dispose();

        return await base.SendAsync(retry, cancellationToken);
    }

    /// <summary>
    /// The response body when this failure could be about the api-version, or
    /// null when it plainly is not and the response should be handed straight
    /// back.
    ///
    /// Servers disagree about how to say it.  Azure DevOps Services rejects a
    /// too-new version at the api layer, with 400 and a message naming its
    /// ceiling.  Azure DevOps Server 2019 rejects it at the routing layer
    /// instead, with a bare 404 -- the same 404 as a genuinely missing
    /// endpoint, and carrying nothing to tell the two apart.  So a 404 is worth
    /// looking into, but only the catalog can say whether it was the version.
    /// </summary>
    private static async Task<string?> GetVersionFailureBody(
        HttpResponseMessage response, string? requestUrl, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.BadRequest &&
            response.StatusCode != HttpStatusCode.NotFound)
        {
            return null;
        }

        // a failure on a url that names no version cannot be about the version
        if (ApiVersionRequestRewriter.ContainsApiVersion(requestUrl) == false)
        {
            return null;
        }

        // buffering first leaves the body readable by whoever gets this
        // response back, in the case where it turns out not to be ours to handle
        await response.Content.LoadIntoBufferAsync();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return body;
        }

        return ApiVersionOutOfRangeReader.IsVersionOutOfRange(body) == true ? body : null;
    }

    private async Task<ServerApiVersionInfo> Discover(string errorBody, CancellationToken cancellationToken)
    {
        try
        {
            var catalog = ApiVersionCatalog.Parse(await _ProbeCatalog(cancellationToken));

            if (catalog != null)
            {
                return ServerApiVersionInfo.FromCatalog(catalog);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // the probe is an optimization -- the rejection already carries a
            // usable answer, so a failure here is not worth surfacing
        }

        if (ApiVersionOutOfRangeReader.TryReadSupportedVersion(errorBody, out var reported) == true)
        {
            return ServerApiVersionInfo.FromReportedMaximum(reported);
        }

        // caching the fact that nothing could be learned stops every subsequent
        // request in this process from probing again
        return ServerApiVersionInfo.FromReportedMaximum(new ApiVersion(0, 0));
    }

    private static bool TryRewrite(HttpRequestMessage request, ServerApiVersionInfo info)
    {
        var uri = request.RequestUri;

        if (uri == null)
        {
            return false;
        }

        var original = uri.IsAbsoluteUri == true ? uri.AbsoluteUri : uri.OriginalString;

        var rewritten = ApiVersionRequestRewriter.Rewrite(original, info);

        if (rewritten == null)
        {
            return false;
        }

        request.RequestUri = new Uri(
            rewritten, uri.IsAbsoluteUri == true ? UriKind.Absolute : UriKind.Relative);

        return true;
    }

    /// <summary>
    /// Replaces the request body with a buffered copy, so that sending it a
    /// second time sends the same bytes.
    /// </summary>
    private static async Task MakeContentRepeatable(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content == null)
        {
            return;
        }

        var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);

        var buffered = new ByteArrayContent(bytes);

        foreach (var header in request.Content.Headers)
        {
            buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        request.Content.Dispose();

        request.Content = buffered;
    }

    private static async Task<HttpRequestMessage> Clone(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in (IDictionary<string, object?>)request.Options)
        {
            ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
        }

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);

            var content = new ByteArrayContent(bytes);

            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        return clone;
    }
}
