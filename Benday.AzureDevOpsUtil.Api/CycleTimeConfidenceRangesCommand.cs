using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameCycleTimeConfidenceRangesCommand,
        Description = "Get item cycle time for 50% and 85% levels. This helps you understand how items typically are delivered.",
        IsAsync = true)]
public class CycleTimeConfidenceRangesCommand : AzureDevOpsCommandBase
{
    public CycleTimeConfidenceRangesCommand(
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

        arguments.AddString(Constants.ArgumentNameTeamName)
            .AsNotRequired()
            .WithDescription("Team name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _NumberOfWeeksOfForecast = Arguments.GetInt32Value(Constants.ArgumentNameForecastNumberOfWeeks);
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandArgumentNameSuggestServiceLevelExpectation, true);

        args.AddArgumentValue(Constants.ArgumentNamePercent, "85");

        var command = new CalculateSuggestedServiceLevelExpectationCommand(args, _OutputProvider);

        await command.ExecuteAsync();

        var cycleTimeAt85Percent = command.CycleTimeAtPercent;
        var cycleTimeAt50Percent = command.GetCycleTimeAtPercent(50);

        if (IsQuietMode == false)
        {
            if (command.DataItemCount < 10 && IsQuietMode == false)
            {
                WriteLine($"Warning: there are only {command.DataItemCount} items for the cycle time calculation. " +
                    $"Due to percentage rounding, the actual reported SLE may be slightly off.");
            }

            WriteLine($"50% of items are completed in {cycleTimeAt50Percent} days or less.");
            WriteLine($"85% of items are completed in {cycleTimeAt85Percent} days or less.");
        }
    }


    private int _NumberOfWeeksOfForecast;
    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName = string.Empty;        
}
