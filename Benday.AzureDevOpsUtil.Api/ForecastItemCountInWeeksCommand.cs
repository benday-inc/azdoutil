using System.Globalization;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_FlowMetrics,
    Name = Constants.CommandArgumentNameGetForecastItemCountInWeeks,
        Description = "Use throughput data to forecast likely number of items done in given number of weeks using Monte Carlo simulation",
        IsAsync = true)]
public class ForecastItemCountInWeeksCommand : AzureDevOpsCommandBase
{
    public ForecastItemCountInWeeksCommand(
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
        arguments.AddInt32(Constants.ArgumentNameForecastNumberOfWeeks)
            .AsRequired()
            .WithDescription("Number of weeks into the future to forecast");

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
        DisplayForecast();
    }

    private void DisplayForecast()
    {        
        var distribution = GetDistribution();

        WriteLine(string.Empty);
        WriteLine($"How many items will we likely get done in {_NumberOfWeeksOfForecast} week(s)?");
        WriteLine(string.Empty);

        var throughput50PercentChance = GetThroughput(distribution, 
            Constants.ForecastNumberOfSimulationsFiftyPercent);

        var throughput80PercentChance = GetThroughput(distribution,
            Constants.ForecastNumberOfSimulationsEightyPercent);

        var throughput90PercentChance = GetThroughput(distribution,
            Constants.ForecastNumberOfSimulationsNinetyPercent);

        var throughput100PercentChance = GetThroughput(distribution,
            Constants.ForecastNumberOfSimulationsHundredPercent);

        var sortedKeys = distribution.Keys.OrderBy(x => x);

        var maxOccurrences = distribution.Values.Max();

        WriteLine($"50% sure {throughput50PercentChance} item(s) can be done");
        WriteLine($"80% sure {throughput80PercentChance} item(s) can be done");
        WriteLine($"90% sure {throughput90PercentChance} item(s) can be done");
        WriteLine($"~99% sure {throughput100PercentChance} item(s) can be done");

        WriteLine(string.Empty);
    }

    private int GetThroughput(Dictionary<int, int> distribution, 
        int getThroughputAtSimulationCount)
    {
        var sortedKeys = distribution.Keys.OrderByDescending(x => x);

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
        // key = throughput
        // value = number of times this thruput happened

        var distribution = new Dictionary<int, int>();

        foreach (var group in _forecasts)
        {
            int throughput = group.TotalThroughput;

            if (distribution.ContainsKey(throughput) == false)
            {
                distribution.Add(throughput, 1);
            }
            else
            {
                distribution[throughput] += 1;
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

            for (int x = 0; x < _NumberOfWeeksOfForecast; x++)
            {
                iterationIndex = rnd.GetNumberInRange(0, numberOfHistoryWeeks - 1);

                var iteration = DataGroupedByWeek[iterationKeys[iterationIndex]];

                forecastGroup.Add(new IterationForecast(
                    iteration.Items.Count));                
            }

            _forecasts.Add(forecastGroup);
        }
    }

    private int _NumberOfWeeksOfForecast;
    private int _NumberOfDaysOfHistory;
    private string _TeamProjectName = string.Empty;

    public Dictionary<DateTime, ThroughputIteration> DataGroupedByWeek { get; private set; } = new();
    private readonly List<ForecastGroup> _forecasts = new();
}
