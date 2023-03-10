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

    public override ArgumentCollection GetArguments()
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
        var configName = Constants.DefaultConfigurationName;

        if (Arguments[Constants.ArgumentNameConfigurationName].HasValue == true)
        {
            configName =
                Arguments[Constants.ArgumentNameConfigurationName].Value;
        }

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = Arguments[Constants.ArgumentNameCollectionUrl].Value,
            Token = Arguments[Constants.ArgumentNameToken].Value,
            Name = configName
        };

        AzureDevOpsConfigurationManager.Instance.Save(config);
    }
}
