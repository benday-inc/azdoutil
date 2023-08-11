using System.Text;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameForecastWorkItemDelivery,
        Description = "Use throughput data to forecast when a work item is likely to be done based on the current backlog priority using Monte Carlo simulation",
        IsAsync = true)]
public class ForecastWorkItemDeliveryCommand : AzureDevOpsCommandBase
{
    public ForecastWorkItemDeliveryCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddInt32(Constants.ArgumentNameCycleTimeNumberOfDays)
            .AsRequired()
            .WithDescription("Number of days of history to compute");
        //arguments.AddString(Constants.ArgumentNameTeamProjectName)
        //    .AsRequired()
        //    .WithDescription("Team project name");
        arguments.AddInt32(Constants.CommandArg_WorkItemId)
            .AsRequired()
            .WithDescription("Id of the work item to forecast");

        arguments.AddString(Constants.ArgumentNameTeamName)
          .AsNotRequired()
          .WithDescription("Team name");

        return arguments;
    }

    private string _TeamProjectName;
    private bool _HasTeamNameQuery;
    private string _TeamName;
    private AreaData? _TeamInfo = null;

    private async Task ValidateTeamName()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        // _odata/v4.0-preview/Areas?$select=AreaName,AreaPath,AreaSk,AreaLevel2&$filter=AreaLevel2 eq 'Team 1'

        var requestUrl = $"{Configuration.AnalyticsUrl}/{teamProjectNameUrlEncoded}/_odata/v1.0/Areas?" +
            "$select=AreaName,AreaPath,AreaSk,AreaLevel2&" +
             $"$filter=AreaLevel2 eq '{_TeamName}'";

        var results = await CallEndpointViaGetAndGetResult<GetAreasFromODataResponse>(requestUrl, false);

        if (results == null || results.Items == null || results.Items.Length == 0)
        {
            throw new KnownException(
                $"Could not find team named '{_TeamName}' in project '{_TeamProjectName}'.");
        }
        else if (results.Items.Length > 1)
        {
            throw new KnownException(
                $"Found more than one team named '{_TeamName}' in project '{_TeamProjectName}'.");
        }
        else
        {
            _TeamInfo = results.Items[0];
        }
    }

    protected override async Task OnExecute()
    {
        var workItemId = Arguments.GetInt32Value(Constants.CommandArg_WorkItemId);

        var workItem = await GetWorkItem(workItemId);

        var teamProjectName = workItem.FieldsAsStrings["System.TeamProject"];

        var teamProject = await GetTeamProject(teamProjectName);

        _TeamProjectName = teamProject.Name;

        _HasTeamNameQuery = Arguments.HasValue(Constants.ArgumentNameTeamName);

        if (_HasTeamNameQuery == true)
        {
            _TeamName = Arguments.GetStringValue(Constants.ArgumentNameTeamName);

            await ValidateTeamName();
        }

        if (_HasTeamNameQuery == true && _TeamInfo == null)
        {
            throw new KnownException(
                $"Could not determine team information fro the supplied team name.");
        }
        else if (teamProject.DefaultTeam == null)
        {
            throw new KnownException(
                $"Could not determine default team for team project '{teamProjectName}'");
        }
        
        int workItemBacklogPosition = await GetWorkItemBacklogPosition(
            workItem, teamProject);

        await GetForecast(teamProject, workItem, workItemBacklogPosition);
    }

    private async Task GetForecast(TeamProjectInfo teamProject,
        GetWorkItemByIdResponse workItem, int position)
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandArgumentNameGetForecastDurationForItemCount,
                        true);

        args.AddArgumentValue(Constants.ArgumentNameTeamProjectName, teamProject.Name);
        args.AddArgumentValue(
            Constants.ArgumentNameForecastNumberOfItems,
            position.ToString());

        var command = new ForecastDurationForItemCountCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.DataGroupedByWeek == null)
        {
            throw new KnownException($"Could not get forecast.");
        }
        else
        {
            var builder = new StringBuilder();

            builder.AppendLine();

            builder.AppendLine(
                $"How many weeks will it take us to get work item #{workItem.Id} \"{workItem.FieldsAsStrings["System.Title"]}\" done?");

            builder.AppendLine($"Item is #{position} in the backlog.");

            command.DisplayForecast(builder.ToString());
        }
    }

    private async Task<int> GetWorkItemBacklogPosition(
        GetWorkItemByIdResponse workItem, TeamProjectInfo teamProject)
    {
        // get the work items from the default team backlog
        string requestUrl;

        if (_HasTeamNameQuery == false)
        {
            requestUrl = $"{teamProject.Id}/{teamProject.DefaultTeam!.Id}/" +
                        $"_apis/work/backlogs/Microsoft.RequirementCategory/workItems";
        }
        else
        {
            requestUrl = $"{teamProject.Id}/{_TeamInfo!.AreaLevel2}/" +
                        $"_apis/work/backlogs/Microsoft.RequirementCategory/workItems";
        }

        var result =
            await CallEndpointViaGetAndGetResult<GetBacklogWorkItemIdsResponse>(
            requestUrl);


        string teamNameMessage = "default team";

        if (_HasTeamNameQuery == true)
        {
            teamNameMessage = $"team '{_TeamName}'";
        }

        if (result == null)
        {
            throw new KnownException($"Error: couldn't get backlog work items on backlog for {teamNameMessage}");
        }
        else if (result.WorkItems.Length == 0)
        {
            throw new KnownException($"Backlog for {teamNameMessage} has zero work items.");
        }
        else
        {
            int position = 0;

            foreach (var item in result.WorkItems)
            {
                position++;

                if (item.Target.Id == workItem.Id)
                {
                    return position;
                }
            }

            throw new KnownException(
                $"Work item isn't part of the backlog for the {teamNameMessage}. Can't determine position of work item in backlog.");
        }
    }

    private async Task<GetWorkItemByIdResponse> GetWorkItem(int workItemId)
    {
        var command = new GetWorkItemByIdCommand(
            ExecutionInfo.GetCloneOfArguments(
                Constants.CommandName_GetWorkItemById,
                true), _OutputProvider);

        await command.ExecuteAsync();

        if (command.WorkItem == null)
        {
            throw new KnownException($"Could not locate work item #{workItemId}.");
        }
        else if (command.WorkItem.FieldsAsStrings.ContainsKey("System.TeamProject") == false)
        {
            throw new KnownException($"Could not locate team project name for work item #{workItemId}.");
        }
        else if (command.WorkItem.FieldsAsStrings.ContainsKey("System.WorkItemType") == false)
        {
            throw new KnownException($"Could not locate work item type for #{workItemId}.");
        }
        else
        {
            var workItemTypeName = command.WorkItem.FieldsAsStrings["System.WorkItemType"];

            if (workItemTypeName != "Product Backlog Item" &&
                workItemTypeName != "User Story" &&
                workItemTypeName != "Bug")
            {
                throw new KnownException($"Work item type for #{workItemId} '{workItemTypeName}' is not supported.");
            }
        }

        return command.WorkItem;
    }

    private async Task<TeamProjectInfo> GetTeamProject(string teamProjectName)
    {
        var args = ExecutionInfo.GetCloneOfArguments(
                        Constants.CommandName_GetProject,
                        true);

        args.AddArgumentValue(Constants.ArgumentNameTeamProjectName, teamProjectName);

        var command = new GetTeamProjectCommand(
            args, _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException($"Could not team project '{teamProjectName}' for work item.");
        }

        return command.LastResult;
    }
}
