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
- Arguments are defined using `[Argument]` attributes on properties

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
- Delivery tools are in `McpTools/DeliveryIntelligenceTools.cs` (`[McpServerToolType]`), each a thin adapter over `FlowMetricsService`; tool descriptions use outcome language (delivery window, "what's stuck") because the description is what the LLM reads. `KnownException`s are rethrown as `McpException` so the friendly message reaches the client.
- `McpTools/ConfigurationTools.cs` adds `list_configurations` so an assistant can see which Azure DevOps connections exist (never returns tokens) and get a friendly "run addconfig" message when none exist.
- `McpTools/AzureDevOpsContextTools.cs` adds read-only "context/discovery" tools (list team projects, get project info, list teams, list process templates, get work item types/states, list/run work item queries, list git repos, analyze repo). Each runs the existing CLI command with a captured `StringBuilderTextOutputProvider` (so its report never reaches stdout) and returns that text — reusing the CLI's formatting instead of duplicating query logic. Only read-only commands are exposed; state-changing commands are intentionally omitted for now.
- `McpTools/CliCommandCatalog.cs` + `McpTools/CliDiscoveryTools.cs` add `discover_cli_commands`, a fallback tool that searches the full CLI command catalog (built by reflecting over the same `[Command]`/`[Argument]` metadata as `azdoutil --json`, so it never drifts) and returns commands with arguments and an example command line. It flags commands already exposed as MCP tools (via a name→tool map — keep it updated when adding tools), and the server instructions tell the model to use it when no dedicated tool covers a request. Nothing is executed; it only describes commands.
- **stdout is the JSON-RPC transport, so nothing may be written to stdout** in the server path; logging is routed to stderr.
- Configuration name is resolved per tool call, then from the `AZDO_CONFIG_NAME` environment variable, then the default configuration. Missing configurations produce an error listing the available ones.
- The server is purely additive; existing CLI commands are unaffected.

### MCP client setup command

`azdoutil mcp-config` (`Commands/Mcp/McpConfigCommand.cs`) shows or manages the MCP server registration for an AI client. With no options it prints ready-to-paste configuration for Claude Code, Claude Desktop, VS Code, Visual Studio 2022/2026, and Cursor; `/install` and `/uninstall` register/remove the server at **user (per-machine) scope** via `claude mcp add`/`remove` or `code --add-mcp`. The pure string/argument builders live in `McpTools/McpClientSetup.cs` (unit tested); the command only handles argument parsing and cross-platform process execution.

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

### BuildReadiness Module

The `BuildReadiness/` directory contains the analysis engine behind the `analyzerepo` and `analyzeallrepos` commands (in `Commands/VersionControl/`) and the `analyze_repository` MCP tool. It inspects a repository's build readiness — languages, solutions, project files, NuGet/external references — **without cloning**, using the Azure DevOps Git Items API:

- **BuildReadinessAnalyzer** - Orchestrates the analysis of a repository's contents
- **SolutionFileParser** / **ProjectFileParser** - Parse `.sln` and project files into `SolutionAnalysisResult` / `ProjectFileAnalysisResult`
- **BuildReadinessReportFormatter** - Formats results into the text report shared by the CLI and MCP paths
- **IFileContentProvider** - Abstraction over file retrieval that enables in-memory unit testing; `DelegateFileContentProvider` bridges the protected base class HTTP methods to this interface

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
- **Version Control** - Git repos and TFVC to Git migration
- **Work Items** - Work item queries and operations

## Creating New Commands

To add a new command:

1. Create a new class in `Benday.AzureDevOpsUtil.Api` (or appropriate subdirectory)
2. Inherit from `AzureDevOpsCommandBase` (or `SynchronousCommand` for simple commands)
3. Add `[Command]` attribute with category, name, and description
4. Add `[Argument]` attributes for parameters
5. Override `OnExecute()` method (or `Execute()` for synchronous commands)

The framework automatically discovers the command and adds it to help.

Example:
```csharp
[Command(
    Category = Constants.Category_WorkItems,
    Name = "mycommand",
    Description = "Does something useful",
    IsAsync = true)]
public class MyCommand : AzureDevOpsCommandBase
{
    [Argument(Name = "teamproject", Description = "Team project name", IsRequired = true)]
    public string TeamProject { get; set; }

    protected override async Task OnExecute()
    {
        var httpClient = GetHttpClientInstanceForAzureDevOps();
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

Commands automatically generate documentation by reflecting on `[Command]` and `[Argument]` attributes. The `GenerateReadmeFiles` test in `MarkdownUsageFormatterFixture` assembles the templates and writes to `generated-readme-files/`; the update scripts copy those over the root READMEs. **Never hand-edit `README.md` / `README-for-nuget.md` directly — regeneration overwrites them.** `McpToolDocumentationFixture` fails the build if an `[McpServerTool]` exists that isn't documented in both MCP readme templates.

Run `./update-readme-files-from-generated.sh` (or `.ps1`) to regenerate README files.

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet.yml`):
1. **Build job**: Builds all target frameworks (8, 9, 10), runs tests, packs NuGet package
2. **Deploy job**: Pushes to NuGet on main branch pushes (requires `NUGET_API_KEY` secret)

## Key Design Patterns

- **Convention over Configuration**: Commands auto-discovered via attributes, minimal bootstrapping code
- **Typed API Interactions**: Strong typing throughout with message classes, JSON serialization handled centrally
- **Resilience**: Retry logic for Azure DevOps API calls, special handling for deadlock errors (TF400037)
- **Extensibility**: New commands added by creating class + attribute, framework automatically integrates
- **Single Responsibility**: Commands are focused on one task, complex operations composed of multiple API calls
