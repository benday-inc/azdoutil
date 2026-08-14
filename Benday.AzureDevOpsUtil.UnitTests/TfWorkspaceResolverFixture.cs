using Benday.AzureDevOpsUtil.Api.TfCommandLine;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfWorkspaceResolverFixture
{
    private const string RealOutput =
        "=========================================================================\r\n" +
        "Workspace : dev-vm-20260325 (Ben Day)\r\n" +
        "Collection: https://dev.azure.com/benday\r\n" +
        " $/TfvcBuildCodeAnalysis: C:\\code\\tfvc\\TfvcBuildCodeAnalysis\r\n" +
        " $/TfvcBuildTaskGroupTest20260430: C:\\code\\azdo\\TfvcBuildTaskGroupTest20260430\r\n";

    private static TfWorkfoldResult RealWorkspace()
    {
        return TfWorkfoldParser.Parse(RealOutput)!;
    }

    [TestMethod]
    public void Resolve_TurnsTheCurrentDirectoryIntoAServerPath()
    {
        // The directory the output above was captured from.
        var actual = TfWorkspaceResolver.Resolve(
            RealWorkspace(),
            @"C:\code\tfvc\TfvcBuildCodeAnalysis\code-analysis\DotnetFrameworkApp");

        Assert.IsNotNull(actual, "The directory should have resolved.");
        Assert.AreEqual(
            "$/TfvcBuildCodeAnalysis/code-analysis/DotnetFrameworkApp",
            actual!.ServerPath,
            "Wrong server path.");
        Assert.AreEqual(
            "TfvcBuildCodeAnalysis", actual.TeamProjectName, "Wrong team project.");
        Assert.AreEqual(
            "https://dev.azure.com/benday/", actual.CollectionUrl, "Wrong collection url.");
    }

    [TestMethod]
    public void Resolve_PicksTheRightMappingOutOfSeveral()
    {
        var actual = TfWorkspaceResolver.Resolve(
            RealWorkspace(), @"C:\code\azdo\TfvcBuildTaskGroupTest20260430\src");

        Assert.AreEqual(
            "$/TfvcBuildTaskGroupTest20260430/src", actual!.ServerPath, "Wrong server path.");
        Assert.AreEqual(
            "TfvcBuildTaskGroupTest20260430", actual.TeamProjectName, "Wrong team project.");
    }

    [TestMethod]
    public void Resolve_TheMappedRootItself()
    {
        var actual = TfWorkspaceResolver.Resolve(
            RealWorkspace(), @"C:\code\tfvc\TfvcBuildCodeAnalysis");

        Assert.AreEqual("$/TfvcBuildCodeAnalysis", actual!.ServerPath, "Wrong server path.");
    }

    [TestMethod]
    public void Resolve_IgnoresCaseAndTrailingSeparators()
    {
        var actual = TfWorkspaceResolver.Resolve(
            RealWorkspace(), @"c:\CODE\tfvc\tfvcbuildcodeanalysis\src\");

        Assert.AreEqual(
            "$/TfvcBuildCodeAnalysis/src", actual!.ServerPath, "Wrong server path.");
    }

    [TestMethod]
    public void Resolve_PrefersTheMostSpecificMapping()
    {
        // A workspace can map a folder and then map something beneath it.
        var workspace = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " $/Project: C:\\code\\Project\r\n" +
            " $/Other/Special: C:\\code\\Project\\Special\r\n")!;

        var actual = TfWorkspaceResolver.Resolve(workspace, @"C:\code\Project\Special\Thing");

        Assert.AreEqual(
            "$/Other/Special/Thing",
            actual!.ServerPath,
            "The deeper mapping is the one that applies.");
    }

    [TestMethod]
    public void Resolve_SiblingSharingANamePrefixIsNotInsideTheMapping()
    {
        var workspace = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " $/Project: C:\\code\\App\r\n")!;

        Assert.IsNull(
            TfWorkspaceResolver.Resolve(workspace, @"C:\code\AppTests"),
            "A sibling that shares a name prefix is not inside the mapped folder.");
    }

    [TestMethod]
    public void Resolve_DirectoryOutsideEveryMapping()
    {
        Assert.IsNull(
            TfWorkspaceResolver.Resolve(RealWorkspace(), @"C:\somewhere\else"),
            "This directory is not in the workspace.");
    }

    [TestMethod]
    public void Resolve_CloakedMappingsNeverResolve()
    {
        var workspace = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " (cloaked) $/Project/Drops: C:\\code\\Drops\r\n")!;

        Assert.IsNull(
            TfWorkspaceResolver.Resolve(workspace, @"C:\code\Drops\thing"),
            "A cloaked folder is excluded from the workspace rather than brought into it.");
    }

    [TestMethod]
    public void Resolve_NothingToResolve()
    {
        Assert.IsNull(TfWorkspaceResolver.Resolve(null, @"C:\code"), "There is no workspace.");
        Assert.IsNull(
            TfWorkspaceResolver.Resolve(RealWorkspace(), null), "There is no directory.");
    }

    [TestMethod]
    [DataRow("$/TfvcBuildCodeAnalysis", "TfvcBuildCodeAnalysis")]
    [DataRow("$/Project/Main/App", "Project")]
    [DataRow("$/", "")]
    [DataRow("", "")]
    public void GetTeamProjectName_IsTheFirstSegment(string serverPath, string expected)
    {
        Assert.AreEqual(
            expected,
            TfWorkspaceResolver.GetTeamProjectName(serverPath),
            $"Wrong team project for '{serverPath}'.");
    }
}
