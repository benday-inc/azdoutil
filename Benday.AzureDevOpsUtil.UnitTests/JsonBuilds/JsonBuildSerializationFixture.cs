using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Benday.AzureDevOpsUtil.Api.JsonBuilds;

namespace Benday.AzureDevOpsUtil.UnitTests.JsonBuilds;
[TestClass]
public class JsonBuildSerializationFixture
{

    private string GetPathToBuildFile(string sampleBuildFileName)
    {
        var temp = Path.Combine(
            Environment.CurrentDirectory,
            "StarterBuilds",
            sampleBuildFileName);

        return temp;
    }

    [TestMethod]
    public void FindSampleBuildFile()
    {
        // arrange
        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        // act

        // assert
        Assert.IsTrue(File.Exists(pathToSampleBuild), $"build file not found at '{pathToSampleBuild}'");
    }

    [TestMethod]
    public void DeserializeSampleBuild()
    {
        // arrange
        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        // act
        var buildDef = System.Text.Json.JsonSerializer.Deserialize<JsonBuildDefinition>(File.ReadAllText(pathToSampleBuild));

        // assert
        Assert.IsNotNull(buildDef);
    }

    private string FormatJson(string sourceJson)
    {
        var options = new System.Text.Json.JsonSerializerOptions();
        options.WriteIndented = true;

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<object>(sourceJson);

        return System.Text.Json.JsonSerializer.Serialize(deserialized, options);
    }

    [TestMethod]
    public void ReserializeSampleBuild()
    {
        // arrange
        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        var sourceJson = File.ReadAllText(pathToSampleBuild);

        // format the sourceJson
        var expected = FormatJson(sourceJson);


        var buildDefDeserializedFromSource = System.Text.Json.JsonSerializer.Deserialize<JsonBuildDefinition>(sourceJson);

        // act
        // reserialize
        var options = new System.Text.Json.JsonSerializerOptions();
        options.WriteIndented = true;

        var actual = System.Text.Json.JsonSerializer.Serialize(buildDefDeserializedFromSource, options);

        // assert

        // get a temp file path
        var dir = Path.Combine(Path.GetTempPath(), "ReserializeSampleBuild", DateTime.Now.Ticks.ToString());
        Directory.CreateDirectory(dir);

        var expectedPath = Path.Combine(dir, "expected.json");
        var actualPath = Path.Combine(dir, "actual.json");

        File.WriteAllText(expectedPath, expected);
        File.WriteAllText(actualPath, actual);

        Console.WriteLine($"Expected path: {expectedPath}");
        Console.WriteLine($"Actual path: {actualPath}");

        // TODO: Write some kind of file comparison code that checks for close enough equality
        // Assert.AreEqual<string>(expected, actual, $"Json didn't match");
    }

    [TestMethod]
    public void UpdateBuildTemplateForCreateNew()
    {
        // arrange
        var pathToSampleBuild = GetPathToBuildFile("json-build-single-solution-tfvc-2024.json");

        var sourceJson = File.ReadAllText(pathToSampleBuild);

        Console.WriteLine(sourceJson);

        // act


        // assert        

        Assert.IsFalse(sourceJson.Contains("pudding"), "json should not contain string 'pudding'");
        Assert.IsFalse(sourceJson.Contains("benday.com"), "json should not contain string 'benday.com'");
    }
}
