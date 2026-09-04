using System.Data;
using System.Xml.Linq;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.WorkItems;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandName_SetWorkItemState,
        Description = "Set the state value on an existing work item")]
public class SetWorkItemStateCommand : AzureDevOpsCommandBase
{
    public SetWorkItemStateCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.CommandArg_State).WithDescription("Work item state value");
        arguments.AddInt32(Constants.CommandArg_WorkItemId).WithDescription("Work item id for the work item to be updated");
        arguments.AddDateTime(Constants.CommandArg_StateTransitionDate).AsNotRequired().WithDescription("Iteration end date");
        arguments.AddBoolean(Constants.CommandArgumentNameOverride).AsNotRequired().AllowEmptyValue().WithDescription("Override non-matching state values and force set the value you want");
        return arguments;
    }

    protected override async Task OnExecute(CancellationToken cancellationToken)
    {
        var workItemId = Arguments.GetInt32Value(Constants.CommandArg_WorkItemId);
        var toState = Arguments.GetStringValue(Constants.CommandArg_State);

        DateTime stateTransitionDate = DateTime.Now;

        if (Arguments.HasValue(Constants.CommandArg_StateTransitionDate) == true)
        {
            stateTransitionDate = Arguments.GetDateTimeValue(Constants.CommandArg_StateTransitionDate);
        }

        WriteLine($"Getting work item info for work item id '{workItemId}'...");

        var getWorkItem = await ExecuteAzdoCommandAsync<GetWorkItemByIdCommand>(args => args
            .Set(Constants.CommandArg_WorkItemId, workItemId));

        if (getWorkItem.WorkItem == null)
        {
            throw new KnownException($"Unknown work item '{workItemId}'.");
        }
        else
        {
            var getStates = await ExecuteAzdoCommandAsync<GetWorkItemStatesCommand>(args => args
                .Set(Constants.ArgumentNameTeamProjectName,
                    getWorkItem.WorkItem.FieldsAsStrings["System.TeamProject"])
                .Set(Constants.ArgumentNameWorkItemTypeName,
                    getWorkItem.WorkItem.FieldsAsStrings["System.WorkItemType"]));

            var states = getStates.LastResult;

            if (states != null)
            {
                toState = GetOfficialStateValue(states, toState);
            }

            var workItemInfo = getWorkItem.WorkItem;

            WriteLine($"Updating state for work item id '{workItemId}' to '{toState}' on date '{stateTransitionDate}'...");

            await UpdateState(workItemInfo, toState, stateTransitionDate);
        }
    }

    private string GetOfficialStateValue(GetWorkItemTypeStatesResponse states, string toState)
    {
        var match = states.States.Where(x => x.Name.Equals(toState, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

        if (match != null)
        {
            return match.Name;
        }
        else if (Arguments.GetBooleanValue(Constants.CommandArgumentNameOverride) == true)
        {
            return toState;
        }
        else
        {
            throw new KnownException($"Work item type does not have a state '{toState}'. Use --{Constants.CommandArgumentNameOverride} to force set the value.");
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