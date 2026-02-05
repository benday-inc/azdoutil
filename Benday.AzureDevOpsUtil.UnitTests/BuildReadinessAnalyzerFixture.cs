using Benday.AzureDevOpsUtil.Api.BuildReadiness;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BuildReadinessAnalyzerFixture
{
    [TestMethod]
    public async Task Analyze_EmptyRepo()
    {
        // arrange
        var filePaths = new List<string>();
        var provider = new InMemoryFileContentProvider(new Dictionary<string, string>());
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasError, "Should have error for empty repo.");
        Assert.AreEqual<string>("MyProject", actual.ProjectName, "Project name was wrong.");
        Assert.AreEqual<string>("MyRepo", actual.RepositoryName, "Repository name was wrong.");
    }

    [TestMethod]
    public async Task Analyze_DetectsSubmodules()
    {
        // arrange
        var filePaths = new List<string> { "/.gitmodules", "/src/readme.md" };
        var provider = new InMemoryFileContentProvider(new Dictionary<string, string>());
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasSubmodules, "Should detect submodules.");
    }

    [TestMethod]
    public async Task Analyze_NoSubmodules()
    {
        // arrange
        var filePaths = new List<string> { "/src/readme.md" };
        var provider = new InMemoryFileContentProvider(new Dictionary<string, string>());
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsFalse(actual.HasSubmodules, "Should not detect submodules.");
    }

    [TestMethod]
    public async Task Analyze_DetectsBuildConfigFiles()
    {
        // arrange
        var filePaths = new List<string>
        {
            "/nuget.config",
            "/Directory.Build.props",
            "/Directory.Build.targets",
            "/global.json",
            "/src/MyProject/MyProject.csproj"
        };
        var provider = new InMemoryFileContentProvider(new Dictionary<string, string>());
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.AreEqual<int>(4, actual.BuildConfigFiles.Count, "Should detect 4 build config files.");
    }

    [TestMethod]
    public async Task Analyze_DetectsPackagesConfig()
    {
        // arrange
        var filePaths = new List<string>
        {
            "/src/MyProject/packages.config",
            "/src/MyProject/MyProject.csproj"
        };
        var provider = new InMemoryFileContentProvider(new Dictionary<string, string>());
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasPackagesConfig, "Should detect packages.config.");
    }

    [TestMethod]
    public async Task Analyze_SolutionWithNoRootViolations()
    {
        // arrange
        var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Api"", ""src\Api\Api.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Tests"", ""test\Tests\Tests.csproj"", ""{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}""
EndProject
Global
EndGlobal
";

        var apiCsprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        var testCsprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\Api\Api.csproj"" />
  </ItemGroup>
</Project>";

        var filePaths = new List<string>
        {
            "/MySolution.sln",
            "/src/Api/Api.csproj",
            "/test/Tests/Tests.csproj"
        };

        var files = new Dictionary<string, string>
        {
            { "/MySolution.sln", slnContent },
            { "/src/Api/Api.csproj", apiCsprojContent },
            { "/test/Tests/Tests.csproj", testCsprojContent }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.AreEqual<int>(1, actual.SolutionCount, "Should have 1 solution.");
        Assert.AreEqual<int>(2, actual.ProjectFileCount, "Should have 2 project files.");
        Assert.IsFalse(actual.HasSolutionRootViolations, "Should have no solution root violations.");
    }

    [TestMethod]
    public async Task Analyze_DetectsSolutionRootViolation()
    {
        // arrange
        var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Api"", ""src\Api\Api.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""SharedLib"", ""..\..\SharedLib\SharedLib.csproj"", ""{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}""
EndProject
Global
EndGlobal
";

        var filePaths = new List<string>
        {
            "/repos/main/MySolution.sln",
            "/repos/main/src/Api/Api.csproj"
        };

        var files = new Dictionary<string, string>
        {
            { "/repos/main/MySolution.sln", slnContent }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasSolutionRootViolations, "Should detect solution root violation.");
        Assert.AreEqual<int>(1, actual.Solutions[0].SolutionRootViolations.Count,
            "Should have 1 violation.");
        Assert.AreEqual<string>("../../SharedLib/SharedLib.csproj",
            actual.Solutions[0].SolutionRootViolations[0],
            "Violation path was wrong.");
    }

    [TestMethod]
    public async Task Analyze_AggregatesDistinctPackages()
    {
        // arrange
        var csproj1 = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";

        var csproj2 = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""xunit"" Version=""2.9.0"" />
  </ItemGroup>
</Project>";

        var filePaths = new List<string>
        {
            "/src/Api/Api.csproj",
            "/test/Tests/Tests.csproj"
        };

        var files = new Dictionary<string, string>
        {
            { "/src/Api/Api.csproj", csproj1 },
            { "/test/Tests/Tests.csproj", csproj2 }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.AreEqual<int>(3, actual.AllDistinctPackageReferences.Count,
            "Should have 3 distinct packages (Newtonsoft.Json, Serilog, xunit).");
    }

    [TestMethod]
    public async Task Analyze_DetectsExternalHintPathOutsideSolutionRoot()
    {
        // arrange
        var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""src\MyProject\MyProject.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Global
EndGlobal
";

        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""ThirdParty"">
      <HintPath>..\..\external\ThirdParty.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>";

        var filePaths = new List<string>
        {
            "/repo/MySolution.sln",
            "/repo/src/MyProject/MyProject.csproj"
        };

        var files = new Dictionary<string, string>
        {
            { "/repo/MySolution.sln", slnContent },
            { "/repo/src/MyProject/MyProject.csproj", csprojContent }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasExternalReferences,
            "Should detect external references (HintPath).");
        Assert.AreEqual<int>(1, actual.ProjectFiles[0].ExternalReferences.Count,
            "Should have 1 external reference.");
        Assert.AreEqual<string>("HintPath",
            actual.ProjectFiles[0].ExternalReferences[0].ReferenceType,
            "Reference type was wrong.");
    }

    [TestMethod]
    public async Task Analyze_MultiSolution()
    {
        // arrange
        var sln1Content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Api"", ""src\Api\Api.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Global
EndGlobal
";

        var sln2Content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Web"", ""src\Web\Web.csproj"", ""{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}""
EndProject
Global
EndGlobal
";

        var filePaths = new List<string>
        {
            "/Api.sln",
            "/Web.sln",
            "/src/Api/Api.csproj",
            "/src/Web/Web.csproj"
        };

        var files = new Dictionary<string, string>
        {
            { "/Api.sln", sln1Content },
            { "/Web.sln", sln2Content }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.AreEqual<int>(2, actual.SolutionCount, "Should have 2 solutions.");
    }

    [TestMethod]
    public async Task Analyze_DetectsPackageReferenceStyle()
    {
        // arrange
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        var filePaths = new List<string> { "/src/MyProject/MyProject.csproj" };

        var files = new Dictionary<string, string>
        {
            { "/src/MyProject/MyProject.csproj", csprojContent }
        };

        var provider = new InMemoryFileContentProvider(files);
        var sut = new BuildReadinessAnalyzer(filePaths, provider);

        // act
        var actual = await sut.AnalyzeAsync("MyProject", "MyRepo", "repo-id-1");

        // assert
        Assert.IsTrue(actual.HasPackageReference, "Should detect PackageReference style.");
        Assert.IsFalse(actual.HasPackagesConfig, "Should not detect packages.config.");
    }
}

public class InMemoryFileContentProvider : IFileContentProvider
{
    private readonly Dictionary<string, string> _files;

    public InMemoryFileContentProvider(Dictionary<string, string> files)
    {
        _files = files;
    }

    public Task<string?> GetFileContentAsync(string filePath)
    {
        _files.TryGetValue(filePath, out var content);
        return Task.FromResult(content);
    }
}
