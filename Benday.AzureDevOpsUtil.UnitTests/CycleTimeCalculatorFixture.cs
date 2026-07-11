using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class CycleTimeCalculatorFixture
{
    private static List<WorkItemCycleTimeData> ItemsWithCycleTimes(params float[] cycleTimes)
    {
        return cycleTimes
            .Select((value, index) => new WorkItemCycleTimeData
            {
                WorkItemId = index + 1,
                Title = $"Item {index + 1}",
                CycleTimeDays = value
            })
            .ToList();
    }

    [TestMethod]
    public void GetCycleTimeAtPercentile_85thPercentile()
    {
        // 10 items with cycle times 1..10. GetIndexForPercentForecast(10, 85) == 8,
        // so the sorted value at index 8 (the 9th value) is expected.
        var items = ItemsWithCycleTimes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        var actual = CycleTimeCalculator.GetCycleTimeAtPercentile(items, 85);

        Assert.AreEqual(9d, actual, "Wrong 85th percentile cycle time.");
    }

    [TestMethod]
    public void GetCycleTimeAtPercentile_SortsUnorderedInput()
    {
        var items = ItemsWithCycleTimes(10, 1, 9, 2, 8, 3, 7, 4, 6, 5);

        var actual = CycleTimeCalculator.GetCycleTimeAtPercentile(items, 50);

        // GetIndexForPercentForecast(10, 50) == 4 -> sorted value at index 4 == 5.
        Assert.AreEqual(5d, actual, "Percentile should be computed over sorted values.");
    }

    [TestMethod]
    public void GetDeliveryWindow_ReturnsPercentiles()
    {
        var cycleTimes = Enumerable.Range(1, 100).Select(x => (float)x).ToArray();
        var items = ItemsWithCycleTimes(cycleTimes);

        var window = CycleTimeCalculator.GetDeliveryWindow(items);

        Assert.AreEqual(100, window.ItemCount, "Wrong item count.");
        Assert.AreEqual(50d, window.CycleTimeDaysAt50Percent, "Wrong 50th percentile.");
        Assert.AreEqual(85d, window.CycleTimeDaysAt85Percent, "Wrong 85th percentile.");
        Assert.AreEqual(95d, window.CycleTimeDaysAt95Percent, "Wrong 95th percentile.");
    }

    [TestMethod]
    public void GetDeliveryWindow_EmptyItems_Throws()
    {
        Assert.ThrowsExactly<KnownException>(() =>
            CycleTimeCalculator.GetDeliveryWindow(new List<WorkItemCycleTimeData>()));
    }

    [TestMethod]
    public void GetCycleTimeAtPercentile_RoundsToTwoDecimals()
    {
        var items = ItemsWithCycleTimes(1.111f, 2.222f, 3.333f);

        // GetIndexForPercentForecast(3, 85) == 2 -> value 3.333 rounded to 3.33.
        var actual = CycleTimeCalculator.GetCycleTimeAtPercentile(items, 85);

        Assert.AreEqual(3.33d, actual, "Value should be rounded to two decimals.");
    }
}
