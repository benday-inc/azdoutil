using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class FlowMetricsServiceFixture
{
    private AzureDevOpsConfigurationManager NewEmptyManager()
    {
        var tempConfig = Path.Combine(Utilities.GetTempFolder(), Constants.ConfigFileName);
        return new AzureDevOpsConfigurationManager(tempConfig);
    }

    [TestMethod]
    public async Task GetTypicalDeliveryWindow_NoConfigurations_ThrowsWithGuidance()
    {
        var manager = NewEmptyManager();
        var service = new FlowMetricsService(manager);

        var ex = await Assert.ThrowsExactlyAsync<KnownException>(() =>
            service.GetTypicalDeliveryWindowAsync("missing", "MyProject"));

        Assert.IsTrue(ex.Message.Contains("missing"),
            $"Message should name the missing configuration. Actual: '{ex.Message}'");
        Assert.IsTrue(ex.Message.Contains("no configurations", StringComparison.OrdinalIgnoreCase),
            $"Message should note there are no configurations. Actual: '{ex.Message}'");
    }

    [TestMethod]
    public async Task ForecastCompletion_MissingConfig_ListsAvailableConfigurations()
    {
        var manager = NewEmptyManager();

        manager.Save(new AzureDevOpsConfiguration
        {
            Name = "alpha",
            CollectionUrl = "https://dev.azure.com/benday",
            Token = "token1"
        });
        manager.Save(new AzureDevOpsConfiguration
        {
            Name = "beta",
            CollectionUrl = "https://dev.azure.com/benday2",
            Token = "token2"
        });

        var service = new FlowMetricsService(manager);

        var ex = await Assert.ThrowsExactlyAsync<KnownException>(() =>
            service.ForecastCompletionAsync("does-not-exist", "MyProject", 5));

        Assert.IsTrue(ex.Message.Contains("alpha") && ex.Message.Contains("beta"),
            $"Message should list available configurations. Actual: '{ex.Message}'");
    }
}
