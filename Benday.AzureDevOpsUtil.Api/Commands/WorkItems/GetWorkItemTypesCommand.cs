using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.WorkItems;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameGetWorkItemTypes,
    Description = "Gets a list of work item types in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetWorkItemTypesCommand : AzureDevOpsCommandBase
{
    public GetWorkItemTypesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item types");

        args.AddBoolean(Constants.ArgumentNameNameOnly).
            AsNotRequired().
            WithDefaultValue(false).
            AllowEmptyValue().
            WithDescription("Only show the name of the work item types in the results.");

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var nameOnly = Arguments.GetBooleanValue(Constants.ArgumentNameNameOnly);

        await RunQuery(projectName);

        if (IsQuietMode == false && AllWorkItemTypes != null)
        {
            foreach (var item in AllWorkItemTypes.Types)
            {
                if (nameOnly == false)
                {
                    WriteLine(string.Empty);

                    WriteLine($"Name: {item.Name}");
                    WriteLine($"ReferenceName: {item.ReferenceName}");
                    WriteLine($"Description: {item.Description}");
                }
                else
                {
                    WriteLine(item.Name);
                }
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


