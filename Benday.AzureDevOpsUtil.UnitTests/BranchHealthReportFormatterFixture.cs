using Benday.AzureDevOpsUtil.Api.BranchHealth;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BranchHealthReportFormatterFixture
{
    private BranchHealthReportFormatter SystemUnderTest => new();

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] ProhibitedPhrases =
    {
        "consider", "recommend", "should", "you may want", "try "
    };

    private static BranchHealthResult BuildResult()
    {
        var result = new BranchHealthResult
        {
            ProjectName = "GnarlyCorp",
            RepositoryName = "Web",
            GeneratedUtc = UtcNow,
            ActivityWindowDays = 7,
            ActiveBranchCount = 3,
            ActiveBranchCountLast30Days = 8,
            DeadBranchCount = 12,
            MedianUnmergedBranchAgeInDays = 34.5
        };

        result.Branches.Add(new BranchInfo
        {
            Name = "main",
            IsDefaultBranch = true,
            AheadCount = 0,
            BehindCount = 0,
            LastCommitDate = UtcNow.AddDays(-1),
            LastCommitBy = "Ann Dev",
            AgeInDays = 1
        });

        result.Branches.Add(new BranchInfo
        {
            Name = "feature/checkout",
            AheadCount = 12,
            BehindCount = 40,
            LastCommitDate = UtcNow.AddDays(-90),
            LastCommitBy = "Bob Coder",
            AgeInDays = 90
        });

        result.OldestUnmergedBranch = result.Branches[1];

        result.Committers.Add(new CommitterActivity
        {
            Name = "Ann Dev",
            BranchNames = new List<string> { "feature/a", "feature/b", "feature/c" }
        });

        return result;
    }

    [TestMethod]
    public void FormatReport_IncludesHeaderAndSummary()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "# Branch Health", "Missing the title.");
        StringAssert.Contains(actual, "GnarlyCorp", "Missing the project name.");
        StringAssert.Contains(actual, "Web", "Missing the repository name.");
        StringAssert.Contains(actual, "2026-06-01", "Missing the generated date.");
        StringAssert.Contains(actual, "34.5 days", "Missing the median age.");
    }

    [TestMethod]
    public void FormatReport_StatesTheHeadlineNumberPlainly()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual,
            "In the last 7 days, 3 branch(es) received commits",
            "Missing the headline sentence.");

        StringAssert.Contains(
            actual,
            "Each active branch is a separate piece of work in progress",
            "Missing the consequence.");
    }

    [TestMethod]
    public void FormatReport_MarksTheDefaultBranch()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "main (default)", "The default branch should be marked.");
    }

    [TestMethod]
    public void FormatReport_ListsPeopleWorkingOnSeveralBranches()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "| Ann Dev | 3 ", "Missing the committer row.");
        StringAssert.Contains(
            actual,
            "These people are working on multiple things at once",
            "Missing the consequence.");
    }

    [TestMethod]
    public void FormatReport_EndsWithTheCaseStudyFooter()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult()).TrimEnd();

        Assert.IsTrue(
            actual.EndsWith(BranchHealthReportFormatter.FooterLine, StringComparison.Ordinal),
            "The report should end with the footer line.");
    }

    [TestMethod]
    public void FormatReport_UsesNoProhibitedLanguage()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        foreach (var phrase in ProhibitedPhrases)
        {
            Assert.IsFalse(
                actual.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"Report text contains prescriptive language: '{phrase}'.");
        }
    }

    [TestMethod]
    public void FormatReport_EmptyRepositoryStillProducesAReport()
    {
        var result = new BranchHealthResult
        {
            ProjectName = "GnarlyCorp",
            RepositoryName = "Empty",
            GeneratedUtc = UtcNow,
            ActivityWindowDays = 7
        };

        result.Notes.Add("No branches were returned for this repository.");

        var actual = SystemUnderTest.FormatReport(result);

        StringAssert.Contains(actual, "| Branches | 0 |", "Missing the branch count.");
        StringAssert.Contains(
            actual, BranchHealthReportFormatter.FooterLine, "Missing the footer.");
    }

    [TestMethod]
    public void FormatCsv_HasOneRowPerBranch()
    {
        var actual = SystemUnderTest.FormatCsv(BuildResult());

        StringAssert.Contains(actual, "Branch", "Missing the header.");
        StringAssert.Contains(actual, "feature/checkout", "Missing a branch row.");
        StringAssert.Contains(actual, "Web", "Missing the repository name.");
    }
}
