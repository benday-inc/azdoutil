using Benday.AzureDevOpsUtil.Api.BuildReadiness;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class SolutionFileParserFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private SolutionFileParser? _SystemUnderTest;

    private SolutionFileParser SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new SolutionFileParser();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void ParseSln_SingleProject()
    {
        // arrange
        var content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""src\MyProject\MyProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject
Global
EndGlobal
";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(1, actual.Count, "Should have 1 project entry.");
        Assert.AreEqual<string>("MyProject", actual[0].Name, "Name was wrong.");
        Assert.AreEqual<string>("src/MyProject/MyProject.csproj", actual[0].RelativePath, "Path was wrong.");
    }

    [TestMethod]
    public void ParseSln_MultipleProjects()
    {
        // arrange
        var content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Api"", ""src\Api\Api.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Web"", ""src\Web\Web.csproj"", ""{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Tests"", ""test\Tests\Tests.csproj"", ""{CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC}""
EndProject
Global
EndGlobal
";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(3, actual.Count, "Should have 3 project entries.");
        Assert.AreEqual<string>("Api", actual[0].Name, "First project name was wrong.");
        Assert.AreEqual<string>("Web", actual[1].Name, "Second project name was wrong.");
        Assert.AreEqual<string>("Tests", actual[2].Name, "Third project name was wrong.");
    }

    [TestMethod]
    public void ParseSln_FiltersSolutionFolders()
    {
        // arrange
        var content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""src\MyProject\MyProject.csproj"", ""{EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE}""
EndProject
Global
EndGlobal
";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(1, actual.Count, "Should have 1 project entry, solution folder should be excluded.");
        Assert.AreEqual<string>("MyProject", actual[0].Name, "Name was wrong.");
    }

    [TestMethod]
    public void ParseSln_HandlesBackslashPaths()
    {
        // arrange
        var content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""src\sub1\sub2\MyProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject
Global
EndGlobal
";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(1, actual.Count, "Should have 1 project entry.");
        Assert.AreEqual<string>("src/sub1/sub2/MyProject.csproj", actual[0].RelativePath,
            "Backslashes should be normalized to forward slashes.");
    }

    [TestMethod]
    public void ParseSln_EmptyFile()
    {
        // arrange
        var content = string.Empty;

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(0, actual.Count, "Empty content should return empty list.");
    }

    [TestMethod]
    public void ParseSln_NullContent()
    {
        // arrange
        string content = null!;

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(0, actual.Count, "Null content should return empty list.");
    }

    [TestMethod]
    public void ParseSlnx_XmlFormat()
    {
        // arrange
        var content = @"<Solution>
  <Project Path=""src/Api/Api.csproj"" />
  <Project Path=""src/Web/Web.csproj"" />
  <Project Path=""test/Tests/Tests.csproj"" />
</Solution>";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content, isSlnx: true);

        // assert
        Assert.AreEqual<int>(3, actual.Count, "Should have 3 project entries.");
        Assert.AreEqual<string>("Api", actual[0].Name, "First project name was wrong.");
        Assert.AreEqual<string>("src/Api/Api.csproj", actual[0].RelativePath, "First project path was wrong.");
        Assert.AreEqual<string>("Web", actual[1].Name, "Second project name was wrong.");
        Assert.AreEqual<string>("Tests", actual[2].Name, "Third project name was wrong.");
    }

    [TestMethod]
    public void ParseSlnx_EmptyXml()
    {
        // arrange
        var content = @"<Solution></Solution>";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content, isSlnx: true);

        // assert
        Assert.AreEqual<int>(0, actual.Count, "Empty solution should return empty list.");
    }

    [TestMethod]
    public void ParseSln_ProjectOutsideSolutionRoot()
    {
        // arrange
        var content = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""src\MyProject\MyProject.csproj"", ""{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""SharedLib"", ""..\..\SharedLib\SharedLib.csproj"", ""{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}""
EndProject
Global
EndGlobal
";

        // act
        var actual = SystemUnderTest.ParseSolutionFile(content);

        // assert
        Assert.AreEqual<int>(2, actual.Count, "Should have 2 project entries.");
        Assert.AreEqual<string>("../../SharedLib/SharedLib.csproj", actual[1].RelativePath,
            "Path with .. should be preserved as-is for violation detection.");
    }
}
