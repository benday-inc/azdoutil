using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetAreas,
    Description = "Gets a list of areas in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetAreasCommand : GetClassificationNodesCommandBase
{
    public GetAreasCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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

        await GetNodes(teamProjectName, "area", verbose);
    }
}

