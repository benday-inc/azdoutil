using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcBranchActivityServiceFixture
{
    private TfvcBranchActivityService SystemUnderTest => new();

    private const string ProjectName = "App";

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static List<BranchCandidate> Candidates(params string[] paths)
    {
        return paths.Select(x => new BranchCandidate(x, true)).ToList();
    }

    [TestMethod]
    public async Task Analyze_BranchWithRecentChangesIsActive()
    {
        var client = new FakeTfvcApiClient();

        client.SetChangesets(
            "$/App/Main",
            FakeTfvcApiClient.Changeset(101, UtcNow.AddDays(-3), "Ann Dev"),
            FakeTfvcApiClient.Changeset(100, UtcNow.AddDays(-45)));

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Main"), UtcNow);

        var branch = actual.Single();

        Assert.AreEqual(
            BranchActivityClassification.Active, branch.Classification, "Wrong classification.");
        Assert.AreEqual(2, branch.ChangesetsLast90Days, "Wrong 90 day count.");
        Assert.AreEqual(2, branch.ChangesetsLast180Days, "Wrong 180 day count.");
        Assert.AreEqual(2, branch.ChangesetsLast365Days, "Wrong 365 day count.");
        Assert.IsNotNull(branch.LastChangesetDate, "Expected a last changeset date.");
        Assert.AreEqual(
            UtcNow.AddDays(-3), branch.LastChangesetDate!.Value, "Wrong last changeset date.");
        Assert.AreEqual("Ann Dev", branch.LastChangesetAuthor, "Wrong last changeset author.");
        Assert.IsFalse(branch.CountsAreCapped, "Counts should not be capped.");
    }

    [TestMethod]
    public async Task Analyze_BranchWithOnlyOlderChangesIsCooling()
    {
        var client = new FakeTfvcApiClient();

        client.SetChangesets(
            "$/App/Release",
            FakeTfvcApiClient.Changeset(200, UtcNow.AddDays(-200)));

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Release"), UtcNow);

        var branch = actual.Single();

        Assert.AreEqual(
            BranchActivityClassification.Cooling, branch.Classification, "Wrong classification.");
        Assert.AreEqual(0, branch.ChangesetsLast90Days, "Wrong 90 day count.");
        Assert.AreEqual(0, branch.ChangesetsLast180Days, "Wrong 180 day count.");
        Assert.AreEqual(1, branch.ChangesetsLast365Days, "Wrong 365 day count.");
    }

    [TestMethod]
    public async Task Analyze_BranchWithNothingInAYearIsDead()
    {
        var client = new FakeTfvcApiClient();

        client.SetChangesets(
            "$/App/Old",
            FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-800), "Bob Retired"));

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Old"), UtcNow);

        var branch = actual.Single();

        Assert.AreEqual(
            BranchActivityClassification.Dead, branch.Classification, "Wrong classification.");
        Assert.AreEqual(0, branch.ChangesetsLast365Days, "Wrong 365 day count.");

        // The follow-up lookup is what gives a dead branch a real date in the table.
        Assert.IsNotNull(branch.LastChangesetDate, "Expected a last changeset date.");
        Assert.AreEqual(
            UtcNow.AddDays(-800), branch.LastChangesetDate!.Value, "Wrong last changeset date.");
        Assert.AreEqual("Bob Retired", branch.LastChangesetAuthor, "Wrong last changeset author.");
    }

    [TestMethod]
    public async Task Analyze_BranchWithNoHistoryAtAllIsDead()
    {
        var client = new FakeTfvcApiClient();

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Empty"), UtcNow);

        var branch = actual.Single();

        Assert.AreEqual(
            BranchActivityClassification.Dead, branch.Classification, "Wrong classification.");
        Assert.IsNull(branch.LastChangesetDate, "There is no date to report.");
    }

    [TestMethod]
    public async Task Analyze_MarksCountsAsCappedWhenTheWalkStops()
    {
        var client = new FakeTfvcApiClient();

        var changesets = Enumerable.Range(1, 5)
            .Select(x => FakeTfvcApiClient.Changeset(x, UtcNow.AddDays(-x)))
            .ToArray();

        client.SetChangesets("$/App/Busy", changesets);

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Busy"), UtcNow, maxChangesetsPerBranch: 3);

        var branch = actual.Single();

        Assert.IsTrue(branch.CountsAreCapped, "Hitting the cap should be flagged.");
        Assert.AreEqual(3, branch.ChangesetsLast90Days, "Counts stop at the cap.");
    }

    [TestMethod]
    public async Task Analyze_ChangesetExactlyOnTheNinetyDayEdgeCountsAsActive()
    {
        var client = new FakeTfvcApiClient();

        client.SetChangesets(
            "$/App/Edge",
            FakeTfvcApiClient.Changeset(300, UtcNow.AddDays(-90)));

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Edge"), UtcNow);

        Assert.AreEqual(
            BranchActivityClassification.Active,
            actual.Single().Classification,
            "The window boundary is inclusive.");
    }

    [TestMethod]
    public async Task Analyze_HandlesSeveralBranches()
    {
        var client = new FakeTfvcApiClient();

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));
        client.SetChangesets("$/App/Dev", FakeTfvcApiClient.Changeset(2, UtcNow.AddDays(-200)));

        var actual = await SystemUnderTest.AnalyzeAsync(
            client, ProjectName, Candidates("$/App/Main", "$/App/Dev"), UtcNow);

        Assert.AreEqual(2, actual.Count, "Expected one row per branch.");
        Assert.AreEqual(
            BranchActivityClassification.Active,
            actual.Single(x => x.Path == "$/App/Main").Classification,
            "Main should be active.");
        Assert.AreEqual(
            BranchActivityClassification.Cooling,
            actual.Single(x => x.Path == "$/App/Dev").Classification,
            "Dev should be cooling.");
    }
}
