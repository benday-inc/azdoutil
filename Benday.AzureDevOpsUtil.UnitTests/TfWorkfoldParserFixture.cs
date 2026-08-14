using Benday.AzureDevOpsUtil.Api.TfCommandLine;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfWorkfoldParserFixture
{
    /// <summary>
    /// Real output from tf workfold, captured from a workspace with two mapped
    /// folders.  The separator line is much longer in reality; its length does
    /// not matter.
    /// </summary>
    private const string RealOutput =
        "=========================================================================\r\n" +
        "Workspace : dev-vm-20260325 (Ben Day)\r\n" +
        "Collection: https://dev.azure.com/benday\r\n" +
        " $/TfvcBuildCodeAnalysis: C:\\code\\tfvc\\TfvcBuildCodeAnalysis\r\n" +
        " $/TfvcBuildTaskGroupTest20260430: C:\\code\\azdo\\TfvcBuildTaskGroupTest20260430\r\n";

    [TestMethod]
    public void Parse_ReadsTheWorkspaceAndOwner()
    {
        var actual = TfWorkfoldParser.Parse(RealOutput);

        Assert.IsNotNull(actual, "The output should have parsed.");
        Assert.AreEqual("dev-vm-20260325", actual!.WorkspaceName, "Wrong workspace name.");
        Assert.AreEqual("Ben Day", actual.OwnerName, "Wrong owner name.");
    }

    [TestMethod]
    public void Parse_ReadsTheCollectionUrlWithATrailingSeparator()
    {
        var actual = TfWorkfoldParser.Parse(RealOutput);

        // tf prints it without one; a stored configuration always has one.
        Assert.AreEqual(
            "https://dev.azure.com/benday/", actual!.CollectionUrl, "Wrong collection url.");
    }

    [TestMethod]
    public void Parse_ReadsEveryMapping()
    {
        var actual = TfWorkfoldParser.Parse(RealOutput);

        Assert.AreEqual(2, actual!.Mappings.Count, "Expected both mappings.");

        Assert.AreEqual(
            "$/TfvcBuildCodeAnalysis", actual.Mappings[0].ServerPath, "Wrong server path.");
        Assert.AreEqual(
            @"C:\code\tfvc\TfvcBuildCodeAnalysis",
            actual.Mappings[0].LocalPath,
            "Wrong local path.");

        Assert.AreEqual(
            "$/TfvcBuildTaskGroupTest20260430",
            actual.Mappings[1].ServerPath,
            "Wrong server path.");
    }

    [TestMethod]
    public void Parse_SplitsOnTheFirstColonSoDriveLettersSurvive()
    {
        // The local path has a colon of its own.  A TFVC server path cannot,
        // which is what makes splitting on the first one safe.
        var actual = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " $/Project/Main: C:\\code\\Main\r\n");

        var mapping = actual!.Mappings.Single();

        Assert.AreEqual("$/Project/Main", mapping.ServerPath, "Wrong server path.");
        Assert.AreEqual(@"C:\code\Main", mapping.LocalPath, "The drive letter should survive.");
    }

    [TestMethod]
    public void Parse_ReadsCloakedMappings()
    {
        var actual = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " $/Project/Main: C:\\code\\Main\r\n" +
            " (cloaked) $/Project/Main/Drops: C:\\code\\Main\\Drops\r\n");

        Assert.AreEqual(2, actual!.Mappings.Count, "Expected both entries.");
        Assert.IsFalse(actual.Mappings[0].IsCloaked, "The first entry is a normal mapping.");
        Assert.IsTrue(actual.Mappings[1].IsCloaked, "The second entry is cloaked.");
    }

    [TestMethod]
    public void Parse_ReadsACloakedEntryWithNoLocalPath()
    {
        var actual = TfWorkfoldParser.Parse(
            "Collection: https://dev.azure.com/benday\r\n" +
            " (cloaked) $/Project/Main/Drops\r\n");

        var mapping = actual!.Mappings.Single();

        Assert.IsTrue(mapping.IsCloaked, "This entry is cloaked.");
        Assert.AreEqual("$/Project/Main/Drops", mapping.ServerPath, "Wrong server path.");
    }

    [TestMethod]
    public void Parse_IgnoresTheSeparatorLine()
    {
        var actual = TfWorkfoldParser.Parse(RealOutput);

        Assert.IsFalse(
            actual!.Mappings.Any(x => x.ServerPath.StartsWith("=")),
            "The rule of equals signs is not a mapping.");
    }

    [TestMethod]
    public void Parse_HandlesUnixLineEndings()
    {
        var actual = TfWorkfoldParser.Parse(
            "Workspace : ws (Ben Day)\nCollection: https://dev.azure.com/benday\n" +
            " $/Project: /Users/benday/code/Project\n");

        Assert.AreEqual(1, actual!.Mappings.Count, "Expected one mapping.");
        Assert.AreEqual(
            "/Users/benday/code/Project", actual.Mappings[0].LocalPath, "Wrong local path.");
    }

    [TestMethod]
    public void Parse_WorkspaceWithoutAnOwner()
    {
        var actual = TfWorkfoldParser.Parse(
            "Workspace : dev-vm\r\nCollection: https://dev.azure.com/benday\r\n" +
            " $/Project: C:\\code\\Project\r\n");

        Assert.AreEqual("dev-vm", actual!.WorkspaceName, "Wrong workspace name.");
        Assert.AreEqual(string.Empty, actual.OwnerName, "There is no owner to read.");
    }

    [TestMethod]
    public void Parse_NothingToRead()
    {
        Assert.IsNull(TfWorkfoldParser.Parse(null), "Null is not output.");
        Assert.IsNull(TfWorkfoldParser.Parse(""), "Empty is not output.");
        Assert.IsNull(TfWorkfoldParser.Parse("   "), "Whitespace is not output.");
    }

    [TestMethod]
    public void Parse_ErrorOutputIsNotAWorkspace()
    {
        // What tf prints when the directory is not in a workspace.
        var actual = TfWorkfoldParser.Parse(
            "Unable to determine the workspace. You may be able to correct this by running " +
            "'tf workspaces -collection:TeamProjectCollectionUrl'.");

        Assert.IsNull(actual, "An error message is not a workspace.");
    }
}
