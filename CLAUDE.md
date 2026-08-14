# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

azdoutil is a .NET CLI tool distributed as a global tool via NuGet that provides utilities for Azure DevOps automation. The tool helps with tasks like managing work items, build/release definitions, flow metrics calculation, and test data generation.

## Solution Structure

The solution uses a three-project architecture:

1. **Benday.AzureDevOpsUtil.Api** - Core library containing all business logic, commands, and Azure DevOps API integration
2. **Benday.AzureDevOpsUtil.ConsoleUi** - Minimal console entry point that bootstraps the CLI (packaged as .NET global tool)
3. **Benday.AzureDevOpsUtil.UnitTests** - Unit tests

Multi-targeted to .NET 8.0, 9.0, and 10.0.

## Build and Test Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Pack for NuGet
```bash
dotnet pack --configuration Release --output ./artifacts
```

### Install Locally for Testing
```bash
# From the repository root
./install.sh       # On Linux/macOS
./install.ps1      # On Windows (PowerShell)
```

### Uninstall
```bash
./uninstall.sh     # On Linux/macOS
./uninstall.ps1    # On Windows (PowerShell)
```

## Core Architecture

### Command Framework Pattern

The entire CLI is built on the **Benday.CommandsFramework** library which provides automatic command discovery and execution:

- Commands are decorated with `[Command]` attributes specifying category, name, and description
- The framework automatically discovers all command classes in the Api assembly
- Program.cs (~30 lines) creates a `DefaultProgram` instance and delegates everything to the framework
- Arguments are defined by overriding `GetArguments()` and returning a fluent-built `ArgumentCollection` (there is **no** `[Argument]` attribute in the framework)

### Base Command Hierarchy

All Azure DevOps commands inherit from **AzureDevOpsCommandBase**, which provides:
- Configuration management (accessing stored credentials from `~/azdoutil/azdoutil-config.json`)
- Authenticated HttpClient creation (supports PAT and Windows Auth)
- Typed Azure DevOps API calling methods with automatic retry logic
- Common argument handling (quiet mode, configuration name)

Commands extend either:
- `AsynchronousCommand` - For async operations (most commands)
- `SynchronousCommand` - For simple synchronous operations

### Configuration Management

- **AzureDevOpsConfigurationManager** singleton manages stored credentials
- Configurations stored in JSON at `~/azdoutil/azdoutil-config.json`
- Supports multiple named configurations with a default "(default)" configuration
- Each configuration contains: URL, account name, PAT token, and auth method

### Azure DevOps API Integration

Commands don't call Azure DevOps REST APIs directly. Instead, the base class provides typed methods:
- `CallEndpointViaGetAndGetResult<T>()` - GET requests with typed responses
- `SendPatchForBodyAndGetTypedResponse<T>()` - PATCH with JSON patch format
- `SendPostForBodyAndGetTypedResponse<T,TRequest>()` - POST operations

DTOs for Azure DevOps API are in `Benday.AzureDevOpsUtil.Api.Messages` namespace.

### Flow Metrics Calculation Services

Flow metrics calculations live in `Benday.AzureDevOpsUtil.Api/FlowMetrics/` as console-free, reusable services so they can be called both from the CLI commands and from the MCP server:

- **`MonteCarloForecaster`** - pure Monte Carlo simulation (weeks-for-item-count and items-in-weeks), returning distribution objects with confidence lookups. Accepts an injectable sampler for deterministic tests.
- **`CycleTimeCalculator`** - cycle time percentile math and the typical delivery window (50th/85th/95th).
- **`ThroughputWeekGrouper`** - groups completed items into throughput-by-week buckets.
- **`AzureDevOpsAnalyticsClient`** - console-free authenticated access to the Analytics OData endpoints, reusing `AzureDevOpsConfiguration`.
- **`FlowMetricsService`** - orchestrates the above into structured result DTOs (`FlowMetricsResults.cs`). This is the programmatic entry point used by the MCP tools.

The Flow Metrics CLI commands in `Commands/FlowMetrics/` are thin adapters that call these services and format the output, so the CLI and MCP paths produce identical numbers.

