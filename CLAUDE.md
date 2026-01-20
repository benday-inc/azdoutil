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
- Program.cs (28 lines) creates a `DefaultProgram` instance and delegates everything to the framework
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

## Command Categories

Commands are organized into logical categories (defined in `Constants.cs`):
- **AzdoUtil Configuration** - Manage stored credentials and connections
- **Builds** - Build and release definition operations
- **Flow Metrics** - Cycle time, throughput, Monte Carlo forecasting
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
- `misc/readme-header.md` - Introduction and getting started
- `misc/readme-categories-github.md` - Category descriptions for GitHub
- `misc/readme-categories-nuget.md` - Category descriptions for NuGet package

Commands automatically generate documentation by reflecting on `[Command]` and `[Argument]` attributes.

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
