using Benday.AzureDevOpsUtil.Api.BranchHealth;
using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BranchHealthAnalyzerFixture
{
    private BranchHealthAnalyzer SystemUnderTest => new();

    private const string ProjectName = "GnarlyCorp";
    private const string RepositoryName = "Web";

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static GitBranchStatsInfo Branch(
        string name,
        double ageInDays,
        int ahead = 1,
        int behind = 0,
        bool isDefault = false,
        string committer = "Ann Dev",
        string? author = null)
    {
        return new GitBranchStatsInfo
        {
            Name = name,
            AheadCount = ahead,
            BehindCount = behind,
            IsBaseVersion = isDefault,
            Commit = new GitCommitStatsInfo
            {
                CommitId = "abc123",
                Committer = new GitUserDateInfo
                {
                    Name = committer,
                    Date = UtcNow.AddDays(-ageInDays)
                },
                Author = new GitUserDateInfo
                {
                    Name = author ?? committer,
                    // Deliberately different from the committer date.
                    Date = UtcNow.AddDays(-ageInDays - 500)
                }
            }
        };
    }

    private BranchHealthResult Analyze(params GitBranchStatsInfo[] branches)
    {
        return SystemUnderTest.Analyze(branches, ProjectName, RepositoryName, UtcNow);
    }

    [TestMethod]
    public void Analyze_CountsBranchesInTheActivityWindow()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("feature/a", 2),
            Branch("feature/b", 20),
            Branch("feature/c", 400));

        Assert.AreEqual(4, actual.BranchCount, "Wrong branch count.");
        Assert.AreEqual(2, actual.ActiveBranchCount, "Wrong count for the last 7 days.");
        Assert.AreEqual(3, actual.ActiveBranchCountLast30Days, "Wrong count for the last 30 days.");
    }

    [TestMethod]
    public void Analyze_UsesTheCommitterDateNotTheAuthorDate()
    {
        // The author date on this fixture is 500 days older than the committer
        // date.  A rebase moves work forward without touching the author date,
        // so using it would report live branches as ancient.
        var actual = Analyze(Branch("feature/rebased", 2));

        var branch = actual.Branches.Single();

        Assert.IsTrue(
            branch.AgeInDays < 3, "The committer date is what says when work landed here.");
    }

    [TestMethod]
    public void Analyze_FallsBackToTheAuthorDateWhenThereIsNoCommitterDate()
    {
        var stats = new GitBranchStatsInfo
        {
            Name = "feature/odd",
            AheadCount = 1,
            Commit = new GitCommitStatsInfo
            {
                Author = new GitUserDateInfo { Name = "Ann Dev", Date = UtcNow.AddDays(-3) }
            }
        };

        var actual = SystemUnderTest.Analyze(
            new[] { stats }, ProjectName, RepositoryName, UtcNow);

        Assert.IsNotNull(actual.Branches.Single().AgeInDays, "An age should still be worked out.");
    }

    [TestMethod]
    public void Analyze_CountsUnmergedBranchesExcludingTheDefault()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("feature/a", 2, ahead: 3),
            Branch("feature/merged", 5, ahead: 0));

        Assert.AreEqual(
            1, actual.UnmergedBranchCount, "Only branches ahead of the default count.");
    }

    [TestMethod]
    public void Analyze_DefaultBranchIsNeverCountedAsUnmergedEvenWhenAhead()
    {
        var actual = Analyze(Branch("main", 1, ahead: 5, isDefault: true));

        Assert.AreEqual(
            0, actual.UnmergedBranchCount, "The default branch is what others merge into.");
    }

    [TestMethod]
    public void Analyze_MedianAgeWithAnOddNumberOfBranches()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("a", 10),
            Branch("b", 20),
            Branch("c", 60));

        Assert.IsNotNull(actual.MedianUnmergedBranchAgeInDays, "Expected a median.");
        Assert.AreEqual(
            20d, actual.MedianUnmergedBranchAgeInDays!.Value, 0.01, "Wrong median.");
    }

    [TestMethod]
    public void Analyze_MedianAgeWithAnEvenNumberOfBranches()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("a", 10),
            Branch("b", 20),
            Branch("c", 30),
            Branch("d", 60));

        Assert.IsNotNull(actual.MedianUnmergedBranchAgeInDays, "Expected a median.");
        Assert.AreEqual(
            25d, actual.MedianUnmergedBranchAgeInDays!.Value, 0.01, "Wrong median.");
    }

    [TestMethod]
    public void Analyze_MedianIsNullWithNoUnmergedBranches()
    {
        var actual = Analyze(Branch("main", 1, ahead: 0, isDefault: true));

        Assert.IsNull(
            actual.MedianUnmergedBranchAgeInDays, "There is no median of nothing.");
    }

    [TestMethod]
    public void Analyze_MedianExcludesTheDefaultBranch()
    {
        // A very old default branch would drag the median if it were counted.
        var actual = Analyze(
            Branch("main", 900, ahead: 0, isDefault: true),
            Branch("a", 10),
            Branch("b", 20));

        Assert.IsNotNull(actual.MedianUnmergedBranchAgeInDays, "Expected a median.");
        Assert.AreEqual(
            15d, actual.MedianUnmergedBranchAgeInDays!.Value, 0.01, "Wrong median.");
    }

    [TestMethod]
    public void Analyze_FindsTheOldestUnmergedBranch()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("recent", 5),
            Branch("ancient", 700));

        Assert.AreEqual("ancient", actual.OldestUnmergedBranch?.Name, "Wrong oldest branch.");
    }

    [TestMethod]
    public void Analyze_CountsDeadBranches()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true),
            Branch("stale", 400),
            Branch("fresh", 3));

        Assert.AreEqual(1, actual.DeadBranchCount, "Wrong dead branch count.");
    }

    [TestMethod]
    public void Analyze_WindowBoundaryIsInclusive()
    {
        var actual = Analyze(Branch("edge", 7));

        Assert.AreEqual(1, actual.ActiveBranchCount, "A branch exactly on the edge is active.");
    }

    [TestMethod]
    public void Analyze_HonoursACustomWindow()
    {
        var branches = new[] { Branch("a", 10), Branch("b", 20) };

        var actual = SystemUnderTest.Analyze(
            branches, ProjectName, RepositoryName, UtcNow, activityWindowDays: 14);

        Assert.AreEqual(1, actual.ActiveBranchCount, "Wrong count for a 14 day window.");
        Assert.AreEqual(14, actual.ActivityWindowDays, "The window should be reported.");
    }

    [TestMethod]
    public void Analyze_GroupsCommittersWithSeveralActiveBranches()
    {
        var actual = Analyze(
            Branch("main", 1, ahead: 0, isDefault: true, committer: "Ann Dev"),
            Branch("feature/a", 2, committer: "Ann Dev"),
            Branch("feature/b", 3, committer: "Ann Dev"),
            Branch("feature/c", 4, committer: "Bob Coder"));

        var ann = actual.Committers.Single(x => x.Name == "Ann Dev");

        Assert.AreEqual(2, ann.BranchCount, "The default branch should not be counted.");
        Assert.AreEqual("Ann Dev", actual.Committers[0].Name, "Busiest committer comes first.");
    }

    [TestMethod]
    public void Analyze_CommitterActivityOnlyCountsTheLastFortnight()
    {
        var actual = Analyze(
            Branch("feature/a", 2, committer: "Ann Dev"),
            Branch("feature/old", 40, committer: "Ann Dev"));

        Assert.AreEqual(
            1,
            actual.Committers.Single().BranchCount,
            "A branch untouched for 40 days is not what somebody is working on now.");
    }

    [TestMethod]
    public void Analyze_EmptyRepository()
    {
        var actual = SystemUnderTest.Analyze(
            Array.Empty<GitBranchStatsInfo>(), ProjectName, RepositoryName, UtcNow);

        Assert.AreEqual(0, actual.BranchCount, "There are no branches.");
        Assert.IsTrue(
            actual.Notes.Any(x => x.Contains("empty repository")),
            "An empty repository should be explained rather than shown as zeroes.");
    }

    [TestMethod]
    public void Analyze_NullInput()
    {
        var actual = SystemUnderTest.Analyze(null, ProjectName, RepositoryName, UtcNow);

        Assert.AreEqual(0, actual.BranchCount, "Nothing to analyze.");
    }

    [TestMethod]
    public void Analyze_BranchWithNoCommitInformation()
    {
        var stats = new GitBranchStatsInfo { Name = "odd", AheadCount = 1 };

        var actual = SystemUnderTest.Analyze(
            new[] { stats }, ProjectName, RepositoryName, UtcNow);

        Assert.IsNull(actual.Branches.Single().AgeInDays, "There is no date to work from.");
        Assert.AreEqual(0, actual.ActiveBranchCount, "An unknown age is not activity.");
        Assert.AreEqual(0, actual.DeadBranchCount, "An unknown age is not death either.");
    }
}
