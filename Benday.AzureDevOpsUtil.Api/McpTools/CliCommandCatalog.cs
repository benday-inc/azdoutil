using System.Reflection;
using System.Text;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// Builds a catalog of every azdoutil CLI command by reflecting over the same
/// <c>[Command]</c> / <c>[Argument]</c> metadata that powers <c>azdoutil --json</c>,
/// so the catalog can never drift from the actual CLI. Used by the
/// <c>discover_cli_commands</c> MCP tool to suggest command-line commands for
/// tasks that aren't exposed as dedicated MCP tools.
/// </summary>
public static class CliCommandCatalog
{
    /// <summary>
    /// Commands that are already surfaced as MCP tools, so the discovery tool
    /// can point the assistant at the tool instead of the command line. Keep in
    /// sync when new tools are added.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> McpToolByCommandName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cycletimeconfidence"] = "get_typical_delivery_window",
            ["throughputcycletime"] = "get_throughput",
            ["forecastdurationforitemcount"] = "forecast_completion_date",
            ["forecastitemsinweeks"] = "forecast_items_in_timeframe",
            ["agingwork"] = "get_aging_work",
            ["listconfig"] = "list_configurations",
            ["listprojects"] = "list_team_projects",
            ["getproject"] = "get_project_info",
            ["listteams"] = "list_teams",
            ["listprocesstemplates"] = "list_process_templates",
            ["getworkitemtypes"] = "get_work_item_types",
            ["getworkitemstates"] = "get_work_item_type_states",
            ["listworkitemqueries"] = "list_work_item_queries",
            ["runworkitemquery"] = "run_work_item_query",
            ["listgitrepos"] = "list_git_repositories",
            ["analyzerepo"] = "analyze_repository",
        };

    public static IReadOnlyList<CliCommandDescriptor> GetCommands()
    {
        var assembly = typeof(Constants).Assembly;
        var factory = new ArgumentCollectionFactory();

        var commands = new List<CliCommandDescriptor>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || type.IsClass == false)
            {
                continue;
            }

            var attribute = type.GetCustomAttribute<CommandAttribute>();

            if (attribute == null)
            {
                continue;
            }

            var descriptor = new CliCommandDescriptor
            {
                Name = attribute.Name,
                Category = attribute.Category,
                Description = attribute.Description,
                IsAsync = attribute.IsAsync
            };

            if (McpToolByCommandName.TryGetValue(attribute.Name, out var toolName))
            {
                descriptor.AvailableAsMcpTool = true;
                descriptor.McpToolName = toolName;
            }

            TryPopulateArguments(type, attribute.Name, factory, descriptor);

            descriptor.CommandLineExample = BuildExample(descriptor);

            commands.Add(descriptor);
        }

        return commands
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static void TryPopulateArguments(
        Type type, string commandName, ArgumentCollectionFactory factory, CliCommandDescriptor descriptor)
    {
        try
        {
            var executionInfo = factory.Parse(new[] { commandName });
            var instance = Activator.CreateInstance(
                type, executionInfo, new StringBuilderTextOutputProvider());

            var getArguments = type.GetMethod("GetArguments");

            if (getArguments?.Invoke(instance, null) is IEnumerable<IArgument> arguments)
            {
                foreach (var argument in arguments)
                {
                    descriptor.Arguments.Add(new CliArgumentDescriptor
                    {
                        Name = argument.Name,
                        Description = argument.Description,
                        DataType = argument.DataType.ToString(),
                        IsRequired = argument.IsRequired,
                        AllowsEmptyValue = argument.AllowEmptyValue,
                        AllowedValues = argument.AllowedValues ?? Array.Empty<string>()
                    });
                }
            }
        }
        catch
        {
            // If a command can't be introspected, still list its name and
            // description without argument detail.
        }
    }

    private static string BuildExample(CliCommandDescriptor descriptor)
    {
        var builder = new StringBuilder($"azdoutil {descriptor.Name}");

        foreach (var argument in descriptor.Arguments.Where(x => x.IsRequired))
        {
            builder.Append($" /{argument.Name}:<{argument.DataType.ToLowerInvariant()}>");
        }

        return builder.ToString();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}

public class CliCommandDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public bool AvailableAsMcpTool { get; set; }
    public string? McpToolName { get; set; }
    public string CommandLineExample { get; set; } = string.Empty;
    public List<CliArgumentDescriptor> Arguments { get; set; } = new();
}

public class CliArgumentDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool AllowsEmptyValue { get; set; }
    public string[] AllowedValues { get; set; } = Array.Empty<string>();
}
