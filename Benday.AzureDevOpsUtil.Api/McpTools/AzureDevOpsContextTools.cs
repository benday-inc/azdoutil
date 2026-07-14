using System.ComponentModel;

using Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;
using Benday.AzureDevOpsUtil.Api.Commands.VersionControl;
using Benday.AzureDevOpsUtil.Api.Commands.WorkItems;
using Benday.CommandsFramework;

using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// Read-only "context" MCP tools that help an assistant discover the projects,
/// teams, work item types, queries, and repositories it can talk about — for
/// example to find the exact team project name to pass to the flow metrics
/// tools.
///
/// Each tool is a thin adapter over an existing azdoutil command. The command
/// runs with a captured (in-memory) output provider so its report text never
/// touches stdout (which is the MCP JSON-RPC transport) and is returned to the
/// caller. This reuses the CLI's already-tested formatting; no calculation or
/// query logic is duplicated here.
/// </summary>
[McpServerToolType]
public class AzureDevOpsContextTools
{
    [McpServerTool(Name = "list_team_projects")]
    [Description(
        "List the team projects in the connected Azure DevOps organization or collection. " +
        "Use this when someone asks 'what projects are there?' or to find the exact team " +
        "project name to pass to the flow metrics tools.")]
    public Task<string> ListTeamProjects(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName)
    {
        return RunAsync(configName,
            (info, output) => new ListTeamProjectsCommand(info, output),
            Constants.CommandName_ListProjects);
    }

    [McpServerTool(Name = "get_project_info")]
    [Description(
        "Get details about a specific team project (id, URL, state, and process). Use this " +
        "when someone asks about a particular project or to confirm a project exists before " +
        "running other tools.")]
    public Task<string> GetProjectInfo(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        RequireValue(teamProject, nameof(teamProject));

        return RunAsync(configName,
            (info, output) => new GetTeamProjectCommand(info, output),
            Constants.CommandName_GetProject,
            (Constants.ArgumentNameTeamProjectName, teamProject));
    }

    [McpServerTool(Name = "list_teams")]
    [Description(
        "List the teams inside a team project. Use this to find the exact team name for " +
        "team-scoped flow metrics, or when someone asks 'what teams are on this project?'")]
    public Task<string> ListTeams(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        RequireValue(teamProject, nameof(teamProject));

        return RunAsync(configName,
            (info, output) => new ListTeamsForProjectCommand(info, output),
            Constants.CommandArgumentName_ListTeams,
            (Constants.ArgumentNameTeamProjectName, teamProject));
    }

    [McpServerTool(Name = "list_process_templates")]
    [Description(
        "List the process templates available in the Azure DevOps organization (for example " +
        "Scrum, Agile, Basic, and any inherited processes). Use this when someone asks 'what " +
        "processes are available?' or before creating a project.")]
    public Task<string> ListProcessTemplates(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName)
    {
        return RunAsync(configName,
            (info, output) => new ListProcessTemplatesCommand(info, output),
            Constants.CommandName_ListProcessTemplates);
    }

    [McpServerTool(Name = "get_work_item_types")]
    [Description(
        "List the work item types available in a project (for example Product Backlog Item, " +
        "Bug, Task). Use this when someone asks 'what kinds of work items can we create?' or " +
        "to find a valid work item type name for other tools.")]
    public Task<string> GetWorkItemTypes(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        RequireValue(teamProject, nameof(teamProject));

        return RunAsync(configName,
            (info, output) => new GetWorkItemTypesCommand(info, output),
            Constants.CommandArgumentNameGetWorkItemTypes,
            (Constants.ArgumentNameTeamProjectName, teamProject));
    }

