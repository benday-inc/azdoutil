using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BuildDefinitionWorkspaceServiceFixture
{
    private BuildDefinitionWorkspaceService SystemUnderTest => new();

    private const string ProjectName = "GnarlyCorp";

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Scan_SingleMapIsASimpleWorkspace()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Simple-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Main", "map")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        var definition = actual.Definitions.Single();

        Assert.IsFalse(definition.IsComplexMapping, "One mapped path is a simple workspace.");
        Assert.AreEqual(1, definition.MappedPaths.Count, "Wrong mapped path count.");
        Assert.AreEqual("$/GnarlyCorp/Main", definition.MappedPaths[0], "Wrong mapped path.");
    }

    [TestMethod]
    public async Task Scan_OneMapPlusCloaksIsStillSimple()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Cloaked-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Main", "map"),
            ("$/GnarlyCorp/Main/Drops", "cloak"),
            ("$/GnarlyCorp/Main/Packages", "cloak"),
            ("$/GnarlyCorp/Main/Docs", "cloak")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        var definition = actual.Definitions.Single();

        Assert.IsFalse(
            definition.IsComplexMapping, "Cloaks do not make a workspace complex.");
        Assert.AreEqual(1, definition.MappedPaths.Count, "Wrong mapped path count.");
        Assert.AreEqual(3, definition.CloakedPaths.Count, "Wrong cloaked path count.");
    }

    [TestMethod]
    public async Task Scan_SeveralMapsIsAComplexWorkspace()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Complex-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Main", "map"),
            ("$/GnarlyCorp/Common", "map"),
            ("$/GnarlyCorp/ThirdParty", "map")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        var definition = actual.Definitions.Single();

        Assert.IsTrue(definition.IsComplexMapping, "Three mapped paths is a complex workspace.");
        Assert.AreEqual(3, definition.MappedPaths.Count, "Wrong mapped path count.");
    }

    [TestMethod]
    public async Task Scan_IgnoresGitBackedDefinitions()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Legacy-CI", UtcNow.AddDays(-5), ("$/GnarlyCorp/Main", "map")));

        client.Add(FakeBuildDefinitionApiClient.GitDefinition(2, "Modern-CI"));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.AreEqual(1, actual.Definitions.Count, "Only the TFVC definition should be kept.");
        Assert.AreEqual("Legacy-CI", actual.Definitions[0].Name, "Wrong definition kept.");
        Assert.AreEqual(
            2, actual.TotalDefinitionsExamined, "Both definitions were looked at.");
    }

    [TestMethod]
    public async Task Scan_CountsHowManyBuildsMapEachPath()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Web-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Web", "map"),
            ("$/GnarlyCorp/Common", "map")));

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Api-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Api", "map"),
            ("$/GnarlyCorp/Common", "map")));

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            3, "Batch-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Common", "map")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        var common = actual.MappedPathUsages.Single(x => x.Path == "$/GnarlyCorp/Common");

        Assert.AreEqual(3, common.DefinitionCount, "Common is mapped by three builds.");
        CollectionAssert.AreEquivalent(
            new[] { "Web-CI", "Api-CI", "Batch-CI" },
            common.DefinitionNames,
            "Wrong definitions listed against the shared path.");

        // Most-mapped first is what makes the table worth reading.
        Assert.AreEqual(
            "$/GnarlyCorp/Common", actual.MappedPathUsages[0].Path, "Wrong sort order.");
    }

    [TestMethod]
    public async Task Scan_CloakedPathsDoNotCountAsMappings()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "One-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Main", "map"),
            ("$/GnarlyCorp/Shared", "cloak")));

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Two-CI", UtcNow.AddDays(-5),
            ("$/GnarlyCorp/Main", "map"),
            ("$/GnarlyCorp/Shared", "cloak")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.IsFalse(
            actual.MappedPathUsages.Any(x => x.Path == "$/GnarlyCorp/Shared"),
            "A cloaked path is excluded from the workspace, not mapped into it.");
    }

    [TestMethod]
    public async Task Scan_MarksDefinitionsWithNoRecentRunsAsInactive()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Fresh-CI", UtcNow.AddDays(-5), ("$/GnarlyCorp/Main", "map")));

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Stale-CI", UtcNow.AddDays(-400), ("$/GnarlyCorp/Old", "map")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.IsFalse(
            actual.Definitions.Single(x => x.Name == "Fresh-CI").IsInactive,
            "A build that ran last week is not inactive.");

        Assert.IsTrue(
            actual.Definitions.Single(x => x.Name == "Stale-CI").IsInactive,
            "A build that last ran over a year ago is inactive.");
    }

    [TestMethod]
    public async Task Scan_DefinitionThatNeverRanIsInactive()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Never-CI", null, ("$/GnarlyCorp/Main", "map")));

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        var definition = actual.Definitions.Single();

        Assert.IsTrue(definition.IsInactive, "A build with no runs is inactive.");
        Assert.IsNull(definition.LastRunDate, "There is no run date to report.");
    }

    [TestMethod]
    public async Task Scan_RecordsDefinitionsItCouldNotRead()
    {
        var client = new FakeBuildDefinitionApiClient();

        client.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Good-CI", UtcNow.AddDays(-5), ("$/GnarlyCorp/Main", "map")));

        client.AddUnreadable(2, "Blocked-CI");

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.AreEqual(1, actual.Definitions.Count, "Only the readable definition is usable.");
        CollectionAssert.AreEqual(
            new[] { "Blocked-CI" },
            actual.UnreadableDefinitions,
            "The unreadable definition should be named rather than dropped.");
    }

    [TestMethod]
    public async Task Scan_DefinitionWithUnreadableMappingsIsStillReported()
    {
        var client = new FakeBuildDefinitionApiClient();

        var detail = FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Odd-CI", UtcNow.AddDays(-5));

        detail.Repository!.Properties["tfvcMapping"] =
            FakeBuildDefinitionApiClient.StringElement("{not json");

        client.Add(detail);

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.AreEqual(
            1, actual.Definitions.Count, "A TFVC build still counts even if its mappings are odd.");
        Assert.AreEqual(0, actual.Definitions[0].MappedPaths.Count, "There is nothing to map.");
    }

    [TestMethod]
    public async Task Scan_NoDefinitionsAtAll()
    {
        var client = new FakeBuildDefinitionApiClient();

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, UtcNow);

        Assert.AreEqual(0, actual.Definitions.Count, "Expected no definitions.");
        Assert.AreEqual(0, actual.MappedPathUsages.Count, "Expected no mapped paths.");
        Assert.AreEqual(0, actual.TotalDefinitionsExamined, "Nothing was examined.");
    }
}
