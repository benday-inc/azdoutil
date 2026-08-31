using System.Net;
using System.Text;

using Benday.AzureDevOpsUtil.Api.ApiVersioning;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ApiVersionClampingHandlerFixture
{
    private const string CollectionUrl = "https://server/tfs/DefaultCollection/";

    private const string CatalogJson = """
    {
      "count": 1,
      "value": [
        { "id": "603fe2ac", "area": "core", "resourceName": "projects",
          "routeTemplate": "_apis/{resource}/{*projectId}",
          "resourceVersion": 4, "minVersion": "1.0", "maxVersion": "5.1", "releasedVersion": "5.0" }
      ]
    }
    """;

    private const string OutOfRangeBody =
        "{\"message\":\"The requested REST API version of 7.0 is out of range for this server. " +
        "The latest REST API version this server supports is 5.1.\"," +
        "\"typeKey\":\"VssVersionOutOfRangeException\"}";

    private int _ProbeCount;

    [TestInitialize]
    public void OnTestInitialize()
    {
        // the cache is process-wide, so one test's server would otherwise
        // answer for the next one
        ServerApiVersionCache.Reset();

        _ProbeCount = 0;
    }

    [TestCleanup]
    public void OnTestCleanup()
    {
        ServerApiVersionCache.Reset();
    }

    /// <summary>
    /// Stands in for the collection: hands back scripted responses and records
    /// exactly what it was asked for.
    /// </summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _Responses;

        public RecordingHandler(params HttpResponseMessage[] responses)
        {
            _Responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<string> RequestUrls { get; } = [];

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUrls.Add(request.RequestUri?.ToString() ?? string.Empty);

            RequestBodies.Add(request.Content == null ?
                string.Empty :
                await request.Content.ReadAsStringAsync(cancellationToken));

            if (_Responses.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Unexpected request to '{request.RequestUri}'.");
            }

            return _Responses.Dequeue();
        }
    }

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private HttpClient CreateClient(RecordingHandler inner, string? catalogJson)
    {
        var handler = new ApiVersionClampingHandler(inner, CollectionUrl, _ =>
        {
            _ProbeCount++;

            return Task.FromResult(catalogJson);
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(CollectionUrl)
        };
    }

    [TestMethod]
    public async Task RetriesWithTheVersionTheCollectionSupports()
    {
        // arrange
        var inner = new RecordingHandler(
            Response(HttpStatusCode.BadRequest, OutOfRangeBody),
            Response(HttpStatusCode.OK, "{\"count\":0,\"value\":[]}"));

        using var client = CreateClient(inner, CatalogJson);

        // act
        using var actual = await client.GetAsync("_apis/projects?$top=10000&api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(2, inner.RequestUrls.Count, "request count");
        Assert.IsTrue(inner.RequestUrls[0].Contains("api-version=7.0"),
            "the first attempt goes out as the command wrote it");
        Assert.IsTrue(inner.RequestUrls[1].Contains("api-version=5.0"),
            $"the retry should be clamped -- was '{inner.RequestUrls[1]}'");
        Assert.IsTrue(inner.RequestUrls[1].Contains("$top=10000"),
            "the rest of the query survives the rewrite");
    }

    [TestMethod]
    public async Task CurrentCollectionIsNeverProbedOrRetried()
    {
        // arrange
        var inner = new RecordingHandler(Response(HttpStatusCode.OK, "{}"));

        using var client = CreateClient(inner, CatalogJson);

        // act
        using var actual = await client.GetAsync("_apis/projects?api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, inner.RequestUrls.Count, "a working call costs nothing extra");
        Assert.AreEqual<int>(0, _ProbeCount, "no probe");
    }

    [TestMethod]
    public async Task AnUnrelatedBadRequestIsPassedStraightBack()
    {
        // arrange
        var body = "{\"message\":\"TF401019: The Git repository does not exist.\"}";

        var inner = new RecordingHandler(Response(HttpStatusCode.BadRequest, body));

        using var client = CreateClient(inner, CatalogJson);

        // act
        using var actual = await client.GetAsync("_apis/git/repositories?api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.BadRequest, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, inner.RequestUrls.Count, "no retry");
        Assert.AreEqual<int>(0, _ProbeCount, "no probe");
        Assert.AreEqual<string>(body, await actual.Content.ReadAsStringAsync(),
            "the body is still readable by the caller");
    }

    /// <summary>
    /// The probe is an optimization.  When it fails, the rejection itself still
    /// named a ceiling, and that is enough to retry on.
    /// </summary>
    [TestMethod]
    public async Task FallsBackToTheCeilingNamedInTheRejection()
    {
        // arrange
        var inner = new RecordingHandler(
            Response(HttpStatusCode.BadRequest, OutOfRangeBody),
            Response(HttpStatusCode.OK, "{}"));

        using var client = CreateClient(inner, catalogJson: null);

        // act
        using var actual = await client.GetAsync("_apis/projects?api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.IsTrue(inner.RequestUrls[1].Contains("api-version=5.1"),
            $"clamped to what the rejection named -- was '{inner.RequestUrls[1]}'");
    }

    [TestMethod]
    public async Task ARequestBodySurvivesTheRetry()
    {
        // arrange
        var inner = new RecordingHandler(
            Response(HttpStatusCode.BadRequest, OutOfRangeBody),
            Response(HttpStatusCode.OK, "{}"));

        using var client = CreateClient(inner, CatalogJson);

        var body = "{\"name\":\"GnarlyCorp\"}";

        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        // act
        using var actual = await client.PostAsync("_apis/projects?api-version=7.0", content);

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(2, inner.RequestBodies.Count, "request count");
        Assert.AreEqual<string>(body, inner.RequestBodies[1], "the retry carries the same body");
    }

    /// <summary>
    /// Azure DevOps Server 2019 turns a too-new api-version down at the routing
    /// layer rather than the api layer, so it answers 404 with nothing in the
    /// body to say why.  Verified against a real 2019 collection: the same url
    /// succeeds with no api-version on it and 404s with api-version=7.0.
    /// </summary>
    [TestMethod]
    public async Task A404FromAnOlderServerIsRetriedWithAClampedVersion()
    {
        // arrange
        var inner = new RecordingHandler(
            Response(HttpStatusCode.NotFound, "<html><body>404 - not found</body></html>"),
            Response(HttpStatusCode.OK, "{\"count\":0,\"value\":[]}"));

        using var client = CreateClient(inner, CatalogJson);

        // act
        using var actual = await client.GetAsync("_apis/projects?api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(2, inner.RequestUrls.Count, "request count");
        Assert.IsTrue(inner.RequestUrls[1].Contains("api-version=5.0"),
            $"the retry should be clamped -- was '{inner.RequestUrls[1]}'");
    }

    /// <summary>
    /// A 404 is ambiguous, so the catalog is what tells the two cases apart.
    /// When it says the version was fine, the endpoint simply is not there and
    /// no api-version will conjure it up.
    /// </summary>
    [TestMethod]
    public async Task A404TheCatalogCannotBlameOnTheVersionIsPassedBack()
    {
        // arrange
        var inner = new RecordingHandler(Response(HttpStatusCode.NotFound, "not found"));

        using var client = CreateClient(inner, CatalogJson);

        // act -- 4.0 is within what this collection serves
        using var actual = await client.GetAsync("_apis/projects?api-version=4.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.NotFound, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, inner.RequestUrls.Count, "no retry");
        Assert.AreEqual<string>("not found", await actual.Content.ReadAsStringAsync(),
            "the body is still readable by the caller");
    }

    [TestMethod]
    public async Task A404OnAUrlWithNoVersionIsNotInvestigated()
    {
        // arrange
        var inner = new RecordingHandler(Response(HttpStatusCode.NotFound, "not found"));

        using var client = CreateClient(inner, CatalogJson);

        // act
        using var actual = await client.GetAsync("_apis/nonesuch");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.NotFound, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, inner.RequestUrls.Count, "no retry");
        Assert.AreEqual<int>(0, _ProbeCount, "nothing to investigate");
    }

    /// <summary>
    /// A pinned version is what makes a collection usable when it will not
    /// answer the OPTIONS probe at all -- there is no failure to learn from in
    /// that case, because a 404 carries no ceiling.
    /// </summary>
    [TestMethod]
    public async Task APinnedVersionIsAppliedWithoutProbingOrFailingFirst()
    {
        // arrange
        Assert.IsTrue(ApiVersion.TryParse("5.0", out var pinned), "should parse");

        ServerApiVersionCache.Set(CollectionUrl, ServerApiVersionInfo.Pinned(pinned));

        var inner = new RecordingHandler(Response(HttpStatusCode.OK, "{}"));

        using var client = CreateClient(inner, catalogJson: null);

        // act
        using var actual = await client.GetAsync("_apis/projects?api-version=7.0");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, inner.RequestUrls.Count, "nothing had to fail first");
        Assert.AreEqual<int>(0, _ProbeCount, "a pin overrules discovery");
        Assert.IsTrue(inner.RequestUrls[0].Contains("api-version=5.0"),
            $"clamped to the pin -- was '{inner.RequestUrls[0]}'");
    }

    /// <summary>
    /// What the first rejection taught applies to everything afterwards, so the
    /// cost of an old collection is one rejected request per run rather than one
    /// per call.
    /// </summary>
    [TestMethod]
    public async Task WhatWasLearnedIsAppliedUpFrontAfterwards()
    {
        // arrange
        var first = new RecordingHandler(
            Response(HttpStatusCode.BadRequest, OutOfRangeBody),
            Response(HttpStatusCode.OK, "{}"));

        using var firstClient = CreateClient(first, CatalogJson);

        (await firstClient.GetAsync("_apis/projects?api-version=7.0")).Dispose();

        var second = new RecordingHandler(Response(HttpStatusCode.OK, "{}"));

        using var secondClient = CreateClient(second, CatalogJson);

        // act
        using var actual = await secondClient.GetAsync("_apis/projects?api-version=7.1");

        // assert
        Assert.AreEqual<HttpStatusCode>(HttpStatusCode.OK, actual.StatusCode, "final status");
        Assert.AreEqual<int>(1, second.RequestUrls.Count, "no second rejection to recover from");
        Assert.IsTrue(second.RequestUrls[0].Contains("api-version=5.0"),
            $"clamped before it was sent -- was '{second.RequestUrls[0]}'");
        Assert.AreEqual<int>(1, _ProbeCount, "the collection is probed once, not once per call");
    }
}
