using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetWorkItemTypes,
    Description = "Gets a list of work item types in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetWorkItemTypesCommand : AzureDevOpsCommandBase
{
    public GetWorkItemTypesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item types");

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        await RunQuery(projectName);

        if (IsQuietMode == false && AllWorkItemTypes != null)
        {
            foreach (var item in AllWorkItemTypes.Types)
            {
                WriteLine(string.Empty);

                WriteLine($"Name: {item.Name}");
                WriteLine($"ReferenceName: {item.ReferenceName}");
                WriteLine($"Description: {item.Description}");
            }
        }
    }

    private async Task RunQuery(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/wit/workitemtypes?api-version=6.0";

        AllWorkItemTypes = await CallEndpointViaGetAndGetResult<WorkItemTypeDefinitionListResponse>(requestUrl);

    }

    public WorkItemTypeDefinitionListResponse? AllWorkItemTypes { get; private set; }
}


