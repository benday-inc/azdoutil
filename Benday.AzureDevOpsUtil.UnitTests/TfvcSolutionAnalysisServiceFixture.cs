using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class TfvcSolutionAnalysisServiceFixture
{
    private TfvcSolutionAnalysisService SystemUnderTest => new();

    private const string ProjectName = "GnarlyCorp";

    private static TfvcItemInfo File(string path)
    {
        return new TfvcItemInfo { Path = path, Size = 100 };
    }

    /// <summary>
    /// A solution file listing the given project paths, in the format Visual
    /// Studio writes.
    /// </summary>
    private static string Solution(params string[] relativeProjectPaths)
    {
        var lines = relativeProjectPaths.Select(x =>
            "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"" +
            System.IO.Path.GetFileNameWithoutExtension(x) + "\", \"" + x + "\", " +
            "\"{11111111-2222-3333-4444-555555555555}\"");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// A project file in the format used before the SDK-style projects, which
    /// is what anything coming out of TFVC looks like.
    /// </summary>
    private static string LegacyProject(params string[] relativeProjectReferences)
    {
        var references = string.Join(Environment.NewLine, relativeProjectReferences.Select(x =>
            $"    <ProjectReference Include=\"{x}\" />"));

        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
            "<Project ToolsVersion=\"12.0\" " +
            "xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">" +
            Environment.NewLine +
            "  <PropertyGroup>" + Environment.NewLine +
            "    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>" + Environment.NewLine +
            "  </PropertyGroup>" + Environment.NewLine +
            "  <ItemGroup>" + Environment.NewLine +
            references + Environment.NewLine +
            "  </ItemGroup>" + Environment.NewLine +
            "</Project>";
    }

    [TestMethod]
    public async Task Analyze_ReadsSolutionsAndResolvesProjectPaths()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent(
            "$/App/Main/Web/Web.sln", Solution(@"Web.Site\Web.Site.csproj"));

        client.SetFileContent("$/App/Main/Web/Web.Site/Web.Site.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Main/Web/Web.sln"),
            File("$/App/Main/Web/Web.Site/Web.Site.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        var solution = actual.Solutions.Single();

        Assert.AreEqual("$/App/Main/Web", solution.RootFolder, "Wrong solution root.");
        Assert.AreEqual(
            "$/App/Main/Web/Web.Site/Web.Site.csproj",
            solution.ProjectPaths.Single(),
            "The relative project path should resolve against the solution folder.");
    }

    [TestMethod]
    public async Task Analyze_ReadsLegacyNamespacedProjectFiles()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent(
            "$/App/Main/Web/Web.Site.csproj", LegacyProject(@"..\Common\Common.csproj"));

        var items = new[] { File("$/App/Main/Web/Web.Site.csproj") };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        var project = actual.Projects.Single();

        Assert.AreEqual(
            "$/App/Main/Common/Common.csproj",
            project.ProjectReferences.Single(),
            "A project reference in a namespaced project file should still be found.");
    }

    [TestMethod]
    public async Task Analyze_FindsReferencesThatLeaveTheSolutionFolder()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Main/Web/Web.sln", Solution(@"Web.Site.csproj"));
        client.SetFileContent(
            "$/App/Main/Web/Web.Site.csproj", LegacyProject(@"..\..\Shared\Common.csproj"));
        client.SetFileContent("$/App/Shared/Common.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Main/Web/Web.sln"),
            File("$/App/Main/Web/Web.Site.csproj"),
            File("$/App/Shared/Common.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        var reference = actual.CrossSolutionReferences.Single();

        Assert.AreEqual(
            "$/App/Main/Web/Web.Site.csproj", reference.FromProject, "Wrong source project.");
        Assert.AreEqual(
            "$/App/Shared/Common.csproj", reference.ToProject, "Wrong referenced project.");
    }

    [TestMethod]
    public async Task Analyze_ReferenceInsideTheSolutionFolderIsNotFlagged()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Web/Web.sln", Solution(@"Site\Site.csproj"));
        client.SetFileContent(
            "$/App/Web/Site/Site.csproj", LegacyProject(@"..\Core\Core.csproj"));
        client.SetFileContent("$/App/Web/Core/Core.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Web/Web.sln"),
            File("$/App/Web/Site/Site.csproj"),
            File("$/App/Web/Core/Core.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        Assert.AreEqual(
            0,
            actual.CrossSolutionReferences.Count,
            "A reference that stays inside the solution folder is not a problem.");
    }

    [TestMethod]
    public async Task Analyze_FindsProjectsSharedBySeveralSolutions()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Web/Web.sln", Solution(@"..\Common\Common.csproj"));
        client.SetFileContent("$/App/Api/Api.sln", Solution(@"..\Common\Common.csproj"));
        client.SetFileContent("$/App/Batch/Batch.sln", Solution(@"..\Common\Common.csproj"));
        client.SetFileContent("$/App/Common/Common.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Web/Web.sln"),
            File("$/App/Api/Api.sln"),
            File("$/App/Batch/Batch.sln"),
            File("$/App/Common/Common.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        var shared = actual.SharedProjects.Single();

        Assert.AreEqual("$/App/Common/Common.csproj", shared.ProjectPath, "Wrong shared project.");
        Assert.AreEqual(3, shared.SolutionCount, "Wrong solution count.");
    }

    [TestMethod]
    public async Task Analyze_SharedProjectDetectionFollowsIndirectReferences()
    {
        // Common is not listed in either solution; it is pulled in through
        // another project, which couples the solutions just as firmly.
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Web/Web.sln", Solution(@"Site.csproj"));
        client.SetFileContent("$/App/Api/Api.sln", Solution(@"Service.csproj"));

        client.SetFileContent(
            "$/App/Web/Site.csproj", LegacyProject(@"..\Common\Common.csproj"));
        client.SetFileContent(
            "$/App/Api/Service.csproj", LegacyProject(@"..\Common\Common.csproj"));
        client.SetFileContent("$/App/Common/Common.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Web/Web.sln"),
            File("$/App/Api/Api.sln"),
            File("$/App/Web/Site.csproj"),
            File("$/App/Api/Service.csproj"),
            File("$/App/Common/Common.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        var shared = actual.SharedProjects.Single(
            x => x.ProjectPath == "$/App/Common/Common.csproj");

        Assert.AreEqual(2, shared.SolutionCount, "Both solutions reach this project.");
    }

    [TestMethod]
    public async Task Analyze_ProjectInOneSolutionIsNotShared()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Web/Web.sln", Solution(@"Site.csproj"));
        client.SetFileContent("$/App/Web/Site.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Web/Web.sln"),
            File("$/App/Web/Site.csproj")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        Assert.AreEqual(0, actual.SharedProjects.Count, "One solution is not sharing.");
    }

    [TestMethod]
    public async Task Analyze_DetectsPackagesConfigInTheProjectFolder()
    {
        var client = new FakeTfvcApiClient();

        client.SetFileContent("$/App/Web/Site.csproj", LegacyProject());

        var items = new[]
        {
            File("$/App/Web/Site.csproj"),
            File("$/App/Web/packages.config")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        Assert.IsTrue(
            actual.Projects.Single().UsesPackagesConfig,
            "A packages.config beside the project should be noticed.");
        Assert.AreEqual(1, actual.ProjectsUsingPackagesConfig, "Wrong rollup count.");
    }

    [TestMethod]
    public async Task Analyze_RecordsFilesItCouldNotRead()
    {
        var client = new FakeTfvcApiClient();

        var items = new[] { File("$/App/Web/Web.sln") };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        CollectionAssert.AreEqual(
            new[] { "$/App/Web/Web.sln" },
            actual.UnreadableFiles,
            "An unreadable file should be named rather than dropped.");
    }

    [TestMethod]
    public async Task Analyze_IgnoresFilesThatAreNotSolutionsOrProjects()
    {
        var client = new FakeTfvcApiClient();

        var items = new[]
        {
            File("$/App/Web/readme.md"),
            File("$/App/Web/Site.dll")
        };

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, items);

        Assert.AreEqual(0, actual.Solutions.Count, "No solutions here.");
        Assert.AreEqual(0, actual.Projects.Count, "No projects here.");
        Assert.AreEqual(
            0, client.FileContentRequests.Count, "Nothing should have been fetched.");
    }

    [TestMethod]
    public async Task Analyze_EmptyInput()
    {
        var client = new FakeTfvcApiClient();

        var actual = await SystemUnderTest.AnalyzeAsync(client, ProjectName, null);

        Assert.AreEqual(0, actual.Solutions.Count, "Nothing to analyze.");
    }
}
