using System.Xml.Linq;

using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.Api.Commands.WorkItems;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameGetIterations,
    Description = "Gets a list of iterations in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetIterationsCommand : GetClassificationNodesCommandBase
{
    public GetIterationsCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the iterations");
        args.AddBoolean(Constants.ArgumentNameVerbose).AsNotRequired().AllowEmptyValue().
            WithDescription("Verbose output");

        return args;
    }

    protected override async Task OnExecute()
    {
        var verbose = Arguments.GetBooleanValue(Constants.ArgumentNameVerbose);
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        await GetNodes(teamProjectName, "iteration", verbose);
    }
}

