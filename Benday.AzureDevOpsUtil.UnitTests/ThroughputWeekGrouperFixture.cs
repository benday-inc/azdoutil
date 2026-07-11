using Benday.AzureDevOpsUtil.Api.FlowMetrics;
using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ThroughputWeekGrouperFixture
{
    [TestMethod]
    public void GetMondayOfWeek_ForMonday_ReturnsSameDay()
    {
        // 2024-01-01 was a Monday.
        var monday = new DateTime(2024, 1, 1);

        var actual = ThroughputWeekGrouper.GetMondayOfWeek(monday);

        Assert.AreEqual(monday.Date, actual, "Monday should map to itself.");
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek, "Result must be a Monday.");
    }

    [TestMethod]
    public void GetMondayOfWeek_ForSunday_ReturnsPrecedingMonday()
    {
        // 2024-01-07 was the Sunday of the same week.
        var sunday = new DateTime(2024, 1, 7);

        var actual = ThroughputWeekGrouper.GetMondayOfWeek(sunday);

        Assert.AreEqual(new DateTime(2024, 1, 1), actual, "Sunday should map back to Monday.");
    }

    private static WorkItemCycleTimeData Completed(int id, int completedDateSk, float cycleTime)
    {
        return new WorkItemCycleTimeData
        {
            WorkItemId = id,
            Title = $"Item {id}",
            CompletedDateSK = completedDateSk,
            CycleTimeDays = cycleTime
        };
    }

    [TestMethod]
    public void GroupByWeek_GroupsItemsByMondayOfWeek()
    {
        var items = new[]
        {
            Completed(1, 20240101, 3), // Monday
            Completed(2, 20240103, 5), // Wednesday, same week
            Completed(3, 20240108, 2)  // next Monday
        };

        var grouped = ThroughputWeekGrouper.GroupByWeek(items);

        Assert.AreEqual(2, grouped.Count, "Expected two week buckets.");
        Assert.AreEqual(2, grouped[new DateTime(2024, 1, 1)].Items.Count, "First week should have 2 items.");
        Assert.AreEqual(1, grouped[new DateTime(2024, 1, 8)].Items.Count, "Second week should have 1 item.");
    }

    [TestMethod]
    public void GetWeeklyThroughputCounts_ReturnsCountPerWeek()
    {
        var items = new[]
        {
            Completed(1, 20240101, 3),
            Completed(2, 20240103, 5),
            Completed(3, 20240108, 2)
        };

        var counts = ThroughputWeekGrouper.GetWeeklyThroughputCounts(items);

        Assert.AreEqual(2, counts.Count, "Expected counts for two weeks.");
        CollectionAssert.AreEquivalent(new[] { 2, 1 }, counts.ToArray(), "Wrong weekly counts.");
    }

    [TestMethod]
    public void GroupByWeek_NullInput_ReturnsEmpty()
    {
        var grouped = ThroughputWeekGrouper.GroupByWeek(null!);

        Assert.AreEqual(0, grouped.Count, "Null input should produce an empty grouping.");
    }
}
