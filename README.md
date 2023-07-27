# azdoutil
A collection of useful Azure DevOps utilities.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
info@benday.com 

*Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/azdoutil/issues*. *Want to contribute? Submit a pull request.*

## Command Categories GITHUB

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
| AzdoUtil Configuration | [addconfig](#addconfig) | Add or update an Azure DevOps configuration. For example, which server or account plus auth information. |
| AzdoUtil Configuration | [listconfig](#listconfig) | List an Azure DevOps configuration. For example, which server or account plus auth information. |
| AzdoUtil Configuration | [removeconfig](#removeconfig) | Remove an Azure DevOps configuration. For example, which server or account plus auth information. |
| Builds | [exportbuilddef](#exportbuilddef) | Export build definition |
| Builds | [listbuilddefs](#listbuilddefs) | List build definitions |
| Flow Metrics | [agingwork](#agingwork) | Get aging in-progress work items |
| Flow Metrics | [cycletimeconfidence](#cycletimeconfidence) | Get item cycle time for 50% and 85% levels. This helps you understand how items typically are delivered. |
| Flow Metrics | [forecastdurationforitemcount](#forecastdurationforitemcount) | Use throughput data to forecast likely number of weeks to get given number of items done using Monte Carlo simulation |
| Flow Metrics | [forecastitemsinweeks](#forecastitemsinweeks) | Use throughput data to forecast likely number of items done in given number of weeks using Monte Carlo simulation |
| Flow Metrics | [forecastworkitem](#forecastworkitem) | Use throughput data to forecast when a work item is likely to be done based on the current backlog priority using Monte Carlo simulation |
| Flow Metrics | [suggest-sle](#suggest-sle) | Calculate a suggested service level expectation (SLE) based on cycle time |
| Flow Metrics | [throughputcycletime](#throughputcycletime) | Get cycle time and throughput data for a team project for a date range |
| Miscellaneous | [connectiondata](#connectiondata) | Get information about a connection to Azure DevOps. |
| Process Templates | [addrefinementprocess](#addrefinementprocess) | Creates backlog refinement process template as described at https://www.benday.com/2022/09/29/streamlining-backlog-refinement-with-azure-devops/ |
| Process Templates | [changeprocess](#changeprocess) | Change the process for a Team Project |
| Project Administration | [createproject](#createproject) | Create team projects |
| Project Administration | [createteam](#createteam) | Creates a new team in an Azure DevOps Team Project. |
| Project Administration | [deleteproject](#deleteproject) | Delete team project |
| Project Administration | [getproject](#getproject) | Get team project info |
| Project Administration | [listprocesstemplates](#listprocesstemplates) | List process templates |
| Project Administration | [listprojects](#listprojects) | List team projects |
| Project Administration | [listteams](#listteams) | Gets list of teams in an Azure DevOps Team Project. |
| Test Data | [createfromexcel](#createfromexcel) | Create work items using Excel script |
| Test Data | [createfromgenerator](#createfromgenerator) | Create work items using random data generator |
| Version Control | [creategitrepo](#creategitrepo) | Creates a Git repository in an Azure DevOps Team Project. |
| Version Control | [listgitrepos](#listgitrepos) | Gets list of Git repositories from an Azure DevOps Team Project. |
| Version Control | [tfvc-to-git](#tfvc-to-git) | Converts a Team Foundation Version Control (TFVC) folder to a Git repository. |
| Work Items | [exportworkitemquery](#exportworkitemquery) | Export work item query results |
| Work Items | [getareas](#getareas) | Gets a list of areas in an Azure DevOps Team Project. |
| Work Items | [getfields](#getfields) | Gets a list of work item fields for a work item type in an Azure DevOps Team Project. |
| Work Items | [getiterations](#getiterations) | Gets a list of iterations in an Azure DevOps Team Project. |
| Work Items | [getworkitem](#getworkitem) | Get work item by id |
| Work Items | [getworkitemstates](#getworkitemstates) | Gets the list of states for a work item type in an Azure DevOps Team Project. |
| Work Items | [getworkitemtypes](#getworkitemtypes) | Gets a list of work item types in an Azure DevOps Team Project. |
| Work Items | [listworkitemqueries](#listworkitemqueries) | Gets a list of all work item queries in an Azure DevOps Team Project. |
| Work Items | [runworkitemquery](#runworkitemquery) | Run work item query |
| Work Items | [setiteration](#setiteration) | Create iteration including start and end date |
| Work Items | [setworkitemstate](#setworkitemstate) | Set the state value on an existing work item |
| Work Items | [showworkitemquery](#showworkitemquery) | Show work item query |
# AzdoUtil Configuration
## <a name="addconfig"></a> addconfig
**Add or update an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Optional | String | Name of the configuration |
| pat | Optional | String | PAT for this collection |
| windowsauth | Optional | Boolean | Use windows authentication with the current logged in user |
| url | Required | String | URL for this collection (example: https://dev.azure.com/accountname) |
## <a name="listconfig"></a> listconfig
**List an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Optional | String | Name of the configuration |
## <a name="removeconfig"></a> removeconfig
**Remove an Azure DevOps configuration. For example, which server or account plus auth information.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| config | Required | String | Name of the configuration |
# Builds
## <a name="exportbuilddef"></a> exportbuilddef
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
## <a name="listbuilddefs"></a> listbuilddefs
**List build definitions**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| teamproject | Required | String | Team project name |
| nameonly | Optional | Boolean | Only display the build definition name |
| xaml | Optional | Boolean | List XAML build definitions |
# Flow Metrics
## <a name="agingwork"></a> agingwork
**Get aging in-progress work items**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
## <a name="cycletimeconfidence"></a> cycletimeconfidence
**Get item cycle time for 50% and 85% levels. This helps you understand how items typically are delivered.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| teamproject | Required | String | Team project name |
| teamname | Optional | String | Team name |
## <a name="forecastdurationforitemcount"></a> forecastdurationforitemcount
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
## <a name="forecastitemsinweeks"></a> forecastitemsinweeks
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
## <a name="forecastworkitem"></a> forecastworkitem
**Use throughput data to forecast when a work item is likely to be done based on the current backlog priority using Monte Carlo simulation**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| numberofdays | Required | Int32 | Number of days of history to compute |
| id | Required | Int32 | Id of the work item to forecast |
| teamname | Optional | String | Team name |
## <a name="suggest-sle"></a> suggest-sle
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
## <a name="throughputcycletime"></a> throughputcycletime
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
## <a name="connectiondata"></a> connectiondata
**Get information about a connection to Azure DevOps.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
# Process Templates
## <a name="addrefinementprocess"></a> addrefinementprocess
**Creates backlog refinement process template as described at https://www.benday.com/2022/09/29/streamlining-backlog-refinement-with-azure-devops/**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## <a name="changeprocess"></a> changeprocess
**Change the process for a Team Project**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| processname | Required | String | New process name |
# Project Administration
## <a name="createproject"></a> createproject
**Create team projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| processname | Required | String | Process template name |
## <a name="createteam"></a> createteam
**Creates a new team in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the team |
| teamname | Required | String | Name of the new team |
| description | Optional | String | Description for the new team |
## <a name="deleteproject"></a> deleteproject
**Delete team project**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name to delete |
| confirm | Optional | Boolean | Confirm delete |
## <a name="getproject"></a> getproject
**Get team project info**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
## <a name="listprocesstemplates"></a> listprocesstemplates
**List process templates**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## <a name="listprojects"></a> listprojects
**List team projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
## <a name="listteams"></a> listteams
**Gets list of teams in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the teams |
# Test Data
## <a name="createfromexcel"></a> createfromexcel
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
## <a name="createfromgenerator"></a> createfromgenerator
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
## <a name="creategitrepo"></a> creategitrepo
**Creates a Git repository in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the git repositories |
| reponame | Required | String | Name of the new git repository |
## <a name="listgitrepos"></a> listgitrepos
**Gets list of Git repositories from an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the git repositories |
## <a name="tfvc-to-git"></a> tfvc-to-git
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
## <a name="exportworkitemquery"></a> exportworkitemquery
**Export work item query results**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name to delete |
| queryname | Required | String | Work item query name |
| exporttopath | Required | String | Export to path |
## <a name="getareas"></a> getareas
**Gets a list of areas in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the iterations |
| verbose | Optional | Boolean | Verbose output |
## <a name="getfields"></a> getfields
**Gets a list of work item fields for a work item type in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item type |
| workitemtypename | Required | String | Name of the work item type |
## <a name="getiterations"></a> getiterations
**Gets a list of iterations in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the iterations |
| verbose | Optional | Boolean | Verbose output |
## <a name="getworkitem"></a> getworkitem
**Get work item by id**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| id | Required | Int32 | Work item id |
## <a name="getworkitemstates"></a> getworkitemstates
**Gets the list of states for a work item type in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item type |
| workitemtypename | Required | String | Name of the work item type |
## <a name="getworkitemtypes"></a> getworkitemtypes
**Gets a list of work item types in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item types |
## <a name="listworkitemqueries"></a> listworkitemqueries
**Gets a list of all work item queries in an Azure DevOps Team Project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name that contains the work item queries |
## <a name="runworkitemquery"></a> runworkitemquery
**Run work item query**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name containing the qork item query to run |
| queryname | Required | String | Work item query name |
## <a name="setiteration"></a> setiteration
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
## <a name="setworkitemstate"></a> setworkitemstate
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
## <a name="showworkitemquery"></a> showworkitemquery
**Show work item query**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project that contains the work item query |
| queryname | Required | String | Work item query name |
