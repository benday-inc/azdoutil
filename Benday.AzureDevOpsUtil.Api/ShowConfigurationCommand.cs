using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameShowConfig,
        Description = "Show the Azure DevOps configurations. For example, which server or account plus auth information.",
        IsAsync = false)]
public class ShowConfigurationCommand : SynchronousCommand
{
    public ShowConfigurationCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}
