using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcAssessmentAnalyzerFixture
{
    private const string ProjectName = "App";

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static TfvcBranchInfo Branch(string path, params TfvcBranchInfo[] children)
    {
        return new TfvcBranchInfo
        {
            Path = path,
            Children = children.ToList()
        };
    }

    private static AssessmentFinding? FindByCategory(TfvcAssessmentResult result, string category)
    {
        return result.Findings.FirstOrDefault(x => x.Category == category);
    }

    [TestMethod]
    public async Task Analyze_NoRegisteredBranchesProducesTheNoMetadataFinding()
    {
        var client = new FakeTfvcApiClient();

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        var finding = FindByCategory(actual, FindingCategories.BranchHierarchy);

        Assert.IsNotNull(finding, "Expected a branch hierarchy finding.");
        StringAssert.Contains(
            finding!.Fact,
            "No TFVC folders under $/App are registered as branches",
            "Wrong fact text.");
        StringAssert.Contains(
            finding.Consequence,
            "cannot be discovered through the branches API",
            "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_NestedBranchProducesAFinding()
    {
        var client = new FakeTfvcApiClient();

        client.Branches.Add(Branch("$/App/Main", Branch("$/App/Main/Feature")));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        var finding = FindByCategory(actual, FindingCategories.NestedBranches);

        Assert.IsNotNull(finding, "Expected a nested branch finding.");
        StringAssert.Contains(finding!.Fact, "$/App/Main/Feature", "Child path missing.");
        StringAssert.Contains(finding.Fact, "$/App/Main", "Parent path missing.");
        StringAssert.Contains(
            finding.Consequence,
            "cannot be represented by the Azure DevOps",
            "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_TwoActiveBranchesProduceTheNoCommonAncestorFinding()
    {
        var client = new FakeTfvcApiClient();

        client.Branches.Add(Branch("$/App/Main", Branch("$/App/Dev")));

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));
        client.SetChangesets("$/App/Dev", FakeTfvcApiClient.Changeset(2, UtcNow.AddDays(-5)));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(2, actual.ActiveBranchCount, "Expected two active branches.");

        var finding = actual.Findings.FirstOrDefault(
            x => x.Consequence.Contains("no common ancestor"));

        Assert.IsNotNull(finding, "Expected the no-common-ancestor finding.");
        StringAssert.Contains(
            finding!.Consequence,
            "cannot be merged after migration through normal means",
            "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_OneActiveBranchDoesNotProduceTheNoCommonAncestorFinding()
    {
        var client = new FakeTfvcApiClient();

        client.Branches.Add(Branch("$/App/Main"));

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(1, actual.ActiveBranchCount, "Expected one active branch.");

        var finding = actual.Findings.FirstOrDefault(
            x => x.Consequence.Contains("no common ancestor"));

        Assert.IsNull(finding, "One active branch cannot have a merge problem with itself.");
    }

    [TestMethod]
    public async Task Analyze_MeasuresActivityForUnregisteredCandidatesToo()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/Main", "$/App/Dev");
        client.SetChildFolders("$/App/Main", "$/App/Main/Web", "$/App/Main/Data");
        client.SetChildFolders("$/App/Dev", "$/App/Dev/Web", "$/App/Dev/Data");

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));
        client.SetChangesets("$/App/Dev", FakeTfvcApiClient.Changeset(2, UtcNow.AddDays(-3)));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(
            1, actual.UnregisteredBranchGroups.Count, "Expected one unregistered branch group.");
        Assert.AreEqual(
            2, actual.BranchActivity.Count, "Unregistered candidates should be measured too.");
        Assert.IsTrue(
            actual.BranchActivity.All(x => x.IsRegisteredBranch == false),
            "Neither folder is a registered branch.");
    }

    [TestMethod]
    public async Task Analyze_DeadBranchesAreCounted()
    {
        var client = new FakeTfvcApiClient();

        client.Branches.Add(Branch("$/App/Main", Branch("$/App/Old")));

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));
        client.SetChangesets("$/App/Old", FakeTfvcApiClient.Changeset(2, UtcNow.AddDays(-900)));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(1, actual.DeadBranchCount, "Expected one dead branch.");

        var finding = actual.Findings.FirstOrDefault(
            x => x.Fact.Contains("no changes in the last 365 days"));

        Assert.IsNotNull(finding, "Expected a finding about dead branches.");
    }

    [TestMethod]
    public async Task Analyze_RecordsWhatTheScanDidNotCover()
    {
        var client = new FakeTfvcApiClient();

        var analyzer = new TfvcAssessmentAnalyzer(client)
        {
            MaxScanDepth = 2
        };

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("2 level(s) below $/App")),
            "The report should say how deep the folder scan went.");
    }

    [TestMethod]
    public async Task Analyze_ScopePathIsNormalized()
    {
        var client = new FakeTfvcApiClient();

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App/", UtcNow);

        Assert.AreEqual("$/App", actual.ScopePath, "The trailing slash should be removed.");
    }

    [TestMethod]
    public async Task Analyze_TfvcBuildDefinitionsProduceAFinding()
    {
        var buildClient = new FakeBuildDefinitionApiClient();

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Legacy-CI", UtcNow.AddDays(-3), ("$/App/Main", "map")));

        var analyzer = new TfvcAssessmentAnalyzer(new FakeTfvcApiClient(), buildClient);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(1, actual.TfvcBuildDefinitions.Count, "Expected one TFVC build.");

        var finding = FindByCategory(actual, FindingCategories.BuildDefinitions);

        Assert.IsNotNull(finding, "Expected a build definition finding.");
        StringAssert.Contains(
            finding!.Fact, "1 build definition(s) pull source from TFVC", "Wrong fact text.");
        StringAssert.Contains(
            finding.Consequence, "stop working when TFVC is retired", "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_ComplexWorkspaceProducesItsOwnFinding()
    {
        var buildClient = new FakeBuildDefinitionApiClient();

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Complex-CI", UtcNow.AddDays(-3),
            ("$/App/Main", "map"),
            ("$/Shared/Common", "map")));

        var analyzer = new TfvcAssessmentAnalyzer(new FakeTfvcApiClient(), buildClient);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        var finding = actual.Findings.FirstOrDefault(
            x => x.Fact.Contains("maps 2 separate TFVC paths"));

        Assert.IsNotNull(finding, "Expected a finding for the complex workspace.");
        StringAssert.Contains(finding!.Fact, "Complex-CI", "The definition should be named.");
        StringAssert.Contains(
            finding.Consequence,
            "cannot be reproduced from a single Git repository",
            "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_PathMappedBySeveralBuildsOutsideTheScopeIsReported()
    {
        var buildClient = new FakeBuildDefinitionApiClient();

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Web-CI", UtcNow.AddDays(-3),
            ("$/App/Web", "map"),
            ("$/Shared/Common", "map")));

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Api-CI", UtcNow.AddDays(-3),
            ("$/App/Api", "map"),
            ("$/Shared/Common", "map")));

        var analyzer = new TfvcAssessmentAnalyzer(new FakeTfvcApiClient(), buildClient);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        var finding = FindByCategory(actual, FindingCategories.SharedFolders);

        Assert.IsNotNull(finding, "Expected a shared folder finding.");
        StringAssert.Contains(finding!.Fact, "$/Shared/Common", "Wrong path named.");
        StringAssert.Contains(finding.Fact, "2 build definitions", "Wrong build count.");
        StringAssert.Contains(
            finding.Consequence,
            "Multiple builds depend on this folder's contents",
            "Wrong consequence text.");
    }

    [TestMethod]
    public async Task Analyze_PathMappedBySeveralBuildsInsideTheScopeIsNotASharedFolderFinding()
    {
        var buildClient = new FakeBuildDefinitionApiClient();

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Web-CI", UtcNow.AddDays(-3), ("$/App/Main", "map")));

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Api-CI", UtcNow.AddDays(-3), ("$/App/Main", "map")));

        var analyzer = new TfvcAssessmentAnalyzer(new FakeTfvcApiClient(), buildClient);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.IsNull(
            FindByCategory(actual, FindingCategories.SharedFolders),
            "A path inside the assessed area is already part of what is being migrated.");

        // It still shows up in the frequency table as a fact.
        Assert.AreEqual(
            2,
            actual.MappedPathUsages.Single(x => x.Path == "$/App/Main").DefinitionCount,
            "The frequency table should still count it.");
    }

    [TestMethod]
    public async Task Analyze_WithoutABuildClientTheSectionIsSkippedOutLoud()
    {
        var analyzer = new TfvcAssessmentAnalyzer(new FakeTfvcApiClient());

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(0, actual.TfvcBuildDefinitions.Count, "Nothing should have been read.");
        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("Build definitions were not examined")),
            "A skipped section should say so.");
    }

    [TestMethod]
    public async Task Analyze_ReportsWhatIsStoredInTheTree()
    {
        var client = new FakeTfvcApiClient();

        client.SetFullListing(
            "$/App",
            new TfvcItemInfo { Path = "$/App", IsFolder = true },
            new TfvcItemInfo { Path = "$/App/src/Program.cs", Size = 1024 },
            new TfvcItemInfo
            {
                Path = "$/App/src/bin/App.dll",
                Size = 120L * 1024 * 1024
            },
            new TfvcItemInfo
            {
                Path = "$/App/packages/Foo/Foo.dll",
                Size = 2048
            });

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(3, actual.Content.FileCount, "Wrong file count.");
        Assert.AreEqual(1, actual.Content.FilesOverPushLimit, "Wrong count over the push limit.");
        Assert.AreEqual(2, actual.Content.GeneratedFolders.Count, "Expected bin and packages.");

        var overall = actual.Findings.FirstOrDefault(
            x => x.Category == FindingCategories.Content && x.Fact.Contains("totalling"));

        Assert.IsNotNull(overall, "Expected a finding about what is stored.");
        StringAssert.Contains(
            overall!.Consequence,
            "Git carries every version of every file",
            "The report should say the figure understates the clone.");

        var pushLimit = actual.Findings.FirstOrDefault(
            x => x.Fact.Contains("larger than 100 MB"));

        Assert.IsNotNull(pushLimit, "Expected a finding about the push limit.");
        StringAssert.Contains(
            pushLimit!.Detail, "App.dll", "The offending file should be named.");
    }

    [TestMethod]
    public async Task Analyze_NoContentFindingsForAnEmptyTree()
    {
        var client = new FakeTfvcApiClient();

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(0, actual.Content.FileCount, "Nothing was read.");
        Assert.IsFalse(
            actual.Findings.Any(x => x.Category == FindingCategories.Content),
            "An empty tree produces nothing to report.");
    }

    private sealed class ThrowingTfvcApiClient : FakeTfvcApiClient, ITfvcApiClient
    {
        public new Task<IReadOnlyList<TfvcItemInfo>> GetItemsAsync(
            string projectName, string scopePath, TfvcRecursionLevel recursionLevel)
        {
            if (recursionLevel == TfvcRecursionLevel.Full)
            {
                throw new InvalidOperationException("response too large");
            }

            return base.GetItemsAsync(projectName, scopePath, recursionLevel);
        }
    }

    [TestMethod]
    public async Task Analyze_ContentFailureDoesNotSinkTheWholeAssessment()
    {
        var client = new ThrowingTfvcApiClient();

        client.Branches.Add(Branch("$/App/Main"));

        var analyzer = new TfvcAssessmentAnalyzer(client);

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(
            1, actual.RegisteredBranchPaths.Count, "The branch section should still be there.");

        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("could not be read")),
            "The failure should be recorded rather than swallowed.");

        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("response too large")),
            "The reason should survive into the report.");
    }

    private sealed class ThrowingBuildDefinitionApiClient : IBuildDefinitionApiClient
    {
        public Task<IReadOnlyList<BuildDefinitionInfo>> GetDefinitionsAsync(string projectName)
        {
            throw new InvalidOperationException("access denied");
        }

        public Task<BuildDefinitionDetail?> GetDefinitionAsync(
            string projectName, int definitionId)
        {
            throw new InvalidOperationException("access denied");
        }
    }

    [TestMethod]
    public async Task Analyze_BuildDefinitionFailureDoesNotSinkTheWholeAssessment()
    {
        var tfvcClient = new FakeTfvcApiClient();

        tfvcClient.Branches.Add(Branch("$/App/Main"));

        var analyzer = new TfvcAssessmentAnalyzer(
            tfvcClient, new ThrowingBuildDefinitionApiClient());

        var actual = await analyzer.AnalyzeAsync(ProjectName, "$/App", UtcNow);

        Assert.AreEqual(
            1, actual.RegisteredBranchPaths.Count, "The branch section should still be there.");

        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("Build definitions could not be read")),
            "The failure should be recorded rather than swallowed.");

        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("access denied")),
            "The reason should survive into the report.");
    }
}
