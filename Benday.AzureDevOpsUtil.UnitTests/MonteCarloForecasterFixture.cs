using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class MonteCarloForecasterFixture
{
    /// <summary>
    /// Returns indices from a fixed sequence, cycling, so simulations are
    /// deterministic.
    /// </summary>
    private static Func<int, int, int> SequenceSampler(params int[] indices)
    {
        int position = 0;

        return (min, max) =>
        {
            var value = indices[position % indices.Length];
            position++;
            return value;
        };
    }

    [TestMethod]
    public void SimulateWeeksForItemCount_DeterministicSampler()
    {
        // one week of history with throughput 2; always sample it.
        var weeklyThroughputs = new[] { 2 };

        // 2, 4, 6 -> reaches 5 after 3 weeks, every simulation.
        var result = MonteCarloForecaster.SimulateWeeksForItemCount(
            weeklyThroughputs, itemCount: 5, numberOfSimulations: 10,
            sampler: (min, max) => 0);

        Assert.AreEqual(10, result.SimulationCount, "Wrong simulation count.");
        Assert.AreEqual(1, result.Distribution.Count, "Distribution should have a single bucket.");
        Assert.AreEqual(10, result.Distribution[3], "All 10 simulations should land on 3 weeks.");
        Assert.AreEqual(3, result.GetWeeksAtSimulationThreshold(10), "Wrong weeks at threshold.");
        Assert.AreEqual(3, result.GetWeeksAtConfidence(85), "Wrong weeks at 85% confidence.");
    }

    [TestMethod]
    public void SimulateWeeksForItemCount_ConfidenceAccumulatesAscending()
    {
        // Two weeks: index 0 -> throughput 5 (done in 1 week),
        //            index 1 -> throughput 1 (needs 5 weeks for 5 items).
        var weeklyThroughputs = new[] { 5, 1 };

        // 4 simulations: 1 fast (index 0) then 3 slow (index 1 repeated).
        var result = MonteCarloForecaster.SimulateWeeksForItemCount(
            weeklyThroughputs, itemCount: 5, numberOfSimulations: 4,
            sampler: SequenceSampler(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

        // sim1: index0 -> 5 >=5 in 1 week.
        // sim2..4: index1 x5 -> 5 in 5 weeks.
        Assert.AreEqual(1, result.Distribution[1], "One simulation should complete in 1 week.");
        Assert.AreEqual(3, result.Distribution[5], "Three simulations should complete in 5 weeks.");

        // Fewest-weeks-first accumulation.
        Assert.AreEqual(1, result.GetWeeksAtSimulationThreshold(1), "1 of 4 sims done in 1 week.");
        Assert.AreEqual(5, result.GetWeeksAtSimulationThreshold(2), "Need 5 weeks to reach 2 of 4 sims.");
    }

    [TestMethod]
    public void SimulateItemsInWeeks_DeterministicSampler()
    {
        var weeklyThroughputs = new[] { 3 };

        // 4 weeks * 3 = 12 items every simulation.
        var result = MonteCarloForecaster.SimulateItemsInWeeks(
            weeklyThroughputs, weekCount: 4, numberOfSimulations: 10,
            sampler: (min, max) => 0);

        Assert.AreEqual(10, result.SimulationCount, "Wrong simulation count.");
        Assert.AreEqual(10, result.Distribution[12], "All simulations should produce 12 items.");
        Assert.AreEqual(12, result.GetItemsAtSimulationThreshold(10), "Wrong items at threshold.");
        Assert.AreEqual(12, result.GetItemsAtConfidence(95), "Wrong items at 95% confidence.");
    }

    [TestMethod]
    public void SimulateItemsInWeeks_ConfidenceAccumulatesDescending()
    {
        // Two weeks: index 0 -> 10 items, index 1 -> 1 item.
        var weeklyThroughputs = new[] { 10, 1 };

        // weekCount 1, alternate indices for 4 simulations: 10, 1, 10, 1.
        var result = MonteCarloForecaster.SimulateItemsInWeeks(
            weeklyThroughputs, weekCount: 1, numberOfSimulations: 4,
            sampler: SequenceSampler(0, 1, 0, 1));

        Assert.AreEqual(2, result.Distribution[10], "Two simulations should produce 10 items.");
        Assert.AreEqual(2, result.Distribution[1], "Two simulations should produce 1 item.");

        // Most-items-first accumulation ("at least X items").
        Assert.AreEqual(10, result.GetItemsAtSimulationThreshold(2), "2 of 4 sims deliver at least 10.");
        Assert.AreEqual(1, result.GetItemsAtSimulationThreshold(3), "3 of 4 sims deliver at least 1.");
        Assert.AreEqual(10, result.GetItemsAtConfidence(50), "50% confident of at least 10 items.");
    }

    [TestMethod]
    public void SimulateWeeksForItemCount_EmptyHistory_Throws()
    {
        Assert.ThrowsExactly<KnownException>(() =>
            MonteCarloForecaster.SimulateWeeksForItemCount(Array.Empty<int>(), 5));
    }

    [TestMethod]
    public void SimulateWeeksForItemCount_AllZeroHistory_Throws()
    {
        Assert.ThrowsExactly<KnownException>(() =>
            MonteCarloForecaster.SimulateWeeksForItemCount(new[] { 0, 0 }, 5));
    }

    [TestMethod]
    public void SimulateWeeksForItemCount_InvalidItemCount_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            MonteCarloForecaster.SimulateWeeksForItemCount(new[] { 2 }, 0));
    }

    [TestMethod]
    public void SimulateItemsInWeeks_InvalidWeekCount_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            MonteCarloForecaster.SimulateItemsInWeeks(new[] { 2 }, 0));
    }
}
