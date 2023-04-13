using System.Linq.Expressions;

using Benday.CommandsFramework;

using OfficeOpenXml.Utils;

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
            .AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameWindowsAuth)
            .WithDescription("Use windows authentication with the current logged in user")
            .AsNotRequired()
            .AllowEmptyValue();

        arguments.AddString(Constants.ArgumentNameCollectionUrl)
            .WithDescription("URL for this collection (example: https://dev.azure.com/accountname)")
            .AsRequired();

        return arguments;
    }

    protected override void OnExecute()
    {
        if (Arguments.HasValue(Constants.ArgumentNameToken) == true &&
            Arguments.HasValue(Constants.ArgumentNameWindowsAuth) == true)
        {
            throw new KnownException($"Cannot set both /{Constants.ArgumentNameToken} and /{Constants.ArgumentNameWindowsAuth}");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameToken) == false &&
            Arguments.HasValue(Constants.ArgumentNameWindowsAuth) == false)
        {
            throw new KnownException($"You must set either /{Constants.ArgumentNameToken} or /{Constants.ArgumentNameWindowsAuth}");
        }

        var configName = Constants.DefaultConfigurationName;

        if (Arguments[Constants.ArgumentNameConfigurationName].HasValue == true)
        {
            configName =
                Arguments[Constants.ArgumentNameConfigurationName].Value;
        }

        var token = string.Empty;

        if (Arguments.HasValue(Constants.ArgumentNameToken) == true)
        {
            token = Arguments.GetStringValue(Constants.ArgumentNameToken);
        }

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = Arguments.GetStringValue(Constants.ArgumentNameCollectionUrl),
            Token = token,
            Name = configName,
            IsWindowsAuth = Arguments.GetBooleanValue(Constants.ArgumentNameWindowsAuth)
        };

        AzureDevOpsConfigurationManager.Instance.Save(config);
    }
}