# azdoutil
A collection of useful Azure DevOps utilities.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
info@benday.com 

*Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/azdoutil/issues*. *Want to contribute? Submit a pull request.*

## Command Categories NUGET

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

## Commands
| Category | Command Name | Description |
| --- | --- | --- |
| AzdoUtil Configuration | addconfig | Add or update an Azure DevOps configuration. For example, which server or account plus auth information. |
| AzdoUtil Configuration | listconfig | List an Azure DevOps configuration. For example, which server or account plus auth information. |
| AzdoUtil Configuration | removeconfig | Remove an Azure DevOps configuration. For example, which server or account plus auth information. |
| Builds | exportbuilddef | Export build definition |
| Builds | listbuilddefs | List build definitions |
| Flow Metrics | agingwork | Get aging in-progress work items |
| Flow Metrics | cycletimeconfidence | Get item cycle time for 50% and 85% levels. This helps you understand how items typically are delivered. |
| Flow Metrics | forecastdurationforitemcount | Use throughput data to forecast likely number of weeks to get given number of items done using Monte Carlo simulation |
| Flow Metrics | forecastitemsinweeks | Use throughput data to forecast likely number of items done in given number of weeks using Monte Carlo simulation |
| Flow Metrics | forecastworkitem | Use throughput data to forecast when a work item is likely to be done based on the current backlog priority using Monte Carlo simulation |
| Flow Metrics | suggest-sle | Calculate a suggested service level expectation (SLE) based on cycle time |
| Flow Metrics | throughputcycletime | Get cycle time and throughput data for a team project for a date range |
| Miscellaneous | connectiondata | Get information about a connection to Azure DevOps. |
| Process Templates | addrefinementprocess | Creates backlog refinement process template as described at https://www.benday.com/2022/09/29/streamlining-backlog-refinement-with-azure-devops/ |
| Process Templates | changeprocess | Change the process for a Team Project |
| Project Administration | createproject | Create team projects |
| Project Administration | createteam | Creates a new team in an Azure DevOps Team Project. |
| Project Administration | deleteproject | Delete team project |
| Project Administration | getproject | Get team project info |
| Project Administration | listprocesstemplates | List process templates |
| Project Administration | listprojects | List team projects |
| Project Administration | listteams | Gets list of teams in an Azure DevOps Team Project. |
| Test Data | createfromexcel | Create work items using Excel script |
| Test Data | createfromgenerator | Create work items using random data generator |
| Version Control | creategitrepo | Creates a Git repository in an Azure DevOps Team Project. |
| Version Control | listgitrepos | Gets list of Git repositories from an Azure DevOps Team Project. |
| Version Control | tfvc-to-git | Converts a Team Foundation Version Control (TFVC) folder to a Git repository. |
| Work Items | exportworkitemquery | Export work item query results |
| Work Items | getareas | Gets a list of areas in an Azure DevOps Team Project. |
| Work Items | getfields | Gets a list of work item fields for a work item type in an Azure DevOps Team Project. |
| Work Items | getiterations | Gets a list of iterations in an Azure DevOps Team Project. |
| Work Items | getworkitem | Get work item by id |
| Work Items | getworkitemstates | Gets the list of states for a work item type in an Azure DevOps Team Project. |
| Work Items | getworkitemtypes | Gets a list of work item types in an Azure DevOps Team Project. |
| Work Items | listworkitemqueries | Gets a list of all work item queries in an Azure DevOps Team Project. |
| Work Items | runworkitemquery | Run work item query |
| Work Items | setiteration | Create iteration including start and end date |
| Work Items | setworkitemstate | Set the state value on an existing work item |
| Work Items | showworkitemquery | Show work item query |
# AzdoUtil Configuration
## addconfig
**Add or update an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Optional | String | Name of the configuration |
| pat | Optional | String | PAT for this collection |
| windowsauth | Optional | Boolean | Use windows authentication with the current logged in user |
| url | Required | String | URL for this collection (example: https://dev.azure.com/accountname) |
## listconfig
**List an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Optional | String | Name of the configuration |
## removeconfig
**Remove an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Required | String | Name of the configuration |
# Builds
## exportbuilddef
**Export build definition**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| teamproject | Required | String | Team project name |
| name | Required | String | Build definition name |
| xaml | Optional | Boolean | List XAML build definitions |
| showlastruninfo | Optional | Boolean | Show last build run info |
| csv | Optional | Boolean | Output results in CSV format |
| csv-noheader | Optional | Boolean | Do not print the CSV column header info |
| raw | Optional | Boolean | Output raw build definition |
## listbuilddefs
**List build definitions**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| teamproject | Required | String | Team project name |
| nameonly | Optional | Boolean | Only display the build definition name |
| xaml | Optional | Boolean | List XAML build definitions |
# Flow Metrics
## agingwork
**Get aging in-progress work items**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
## cycletimeconfidence
**Get item cycle time for 50% and 85% levels. This helps you understand how items typically are delivered.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
## forecastdurationforitemcount
**Use throughput data to forecast likely number of weeks to get given number of items done using Monte Carlo simulation**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| forecastitemcount | Required | Int32 | Number of items to forecast duration for |
| teamname | Optional | String | Team name |
## forecastitemsinweeks
**Use throughput data to forecast likely number of items done in given number of weeks using Monte Carlo simulation**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| forecastweeks | Required | Int32 | Number of weeks into the future to forecast |
| teamname | Optional | String | Team name |
## forecastworkitem
**Use throughput data to forecast when a work item is likely to be done based on the current backlog priority using Monte Carlo simulation**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| id | Required | Int32 | Id of the work item to forecast |
| teamname | Optional | String | Team name |
## suggest-sle
**Calculate a suggested service level expectation (SLE) based on cycle time**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
| percent | Optional | Int32 | Percentage level to calculate. (For example, 85% of our items complete in X days) |
## throughputcycletime
**Get cycle time and throughput data for a team project for a date range**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
# Miscellaneous
## connectiondata
**Get information about a connection to Azure DevOps.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
# Process Templates
## addrefinementprocess
**Creates backlog refinement process template as described at https://www.benday.com/2022/09/29/streamlining-backlog-refinement-with-azure-devops/**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## changeprocess
**Change the process for a Team Project**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| processname | Required | String | New process name |
# Project Administration
## createproject
**Create team projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| processname | Required | String | Process template name |
## createteam
**Creates a new team in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the team |
| teamname | Required | String | Name of the new team |
| description | Optional | String | Description for the new team |
## deleteproject
**Delete team project**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name to delete |
| confirm | Optional | Boolean | Confirm delete |
## getproject
**Get team project info**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
## listprocesstemplates
**List process templates**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## listprojects
**List team projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## listteams
**Gets list of teams in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the teams |
# Test Data
## createfromexcel
**Create work items using Excel script**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| skipfuturedates | Optional | Boolean | Skip script steps that occur in the future |
| pathtoexcel | Required | String | Path to the Excel script |
| startdate | Required | DateTime | Date for the start of the Excel script |
| teamproject | Required | String | Name of the team project |
| processname | Required | String | Process template name |
| createproject | Required | Boolean | Creates the team project if it doesn't exist |
## createfromgenerator
**Create work items using random data generator**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| skipfuturedates | Optional | Boolean | Skip script steps that occur in the future |
| numberofsprints | Required | Int32 | Number of sprints to generate |
| teamproject | Required | String | Name of the team project |
| processname | Required | String | Process template name |
| createproject | Required | Boolean | Creates the team project if it doesn't exist |
| teamcount | Optional | Int32 | Creates data for multiple teams. This option is only available when creating a new project. |
| alldone | Optional | Boolean | All PBIs in a sprint makes it to done |
| addsessiontag | Optional | Boolean | Add a session tag to work items |
| output | Optional | String | Save generated script file to disk in this directory. Note the filename will be auto-generated. |
| scriptonly | Optional | Boolean | Creates the excel export script. Requires an arg value for 'output' |
# Version Control
## creategitrepo
**Creates a Git repository in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the git repositories |
| reponame | Required | String | Name of the new git repository |
## listgitrepos
**Gets list of Git repositories from an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the git repositories |
## tfvc-to-git
**Converts a Team Foundation Version Control (TFVC) folder to a Git repository.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the TFVC and Git repositories |
| reponame | Required | String | Name of the new git repository |
| tfvc-path | Required | String | Source TFVC folder to convert |
# Work Items
## exportworkitemquery
**Export work item query results**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name to delete |
| queryname | Required | String | Work item query name |
| exporttopath | Required | String | Export to path |
## getareas
**Gets a list of areas in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the iterations |
| verbose | Optional | Boolean | Verbose output |
## getfields
**Gets a list of work item fields for a work item type in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item type |
| workitemtypename | Required | String | Name of the work item type |
## getiterations
**Gets a list of iterations in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the iterations |
| verbose | Optional | Boolean | Verbose output |
## getworkitem
**Get work item by id**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| id | Required | Int32 | Work item id |
## getworkitemstates
**Gets the list of states for a work item type in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item type |
| workitemtypename | Required | String | Name of the work item type |
## getworkitemtypes
**Gets a list of work item types in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item types |
## listworkitemqueries
**Gets a list of all work item queries in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item queries |
## runworkitemquery
**Run work item query**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name containing the qork item query to run |
| queryname | Required | String | Work item query name |
## setiteration
**Create iteration including start and end date**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| startdate | Required | DateTime | Iteration start date |
| enddate | Required | DateTime | Iteration end date |
| name | Required | String | Iteration name |
## setworkitemstate
**Set the state value on an existing work item**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| state | Required | String | Work item state value |
| id | Required | Int32 | Work item id for the work item to be updated |
| date | Optional | DateTime | Iteration end date |
| override | Optional | Boolean | Override non-matching state values and force set the value you want |
## showworkitemquery
**Show work item query**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project that contains the work item query |
| queryname | Required | String | Work item query name |
