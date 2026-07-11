using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.McpTools;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class McpClientSetupFixture
{
    [TestMethod]
    public void VsCodeAddMcpJson_WithoutConfig_OmitsEnv()
    {
        var json = McpClientSetup.VsCodeAddMcpJson(null);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("azdoutil", root.GetProperty("name").GetString(), "Wrong name.");
        Assert.AreEqual("stdio", root.GetProperty("type").GetString(), "VS Code entry should be stdio.");
        Assert.AreEqual("azdoutil", root.GetProperty("command").GetString(), "Wrong command.");
        Assert.AreEqual("mcp-server", root.GetProperty("args")[0].GetString(), "Wrong args.");
        Assert.IsFalse(root.TryGetProperty("env", out _), "Env should be omitted when no config is given.");
    }

    [TestMethod]
    public void VsCodeAddMcpJson_WithConfig_IncludesEnv()
    {
        var json = McpClientSetup.VsCodeAddMcpJson("myconfig");

        using var doc = JsonDocument.Parse(json);
        var env = doc.RootElement.GetProperty("env");

        Assert.AreEqual("myconfig", env.GetProperty("AZDO_CONFIG_NAME").GetString(),
            "Env should carry the configuration name.");
    }

    [TestMethod]
    public void ClaudeAddArguments_WithoutConfig_HasNoEnvFlag()
    {
        var args = McpClientSetup.ClaudeAddArguments(null);

        CollectionAssert.AreEqual(
            new[] { "mcp", "add", "azdoutil", "-s", "user", "--", "azdoutil", "mcp-server" },
            args.ToArray(),
            "Wrong claude add arguments without a config.");
    }

    [TestMethod]
    public void ClaudeAddArguments_WithConfig_IncludesEnvFlagBeforeSeparator()
    {
        var args = McpClientSetup.ClaudeAddArguments("myconfig").ToArray();

        CollectionAssert.AreEqual(
            new[] { "mcp", "add", "azdoutil", "-s", "user", "-e", "AZDO_CONFIG_NAME=myconfig", "--", "azdoutil", "mcp-server" },
            args,
            "Wrong claude add arguments with a config.");

        // The '--' separator must come after the variadic -e flag so the server
        // command is not swallowed as an environment variable.
        var envIndex = Array.IndexOf(args, "-e");
        var separatorIndex = Array.IndexOf(args, "--");
        Assert.IsTrue(envIndex < separatorIndex, "'-e' must precede the '--' separator.");
    }

    [TestMethod]
    public void ClaudeRemoveArguments_TargetsUserScope()
    {
        var args = McpClientSetup.ClaudeRemoveArguments();

        CollectionAssert.AreEqual(
            new[] { "mcp", "remove", "azdoutil", "-s", "user" },
            args.ToArray(),
            "Wrong claude remove arguments.");
    }

    [TestMethod]
    public void PrintableInstructions_WithConfig_MentionsConfigAndEnvVar()
    {
        var text = McpClientSetup.PrintableInstructions("myconfig");

        Assert.IsTrue(text.Contains("myconfig"), "Should mention the configuration name.");
        Assert.IsTrue(text.Contains("AZDO_CONFIG_NAME"), "Should mention the env var.");
        Assert.IsTrue(text.Contains("mcpServers") && text.Contains("servers"),
            "Should mention both client config key names.");
    }
}
