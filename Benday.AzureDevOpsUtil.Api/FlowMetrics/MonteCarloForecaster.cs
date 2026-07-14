using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Console-free Monte Carlo forecasting over a team's weekly throughput history.
/// This is the single source of truth for both the CLI commands
/// (forecastdurationforitemcount / forecastitemsinweeks) and the MCP tools.
/// </summary>
public static class MonteCarloForecaster
{
    /// <summary>
    /// Simulates how many weeks it is likely to take to complete
    /// <paramref name="itemCount"/> items by repeatedly sampling weeks of
    /// historical throughput until the target item count is reached.
    /// </summary>
    /// <param name="weeklyThroughputs">Completed-item counts, one entry per week of history. Each entry must be &gt;= 1.</param>
    /// <param name="itemCount">Number of items to forecast a duration for.</param>
    /// <param name="numberOfSimulations">Number of simulation runs.</param>
    /// <param name="sampler">
    /// Optional index sampler invoked as sampler(min, max). Defaults to a
    /// cryptographic RNG. Injectable so tests can be deterministic.
    /// </param>
    public static WeeksForItemCountDistribution SimulateWeeksForItemCount(
        IReadOnlyList<int> weeklyThroughputs,
        int itemCount,
        int numberOfSimulations = Constants.ForecastNumberOfSimulations,
        Func<int, int, int>? sampler = null)
    {
        AssertHistory(weeklyThroughputs);

        if (itemCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount,
                "Value must be greater than or equal to one.");
        }

        using var owner = new SamplerOwner(sampler);
        var sample = owner.Sample;

        var numberOfHistoryWeeks = weeklyThroughputs.Count;

        // key = number of weeks to complete the item count
        // value = number of simulations that landed on that number of weeks
        var distribution = new Dictionary<int, int>();

        for (int i = 0; i < numberOfSimulations; i++)
        {
            int totalThroughput = 0;
            int numberOfWeeks = 0;

            do
            {
                var index = sample(0, numberOfHistoryWeeks - 1);
                totalThroughput += weeklyThroughputs[index];
                numberOfWeeks++;
            } while (totalThroughput < itemCount);

            Increment(distribution, numberOfWeeks);
        }

        return new WeeksForItemCountDistribution(distribution, numberOfSimulations);
    }

    /// <summary>
    /// Simulates how many items are likely to be completed in
    /// <paramref name="weekCount"/> weeks by sampling that many weeks of
    /// historical throughput per simulation run.
    /// </summary>
    public static ItemsInWeeksDistribution SimulateItemsInWeeks(
        IReadOnlyList<int> weeklyThroughputs,
        int weekCount,
        int numberOfSimulations = Constants.ForecastNumberOfSimulations,
        Func<int, int, int>? sampler = null)
    {
        AssertHistory(weeklyThroughputs);

        if (weekCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(weekCount), weekCount,
                "Value must be greater than or equal to one.");
        }

        using var owner = new SamplerOwner(sampler);
        var sample = owner.Sample;

        var numberOfHistoryWeeks = weeklyThroughputs.Count;

        // key = throughput (items completed)
        // value = number of simulations that produced that throughput
        var distribution = new Dictionary<int, int>();

        for (int i = 0; i < numberOfSimulations; i++)
        {
            int throughput = 0;

            for (int week = 0; week < weekCount; week++)
            {
                var index = sample(0, numberOfHistoryWeeks - 1);
                throughput += weeklyThroughputs[index];
            }

            Increment(distribution, throughput);
        }

        return new ItemsInWeeksDistribution(distribution, numberOfSimulations);
    }

    private static void AssertHistory(IReadOnlyList<int> weeklyThroughputs)
    {
        if (weeklyThroughputs == null)
        {
            throw new ArgumentNullException(nameof(weeklyThroughputs));
        }

        if (weeklyThroughputs.Count == 0)
        {
            throw new KnownException(
                "Cannot run a forecast without any weeks of throughput history.");
        }

        if (weeklyThroughputs.All(x => x <= 0))
        {
            throw new KnownException(
                "Cannot run a forecast because every week of history has zero throughput.");
        }
    }

    private static void Increment(Dictionary<int, int> distribution, int key)
    {
        if (distribution.ContainsKey(key) == false)
        {
            distribution.Add(key, 1);
        }
        else
        {
            distribution[key] += 1;
        }
    }

    /// <summary>
    /// Wraps the optional injected sampler or a crypto RNG, disposing the RNG
    /// when it owns one.
    /// </summary>
    private sealed class SamplerOwner : IDisposable
    {
        private readonly CryptoRandomNumberGenerator? _rng;

        public SamplerOwner(Func<int, int, int>? sampler)
        {
            if (sampler == null)
            {
                _rng = new CryptoRandomNumberGenerator();
                Sample = _rng.GetNumberInRange;
            }
            else
            {
                Sample = sampler;
            }
        }

        public Func<int, int, int> Sample { get; }

        public void Dispose()
        {
            _rng?.Dispose();
        }
    }
}
