# azdoutil
A collection of useful Azure DevOps utilities.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
info@benday.com 

*Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/azdoutil/issues*. *Want to contribute? Submit a pull request.*

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
