using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcFolderHeuristicScannerFixture
{
    private TfvcFolderHeuristicScanner SystemUnderTest => new();

    private const string ProjectName = "App";

    [TestMethod]
    [DataRow("Main")]
    [DataRow("main")]
    [DataRow("TRUNK")]
    [DataRow("Dev")]
    [DataRow("Development")]
    [DataRow("QA")]
    [DataRow("Staging")]
    [DataRow("Production")]
    [DataRow("Release2019")]
    [DataRow("Hotfix-1234")]
    [DataRow("FeatureLogin")]
    [DataRow("v1.0")]
    [DataRow("2.1")]
    [DataRow("R2024")]
    public void LooksLikeBranchName_True(string name)
    {
        Assert.IsTrue(
            TfvcFolderHeuristicScanner.LooksLikeBranchName(name),
            $"'{name}' should read as a branch name.");
    }

    [TestMethod]
    [DataRow("src")]
    [DataRow("docs")]
    [DataRow("tools")]
    [DataRow("packages")]
    [DataRow("Customer.Web")]
    [DataRow("")]
    public void LooksLikeBranchName_False(string name)
    {
        Assert.IsFalse(
            TfvcFolderHeuristicScanner.LooksLikeBranchName(name),
            $"'{name}' should not read as a branch name.");
    }

    private static FakeTfvcApiClient BuildClientWithBranchLikeSiblings()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/Main", "$/App/Dev", "$/App/Prod");

        // All three hold the same things, which is what makes them look like copies.
        client.SetChildFolders("$/App/Main", "$/App/Main/Web", "$/App/Main/Data");
        client.SetChildFolders("$/App/Dev", "$/App/Dev/Web", "$/App/Dev/Data");
        client.SetChildFolders("$/App/Prod", "$/App/Prod/Web", "$/App/Prod/Data");

        return client;
    }

    [TestMethod]
    public async Task Scan_FindsClassicUnregisteredSiblings()
    {
        var client = BuildClientWithBranchLikeSiblings();

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 3);

        Assert.AreEqual(1, actual.Count, "Expected one group of branch-like folders.");
        Assert.AreEqual(3, actual[0].FolderPaths.Count, "Expected all three folders in the group.");
        Assert.AreEqual("$/App", actual[0].ParentPath, "Wrong parent path on the group.");
    }

    [TestMethod]
    public async Task Scan_IgnoresOrdinaryFolders()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/src", "$/App/docs", "$/App/tools");
        client.SetChildFolders("$/App/src", "$/App/src/Web");
        client.SetChildFolders("$/App/docs", "$/App/docs/Web");
        client.SetChildFolders("$/App/tools", "$/App/tools/Web");

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 3);

        Assert.AreEqual(0, actual.Count, "Ordinary folder names should not be reported.");
    }

    [TestMethod]
    public async Task Scan_IgnoresFoldersAlreadyRegisteredAsBranches()
    {
        var client = BuildClientWithBranchLikeSiblings();

        var registered = new[] { "$/App/Main" };

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", registered, 3);

        Assert.AreEqual(1, actual.Count, "Expected one group from the remaining folders.");
        Assert.AreEqual(2, actual[0].FolderPaths.Count, "Registered branch should be excluded.");
        Assert.IsFalse(
            actual[0].FolderPaths.Contains("$/App/Main"),
            "A folder already reported by the branches API should not appear here.");
    }

    [TestMethod]
    public async Task Scan_IgnoresFoldersFlaggedAsBranchesByTheApi()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildren(
            "$/App",
            FakeTfvcApiClient.FolderItem("$/App/Main", isBranch: true),
            FakeTfvcApiClient.FolderItem("$/App/Dev"),
            FakeTfvcApiClient.FolderItem("$/App/Prod"));

        client.SetChildFolders("$/App/Dev", "$/App/Dev/Web", "$/App/Dev/Data");
        client.SetChildFolders("$/App/Prod", "$/App/Prod/Web", "$/App/Prod/Data");

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 3);

        Assert.AreEqual(1, actual.Count, "Expected one group.");
        Assert.IsFalse(
            actual[0].FolderPaths.Contains("$/App/Main"),
            "isBranch on the item should exclude the folder.");
    }

    [TestMethod]
    public async Task Scan_RequiresMoreThanOneCandidate()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/Main", "$/App/src");
        client.SetChildFolders("$/App/Main", "$/App/Main/Web");
        client.SetChildFolders("$/App/src", "$/App/src/Web");

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 3);

        Assert.AreEqual(0, actual.Count, "A lone branch-like folder is not evidence of branching.");
    }

    [TestMethod]
    public async Task Scan_RequiresSimilarContents()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/Main", "$/App/Dev");
        client.SetChildFolders("$/App/Main", "$/App/Main/Web", "$/App/Main/Data");
        client.SetChildFolders("$/App/Dev", "$/App/Dev/Totally", "$/App/Dev/Different");

        var actual = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 3);

        Assert.AreEqual(
            0, actual.Count, "Branch-like names with unrelated contents should not be grouped.");
    }

    [TestMethod]
    public async Task Scan_RespectsDepthCap()
    {
        var client = new FakeTfvcApiClient();

        client.SetChildFolders("$/App", "$/App/Teams");
        client.SetChildFolders("$/App/Teams", "$/App/Teams/Main", "$/App/Teams/Dev");
        client.SetChildFolders("$/App/Teams/Main", "$/App/Teams/Main/Web");
        client.SetChildFolders("$/App/Teams/Dev", "$/App/Teams/Dev/Web");

        var shallow = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 1);

        Assert.AreEqual(
            0, shallow.Count, "A depth of 1 should not reach the folders one level further down.");

        var deeper = await SystemUnderTest.ScanAsync(client, ProjectName, "$/App", null, 2);

        Assert.AreEqual(1, deeper.Count, "A depth of 2 should find the group.");
        Assert.AreEqual("$/App/Teams", deeper[0].ParentPath, "Wrong parent path.");
    }

    [TestMethod]
    public void IsSimilar_MeasuresAgainstTheSmallerFolder()
    {
        var small = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Web", "Data" };

        var grown = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Web", "Data", "Api", "Tests", "Tools", "Docs"
        };

        Assert.IsTrue(
            TfvcFolderHeuristicScanner.IsSimilar(small, grown),
            "A branch that has grown since it was copied should still match its origin.");
    }

    [TestMethod]
    public void IsSimilar_EmptyFolderNeverMatches()
    {
        var empty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var populated = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Web" };

        Assert.IsFalse(
            TfvcFolderHeuristicScanner.IsSimilar(empty, populated),
            "An empty folder is not evidence of a copy.");
    }
}
