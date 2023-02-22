using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetWorkItemFields,
    Description = "Gets a list of work item fields for a work item type in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetWorkItemFieldsCommand : AzureDevOpsCommandBase
{
    public GetWorkItemFieldsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item type");
        args.AddString(Constants.ArgumentNameWorkItemTypeName).AsRequired().
            WithDescription("Name of the work item type");

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var workItemTypeName = Arguments.GetStringValue(Constants.ArgumentNameWorkItemTypeName);

        await GetFieldsForWorkItemType(projectName, workItemTypeName);
        await GetWorkItemFieldsForProject(projectName);

        PopulateDataTypes();

        if (IsQuietMode == false && LastResult != null)
        {
            foreach (var item in LastResult.Fields)
            {
                WriteLine($"{item.ReferenceName}: Required = '{item.AlwaysRequired}', Default Value = '{item.DefaultValue}'");
            }
        }
    }
    private void PopulateDataTypes()
    {
        if (LastResult == null)
        {
            throw new InvalidOperationException($"LastResult is null.");
        }

        foreach (var field in LastResult.Fields)
        {
            field.DataType = GetDataType(field);
        }
    }
    private string GetDataType(WorkItemFieldInfo field)
    {
        if (AllProjectFields == null)
        {
            throw new InvalidOperationException($"AllProjectFields is null.");
        }

        var match = (from temp in AllProjectFields.Fields
                     where temp.ReferenceName == field.ReferenceName
                     select temp).FirstOrDefault();

        if (match == null)
        {
            return String.Empty;
        }
        else
        {
            return match.Type;
        }
    }

    private async Task GetWorkItemFieldsForProject(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/wit/fields?api-version=6.0";

        AllProjectFields = await CallEndpointViaGetAndGetResult<WorkItemProjectFieldsResponse>(requestUrl);
    }

    private async Task GetFieldsForWorkItemType(string teamProjectName, string workItemTypeName)
    {
        var requestUrl = $"{teamProjectName}/_apis/wit/workitemtypes/{workItemTypeName}/fields?api-version=6.0&$expand=all";

        LastResult = await CallEndpointViaGetAndGetResult<WorkItemFieldsResponse>(requestUrl);
    }

    public WorkItemFieldsResponse? LastResult { get; private set; }
    public WorkItemProjectFieldsResponse? AllProjectFields { get; private set; }
}

