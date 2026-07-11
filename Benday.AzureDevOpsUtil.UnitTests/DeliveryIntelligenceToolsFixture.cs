using Benday.AzureDevOpsUtil.Api;
using Benday.AzureDevOpsUtil.Api.McpTools;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class DeliveryIntelligenceToolsFixture
{
    private const string EnvVarName = "AZDO_CONFIG_NAME";

    [TestCleanup]
    public void OnCleanup()
    {
        Environment.SetEnvironmentVariable(EnvVarName, null);
    }

    [TestMethod]
    public void ResolveConfigName_PrefersExplicitParameter()
    {
        Environment.SetEnvironmentVariable(EnvVarName, "from-env");

        var actual = DeliveryIntelligenceTools.ResolveConfigName("from-param");

        Assert.AreEqual("from-param", actual, "Explicit parameter should win.");
    }

    [TestMethod]
    public void ResolveConfigName_FallsBackToEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(EnvVarName, "from-env");

        var actual = DeliveryIntelligenceTools.ResolveConfigName(null);

        Assert.AreEqual("from-env", actual, "Environment variable should be used when no parameter is provided.");
    }

    [TestMethod]
    public void ResolveConfigName_FallsBackToDefaultWhenNothingProvided()
    {
        Environment.SetEnvironmentVariable(EnvVarName, null);

        var actual = DeliveryIntelligenceTools.ResolveConfigName("   ");

        Assert.AreEqual(Constants.DefaultConfigurationName, actual,
            "Should fall back to the default configuration name.");
    }
}
