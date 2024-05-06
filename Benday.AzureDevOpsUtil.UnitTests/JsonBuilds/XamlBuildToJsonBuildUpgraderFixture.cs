
using Benday.AzureDevOpsUtil.Api.BuildUpgraders;
using Benday.AzureDevOpsUtil.Api.JsonBuilds;

using System;

using System.Linq;

namespace Benday.AzureDevOpsUtil.UnitTests.JsonBuilds;

[TestClass]
public class XamlBuildToJsonBuildUpgraderFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
     //    _SystemUnderTest = null;
    }

    [TestMethod]
    public void DeserializeXamlBuildInfoDumpFile()
    {
        // arrange
        var pathToSampleFile = @"C:\Users\benday\Downloads\BuildDefinitionListCommand-v2\BuildDefinitionListCommand\HE\HENightlyBuild.json";

        Assert.IsTrue(File.Exists(pathToSampleFile), "File should exist");

        // act
        var infoDump = System.Text.Json.JsonSerializer.Deserialize<XamlBuildDumpInfo>(File.ReadAllText(pathToSampleFile));

        // assert
        Console.WriteLine(infoDump.ProcessParameters);
    }

    [TestMethod]
    public void ProcessParameterCollection_ReadFromDictionary()
    {
        // arrange
        var pathToSampleFile = @"C:\Users\benday\Downloads\BuildDefinitionListCommand-v2\BuildDefinitionListCommand\HE\HENightlyBuild.json";

        Assert.IsTrue(File.Exists(pathToSampleFile), "File should exist");

        var infoDump = System.Text.Json.JsonSerializer.Deserialize<XamlBuildDumpInfo>(File.ReadAllText(pathToSampleFile));

        // act


        // assert
        Console.WriteLine(infoDump.ProcessParameters);
    }




    /*
    private XamlBuildToJsonBuildUpgrader? _SystemUnderTest;

    private XamlBuildToJsonBuildUpgrader SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new XamlBuildToJsonBuildUpgrader();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void UpdateBuildTemplateForCreateNew()
    {
        // arrange
        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        var sourceJson = File.ReadAllText(pathToSampleBuild);

        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        var sourceJson = File.ReadAllText(pathToSampleBuild);

        // format the sourceJson
        var expected = FormatJson(sourceJson);


        var buildDefDeserializedFromSource = System.Text.Json.JsonSerializer.Deserialize<JsonBuildDefinition>(sourceJson);

        // act


        // assert        
    }
    */
}
