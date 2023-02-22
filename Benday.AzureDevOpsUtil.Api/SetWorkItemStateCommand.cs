using System.Data;
using System.Xml.Linq;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_SetWorkItemState,
        Description = "Set the state value on an existing work item",
        IsAsync = true)]
public class SetWorkItemStateCommand : AzureDevOpsCommandBase
{
    public SetWorkItemStateCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.CommandArg_State).WithDescription("Work item state value");
        arguments.AddInt32(Constants.CommandArg_WorkItemId).WithDescription("Work item id for the work item to be updated");
        arguments.AddDateTime(Constants.CommandArg_StateTransitionDate).AsNotRequired().WithDescription("Iteration end date");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var workItemId = Arguments.GetInt32Value(Constants.CommandArg_WorkItemId);
        var toState = Arguments.GetStringValue(Constants.CommandArg_State);

        DateTime stateTransitionDate = DateTime.Now;

        if (Arguments[Constants.CommandArg_StateTransitionDate].HasValue == true)
        {
            stateTransitionDate = Arguments.GetDateTimeValue(Constants.CommandArg_StateTransitionDate);
        }

        WriteLine($"Getting work item info for work item id '{workItemId}'...");

        var getWorkItemArgs = ExecutionInfo.GetCloneOfArguments(
            Constants.CommandName_GetWorkItemById, true);
        var getWorkItem = new GetWorkItemByIdCommand(getWorkItemArgs, _OutputProvider);

        await getWorkItem.ExecuteAsync();

        if (getWorkItem.WorkItem == null)
        {
            throw new KnownException($"Unknown work item '{workItemId}'.");
        }
        else
        {
            var workItemInfo = getWorkItem.WorkItem;

            WriteLine($"Updating state for work item id '{workItemId}' to '{toState}' on date '{stateTransitionDate}'...");

            await UpdateState(workItemInfo, toState, stateTransitionDate);
        }
    }
    

    private async Task UpdateState(GetWorkItemByIdResponse item, string stateValue, DateTime stateTransitionDate)
    {
        var teamProjectName = item.FieldsAsStrings["System.TeamProject"];

        var body = new WorkItemFieldOperationValueCollection();

        body.AddValue("System.State", stateValue);
        body.AddValue("System.ChangedDate", stateTransitionDate.ToString());

        if (string.Equals("Done", stateValue, StringComparison.CurrentCultureIgnoreCase) == true)
        {
            body.AddValue("Microsoft.VSTS.Common.ClosedDate", stateTransitionDate.ToString());
        }

        var requestUrl = $"{teamProjectName}/_apis/wit/workitems/{item.Id}?api-version=6.0&bypassRules=true";

        WriteLine($"Setting work item id '{item.Id}' state from '{item.FieldsAsStrings["System.State"]}' to '{stateValue}' in {teamProjectName}");

        await SendPatchForBodyAndGetTypedResponse<ModifyWorkItemResponse>(
            requestUrl, body);
    }
}