using System.Linq.Expressions;

using Benday.AzureDevOpsUtil.Api.ApiVersioning;
using Benday.CommandsFramework;

using OfficeOpenXml.Utils;

namespace Benday.AzureDevOpsUtil.Api.Commands.Configuration;

[Command(
    Category = Constants.Category_AzdoUtilConfig,
    Name = Constants.CommandArgumentNameAddUpdateConfig,
    Description = "Add or update an Azure DevOps configuration. For example, which server or account plus auth information.")]
public class AddUpdateConfigurationCommand : Command
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

        arguments.AddString(Constants.ArgumentNameMaxApiVersion)
            .WithDescription(
                "Highest REST api-version to use with this collection (example: 5.0). " +
                "Only needed for an older server that will not answer the automatic check.")
            .AsNotRequired();

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        if (Arguments.HasValue(Constants.ArgumentNameToken) == true &&
            Arguments.HasValue(Constants.ArgumentNameWindowsAuth) == true)
        {
            throw new KnownException($"Cannot set both --{Constants.ArgumentNameToken} and --{Constants.ArgumentNameWindowsAuth}");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameToken) == false &&
            Arguments.HasValue(Constants.ArgumentNameWindowsAuth) == false)
        {
            throw new KnownException($"You must set either --{Constants.ArgumentNameToken} or --{Constants.ArgumentNameWindowsAuth}");
        }

        var configName = Constants.DefaultConfigurationName;

        if (Arguments.HasValue(Constants.ArgumentNameConfigurationName) == true)
        {
            configName =
                Arguments.GetStringValue(Constants.ArgumentNameConfigurationName);
        }

        var token = string.Empty;

        if (Arguments.HasValue(Constants.ArgumentNameToken) == true)
        {
            token = Arguments.GetStringValue(Constants.ArgumentNameToken);
        }

        var maxApiVersion = string.Empty;

        if (Arguments.HasValue(Constants.ArgumentNameMaxApiVersion) == true)
        {
            maxApiVersion = Arguments.GetStringValue(Constants.ArgumentNameMaxApiVersion);

            if (ApiVersion.TryParse(maxApiVersion, out _) == false)
            {
                throw new KnownException(
                    $"'{maxApiVersion}' is not an api-version. Expected something like 5.0 or 5.0-preview.1.");
            }
        }

        var config = new AzureDevOpsConfiguration()
        {
            CollectionUrl = Arguments.GetStringValue(Constants.ArgumentNameCollectionUrl),
            Token = token,
            Name = configName,
            IsWindowsAuth = Arguments.GetBooleanValue(Constants.ArgumentNameWindowsAuth),
            MaxApiVersion = maxApiVersion
        };

        AzureDevOpsConfigurationManager.Instance.Save(config);

        return Task.CompletedTask;
    }
}