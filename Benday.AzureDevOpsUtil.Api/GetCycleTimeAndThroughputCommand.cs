﻿using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetCycleTimeAndThroughput,
        Description = "Get cycle time and throughput data for a team project for a date range",
        IsAsync = true)]
public class GetCycleTimeAndThroughputCommand : AzureDevOpsCommandBase
{
    public GetCycleTimeAndThroughputCommand(
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
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var now = DateTime.Now;
        _StartOfRange = now.AddDays(-1 * _NumberOfDaysOfHistory);

        await GetData();

        WriteLine($"Number of days: {(now - _StartOfRange).TotalDays}");


        if (Data == null || Data.Items == null)
        {
            WriteLine("No data.");
        }
        else
        {
            WriteLine($"Number of completed items: {Data.Items.Length}");
        }
    }

    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName;
    private DateTime _StartOfRange;

    private async Task GetData()
    {
        var teamProjectNameUrlEncoded = HttpUtility.UrlEncode(_TeamProjectName);

        var startOfRangeODataFormatted = _StartOfRange.ToString("yyyyMMdd");

        var requestUrl = "https://analytics.dev.azure.com/benday/Metrics%20and%20Dashboards%20Research/_odata/v1.0/WorkItems?" +
            "$select=WorkItemId,Title,CycleTimeDays,CompletedDateSK&" +
            "$filter=" +
            HttpUtility.UrlEncode($"WorkItemType eq 'Product Backlog Item' and State eq 'Done' and CompletedDateSK ge {startOfRangeODataFormatted}");
        
        Data = await CallEndpointViaGetAndGetResult<CycleTimeDataResponse>(requestUrl, false);
    }

    public CycleTimeDataResponse? Data { get; private set; }
}