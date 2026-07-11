namespace Benday.AzureDevOpsUtil.Api.FlowMetrics;

/// <summary>
/// Result of a "how many weeks to complete N items?" Monte Carlo run.
/// Lower week counts are more likely (sooner), so confidence accumulates
/// from the smallest number of weeks upward.
/// </summary>
public class WeeksForItemCountDistribution
{
    private readonly Dictionary<int, int> _distribution;

    public WeeksForItemCountDistribution(Dictionary<int, int> distribution, int simulationCount)
    {
        _distribution = distribution;
        SimulationCount = simulationCount;
    }

    public int SimulationCount { get; }

    public IReadOnlyDictionary<int, int> Distribution => _distribution;

    /// <summary>
    /// Returns the number of weeks by which at least <paramref name="simulationThreshold"/>
    /// of the simulations had completed the target item count. Accumulates
    /// ascending (fewest weeks first).
    /// </summary>
    public int GetWeeksAtSimulationThreshold(int simulationThreshold)
    {
        var sortedKeys = _distribution.Keys.OrderBy(x => x);

        int total = 0;

        foreach (var key in sortedKeys)
        {
            total += _distribution[key];

            if (total >= simulationThreshold)
            {
                return key;
            }
        }

        throw new InvalidOperationException(
            $"Something went wrong. Never found a simulation count >= {simulationThreshold}.");
    }

    /// <summary>
    /// Returns the number of weeks needed to be <paramref name="confidencePercent"/>%
    /// confident the item count is complete.
    /// </summary>
    public int GetWeeksAtConfidence(double confidencePercent)
    {
        return GetWeeksAtSimulationThreshold(ToThreshold(confidencePercent, SimulationCount));
    }

    internal static int ToThreshold(double confidencePercent, int simulationCount)
    {
        var threshold = (int)Math.Round(simulationCount * (confidencePercent / 100.0),
            MidpointRounding.AwayFromZero);

        if (threshold < 1)
        {
            threshold = 1;
        }
        else if (threshold > simulationCount)
        {
            threshold = simulationCount;
        }

        return threshold;
    }
}

/// <summary>
/// Result of a "how many items in N weeks?" Monte Carlo run. Higher item
/// counts are less certain, so confidence accumulates from the largest
/// throughput downward (i.e. "95% sure of at least X items").
/// </summary>
public class ItemsInWeeksDistribution
{
    private readonly Dictionary<int, int> _distribution;

    public ItemsInWeeksDistribution(Dictionary<int, int> distribution, int simulationCount)
    {
        _distribution = distribution;
        SimulationCount = simulationCount;
    }

    public int SimulationCount { get; }

    public IReadOnlyDictionary<int, int> Distribution => _distribution;

    /// <summary>
    /// Returns the item count that at least <paramref name="simulationThreshold"/>
    /// of the simulations met or exceeded. Accumulates descending (most items first).
    /// </summary>
    public int GetItemsAtSimulationThreshold(int simulationThreshold)
    {
        var sortedKeys = _distribution.Keys.OrderByDescending(x => x);

        int total = 0;

        foreach (var key in sortedKeys)
        {
            total += _distribution[key];

            if (total >= simulationThreshold)
            {
                return key;
            }
        }

        throw new InvalidOperationException(
            $"Something went wrong. Never found a simulation count >= {simulationThreshold}.");
    }

    /// <summary>
    /// Returns the number of items you can be <paramref name="confidencePercent"/>%
    /// confident of completing.
    /// </summary>
    public int GetItemsAtConfidence(double confidencePercent)
    {
        return GetItemsAtSimulationThreshold(
            WeeksForItemCountDistribution.ToThreshold(confidencePercent, SimulationCount));
    }
}
