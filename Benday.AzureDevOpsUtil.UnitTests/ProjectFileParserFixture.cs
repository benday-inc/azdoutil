using Benday.AzureDevOpsUtil.Api.BuildReadiness;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class ProjectFileParserFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private ProjectFileParser? _SystemUnderTest;

    private ProjectFileParser SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new ProjectFileParser();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void ParseCsproj_PackageReferences()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(2, actual.PackageReferences.Count, "Should have 2 package references.");
        Assert.AreEqual<string>("Newtonsoft.Json", actual.PackageReferences[0].Name, "First package name was wrong.");
        Assert.AreEqual<string>("13.0.3", actual.PackageReferences[0].Version, "First package version was wrong.");
        Assert.AreEqual<string>("Serilog", actual.PackageReferences[1].Name, "Second package name was wrong.");
        Assert.AreEqual<string>("3.1.1", actual.PackageReferences[1].Version, "Second package version was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_PackageReferenceWithChildVersionElement()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"">
      <Version>13.0.3</Version>
    </PackageReference>
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(1, actual.PackageReferences.Count, "Should have 1 package reference.");
        Assert.AreEqual<string>("13.0.3", actual.PackageReferences[0].Version, "Version from child element was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_ProjectReferences()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Api\Api.csproj"" />
    <ProjectReference Include=""..\Common\Common.csproj"" />
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/Web/Web.csproj");

        // assert
        Assert.AreEqual<int>(2, actual.ExternalReferences.Count, "Should have 2 external references.");
        Assert.AreEqual<string>("ProjectReference", actual.ExternalReferences[0].ReferenceType,
            "First reference type was wrong.");
        Assert.AreEqual<string>("../Api/Api.csproj", actual.ExternalReferences[0].Path,
            "First reference path was wrong.");
        Assert.AreEqual<string>("../Common/Common.csproj", actual.ExternalReferences[1].Path,
            "Second reference path was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_HintPaths()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""ThirdParty.Lib"">
      <HintPath>..\packages\ThirdParty.Lib.1.0\lib\net472\ThirdParty.Lib.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(1, actual.ExternalReferences.Count, "Should have 1 external reference.");
        Assert.AreEqual<string>("HintPath", actual.ExternalReferences[0].ReferenceType,
            "Reference type was wrong.");
        Assert.AreEqual<string>("../packages/ThirdParty.Lib.1.0/lib/net472/ThirdParty.Lib.dll",
            actual.ExternalReferences[0].Path, "HintPath was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_DetectsHardcodedWindowsPaths()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputPath>C:\Builds\Output</OutputPath>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.IsTrue(actual.HardcodedPaths.Count > 0, "Should detect hardcoded Windows path.");
        Assert.IsTrue(actual.HardcodedPaths.Any(p => p.Contains(@"C:\Builds\Output")),
            "Should contain the hardcoded output path.");
    }

    [TestMethod]
    public void ParseCsproj_DetectsHardcodedUnixPaths()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputPath>/Users/developer/builds/output</OutputPath>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.IsTrue(actual.HardcodedPaths.Count > 0, "Should detect hardcoded Unix path.");
        Assert.IsTrue(actual.HardcodedPaths.Any(p => p.Contains("/Users/developer/builds/output")),
            "Should contain the hardcoded Unix path.");
    }

    [TestMethod]
    public void ParseCsproj_TargetFramework_Single()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(1, actual.TargetFrameworks.Count, "Should have 1 target framework.");
        Assert.AreEqual<string>("net8.0", actual.TargetFrameworks[0], "Target framework was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_TargetFrameworks_Multiple()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(3, actual.TargetFrameworks.Count, "Should have 3 target frameworks.");
        Assert.AreEqual<string>("net8.0", actual.TargetFrameworks[0], "First TFM was wrong.");
        Assert.AreEqual<string>("net9.0", actual.TargetFrameworks[1], "Second TFM was wrong.");
        Assert.AreEqual<string>("net10.0", actual.TargetFrameworks[2], "Third TFM was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_NuGetStyle_PackageReference()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<string>("PackageReference", actual.NuGetManagementStyle,
            "NuGet style should be PackageReference.");
    }

    [TestMethod]
    public void ParseCsproj_NuGetStyle_PackagesConfig()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj",
            hasPackagesConfig: true);

        // assert
        Assert.AreEqual<string>("PackagesConfig", actual.NuGetManagementStyle,
            "NuGet style should be PackagesConfig.");
    }

    [TestMethod]
    public void ParseCsproj_NuGetStyle_Mixed()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj",
            hasPackagesConfig: true);

        // assert
        Assert.AreEqual<string>("Mixed", actual.NuGetManagementStyle,
            "NuGet style should be Mixed when both PackageReference and packages.config exist.");
    }

    [TestMethod]
    public void ParseCsproj_NuGetStyle_None()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<string>("None", actual.NuGetManagementStyle,
            "NuGet style should be None when no packages.");
    }

    [TestMethod]
    public void ParseCsproj_ProjectType()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<string>("csproj", actual.ProjectType, "Project type was wrong.");
    }

    [TestMethod]
    public void ParseVbproj_Works()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.vbproj");

        // assert
        Assert.AreEqual<string>("vbproj", actual.ProjectType, "Project type was wrong.");
        Assert.AreEqual<int>(1, actual.PackageReferences.Count, "Should have 1 package reference.");
    }

    [TestMethod]
    public void ParseCsproj_FileName()
    {
        // arrange
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<string>("MyProject.csproj", actual.FileName, "FileName was wrong.");
        Assert.AreEqual<string>("/src/MyProject/MyProject.csproj", actual.Path, "Path was wrong.");
    }

    [TestMethod]
    public void ParseCsproj_EmptyContent()
    {
        // arrange
        var content = string.Empty;

        // act
        var actual = SystemUnderTest.ParseProjectFile(content, "/src/MyProject/MyProject.csproj");

        // assert
        Assert.AreEqual<int>(0, actual.PackageReferences.Count, "Should have no package references.");
        Assert.AreEqual<int>(0, actual.ExternalReferences.Count, "Should have no external references.");
        Assert.AreEqual<string>("None", actual.NuGetManagementStyle, "NuGet style should be None.");
    }
}
