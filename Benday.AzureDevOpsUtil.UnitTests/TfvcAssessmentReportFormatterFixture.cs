using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcAssessmentReportFormatterFixture
{
    private TfvcAssessmentReportFormatter SystemUnderTest => new();

    private static readonly DateTime UtcNow = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Language that turns a description into a recommendation.  The reports in
    /// this tool state facts and consequences and leave the decision alone.
    /// </summary>
    private static readonly string[] ProhibitedPhrases =
    {
        "consider", "recommend", "should", "you may want", "try "
    };

    /// <summary>
    /// Phrases that contain a prohibited word but are not advice.  Keep this
    /// list short and justify every entry.
    /// </summary>
    private static readonly string[] AllowedExceptions =
    {
    };

    private static TfvcAssessmentResult BuildResult()
    {
        var result = new TfvcAssessmentResult
        {
            ProjectName = "GnarlyCorp",
            ScopePath = "$/GnarlyCorp",
            GeneratedUtc = UtcNow
        };

        var main = new TfvcBranchNode { Path = "$/GnarlyCorp/Main" };
        var dev = new TfvcBranchNode { Path = "$/GnarlyCorp/Dev" };
        var nested = new TfvcBranchNode { Path = "$/GnarlyCorp/Main/Feature" };

        main.Children.Add(dev);
        main.Children.Add(nested);

        result.RegisteredBranchRoots.Add(main);

        result.RegisteredBranchPaths.AddRange(new[]
        {
            "$/GnarlyCorp/Main", "$/GnarlyCorp/Dev", "$/GnarlyCorp/Main/Feature"
        });

        result.NestedBranches.Add(new NestedBranchPair
        {
            ChildPath = "$/GnarlyCorp/Main/Feature",
            ParentPath = "$/GnarlyCorp/Main"
        });

        result.UnregisteredBranchGroups.Add(new UnregisteredBranchGroup
        {
            ParentPath = "$/GnarlyCorp/Legacy",
            FolderPaths = new List<string>
            {
                "$/GnarlyCorp/Legacy/Prod", "$/GnarlyCorp/Legacy/QA"
            }
        });

        result.BranchActivity.Add(new BranchActivity
        {
            Path = "$/GnarlyCorp/Main",
            IsRegisteredBranch = true,
            LastChangesetDate = UtcNow.AddDays(-2),
            LastChangesetAuthor = "Ann Dev",
            ChangesetsLast90Days = 40,
            ChangesetsLast180Days = 80,
            ChangesetsLast365Days = 120,
            Classification = BranchActivityClassification.Active
        });

        result.BranchActivity.Add(new BranchActivity
        {
            Path = "$/GnarlyCorp/Old",
            IsRegisteredBranch = true,
            LastChangesetDate = UtcNow.AddDays(-900),
            LastChangesetAuthor = "Bob Retired",
            Classification = BranchActivityClassification.Dead
        });

        result.Findings.Add(new AssessmentFinding(
            FindingCategories.BranchActivity,
            "2 branches are active at the same time.",
            "The Azure DevOps import creates each branch as an unrelated Git history " +
                "with no common ancestor."));

        result.TfvcBuildDefinitions.Add(new TfvcBuildDefinitionInfo
        {
            Id = 1,
            Name = "Web-CI",
            LastRunDate = UtcNow.AddDays(-4),
            MappedPaths = new List<string> { "$/GnarlyCorp/Web", "$/GnarlyCorp/Common" },
            CloakedPaths = new List<string> { "$/GnarlyCorp/Web/Drops" },
            Mappings = new List<TfvcWorkspaceMapping>
            {
                new() { ServerPath = "$/GnarlyCorp/Web", MappingType = "map" },
                new() { ServerPath = "$/GnarlyCorp/Common", MappingType = "map" },
                new() { ServerPath = "$/GnarlyCorp/Web/Drops", MappingType = "cloak" }
            }
        });

        result.TfvcBuildDefinitions.Add(new TfvcBuildDefinitionInfo
        {
            Id = 2,
            Name = "Batch-CI",
            LastRunDate = UtcNow.AddDays(-500),
            IsInactive = true,
            MappedPaths = new List<string> { "$/GnarlyCorp/Common" },
            Mappings = new List<TfvcWorkspaceMapping>
            {
                new() { ServerPath = "$/GnarlyCorp/Common", MappingType = "map" }
            }
        });

        result.MappedPathUsages.Add(new MappedPathUsage
        {
            Path = "$/GnarlyCorp/Common",
            DefinitionNames = new List<string> { "Web-CI", "Batch-CI" }
        });

        result.MappedPathUsages.Add(new MappedPathUsage
        {
            Path = "$/GnarlyCorp/Web",
            DefinitionNames = new List<string> { "Web-CI" }
        });

        result.Content = new TfvcContentScanResult
        {
            FileCount = 4820,
            TotalSizeBytes = 3L * 1024 * 1024 * 1024,
            FilesOverWarningSize = 3,
            FilesOverPushLimit = 1,
            LargestFiles = new List<LargeFileInfo>
            {
                new()
                {
                    Path = "$/GnarlyCorp/Main/db/Backup.bak",
                    SizeBytes = 300L * 1024 * 1024
                },
                new()
                {
                    Path = "$/GnarlyCorp/Main/tools/Installer.iso",
                    SizeBytes = 60L * 1024 * 1024
                }
            },
            ExtensionUsages = new List<ExtensionUsage>
            {
                new() { Extension = ".dll", FileCount = 412, TotalSizeBytes = 380L * 1024 * 1024 },
                new() { Extension = ".pdb", FileCount = 210, TotalSizeBytes = 90L * 1024 * 1024 }
            },
            GeneratedFolders = new List<GeneratedFolderUsage>
            {
                new()
                {
                    Name = "packages",
                    FileCount = 3847,
                    TotalSizeBytes = 1200L * 1024 * 1024,
                    ExamplePath = "$/GnarlyCorp/Main/packages/Newtonsoft.Json/lib/net45/x.dll"
                },
                new()
                {
                    Name = "bin",
                    FileCount = 610,
                    TotalSizeBytes = 400L * 1024 * 1024,
                    ExamplePath = "$/GnarlyCorp/Main/src/bin/App.dll"
                }
            }
        };

        result.Notes.Add("Folder scan walked 3 level(s) below $/GnarlyCorp.");

        return result;
    }

    [TestMethod]
    public void FormatReport_IncludesHeaderDetails()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "# TFVC Migration Assessment", "Missing report title.");
        StringAssert.Contains(actual, "GnarlyCorp", "Missing project name.");
        StringAssert.Contains(actual, "$/GnarlyCorp", "Missing scope path.");
        StringAssert.Contains(actual, "2026-06-01", "Missing generated date.");
    }

    [TestMethod]
    public void FormatReport_EndsWithTheFixedFooter()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult()).TrimEnd();

        Assert.IsTrue(
            actual.EndsWith(TfvcAssessmentReportFormatter.FooterLine, StringComparison.Ordinal),
            "The report should end with the footer line.");
    }

    [TestMethod]
    public void FormatReport_IncludesTheIndentedTree()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "$/GnarlyCorp/Main", "Missing root branch in the tree.");
        StringAssert.Contains(
            actual, "  $/GnarlyCorp/Dev", "Child branches should be indented.");
    }

    [TestMethod]
    public void FormatMermaidDiagram_RendersNodesAndEdges()
    {
        var result = BuildResult();

        var actual = SystemUnderTest.FormatMermaidDiagram(result.RegisteredBranchRoots);

        var expected =
            "```mermaid" + Environment.NewLine +
            "graph TD" + Environment.NewLine +
            "    B0[\"$/GnarlyCorp/Main\"]" + Environment.NewLine +
            "    B1[\"$/GnarlyCorp/Dev\"]" + Environment.NewLine +
            "    B2[\"$/GnarlyCorp/Main/Feature\"]" + Environment.NewLine +
            "    B0 --> B1" + Environment.NewLine +
            "    B0 --> B2" + Environment.NewLine +
            "```";

        Assert.AreEqual(expected, actual, "Mermaid diagram did not match.");
    }

    [TestMethod]
    public void FormatReport_IncludesBranchActivityTable()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "## Branch activity", "Missing branch activity section.");
        StringAssert.Contains(actual, "Ann Dev", "Missing last changeset author.");
        StringAssert.Contains(actual, "Active", "Missing the active classification.");
        StringAssert.Contains(actual, "Dead", "Missing the dead classification.");
    }

    [TestMethod]
    public void FormatReport_MarksCappedCountsWithAPlus()
    {
        var result = BuildResult();

        result.BranchActivity[0].CountsAreCapped = true;

        var actual = SystemUnderTest.FormatReport(result);

        StringAssert.Contains(actual, "40+", "A capped count should be shown as a floor.");
    }

    [TestMethod]
    public void FormatReport_ListsUnregisteredBranchGroups()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual, "$/GnarlyCorp/Legacy/Prod", "Missing unregistered branch folder.");
        StringAssert.Contains(
            actual, "$/GnarlyCorp/Legacy/QA", "Missing unregistered branch folder.");
    }

    [TestMethod]
    public void FormatReport_IncludesTheBuildDefinitionTable()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual, "## Build definitions that pull from TFVC", "Missing the build section.");
        StringAssert.Contains(actual, "Web-CI", "Missing a definition name.");
        StringAssert.Contains(actual, "complex", "The multi-path workspace should be labelled.");
        StringAssert.Contains(actual, "simple", "The single-path workspace should be labelled.");
    }

    [TestMethod]
    public void FormatReport_MarksInactiveBuildsInTheTable()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual, "(inactive)", "A build with no recent runs should be marked in the table.");
    }

    [TestMethod]
    public void FormatReport_ExpandsComplexWorkspaces()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual,
            "### Workspaces built from more than one path",
            "Missing the expanded mapping section.");

        StringAssert.Contains(
            actual, "cloak: $/GnarlyCorp/Web/Drops", "Cloak entries should be listed too.");
        StringAssert.Contains(actual, "map: $/GnarlyCorp/Web", "Map entries should be listed.");
    }

    [TestMethod]
    public void FormatReport_ShowsHowManyBuildsMapEachPath()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual, "### How many builds map each path", "Missing the path frequency table.");
        StringAssert.Contains(
            actual, "| $/GnarlyCorp/Common | 2 ", "The shared path should show its build count.");
    }

    [TestMethod]
    public void FormatReport_NeverRunBuildSaysSo()
    {
        var result = BuildResult();

        result.TfvcBuildDefinitions[1].LastRunDate = null;

        var actual = SystemUnderTest.FormatReport(result);

        StringAssert.Contains(actual, "never run", "A build with no runs should say so.");
    }

    [DataTestMethod]
    [DataRow(512L, "512 bytes")]
    [DataRow(2048L, "2 KB")]
    [DataRow(1572864L, "1.5 MB")]
    [DataRow(3221225472L, "3 GB")]
    public void FormatSize_IsReadable(long bytes, string expected)
    {
        Assert.AreEqual(
            expected, TfvcAssessmentReportFormatter.FormatSize(bytes), "Wrong size formatting.");
    }

    [TestMethod]
    public void FormatReport_IncludesTheLargestFiles()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "## What is stored here", "Missing the content section.");
        StringAssert.Contains(actual, "largest files", "Missing the largest files table.");
        StringAssert.Contains(actual, "Backup.bak", "Missing the largest file.");
        StringAssert.Contains(actual, "300 MB", "Missing the file size.");
    }

    [TestMethod]
    public void FormatReport_IncludesFileTypes()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(actual, "| .dll | 412 ", "Missing the extension row.");
        StringAssert.Contains(actual, "380 MB", "Missing the extension size.");
    }

    [TestMethod]
    public void FormatReport_IncludesGeneratedFolders()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual,
            "### Generated output and downloaded dependencies",
            "Missing the generated folder section.");

        StringAssert.Contains(actual, "| packages | 3847 ", "Missing the packages row.");
        StringAssert.Contains(actual, "1.2 GB", "Missing the packages size.");
        StringAssert.Contains(
            actual, "counted once", "The report should say how the counts avoid overlapping.");
    }

    [TestMethod]
    public void FormatReport_ContentSectionIsAbsentWhenNothingWasRead()
    {
        var result = BuildResult();

        result.Content = new TfvcContentScanResult();

        var actual = SystemUnderTest.FormatReport(result);

        Assert.IsFalse(
            actual.Contains("## What is stored here"),
            "An unread tree should not produce an empty section.");
    }

    [TestMethod]
    public void FormatReport_SaysWhatWasNotCovered()
    {
        var actual = SystemUnderTest.FormatReport(BuildResult());

        StringAssert.Contains(
            actual, "## What this scan did not cover", "Missing the limitations section.");
    }

    [TestMethod]
    public void FormatReport_UsesNoProhibitedLanguage()
    {
        var report = SystemUnderTest.FormatReport(BuildResult());

        AssertNoProhibitedLanguage(report);
    }

    [TestMethod]
    public async Task AnalyzerFindings_UseNoProhibitedLanguage()
    {
        // Runs a real assessment so that every finding string the analyzer can
        // produce is checked, not just the ones in the formatter fixture.
        var client = new FakeTfvcApiClient();

        client.Branches.Add(new TfvcBranchInfo
        {
            Path = "$/App/Main",
            Children = new List<TfvcBranchInfo>
            {
                new() { Path = "$/App/Dev" },
                new() { Path = "$/App/Main/Feature" }
            }
        });

        client.SetChangesets("$/App/Main", FakeTfvcApiClient.Changeset(1, UtcNow.AddDays(-2)));
        client.SetChangesets("$/App/Dev", FakeTfvcApiClient.Changeset(2, UtcNow.AddDays(-3)));
        client.SetChangesets(
            "$/App/Main/Feature", FakeTfvcApiClient.Changeset(3, UtcNow.AddDays(-900)));

        client.SetFullListing(
            "$/App",
            new TfvcItemInfo { Path = "$/App/src/bin/App.dll", Size = 200L * 1024 * 1024 },
            new TfvcItemInfo { Path = "$/App/packages/Foo/Foo.dll", Size = 1024 },
            new TfvcItemInfo { Path = "$/App/src/Program.cs", Size = 900 });

        var buildClient = new FakeBuildDefinitionApiClient();

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            1, "Web-CI", UtcNow.AddDays(-3),
            ("$/App/Web", "map"),
            ("$/Shared/Common", "map")));

        buildClient.Add(FakeBuildDefinitionApiClient.TfvcDefinition(
            2, "Batch-CI", UtcNow.AddDays(-500), ("$/Shared/Common", "map")));

        var analyzer = new TfvcAssessmentAnalyzer(client, buildClient);

        var result = await analyzer.AnalyzeAsync("App", "$/App", UtcNow);

        Assert.IsTrue(result.Findings.Count > 0, "Expected the assessment to produce findings.");

        AssertNoProhibitedLanguage(SystemUnderTest.FormatReport(result));
    }

    private static void AssertNoProhibitedLanguage(string text)
    {
        var scrubbed = text;

        foreach (var exception in AllowedExceptions)
        {
            scrubbed = scrubbed.Replace(
                exception, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var phrase in ProhibitedPhrases)
        {
            Assert.IsFalse(
                scrubbed.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"Report text contains prescriptive language: '{phrase}'.");
        }
    }

    [TestMethod]
    public void FormatFindingsCsv_HasHeaderAndOneRowPerFinding()
    {
        var result = BuildResult();

        var actual = SystemUnderTest.FormatFindingsCsv(result);

        StringAssert.Contains(actual, "Category", "Missing the CSV header.");
        StringAssert.Contains(actual, "Consequence", "Missing the CSV header.");
        StringAssert.Contains(actual, "no common ancestor", "Missing the finding row.");
    }

    [TestMethod]
    public void FormatReport_EmptyAssessmentStillProducesAReport()
    {
        var result = new TfvcAssessmentResult
        {
            ProjectName = "Empty",
            ScopePath = "$/Empty",
            GeneratedUtc = UtcNow
        };

        var actual = SystemUnderTest.FormatReport(result);

        StringAssert.Contains(
            actual,
            "No TFVC folders under $/Empty are registered as branches",
            "An empty assessment should say so plainly.");

        StringAssert.Contains(actual, TfvcAssessmentReportFormatter.FooterLine, "Missing footer.");
    }
}
