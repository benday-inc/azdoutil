using Benday.AzureDevOpsUtil.Api.McpTools;

using ModelContextProtocol;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AzureDevOpsContextToolsFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        // Isolate the configuration manager so there are no real configs.
        Utilities.InitializeTestModeConfigurationManager();
    }

    private readonly AzureDevOpsContextTools _tools = new();

    [TestMethod]
    public void GetProjectInfo_EmptyTeamProject_ThrowsMcpException()
    {
        Assert.ThrowsExactly<McpException>(() => _tools.GetProjectInfo("cfg", ""));
    }

    [TestMethod]
    public void GetWorkItemTypeStates_MissingWorkItemType_ThrowsMcpException()
    {
        Assert.ThrowsExactly<McpException>(() => _tools.GetWorkItemTypeStates("cfg", "MyProject", "   "));
    }

    [TestMethod]
    public void AnalyzeRepository_MissingRepositoryName_ThrowsMcpException()
    {
        Assert.ThrowsExactly<McpException>(() => _tools.AnalyzeRepository("cfg", "MyProject", ""));
    }

    [TestMethod]
    public async Task ListTeamProjects_MissingConfiguration_ThrowsMcpException()
    {
        // No configurations exist, so resolving the connection fails before any
        // network call and the friendly error is surfaced as an McpException.
        var ex = await Assert.ThrowsExactlyAsync<McpException>(() => _tools.ListTeamProjects("does-not-exist"));

        Assert.IsTrue(ex.Message.Contains("does-not-exist"),
            $"Message should name the missing configuration. Actual: '{ex.Message}'");
    }
}
