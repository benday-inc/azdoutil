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
}
