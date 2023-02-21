﻿using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_ShowWorkItemQuery,
        Description = "Show work item query",
        IsAsync = true)]
public class ShowWorkItemQueryCommand : AzureDevOpsCommandBase
{
    public ShowWorkItemQueryCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project that contains the work item query");

        arguments.AddString(Constants.ArgumentNameWorkItemQueryName)
            .AsRequired()
            .WithDescription("Work item query name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        var workItemQueryName = Arguments.GetStringValue(Constants.ArgumentNameWorkItemQueryName);


        var result = await RunWorkItemQuerySearch(teamProjectName, workItemQueryName);
    }

    private async Task<string> RunWorkItemQuerySearch(
        string teamProjectName, string workItemQueryName)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl = $"{teamProjectName}/_apis/wit/queries?$expand=1&$filter={HttpUtility.UrlEncode(workItemQueryName)}";

        var result = await client.GetStringAsync(requestUrl);

        var resultAsJson = JsonUtilities.GetJsonValueAsType<WorkItemQuerySearchResponse>(result);

        LastResult = resultAsJson;

        return result;
    }

    public WorkItemQuerySearchResponse? LastResult
    {
        get;
        private set;
    }
}