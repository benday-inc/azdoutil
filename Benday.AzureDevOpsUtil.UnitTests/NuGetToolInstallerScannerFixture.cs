using Benday.AzureDevOpsUtil.Api.NuGetTasks;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class NuGetToolInstallerScannerFixture
{
    private const string NuGetToolInstallerTaskId = NuGetToolInstallerScanner.NuGetToolInstallerTaskId;

    private static string BuildDefinitionWithTwoInstallerSteps()
    {
        return $$"""
        {
          "id": 42,
          "name": "Sample-CI",
          "revision": 7,
          "process": {
            "phases": [
              {
                "name": "Agent job 1",
                "steps": [
                  {
                    "displayName": "Use NuGet 4.4.1",
                    "enabled": true,
                    "task": {
                      "id": "{{NuGetToolInstallerTaskId}}",
                      "versionSpec": "0.*",
                      "definitionType": "task"
                    },
                    "inputs": {
                      "versionSpec": "4.4.1",
                      "checkLatest": "false"
                    }
                  },
                  {
                    "displayName": "Build solution",
                    "enabled": true,
                    "task": {
                      "id": "71a9a2d3-a98a-4caa-96ab-affca411ecda",
                      "versionSpec": "1.*",
                      "definitionType": "task"
                    },
                    "inputs": { }
                  }
                ]
              },
              {
                "name": "Agent job 2",
                "steps": [
                  {
                    "displayName": "Use NuGet 5.x",
                    "enabled": false,
                    "task": {
                      "id": "{{NuGetToolInstallerTaskId.ToUpperInvariant()}}",
                      "versionSpec": "1.*",
                      "definitionType": "task"
                    },
                    "inputs": {
                      "versionSpec": "5.x"
                    }
                  }
                ]
              }
            ],
            "type": 1
          }
        }
        """;
    }

    [TestMethod]
    public void FindReferences_FindsInstallerStepsAcrossPhases()
    {
        // arrange
        var json = BuildDefinitionWithTwoInstallerSteps();

        // act
        var actual = NuGetToolInstallerScanner.FindReferences(json);

        // assert
        Assert.AreEqual<int>(2, actual.Count, "reference count");

        var first = actual[0];
        Assert.AreEqual<string>("Agent job 1", first.PhaseName, "first phase name");
        Assert.AreEqual<int>(0, first.PhaseIndex, "first phase index");
        Assert.AreEqual<int>(0, first.StepIndex, "first step index");
        Assert.AreEqual<string>("Use NuGet 4.4.1", first.StepDisplayName, "first display name");
        Assert.AreEqual<bool>(true, first.Enabled, "first enabled");
        Assert.AreEqual<string>("0.*", first.TaskVersionSpec, "first task version spec");
        Assert.AreEqual<string>("4.4.1", first.NuGetVersionSpec, "first nuget version spec");
        Assert.AreEqual<string>("false", first.CheckLatest, "first check latest");

        var second = actual[1];
        Assert.AreEqual<string>("Agent job 2", second.PhaseName, "second phase name");
        Assert.AreEqual<bool>(false, second.Enabled, "second enabled -- matched despite uppercased task id");
        Assert.AreEqual<string>("1.*", second.TaskVersionSpec, "second task version spec");
        Assert.AreEqual<string>("5.x", second.NuGetVersionSpec, "second nuget version spec");
        Assert.AreEqual<string>(string.Empty, second.CheckLatest, "second check latest defaults to empty");
    }

    [TestMethod]
    public void FindReferences_NoInstallerSteps_ReturnsEmpty()
    {
        // arrange
        var json = """
        {
          "id": 42,
          "process": {
            "phases": [
              {
                "name": "Agent job 1",
                "steps": [
                  {
                    "displayName": "Build solution",
                    "task": { "id": "71a9a2d3-a98a-4caa-96ab-affca411ecda", "versionSpec": "1.*", "definitionType": "task" }
                  }
                ]
              }
            ]
          }
        }
        """;

        // act
        var actual = NuGetToolInstallerScanner.FindReferences(json);

        // assert
        Assert.AreEqual<int>(0, actual.Count, "reference count");
    }

    [TestMethod]
    public void FindReferences_NullOrEmptyJson_ReturnsEmpty()
    {
        // act & assert
        Assert.AreEqual<int>(0, NuGetToolInstallerScanner.FindReferences(null).Count, "null");
        Assert.AreEqual<int>(0, NuGetToolInstallerScanner.FindReferences(string.Empty).Count, "empty");
    }

    [TestMethod]
    public void FindReferences_YamlDefinitionWithoutPhases_ReturnsEmpty()
    {
        // arrange
        var json = """
        {
          "id": 42,
          "process": { "yamlFilename": "azure-pipelines.yml", "type": 2 }
        }
        """;

        // act
        var actual = NuGetToolInstallerScanner.FindReferences(json);

        // assert
        Assert.AreEqual<int>(0, actual.Count, "reference count");
    }
}
