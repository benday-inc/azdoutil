using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// MCP tool for discovering azdoutil command-line commands that aren't exposed
/// as dedicated MCP tools, so the assistant can fall back to suggesting the CLI.
/// </summary>
[McpServerToolType]
public class CliDiscoveryTools
{
    [McpServerTool(Name = "discover_cli_commands")]
    [Description(
        "Search the full azdoutil command-line catalog to find a command for an Azure DevOps " +
        "task that these MCP tools don't directly cover. Returns matching commands with their " +
        "description, arguments, and an example command line, and flags when a command is " +
        "already available as one of these MCP tools. Use this when a user asks for something " +
        "you don't have a dedicated tool for, so you can tell them the exact 'azdoutil ...' " +
        "command to run instead of saying it isn't possible.")]
    public CliCommandSearchResult DiscoverCliCommands(
        [Description("Optional case-insensitive search over command name, description, and category " +
            "(e.g. 'release', 'process template', 'tfvc', 'iteration'). Leave blank to list every " +
            "command in compact form.")]
        string query = "")
    {
        var allCommands = CliCommandCatalog.GetCommands();

        if (string.IsNullOrWhiteSpace(query))
        {
            return new CliCommandSearchResult
            {
                Query = null,
                MatchCount = allCommands.Count,
                Commands = allCommands.Select(ToCompact).ToList(),
                Note = "Compact list of every command (name, category, description). Call again " +
                    "with a query to get the arguments and an example command line for specific commands."
            };
        }

        var term = query.Trim();

        var matches = allCommands
            .Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new CliCommandSearchResult
        {
            Query = term,
            MatchCount = matches.Count,
            Commands = matches,
            Note = matches.Count == 0
                ? $"No azdoutil commands matched '{term}'. Try a broader term, or call this tool " +
                  "with a blank query to see everything."
                : "For any command that is also available as an MCP tool (availableAsMcpTool = true), " +
                  "prefer calling that tool. Otherwise tell the user the command-line example."
        };
    }

    private static CliCommandDescriptor ToCompact(CliCommandDescriptor command)
    {
        return new CliCommandDescriptor
        {
            Name = command.Name,
            Category = command.Category,
            Description = command.Description,
            AvailableAsMcpTool = command.AvailableAsMcpTool,
            McpToolName = command.McpToolName
            // Arguments and example intentionally omitted from the compact list.
        };
    }
}

public class CliCommandSearchResult
{
    public string? Query { get; set; }
    public int MatchCount { get; set; }
    public List<CliCommandDescriptor> Commands { get; set; } = new();
    public string Note { get; set; } = string.Empty;
}
