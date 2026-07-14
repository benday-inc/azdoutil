using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.McpTools;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ConfigurationToolsFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        // Point the configuration manager singleton at an isolated temp file.
        Utilities.InitializeTestModeConfigurationManager();
    }

    [TestMethod]
    public void ListConfigurations_NoConfigurations_ReturnsFriendlyMessage()
    {
        var tools = new ConfigurationTools();

        var result = tools.ListConfigurations();

        Assert.AreEqual(0, result.Count, "Should report zero configurations.");
        Assert.AreEqual(0, result.Configurations.Count, "Configuration list should be empty.");
        Assert.IsTrue(result.Message.Contains("addconfig"),
            $"Empty-state message should explain how to add a config. Actual: '{result.Message}'");
    }

    [TestMethod]
    public void ListConfigurations_WithConfigurations_ReturnsSummariesWithoutToken()
    {
        var manager = AzureDevOpsConfigurationManager.Instance;

        manager.Save(new AzureDevOpsConfiguration
        {
            Name = "prod",
            CollectionUrl = "https://dev.azure.com/contoso",
            Token = "SECRET-TOKEN-VALUE"
        });
        manager.Save(new AzureDevOpsConfiguration
        {
            Name = "onprem",
            CollectionUrl = "https://tfs.contoso.com/DefaultCollection",
            IsWindowsAuth = true
        });

        var tools = new ConfigurationTools();

        var result = tools.ListConfigurations();

        Assert.AreEqual(2, result.Count, "Should report two configurations.");

        var prod = result.Configurations.Single(x => x.Name == "prod");
        Assert.AreEqual("Personal access token", prod.AuthMethod, "Wrong auth method for PAT config.");
        Assert.AreEqual("contoso", prod.AccountOrCollectionName, "Wrong account name.");

        var onprem = result.Configurations.Single(x => x.Name == "onprem");
        Assert.AreEqual("Windows authentication", onprem.AuthMethod, "Wrong auth method for Windows-auth config.");

        // The stored PAT must never appear in the summary output.
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.IsFalse(serialized.Contains("SECRET-TOKEN-VALUE"),
            "The access token must not be exposed in the configuration summary.");
    }
}
