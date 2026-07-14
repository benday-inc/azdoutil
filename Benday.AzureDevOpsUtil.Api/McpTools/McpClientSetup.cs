using System.Text;
using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// Pure helpers that build the MCP client configuration snippets and CLI
/// arguments used by the <c>mcp-config</c> command. Kept free of process
/// execution and console I/O so they can be unit tested.
/// </summary>
public static class McpClientSetup
{
    public const string ServerName = "azdoutil";
    public const string ConfigEnvironmentVariableName = "AZDO_CONFIG_NAME";

    private static bool HasConfig(string? configName)
    {
        return string.IsNullOrWhiteSpace(configName) == false;
    }

    /// <summary>
    /// The server entry as a single line of JSON. <paramref name="includeType"/>
    /// adds "type":"stdio" (required by VS Code and Visual Studio; Claude Desktop
    /// and Cursor do not use it).
    /// </summary>
    public static string ServerEntryJson(string? configName, bool includeType)
    {
        // Build with a Dictionary so key order is stable and values are escaped.
        var entry = new Dictionary<string, object>();

        if (includeType)
        {
            entry["type"] = "stdio";
        }

        entry["command"] = ServerName;
        entry["args"] = new[] { "mcp-server" };

        if (HasConfig(configName))
        {
            entry["env"] = new Dictionary<string, string>
            {
                { ConfigEnvironmentVariableName, configName! }
            };
        }

        return JsonSerializer.Serialize(entry);
    }

    /// <summary>
    /// The single-line JSON passed to <c>code --add-mcp</c>. Includes the server
    /// name because that is how the VS Code CLI identifies the entry.
    /// </summary>
    public static string VsCodeAddMcpJson(string? configName)
    {
        var entry = new Dictionary<string, object> { { "name", ServerName }, { "type", "stdio" } };

        entry["command"] = ServerName;
        entry["args"] = new[] { "mcp-server" };

        if (HasConfig(configName))
        {
            entry["env"] = new Dictionary<string, string>
            {
                { ConfigEnvironmentVariableName, configName! }
            };
        }

        return JsonSerializer.Serialize(entry);
    }

    /// <summary>Arguments for <c>claude mcp add</c> at user scope.</summary>
    public static IReadOnlyList<string> ClaudeAddArguments(string? configName)
    {
        var args = new List<string> { "mcp", "add", ServerName, "-s", "user" };

        if (HasConfig(configName))
        {
            args.Add("-e");
            args.Add($"{ConfigEnvironmentVariableName}={configName}");
        }

        args.Add("--");
        args.Add(ServerName);
        args.Add("mcp-server");

        return args;
    }

    /// <summary>Arguments for <c>claude mcp remove</c> at user scope.</summary>
    public static IReadOnlyList<string> ClaudeRemoveArguments()
    {
        return new List<string> { "mcp", "remove", ServerName, "-s", "user" };
    }

    /// <summary>
    /// Human-readable, copy-paste setup instructions for each supported client.
    /// </summary>
    public static string PrintableInstructions(string? configName)
    {
        var configNote = HasConfig(configName)
            ? $"using configuration '{configName}'"
            : "using the default configuration";

        var configFlag = HasConfig(configName) ? $" /config:{configName}" : string.Empty;

        var claudeEnvFlag = HasConfig(configName)
            ? $" -e {ConfigEnvironmentVariableName}={configName}"
            : string.Empty;

        var withType = ServerEntryJson(configName, includeType: true);
        var withoutType = ServerEntryJson(configName, includeType: false);

        var builder = new StringBuilder();

        builder.AppendLine($"azdoutil MCP server configuration ({configNote}).");
        builder.AppendLine("The server runs per-machine because 'azdoutil' is a global .NET tool.");
        builder.AppendLine();

        builder.AppendLine("--- Claude Code (CLI) ---");
        builder.AppendLine($"  azdoutil mcp-config /install{configFlag}");
        builder.AppendLine($"  or:  claude mcp add azdoutil -s user{claudeEnvFlag} -- azdoutil mcp-server");
        builder.AppendLine();

        builder.AppendLine("--- Claude Desktop (GUI) ---");
        builder.AppendLine("  Settings > Developer > Edit Config, then add under \"mcpServers\":");
        builder.AppendLine($"    \"azdoutil\": {withoutType}");
        builder.AppendLine("  File: Windows %APPDATA%\\Claude\\claude_desktop_config.json");
        builder.AppendLine("        macOS   ~/Library/Application Support/Claude/claude_desktop_config.json");
        builder.AppendLine("        Linux   ~/.config/Claude/claude_desktop_config.json");
        builder.AppendLine("  Restart Claude Desktop afterwards.");
        builder.AppendLine();

        builder.AppendLine("--- VS Code (GitHub Copilot) ---");
        builder.AppendLine($"  azdoutil mcp-config /install /client:vscode{configFlag}");
        builder.AppendLine("  or run the 'MCP: Open User Configuration' command and add under \"servers\":");
        builder.AppendLine($"    \"azdoutil\": {withType}");
        builder.AppendLine("  Then open Copilot Chat and switch to Agent mode.");
        builder.AppendLine();

        builder.AppendLine("--- Visual Studio 2022 (17.14+) / Visual Studio 2026 (GitHub Copilot) ---");
        builder.AppendLine("  Create %USERPROFILE%\\.mcp.json (all solutions) or <solution>\\.mcp.json, add under \"servers\":");
        builder.AppendLine($"    \"azdoutil\": {withType}");
        builder.AppendLine("  Then open Copilot Chat, choose Agent, and enable the azdoutil tools.");
        builder.AppendLine();

        builder.AppendLine("--- Cursor ---");
        builder.AppendLine("  ~/.cursor/mcp.json, add under \"mcpServers\":");
        builder.AppendLine($"    \"azdoutil\": {withoutType}");

        return builder.ToString();
    }
}
