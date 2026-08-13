using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcWorkspaceMappingParserFixture
{
    /// <summary>
    /// The shape a real classic definition uses: the mappings are a JSON
    /// document held inside a string value.
    /// </summary>
    private const string RealWorldMappingJson =
        "{\"mappings\":[" +
        "{\"serverPath\":\"$/GnarlyCorp/Main\",\"mappingType\":\"map\",\"localPath\":\"\\\\\"}," +
        "{\"serverPath\":\"$/GnarlyCorp/Main/Drops\",\"mappingType\":\"cloak\",\"localPath\":\"\\\\\"}" +
        "]}";

    [TestMethod]
    public void Parse_ReadsMapAndCloakEntries()
    {
        var actual = TfvcWorkspaceMappingParser.Parse(RealWorldMappingJson);

        Assert.AreEqual(2, actual.Count, "Expected two mapping entries.");

        Assert.AreEqual("$/GnarlyCorp/Main", actual[0].ServerPath, "Wrong server path.");
        Assert.IsTrue(actual[0].IsMap, "First entry should be a map.");
        Assert.IsFalse(actual[0].IsCloak, "First entry should not be a cloak.");

        Assert.AreEqual("$/GnarlyCorp/Main/Drops", actual[1].ServerPath, "Wrong server path.");
        Assert.IsTrue(actual[1].IsCloak, "Second entry should be a cloak.");
        Assert.IsFalse(actual[1].IsMap, "Second entry should not be a map.");
    }

    [TestMethod]
    public void Parse_MappingTypeIsCaseInsensitive()
    {
        // The REST payload uses lower case; the older object model uses "Map".
        var json =
            "{\"mappings\":[{\"serverPath\":\"$/App/Main\",\"mappingType\":\"Map\"}," +
            "{\"serverPath\":\"$/App/Bin\",\"mappingType\":\"Cloak\"}]}";

        var actual = TfvcWorkspaceMappingParser.Parse(json);

        Assert.IsTrue(actual[0].IsMap, "Capitalized Map should still read as a map.");
        Assert.IsTrue(actual[1].IsCloak, "Capitalized Cloak should still read as a cloak.");
    }

    [TestMethod]
    public void Parse_NullOrEmptyReturnsNothing()
    {
        Assert.AreEqual(0, TfvcWorkspaceMappingParser.Parse(null).Count, "Null should be empty.");
        Assert.AreEqual(0, TfvcWorkspaceMappingParser.Parse("").Count, "Empty should be empty.");
        Assert.AreEqual(
            0, TfvcWorkspaceMappingParser.Parse("   ").Count, "Whitespace should be empty.");
    }

    [TestMethod]
    public void Parse_MalformedJsonDoesNotThrow()
    {
        var actual = TfvcWorkspaceMappingParser.Parse("{not json at all");

        Assert.AreEqual(0, actual.Count, "Unreadable mappings should come back empty.");
    }

    [TestMethod]
    public void Parse_SkipsEntriesWithNoServerPath()
    {
        var json =
            "{\"mappings\":[{\"serverPath\":\"$/App/Main\",\"mappingType\":\"map\"}," +
            "{\"mappingType\":\"map\"}]}";

        var actual = TfvcWorkspaceMappingParser.Parse(json);

        Assert.AreEqual(1, actual.Count, "An entry with no server path is not usable.");
    }

    [TestMethod]
    public void GetTfvcMappingJson_PullsTheValueOutOfRepositoryProperties()
    {
        var repository = new BuildRepositoryInfo
        {
            Type = BuildRepositoryInfo.TypeTfvc
        };

        repository.Properties["tfvcMapping"] =
            FakeBuildDefinitionApiClient.StringElement(RealWorldMappingJson);

        var actual = repository.GetTfvcMappingJson();

        Assert.AreEqual(RealWorldMappingJson, actual, "Wrong mapping json.");
        Assert.IsTrue(repository.IsTfvc, "Repository should read as TFVC.");
    }

    [TestMethod]
    public void GetTfvcMappingJson_ReturnsNullWhenAbsent()
    {
        var repository = new BuildRepositoryInfo();

        Assert.IsNull(repository.GetTfvcMappingJson(), "There is no mapping to return.");
        Assert.IsFalse(repository.IsTfvc, "An empty repository type is not TFVC.");
    }

    [TestMethod]
    public void IsTfvc_IgnoresCase()
    {
        var repository = new BuildRepositoryInfo { Type = "tfsversioncontrol" };

        Assert.IsTrue(repository.IsTfvc, "Repository type comparison should ignore case.");
    }
}
