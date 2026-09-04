using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Configuration;

[Command(
    Category = Constants.Category_AzdoUtilConfig,
    Name = Constants.CommandArgumentNameRemoveConfig,
        Description = "Remove an Azure DevOps configuration. For example, which server or account plus auth information.")]
public class RemoveConfigurationCommand : Command
{
    public RemoveConfigurationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .WithDescription("Name of the configuration")
            .AsRequired();

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        AzureDevOpsConfigurationManager.Instance.Remove(Arguments.GetStringValue(Constants.ArgumentNameConfigurationName));

        return Task.CompletedTask;
    }
}
