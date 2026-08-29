using Benday.AzureDevOpsUtil.Api.McpTools;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class CliCommandCatalogFixture
{
    [TestMethod]
    public void GetCommands_ReturnsManyCommands()
    {
        var commands = CliCommandCatalog.GetCommands();

        Assert.IsTrue(commands.Count > 30,
            $"Expected the full CLI surface (30+ commands), found {commands.Count}.");
    }

    [TestMethod]
    public void GetCommands_IncludesKnownCommandsWithMetadata()
    {
        var commands = CliCommandCatalog.GetCommands();

        var getProject = commands.SingleOrDefault(x => x.Name == "getproject");
        Assert.IsNotNull(getProject, "getproject should be in the catalog.");
        Assert.AreEqual("Project Administration", getProject!.Category, "Wrong category.");
        Assert.IsTrue(getProject.Arguments.Any(a => a.Name == "teamproject" && a.IsRequired),
            "getproject should have a required teamproject argument.");
        Assert.IsTrue(getProject.CommandLineExample.Contains("--teamproject "),
            $"Example should include required args. Actual: '{getProject.CommandLineExample}'");

        Assert.IsTrue(commands.Any(x => x.Name == "tfvc-to-git"),
            "A CLI-only command (tfvc-to-git) should be discoverable.");
    }

    [TestMethod]
    public void GetCommands_FlagsCommandsAlreadyAvailableAsMcpTools()
    {
        var commands = CliCommandCatalog.GetCommands();

        var listProjects = commands.Single(x => x.Name == "listprojects");
        Assert.IsTrue(listProjects.AvailableAsMcpTool, "listprojects is exposed as an MCP tool.");
        Assert.AreEqual("list_team_projects", listProjects.McpToolName, "Wrong mapped MCP tool name.");

        var createProject = commands.Single(x => x.Name == "createproject");
        Assert.IsFalse(createProject.AvailableAsMcpTool,
            "createproject is not exposed as an MCP tool.");
    }

    [TestMethod]
    public void DiscoverCliCommands_WithQuery_ReturnsMatchesWithArguments()
    {
        var tools = new CliDiscoveryTools();

        var result = tools.DiscoverCliCommands("tfvc");

        Assert.AreEqual("tfvc", result.Query, "Query should be echoed.");
        Assert.IsTrue(result.MatchCount >= 1, "Expected at least one match for 'tfvc'.");
        Assert.IsTrue(result.Commands.Any(c => c.Name == "tfvc-to-git"), "tfvc-to-git should match.");
        Assert.IsTrue(result.Commands.Single(c => c.Name == "tfvc-to-git").Arguments.Count > 0,
            "Query results should include argument detail.");
    }

    [TestMethod]
    public void DiscoverCliCommands_MatchesOnCategoryAndDescription()
    {
        var tools = new CliDiscoveryTools();

        var result = tools.DiscoverCliCommands("process template");

        Assert.IsTrue(result.Commands.Any(c => c.Name == "changeprocess"),
            "Searching 'process template' should surface changeprocess.");
    }

    [TestMethod]
    public void DiscoverCliCommands_BlankQuery_ReturnsCompactListWithoutArguments()
    {
        var tools = new CliDiscoveryTools();

        var result = tools.DiscoverCliCommands("");

        Assert.IsNull(result.Query, "Blank query should be reported as null.");
        Assert.IsTrue(result.MatchCount > 30, "Compact list should include every command.");
        Assert.IsTrue(result.Commands.All(c => c.Arguments.Count == 0),
            "Compact list should omit argument detail.");
    }

    [TestMethod]
    public void DiscoverCliCommands_NoMatch_ReturnsEmptyWithGuidance()
    {
        var tools = new CliDiscoveryTools();

        var result = tools.DiscoverCliCommands("zzz-not-a-real-command");

        Assert.AreEqual(0, result.MatchCount, "Should find no matches.");
        Assert.IsTrue(result.Note.Contains("No azdoutil commands matched"),
            "Should return guidance when nothing matches.");
    }
}
