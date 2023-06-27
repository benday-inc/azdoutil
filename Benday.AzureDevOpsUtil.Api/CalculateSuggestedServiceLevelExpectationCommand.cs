using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandArgumentNameSuggestServiceLevelExpectation,
        Description = "Calculate a suggested service level expectation (SLE) based on cycle time",
        IsAsync = true)]
public class CalculateSuggestedServiceLevelExpectationCommand : AzureDevOpsCommandBase
{
    public CalculateSuggestedServiceLevelExpectationCommand(
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

        arguments.AddInt32(Constants.ArgumentNamePercent)
            .AsNotRequired()
            .WithDescription("Percentage level to calculate. (For example, 85% of our items complete in X days)")
            .WithDefaultValue(85);

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _NumberOfWeeksOfForecast = Arguments.GetInt32Value(Constants.ArgumentNameForecastNumberOfWeeks);
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _SlePercent = Arguments.GetInt32Value(Constants.ArgumentNamePercent);

        var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandArgumentNameGetCycleTimeAndThroughput, true);

        var getDataCommand = new GetCycleTimeAndThroughputCommand(args, _OutputProvider);

        await getDataCommand.ExecuteAsync();

        if (getDataCommand.Data == null ||
            getDataCommand.Data.Items == null ||
            getDataCommand.Data.Items.Length == 0)
        {
            throw new KnownException("No data");
        }

        _Data = getDataCommand.Data;
        DataItemCount = _Data.Items.Length;

        var suggestedSleCycleTime = GetSuggestedSle();

        CycleTimeAtPercent = suggestedSleCycleTime;

        if (IsQuietMode == false)
        {
            WriteLine($"{_SlePercent}% of items are completed in {suggestedSleCycleTime} days or less.");
        }
    }

    public double CycleTimeAtPercent { get; private set; } = -1;
    public int DataItemCount { get; private set; } = -1;

    private double GetSuggestedSle()
    {
        return GetCycleTimeAtPercent(_SlePercent);
    }

    public double GetCycleTimeAtPercent(int percent)
    {
        if (_Data == null)
        {
            throw new InvalidOperationException("Data is null.");
        }

        int dataItemCount = _Data.Items.Length;

        if (dataItemCount < 10 && IsQuietMode == false)
        {
            WriteLine($"Warning: there are only {dataItemCount} items for the cycle time calculation. " +
                $"Due to percentage rounding, the actual reported SLE may be slightly off.");
        }

        var indexForPercentForecast =
            Utilities.GetIndexForPercentForecast(dataItemCount, percent);

        if (indexForPercentForecast < 0)
        {
            throw new KnownException($"Could not calculate an SLE for {dataItemCount} items and {percent}.");
        }
        else
        {
            var cycleTimes = _Data.Items.OrderBy(x => x.CycleTimeDays)
                .Select(x => x.CycleTimeDays)
                .ToArray();

            return Math.Round(cycleTimes[indexForPercentForecast], 2);
        }
    }

    private int _NumberOfWeeksOfForecast;
    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName = string.Empty;
    private int _SlePercent;
    private CycleTimeDataResponse? _Data;    
}