### MCP Server

`azdoutil mcp-server` starts a [Model Context Protocol](https://modelcontextprotocol.io) server over **stdio** (using `Microsoft.Extensions.Hosting` + the `ModelContextProtocol` SDK) that exposes the flow metrics calculations to an AI assistant.

- The command is `McpServerCommand` (`Commands/Mcp/`), a normal CommandsFramework command that stays alive by awaiting `host.RunAsync()`. It also sets `McpServerOptions.ServerInstructions` (sent at startup) to help clients route delivery/flow-metrics questions to these tools.
- Delivery tools are in `McpTools/DeliveryIntelligenceTools.cs` (`[McpServerToolType]`) — `get_typical_delivery_window`, `get_throughput`, `forecast_completion_date`, `forecast_items_in_timeframe`, `get_aging_work`, `get_project_summary` — each a thin adapter over `FlowMetricsService`; tool descriptions use outcome language (delivery window, "what's stuck") because the description is what the LLM reads. `KnownException`s are rethrown as `McpException` so the friendly message reaches the client.
- `McpTools/ConfigurationTools.cs` adds `list_configurations` so an assistant can see which Azure DevOps connections exist (never returns tokens) and get a friendly "run addconfig" message when none exist.
- `McpTools/AzureDevOpsContextTools.cs` adds read-only "context/discovery" tools (`list_team_projects`, `get_project_info`, `list_teams`, `list_process_templates`, `get_work_item_types`, `get_work_item_type_states`, `list_work_item_queries`, `run_work_item_query`, `list_git_repositories`, `analyze_repository`). Each runs the existing CLI command with a captured `StringBuilderTextOutputProvider` (so its report never reaches stdout) and returns that text — reusing the CLI's formatting instead of duplicating query logic. Only read-only commands are exposed; state-changing commands are intentionally omitted for now.
- `McpTools/CliCommandCatalog.cs` + `McpTools/CliDiscoveryTools.cs` add `discover_cli_commands`, a fallback tool that searches the full CLI command catalog (built by reflecting over the `[Command]` attributes and invoking each command's `GetArguments()`, the same metadata `azdoutil --json` uses, so it never drifts) and returns commands with arguments and an example command line. It flags commands already exposed as MCP tools (via the `McpToolByCommandName` map in `CliCommandCatalog.cs` — keep it updated when adding tools), and the server instructions tell the model to use it when no dedicated tool covers a request. Nothing is executed; it only describes commands.
- All four tool types are registered on the server in `McpServerCommand.OnExecute()` via `.WithTools<T>()`; a new `[McpServerToolType]` class must be added there or its tools won't be exposed.
- `McpToolDocumentationFixture` (unit tests) fails the build if an `[McpServerTool]` isn't documented in both `misc/readme-mcpserver-github.md` and `misc/readme-mcpserver-nuget.md`, so adding a tool means updating those templates and regenerating the READMEs.
- **stdout is the JSON-RPC transport, so nothing may be written to stdout** in the server path; logging is routed to stderr.
- Configuration name is resolved per tool call, then from the `AZDO_CONFIG_NAME` environment variable, then the default configuration. Missing configurations produce an error listing the available ones.
- The server is purely additive; existing CLI commands are unaffected.

### MCP client setup command

`azdoutil mcp-config` (`Commands/Mcp/McpConfigCommand.cs`) shows or manages the MCP server registration for an AI client. With no options it prints ready-to-paste configuration for Claude Code, Claude Desktop, VS Code, Visual Studio 2022/2026, and Cursor; `/install` and `/uninstall` register/remove the server at **user (per-machine) scope** via `claude mcp add`/`remove` or `code --add-mcp`. `/client:<name>` picks the target client (install/uninstall default to Claude Code) and `/config:<name>` bakes an `AZDO_CONFIG_NAME` environment variable into the registration. The pure string/argument builders live in `McpTools/McpClientSetup.cs` (unit tested); the command only handles argument parsing and cross-platform process execution.

### ScriptGenerator System

The `ScriptGenerator/` directory contains a sophisticated work item simulation engine that generates realistic sprint data:

- **WorkItemScriptGenerator** - Core logic that simulates Scrum sprints with refinement meetings, sprint planning, daily burndown, etc.
- Generates hierarchical work items (PBIs with child tasks)
- Simulates realistic team velocity and workflow progression
- Supports both direct execution and script export (Excel format)
- See `ScriptGenerator-Summary.md` for detailed logic explanation

Execution modes:
1. Direct execution - Creates work items immediately in Azure DevOps
2. Script export - Saves to Excel for later execution
3. Script-only mode - Just generates the plan

The **Test Data** category commands (`CreateWorkItemsFromDataGeneratorScriptCommand`, `CreateWorkItemInfoFromDataGeneratorCommand`, `CreateWorkItemsFromExcelScriptCommand`) live here in `ScriptGenerator/` rather than under `Commands/` — the framework discovers them by attribute regardless of directory.

### BuildReadiness Module

The `BuildReadiness/` directory contains the analysis engine behind the `analyzerepo` and `analyzeallrepos` commands (in `Commands/VersionControl/`) and the `analyze_repository` MCP tool. It inspects a repository's build readiness — languages, solutions, project files, NuGet/external references — **without cloning**, using the Azure DevOps Git Items API:

- **BuildReadinessAnalyzer** - Orchestrates the analysis of a repository's contents
- **SolutionFileParser** / **ProjectFileParser** - Parse `.sln` and project files into `SolutionAnalysisResult` / `ProjectFileAnalysisResult`
- **BuildReadinessReportFormatter** - Formats results into the text report shared by the CLI and MCP paths
- **IFileContentProvider** - Abstraction over file retrieval that enables in-memory unit testing; `DelegateFileContentProvider` bridges the protected base class HTTP methods to this interface

### TfvcAssessment Module

The `TfvcAssessment/` directory backs the `assess-tfvc-migration` command (in `Commands/VersionControl/`). It reads a TFVC path and reports what a conversion to Git would have to deal with. Everything is read-only.

Reports here are **descriptive, never prescriptive**: a finding is a fact plus its consequence, with no severity rating, no ranking, and no recommendation. `TfvcAssessmentReportFormatterFixture` fails the build if report text contains prescriptive language ("consider", "recommend", "should", "you may want", "try ").

- **ITfvcApiClient** - The read-only TFVC calls the assessment needs. Services depend on this rather than HTTP, so they run against canned payloads in tests (`FakeTfvcApiClient`). `TfvcApiClient` is the real implementation: it builds the urls and deserializes, taking HTTP itself as a delegate the same way `DelegateFileContentProvider` does
- **TfvcPath** - TFVC server path comparison. Containment is compared on directory boundaries, so `$/App/MainFrame` is not treated as living inside `$/App/Main`
- **TfvcBranchHierarchyService** - Scopes the branches payload to a path, rebuilds the lineage tree, and finds branches whose root folder sits inside another branch's root folder
- **TfvcFolderHeuristicScanner** - Finds folders used as branches without being registered as branches. Requires both a branch-like name and similar contents to sibling folders; caches listings per scan
- **TfvcBranchActivityService** - Changeset counts per branch over 90/180/365 days. The changesets endpoint returns no total, so counts come from a capped walk and a capped branch is flagged so the report shows its numbers as a floor
- **TfvcContentScanner** - Reports the largest files, the file types Git would carry forever, and folders that normally hold generated output or downloaded dependencies (`bin`, `obj`, `packages`, `node_modules`, …). A single `recursionLevel=Full` listing answers all three questions, so the scanner does no I/O — it is a pure function over the item list. Each file is attributed to the **outermost** matching folder in its path so the counts do not overlap, and the ambiguous names (`build`, `lib`, `out`, `target`, `Debug`, `Release`) are deliberately excluded because flagging them would make the section worth ignoring. Sizes are the current version of each file, which the report says plainly because Git carries every version
- **IBuildDefinitionApiClient** / **BuildDefinitionApiClient** - The build definition reads, same delegate arrangement. The definitions list returns shallow objects, so workspace mappings need a per-definition fetch; that fetch asks for `includeLatestBuilds=true` so the last-run date arrives without another call
- **TfvcWorkspaceMappingParser** - Workspace mappings live in `repository.properties.tfvcMapping` as a JSON document inside a string, so reading them is a second parse. `mappingType` casing varies between the REST payload and the older object model, so it is compared case-insensitively
- **BuildDefinitionWorkspaceService** - Finds the TFVC-connected build definitions, classifies each workspace as simple (one `map`, any number of `cloak`) or complex (2+ `map`), and counts how many definitions map each path. Runs against the whole team project regardless of the assessed path, because a build defined elsewhere can still map a folder inside it
- **TfvcAssessmentAnalyzer** - Orchestrates the above and turns observations into findings. The build definition section is wrapped so a permissions failure records a note instead of sinking the whole assessment
- **TfvcAssessmentReportFormatter** - Markdown report (including a Mermaid `graph TD` of branch lineage) plus a findings CSV

TFVC API notes worth not rediscovering, verified against a live collection: use `api-version=7.0` for the TFVC endpoints (matches the rest of the repo, and is what Server 2022 supports) and `7.1` for build definitions (matches the other build calls in this tool); recursion levels are `None`/`OneLevel`/`Full`. A one-level item listing **includes the folder that was asked for**, so callers filter it out, and it **omits `isFolder`, `isBranch`, and `size` rather than sending false or null** — those properties are nullable on `TfvcItemInfo` and "not a branch" is tested as `!= true`, never `== false`. The changesets endpoint supports `searchCriteria.itemPath`/`.fromDate`/`.toDate` plus `$top`/`$skip`, returns newest first, and returns no total count; `fromDate` accepts ISO 8601 (what this tool sends) as well as the `MM-dd-yyyy` form the REST samples show. Build definition repository type for TFVC is `TfsVersionControl`; YAML pipelines cannot use TFVC, so every TFVC-connected build is classic. There is **no merge-candidates endpoint** in the TFVC REST API, and shelvesets are collection-scoped rather than project- or path-scoped.

### BranchHealth Module

The `BranchHealth/` directory backs the `branchhealth` command (in `Commands/VersionControl/`). It surveys the branches of one Git repository and reports how much work is in flight: active branches, unmerged branches and their median age, dead branches, and who is working on more than one thing at once. Read-only and descriptive, with the same no-severity rule as the TFVC assessment.

- **BranchHealthAnalyzer** - Does no I/O. `GET .../git/repositories/{repo}/stats/branches` returns ahead/behind counts and the last commit for every branch in one call, so the command fetches and the analyzer does the arithmetic. Ages come from the **committer** date, not the author date, because an author date survives a rebase and can be set to anything. The median excludes the default branch, since one very old default would drag it
- **BranchHealthReportFormatter** - Markdown report plus a CSV with one row per branch. Its footer is the case study link, not the TFVC one

Note that the stats endpoint is known to be slow on repositories with hundreds of branches.

### TaskGroups Module

The `TaskGroups/` directory backs the task group commands in `Commands/Builds/` (`listtaskgroups`, `findtaskgroupusages`, `inlinetaskgroup`) — the main use case is retiring classic task groups by inlining their steps into the build definitions that use them:

- **TaskGroupClient** - Retrieves task groups and their versions from the Azure DevOps task group API
- **BuildDefinitionTaskGroupScanner** - Walks a build definition's phases/steps to find `TaskGroupReference`s, producing `TaskGroupUsage` records
- **BuildDefinitionInliner** - Replaces a task group step with the task group's own steps (parameter substitution) and disables the original reference; returns an `InlineResult`. Unit tested in `BuildDefinitionInlinerFixture`

### Other Api Directories

- `WorkItems/` - Work item type/field/state definition models plus a few work item commands that live outside `Commands/`
- `Excel/` - Excel read/write helpers used by the ScriptGenerator import/export paths
- `UsageFormatters/` - `MarkdownUsageFormatter`, used by the README generation tests
- `Messages/` - Azure DevOps REST API DTOs

## Command Categories

Commands are organized into logical categories (defined in `Constants.cs`):
- **AzdoUtil Configuration** - Manage stored credentials and connections
- **Builds** - Build and release definition operations
- **Flow Metrics** - Cycle time, throughput, Monte Carlo forecasting
- **MCP Server** - Run the MCP server and manage AI client registrations
- **Miscellaneous** - Uncategorized utilities (e.g. connection data)
- **Process Templates** - Process template operations
- **Project Administration** - Team projects and teams
- **Test Data** - ScriptGenerator-based data creation
- **Version Control** - Git repos, TFVC to Git migration, and TFVC migration assessment
- **Work Items** - Work item queries and operations

## Creating New Commands

To add a new command:

1. Create a new class in `Benday.AzureDevOpsUtil.Api` (or appropriate subdirectory)
2. Inherit from `AzureDevOpsCommandBase` (or `SynchronousCommand` for simple commands)
3. Add `[Command]` attribute with category, name, and description
4. Add a constructor taking `(CommandExecutionInfo info, ITextOutputProvider outputProvider)` — the framework activates commands through it
5. Override `GetArguments()` to declare parameters on an `ArgumentCollection`
6. Override `OnExecute()` method (or `Execute()` for synchronous commands), reading values via `Arguments.GetStringValue(...)` / `GetBooleanValue(...)`

The framework automatically discovers the command and adds it to help. Argument names live in `Constants.cs` so the CLI, help output, and README generation stay consistent.

Example:
```csharp
[Command(
    Category = Constants.Category_WorkItems,
    Name = "mycommand",
    Description = "Does something useful",
    IsAsync = true)]
public class MyCommand : AzureDevOpsCommandBase
{
    public MyCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        // adds the shared config-name / quiet arguments
        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        using var httpClient = GetHttpClientInstanceForAzureDevOps();
        // Use base class methods to call Azure DevOps API
    }
}
```

## README Generation

The README files are generated from templates in the `misc/` directory:
- `misc/readme-header.md` - Introduction and getting started (contains `%%CATEGORIES%%` and `%%MCPSERVER%%` tokens)
- `misc/readme-categories-github.md` - Category descriptions for GitHub
- `misc/readme-categories-nuget.md` - Category descriptions for NuGet package
- `misc/readme-mcpserver-github.md` - Full MCP server section (setup per client, routing, tool tables) for GitHub
- `misc/readme-mcpserver-nuget.md` - Condensed MCP server section for the NuGet package

Command documentation is generated by reflecting over the `[Command]` attributes and calling each command's `GetArguments()`. The `GenerateReadmeFiles` test in `MarkdownUsageFormatterFixture` assembles the templates and writes to `generated-readme-files/`; the update scripts copy those over the root READMEs. **Never hand-edit `README.md` / `README-for-nuget.md` directly — regeneration overwrites them.** `McpToolDocumentationFixture` fails the build if an `[McpServerTool]` exists that isn't documented in both MCP readme templates.

Run `./update-readme-files-from-generated.sh` (or `.ps1`) to regenerate README files.

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet.yml`):
1. **Build job**: Builds all target frameworks (8, 9, 10) in Release, uploads build output, packs the NuGet package. Note: the workflow does **not** run `dotnet test` — tests only run locally
2. **Deploy job**: Pushes to NuGet on main branch pushes (requires `NUGET_API_KEY` secret)

## Key Design Patterns

- **Convention over Configuration**: Commands auto-discovered via attributes, minimal bootstrapping code
- **Typed API Interactions**: Strong typing throughout with message classes, JSON serialization handled centrally
- **Resilience**: Retry logic for Azure DevOps API calls, special handling for deadlock errors (TF400037)
- **Extensibility**: New commands added by creating class + attribute, framework automatically integrates
- **Single Responsibility**: Commands are focused on one task, complex operations composed of multiple API calls
