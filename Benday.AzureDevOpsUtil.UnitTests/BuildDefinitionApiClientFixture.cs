using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BuildDefinitionApiClientFixture
{
    private const string ProjectName = "GnarlyCorp";

    private const string MappingJson =
        "{\"mappings\":[" +
        "{\"serverPath\":\"$/GnarlyCorp/Main\",\"mappingType\":\"map\",\"localPath\":\"\\\\\"}," +
        "{\"serverPath\":\"$/GnarlyCorp/Common\",\"mappingType\":\"map\",\"localPath\":\"\\\\\"}," +
        "{\"serverPath\":\"$/GnarlyCorp/Main/Drops\",\"mappingType\":\"cloak\",\"localPath\":\"\\\\\"}" +
        "]}";

    /// <summary>
    /// Serializing rather than hand-escaping produces the same shape the server
    /// sends: the mappings arrive as a JSON document inside a string value.
    /// </summary>
    private static string BuildDefinitionPayload()
    {
        var payload = new
        {
            id = 12,
            name = "Legacy-CI",
            path = "\\",
            repository = new
            {
                id = "$/GnarlyCorp",
                name = "$/GnarlyCorp",
                type = "TfsVersionControl",
                properties = new Dictionary<string, string>
                {
                    { "tfvcMapping", MappingJson },
                    { "cleanOptions", "0" }
                }
            },
            latestCompletedBuild = new
            {
                id = 4321,
                finishTime = "2026-05-01T10:00:00Z"
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    [TestMethod]
    public async Task GetDefinitions_ReadsTheShallowList()
    {
        var json = "{\"count\":2,\"value\":[" +
            "{\"id\":12,\"name\":\"Legacy-CI\"}," +
            "{\"id\":13,\"name\":\"Modern-CI\"}]}";

        var requestedUrls = new List<string>();

        var client = new BuildDefinitionApiClient(url =>
        {
            requestedUrls.Add(url);
            return Task.FromResult<string?>(json);
        });

        var actual = await client.GetDefinitionsAsync(ProjectName);

        Assert.AreEqual(2, actual.Count, "Expected two definitions.");
        Assert.AreEqual("Legacy-CI", actual[0].Name, "Wrong definition name.");

        StringAssert.Contains(
            requestedUrls.Single(), "_apis/build/definitions", "Wrong endpoint.");
        StringAssert.Contains(
            requestedUrls.Single(), "api-version=", "Request should carry an api-version.");
    }

    [TestMethod]
    public async Task GetDefinition_ReadsRepositoryAndWorkspaceMappings()
    {
        var requestedUrls = new List<string>();

        var client = new BuildDefinitionApiClient(url =>
        {
            requestedUrls.Add(url);
            return Task.FromResult<string?>(BuildDefinitionPayload());
        });

        var actual = await client.GetDefinitionAsync(ProjectName, 12);

        Assert.IsNotNull(actual, "Expected a definition.");
        Assert.AreEqual("Legacy-CI", actual!.Name, "Wrong definition name.");
        Assert.IsNotNull(actual.Repository, "Expected a repository.");
        Assert.IsTrue(actual.Repository!.IsTfvc, "Repository should read as TFVC.");

        var mappings = TfvcWorkspaceMappingParser.Parse(actual.Repository.GetTfvcMappingJson());

        Assert.AreEqual(3, mappings.Count, "Expected three mapping entries.");
        Assert.AreEqual(
            2, mappings.Count(x => x.IsMap), "Expected two map entries.");
        Assert.AreEqual(
            1, mappings.Count(x => x.IsCloak), "Expected one cloak entry.");
    }

    [TestMethod]
    public async Task GetDefinition_AsksForTheLatestBuildsSoTheDateComesForFree()
    {
        var requestedUrls = new List<string>();

        var client = new BuildDefinitionApiClient(url =>
        {
            requestedUrls.Add(url);
            return Task.FromResult<string?>(BuildDefinitionPayload());
        });

        var actual = await client.GetDefinitionAsync(ProjectName, 12);

        StringAssert.Contains(
            requestedUrls.Single(),
            "includeLatestBuilds=true",
            "The last run date should arrive with the definition.");

        StringAssert.Contains(requestedUrls.Single(), "/definitions/12", "Wrong definition id.");

        Assert.IsNotNull(actual!.LatestCompletedBuild, "Expected the latest completed build.");
        Assert.AreEqual(
            new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc).ToUniversalTime(),
            actual.LatestCompletedBuild!.FinishTime.ToUniversalTime(),
            "Wrong finish time.");
    }

    [TestMethod]
    public async Task GetDefinition_FailedCallReturnsNull()
    {
        var client = new BuildDefinitionApiClient(url => Task.FromResult<string?>(null));

        var actual = await client.GetDefinitionAsync(ProjectName, 12);

        Assert.IsNull(actual, "A failed call should not produce a definition.");
    }

    [TestMethod]
    public async Task GetDefinition_MalformedJsonReturnsNull()
    {
        var client = new BuildDefinitionApiClient(url => Task.FromResult<string?>("{not json"));

        var actual = await client.GetDefinitionAsync(ProjectName, 12);

        Assert.IsNull(actual, "Unreadable json should not throw.");
    }

    [TestMethod]
    public async Task GetDefinitions_FailedCallReturnsEmptyList()
    {
        var client = new BuildDefinitionApiClient(url => Task.FromResult<string?>(null));

        var actual = await client.GetDefinitionsAsync(ProjectName);

        Assert.AreEqual(0, actual.Count, "A failed call should produce no definitions.");
    }
}