    [McpServerTool(Name = "get_work_item_type_states")]
    [Description(
        "List the workflow states for a work item type in a project (for example New, " +
        "Approved, Committed, Done). Use this when someone asks 'what states does a Bug go " +
        "through?' or to understand a team's workflow.")]
    public Task<string> GetWorkItemTypeStates(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("The work item type name, e.g. 'Bug' or 'Product Backlog Item'.")]
        string workItemType)
    {
        RequireValue(teamProject, nameof(teamProject));
        RequireValue(workItemType, nameof(workItemType));

        return RunAsync(configName,
            (info, output) => new GetWorkItemStatesCommand(info, output),
            Constants.CommandArgumentNameGetWorkItemStates,
            (Constants.ArgumentNameTeamProjectName, teamProject),
            (Constants.ArgumentNameWorkItemTypeName, workItemType));
    }

    [McpServerTool(Name = "list_work_item_queries")]
    [Description(
        "List the saved work item queries (shared queries and My Queries) in a project. Use " +
        "this when someone asks 'what queries are available?' or to find a query to run.")]
    public Task<string> ListWorkItemQueries(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        RequireValue(teamProject, nameof(teamProject));

        return RunAsync(configName,
            (info, output) => new ListWorkItemQueriesCommand(info, output),
            Constants.CommandArgumentNameListWorkItemQueries,
            (Constants.ArgumentNameTeamProjectName, teamProject));
    }

    [McpServerTool(Name = "run_work_item_query")]
    [Description(
        "Run a saved work item query by name and return the matching work items. Use this " +
        "when someone asks to 'run the X query' or 'show me the work items from query Y'.")]
    public Task<string> RunWorkItemQuery(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("The name (or path) of the saved query to run.")]
        string queryName)
    {
        RequireValue(teamProject, nameof(teamProject));
        RequireValue(queryName, nameof(queryName));

        return RunAsync(configName,
            (info, output) => new RunWorkItemQueryCommand(info, output),
            Constants.CommandName_RunWorkItemQuery,
            (Constants.ArgumentNameTeamProjectName, teamProject),
            (Constants.ArgumentNameWorkItemQueryName, queryName));
    }

    [McpServerTool(Name = "list_git_repositories")]
    [Description(
        "List the Git repositories in a team project. Use this when someone asks 'what repos " +
        "are in this project?'")]
    public Task<string> ListGitRepositories(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject)
    {
        RequireValue(teamProject, nameof(teamProject));

        return RunAsync(configName,
            (info, output) => new ListGitRepositoriesForProjectCommand(info, output),
            Constants.CommandArgumentName_ListGitRepos,
            (Constants.ArgumentNameTeamProjectName, teamProject));
    }

    [McpServerTool(Name = "analyze_repository")]
    [Description(
        "Analyze a Git repository for build readiness — what languages, build files, and " +
        "project types it contains — without cloning it. Use this when someone asks 'what's " +
        "in this repo?' or 'is this repo ready to build / what would it take to build it?'")]
    public Task<string> AnalyzeRepository(
        [Description("Stored azdoutil configuration to use. Leave blank for AZDO_CONFIG_NAME or the default.")]
        string configName,
        [Description("The Azure DevOps team project name.")]
        string teamProject,
        [Description("The Git repository name to analyze.")]
        string repositoryName)
    {
        RequireValue(teamProject, nameof(teamProject));
        RequireValue(repositoryName, nameof(repositoryName));

        return RunAsync(configName,
            (info, output) => new AnalyzeRepoCommand(info, output),
            Constants.CommandName_AnalyzeRepo,
            (Constants.ArgumentNameTeamProjectName, teamProject),
            (Constants.ArgumentNameRepositoryName, repositoryName));
    }

    private static void RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new McpException($"The '{parameterName}' parameter is required.");
        }
    }

    /// <summary>
    /// Builds the command-line arguments, runs the command with a captured
    /// output provider (so nothing reaches stdout), and returns its report text.
    /// azdoutil's user-facing <see cref="KnownException"/> is surfaced as an
    /// <see cref="McpException"/> so the message reaches the assistant.
    /// </summary>
    private static async Task<string> RunAsync(
        string configName,
        Func<CommandExecutionInfo, ITextOutputProvider, AsynchronousCommand> commandFactory,
        string commandName,
        params (string Name, string Value)[] namedArguments)
    {
        var resolvedConfig = DeliveryIntelligenceTools.ResolveConfigName(configName);

        var argumentList = new List<string> { commandName };

        // Only pass /config when it differs from the default sentinel so the
        // command falls back to its own default handling otherwise.
        if (resolvedConfig != Constants.DefaultConfigurationName)
        {
            argumentList.Add($"/{Constants.ArgumentNameConfigurationName}:{resolvedConfig}");
        }

        foreach (var (name, value) in namedArguments)
        {
            argumentList.Add($"/{name}:{value}");
        }

        try
        {
            var executionInfo = new ArgumentCollectionFactory().Parse(argumentList.ToArray());
            var output = new StringBuilderTextOutputProvider();

            var command = commandFactory(executionInfo, output);

            await command.ExecuteAsync();

            var report = output.GetOutput();

            return string.IsNullOrWhiteSpace(report)
                ? "The command completed but produced no output."
                : report;
        }
        catch (KnownException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
