using System.Web;

using Benday.AzureDevOpsUtil.Api.DataFormatting;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameGetWorkItemFields,
    Description = "Gets a list of work item fields for a work item type in an Azure DevOps Team Project.",
    IsAsync = true)]
public class GetWorkItemFieldsCommand : AzureDevOpsCommandBase
{
    public GetWorkItemFieldsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item type");

        args.AddString(Constants.ArgumentNameWorkItemTypeName).AsRequired().
            WithDescription("Name of the work item type");

        args.AddString(Constants.ArgumentNameFilter).AsNotRequired().
            WithDescription("Case insensitive string filter for the results.").
            WithDefaultValue(string.Empty);

        return args;
    }

    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var workItemTypeName = Arguments.GetStringValue(Constants.ArgumentNameWorkItemTypeName);
        var filter = Arguments.GetStringValue(Constants.ArgumentNameFilter);
        var hasFilter = String.IsNullOrWhiteSpace(filter) == false;

        await GetFieldsForWorkItemType(projectName, workItemTypeName);
        await GetWorkItemFieldsForProject(projectName);

        PopulateDataTypes();

        if (IsQuietMode == false && LastResult != null)
        {
            var formatter = new TableFormatter();

            formatter.AddColumn("Ref Name");
            formatter.AddColumn("Field Name");
            formatter.AddColumn("Data Type");
            formatter.AddColumn("Required");
            formatter.AddColumn("Default Value");

            foreach (var item in LastResult.Fields)
            {
                if (hasFilter == true)
                {
                    formatter.AddDataWithFilter(filter, item.ReferenceName, item.Name, item.DataType, item.AlwaysRequired.ToString(), item.DefaultValue);
                }
                else
                {
                    // no filter
                    formatter.AddData(item.ReferenceName, item.Name, item.DataType, item.AlwaysRequired.ToString(), item.DefaultValue);
                }
            }

            var formatted = formatter.FormatTable();

            WriteLine(formatted);
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
        var workItemTypeNameEscaped = HttpUtility.UrlPathEncode(workItemTypeName);

        var requestUrl = $"{teamProjectName}/_apis/wit/workitemtypes/{workItemTypeNameEscaped}/fields?api-version=7.1&$expand=all";

        var result = await CallEndpointViaGetAndGetResult<WorkItemFieldsResponse>(requestUrl);

        LastResult = result;
    }

    public WorkItemFieldsResponse? LastResult { get; private set; }
    public WorkItemProjectFieldsResponse? AllProjectFields { get; private set; }
}

