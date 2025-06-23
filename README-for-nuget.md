# azdoutil
A collection of useful Azure DevOps utilities.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
https://www.slidespeaker.ai  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

*Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/azdoutil/issues*. *Want to contribute? Submit a pull request.*

## Command Categories

* **Azure DevOps Utility Configuration** - 
Commands for setting up this tool and connecting to Azure DevOps

* **Automated Builds** - 
Commands that help with automated builds and automated releases

* **Flow Metrics** - 
Tools for forecasting project management details using Flow Metrics such as throughput and cycle time. 

_Want to learn more about how to use Flow Metrics to run your projects? Check out this course:   
**[Predicting the Future, Estimating, and Running Your Projects with Flow Metrics](https://www.youtube.com/playlist?list=PLGxFXI4dC2sh6yEbibjMCWHECEVB7lRFi)**._

* **Process Templates** - 
Process template customization and administration utilities

* **Team Project Administration** - 
Tools for creating, editing, and managing Team Projects in Azure DevOps

* **Test Data** - 
Utilities for populating Azure DevOps Team Projects with test data

* **Version Control** - 
Tools for creating, converting, managing version control repositories

* **Work Items** - 
Utilities for editing work items and working with work item queries (WIQL)

* **Miscellaneous** - 
Miscellaneous commands

## Installing
The azdoutil is distributed as a .NET Core Tool via NuGet. To install it go to the command prompt and type  
`dotnet tool install azdoutil -g`

### Prerequisites
- You'll need to install .NET Core 8+ from https://dotnet.microsoft.com/

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
| Builds | exportreleasedef | Export release definition |
| Builds | listagentpools | List agent pools |
| Builds | listbuilddefs | List build definitions |
| Builds | listqueues | List build queues in a team project or team projects |
| Builds | listreleasedefs | List release definitions |
| Builds | repairbuilddefagentpool | Repairs the agent pool setting for the build definitions in a team project or team projects. This is helpful after an on-prem to cloud migration. |
| Builds | repairreleasedefagentpool | Repairs the agent pool setting for the release definitions in a team project or team projects. This is helpful after an on-prem to cloud migration. |
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
| Test Data | createrandomtitles | Create fake work item titles using random data generator without creating any work items. |
| Version Control | creategitrepo | Creates a Git repository in an Azure DevOps Team Project. |
| Version Control | listgitrepos | Gets list of Git repositories from an Azure DevOps Team Project. |
| Version Control | tfvc-to-git | Converts a Team Foundation Version Control (TFVC) folder to a Git repository. |
| Work Items | comparewitdfields | Compare work item fields between two work item type definition files. |
| Work Items | copycategory | Copy category type from one category file to another. |
| Work Items | copywitdfield | Copy work item field from one work item type definition to another. |
| Work Items | exportprocesstemplate | Exports the process template configuration for one or more projects. This command only works on Windows and requires witadmin.exe to be installed. |
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
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| name | Required | String | Build definition name |
| xaml | Optional | Boolean | List XAML build definitions |
| showlastruninfo | Optional | Boolean | Show last build run info |
| csv | Optional | Boolean | Output results in CSV format |
| csv-noheader | Optional | Boolean | Do not print the CSV column header info |
| raw | Optional | Boolean | Output raw build definition |
## exportreleasedef
**Export release definition**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Required | String | Team project name |
| name | Required | String | Release definition name |
| queueinfo | Optional | Boolean | Only display queue info |
| json | Optional | Boolean | Export to JSON |
## listagentpools
**List agent pools**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| agents | Optional | Boolean | Get agents in each pool |
| json | Optional | Boolean | Output as JSON |
## listbuilddefs
**List build definitions**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name |
| all | Optional | Boolean | All builds in all projects in this collection |
| nameonly | Optional | Boolean | Only display the build definition name |
| xaml | Optional | Boolean | List XAML build definitions |
| json | Optional | Boolean | Export to JSON |
## listqueues
**List build queues in a team project or team projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name |
| all | Optional | Boolean | All builds in all projects in this collection |
| json | Optional | Boolean | Output as JSON |
## listreleasedefs
**List release definitions**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name |
| all | Optional | Boolean | All releases in all projects in this collection |
| json | Optional | Boolean | Export to JSON |
| queueinfo | Optional | Boolean | Only display queue info |
## repairbuilddefagentpool
**Repairs the agent pool setting for the build definitions in a team project or team projects. This is helpful after an on-prem to cloud migration.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name |
| all | Optional | Boolean | All builds in all projects in this collection |
| PrintJsonOnPreview | Optional | Boolean | Print modified json in preview mode |
| preview | Optional | Boolean | Preview only. Do not update build definitions. |
| buildname | Optional | String | Build definition name filter. This will only apply if the build name contains this value and only if /all is not specified. |
| originalbuildinfofile | Required | String | Build def JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name. |
## repairreleasedefagentpool
**Repairs the agent pool setting for the release definitions in a team project or team projects. This is helpful after an on-prem to cloud migration.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name |
| all | Optional | Boolean | All releases in all projects in this collection |
| PrintJsonOnPreview | Optional | Boolean | Print modified json in preview mode |
| preview | Optional | Boolean | Preview only. Do not update release definitions. |
| originalreleaseinfofile | Required | String | Release def agent pool references JSON file from on-prem server. Assumes that pools have been recreated in the cloud using the same name. |
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
## createrandomtitles
**Create fake work item titles using random data generator without creating any work items.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
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
## comparewitdfields
**Compare work item fields between two work item type definition files.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| file1 | Required | String | Path to the source work item type definition file. |
| file2 | Required | String | Path to the source work item type definition file. |
| flip | Optional | Boolean | Reverse the source and target files. |
## copycategory
**Copy category type from one category file to another.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| file1 | Required | String | Path to the source category definition file. |
| file2 | Required | String | Path to the target category definition file. |
| refname | Required | String | Refname of the category to copy. |
| overwrite | Required | Boolean | Overwrite the target field if it already exists. |
## copywitdfield
**Copy work item field from one work item type definition to another.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| file1 | Required | String | Path to the source work item type definition file. |
| file2 | Required | String | Path to the target work item type definition file. |
| refname | Required | String | Refname of the field to copy. |
| overwrite | Required | Boolean | Overwrite the target field if it already exists. |
## exportprocesstemplate
**Exports the process template configuration for one or more projects. This command only works on Windows and requires witadmin.exe to be installed.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| quiet | Optional | Boolean | Quiet mode |
| config | Optional | String | Configuration name to use |
| teamproject | Optional | String | Team project name to export. |
| all | Optional | Boolean | Export all projects in the organization or team project collection. |
| exporttopath | Optional | String | Path to export the process template to.  If not specified, the current directory is used. |
| witadminpath | Optional | String | Specify path to witadmin.exe if it can't be located automatically. |
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
| filter | Optional | String | Case insensitive string filter for the results. |
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
| nameonly | Optional | Boolean | Only show the name of the work item types in the results. |
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
