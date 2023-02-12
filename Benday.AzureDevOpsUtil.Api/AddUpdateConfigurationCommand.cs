using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameAddUpdateConfig,
        Description = "Add or update an Azure DevOps configuration. For example, which server or account plus auth information.",
        IsAsync = false)]
public class AddUpdateConfigurationCommand : SynchronousCommand
{
    public AddUpdateConfigurationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .WithDescription("Name of the configuration")
            .AsNotRequired();
        arguments.AddString(Constants.ArgumentNameToken)
            .WithDescription("PAT for this collection")
            .AsRequired();
        arguments.AddString(Constants.ArgumentNameCollectionUrl)
            .WithDescription("URL for this collection (example: https://dev.azure.com/accountname)")
            .AsRequired();

        return arguments;
    }

    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}
