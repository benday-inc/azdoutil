# azdoutil
A collection of useful Azure DevOps utilities.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
info@benday.com 

*Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/azdoutil/issues*. *Want to contribute? Submit a pull request.*

## Command Categories

<dl>

<dt>Azure DevOps Utility Configuration</dt>
<dd>Commands for setting up this tool and connecting to Azure DevOps</dd>

<dt>Automated Builds</dt>
<dd>Commands that help with automated builds and automated releases</dd>

<dt>Flow Metrics</dt>
<dd>Tools for forecasting project management details using Flow Metrics such as throughput and cycle time. <p /><p><i>Want to learn more about how to use Flow Metrics to run your projects? Check out this course: <br />
    <b><a href="https://courses.benday.com/c/flow-metrics-2023">Predicting the Future, Estimating, and Running Your Projects with Flow Metrics</a></b>.</i></p></dd>

<dt>Process Templates</dt>
<dd>Process template customization and administration utilities</dd>

<dt>Team Project Administration</dt>
<dd>Tools for creating, editing, and managing Team Projects in Azure DevOps</dd>

<dt>Test Data</dt>
<dd>Utilities for populating Azure DevOps Team Projects with test data</dd>

<dt>Version Control</dt>
<dd>Tools for creating, converting, managing version control repositories</dd>

<dt>Work Items</dt>
<dd>Utilities for editing work items and working with work item queries (WIQL)</dd>

<dt>Miscellaneous</dt>
<dd>Miscellaneous commands</dd>
</dl>

## Installing
The azdoutil is distributed as a .NET Core Tool via NuGet. To install it go to the command prompt and type  
`dotnet tool install azdoutil -g`

### Prerequisites
- You'll need to install .NET Core 7 from https://dotnet.microsoft.com/

## Getting Started
Everything starts with a configuration. After you've installed azdoutil, you'll need to run `azdoutil addconfig` to add a configuration. A configuration is how you store the URL for your Azure DevOps instance and the personal access token (PAT) for authenticating to that instance.  

Configurations are named and you can have as many as you'd like.

### Set a Default Configuration
There's one default configuration named `(default)`. If you only work with one Azure DevOps instance, then all you'll need to do is to is run `azdoutil addconfig /url:{url} /pat:{pat}` and that will set your default configuration. 

### Additional Named Configurations
If you want to add additional named configurations, you'll run `azdoutil addconfig /config:{name} /url:{url} /pat:{pat}`. 

### Running Commands
Once you've set a default configuration, you can run any azdoutil command without having to specify any additional URL or PAT info.  

If you want to run a command against an Azure DevOps instance that is NOT your default, you'll need to supply the `/config:{name}`.

### Managing Configurations
To add new configuration or modify an existing configuration, use the `azdoutil addconfig` command. You can list your configurations using the `azdoutil listconfig` command. To delete a configuration, use the `azdoutil removeconfig` command.
