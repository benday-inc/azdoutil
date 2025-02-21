using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameGetForecastDurationForItemCount,
        Description = "Use throughput data to forecast likely number of weeks to get given number of items done using Monte Carlo simulation",
        IsAsync = true)]
public class ForecastDurationForItemCountCommand : AzureDevOpsCommandBase
{
    public ForecastDurationForItemCountCommand(
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
        arguments.AddInt32(Constants.ArgumentNameForecastNumberOfItems)
            .AsRequired()
            .WithDescription("Number of items to forecast duration for");

        arguments.AddString(Constants.ArgumentNameTeamName)
            .AsNotRequired()
            .WithDescription("Team name");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        _NumberOfItemsToForecast = Arguments.GetInt32Value(Constants.ArgumentNameForecastNumberOfItems);
        _NumberOfDaysOfHistory = Arguments.GetInt32Value(Constants.ArgumentNameCycleTimeNumberOfDays);
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);

        var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandArgumentNameGetCycleTimeAndThroughput, true);

        var getDataCommand = new GetCycleTimeAndThroughputCommand(args, _OutputProvider);

        await getDataCommand.ExecuteAsync();

        if (getDataCommand.Data == null ||
            getDataCommand.Data.Items == null ||
            getDataCommand.Data.Items.Length == 0)
        {
            throw new KnownException("No data");
        }
        
        DataGroupedByWeek = getDataCommand.GroupedByWeek;

        CreateForecast();
        if (IsQuietMode == false)
        {
            DisplayForecast(getDataCommand);
        }
    }

    public void DisplayForecast(GetCycleTimeAndThroughputCommand getDataCommand)
    {
        var desc = $"How many weeks will it take us to get {_NumberOfItemsToForecast} item(s) done?";

        WriteThroughputByWeek(getDataCommand);

        DisplayForecast(desc);
    }

    private void WriteThroughputByWeek(GetCycleTimeAndThroughputCommand getDataCommand)
    {
        WriteLine(string.Empty);
        WriteLine($"Throughput for the last {getDataCommand.GroupedByWeek.Count} week(s):");
        
        var keysOrderedByAscending = getDataCommand.GroupedByWeek.Keys.OrderBy(x => x);

        foreach (var key in keysOrderedByAscending)
        {
            WriteThroughputForWeek(getDataCommand.GroupedByWeek[key]);
        }

        WriteLine(string.Empty);
    }

    private void WriteThroughputForWeek(ThroughputIteration throughputIteration)
    {
        var longestString = "mm/dd/yyyy".Length;

        string dateString = throughputIteration.StartOfWeek.ToShortDateString();

        // pad string to length of longest date string
        dateString = dateString.PadRight(longestString);

        WriteLine($"\t{dateString}: {throughputIteration.Items.Count}");
    }

    public void DisplayForecast(string forecastDescription)
    {
        WriteLine(forecastDescription);
        WriteLine(string.Empty);

        var distribution = GetDistribution();

        var throughput50PercentChance = GetIterationCount(distribution,
            Constants.ForecastNumberOfSimulationsFiftyPercent);

        var throughput80PercentChance = GetIterationCount(distribution,
            Constants.ForecastNumberOfSimulationsEightyPercent);

        var throughput90PercentChance = GetIterationCount(distribution,
            Constants.ForecastNumberOfSimulationsNinetyPercent);

        var throughput100PercentChance = GetIterationCount(distribution,
            Constants.ForecastNumberOfSimulationsHundredPercent);

        var sortedKeys = distribution.Keys.OrderBy(x => x);

        var maxOccurrences = distribution.Values.Max();

        // WriteLine($"Max occurrences: {maxOccurrences}");
        WriteLine($"50% sure it can be done in {throughput50PercentChance} week(s)");
        WriteLine($"80% sure it can be done in {throughput80PercentChance} week(s)");
        WriteLine($"90% sure it can be done in {throughput90PercentChance} week(s)");
        WriteLine($"~99% sure it can be done in {throughput100PercentChance} week(s)");
        WriteLine(string.Empty);
    }

    private int GetIterationCount(Dictionary<int, int> distribution, 
        int getThroughputAtSimulationCount)
    {
        var sortedKeys = distribution.Keys.OrderBy(x => x);

        int total = 0;

        foreach (var key in sortedKeys)
        {
            var value = distribution[key];

            total+= value;

            if (total >= getThroughputAtSimulationCount)
            {
                return key;
            }
        }

        throw new InvalidOperationException($"Something went wrong. Never found a simulation count >= {getThroughputAtSimulationCount}.");
    }

    private Dictionary<int, int> GetDistribution()
    {
        // key = weeks to complete item count
        // value = number of times this thruput happened

        var distribution = new Dictionary<int, int>();

        foreach (var group in _forecasts)
        {
            int numberOfWeeks = group.Forecasts.Count;

            if (distribution.ContainsKey(numberOfWeeks) == false)
            {
                distribution.Add(numberOfWeeks, 1);
            }
            else
            {
                distribution[numberOfWeeks] += 1;
            }
        }

        return distribution;
    }

    private void CreateForecast()
    {
        using var rnd = new CryptoRandomNumberGenerator();

        var numberOfHistoryWeeks = DataGroupedByWeek.Count;

        var iterationKeys = DataGroupedByWeek.Keys.ToArray();

        int iterationIndex;

        ForecastGroup forecastGroup;

        for (int i = 0; i < Constants.ForecastNumberOfSimulations; i++)
        {
            forecastGroup = new ForecastGroup();

            do
            {
                iterationIndex = rnd.GetNumberInRange(0, numberOfHistoryWeeks - 1);

                var iteration = DataGroupedByWeek[iterationKeys[iterationIndex]];

                forecastGroup.Add(new IterationForecast(
                    iteration.Items.Count));
            } while (forecastGroup.TotalThroughput < _NumberOfItemsToForecast);
            
            _forecasts.Add(forecastGroup);
        }
    }

    private int _NumberOfItemsToForecast;
    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName = string.Empty;

    public Dictionary<DateTime, ThroughputIteration> DataGroupedByWeek { get; private set; } = new();
    private readonly List<ForecastGroup> _forecasts = new();
}
