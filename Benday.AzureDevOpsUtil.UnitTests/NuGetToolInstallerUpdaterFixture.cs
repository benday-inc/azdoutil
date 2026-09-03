using System.Text.Json.Nodes;

using Benday.AzureDevOpsUtil.Api.NuGetTasks;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class NuGetToolInstallerUpdaterFixture
{
    private const string NuGetToolInstallerTaskId = NuGetToolInstallerScanner.NuGetToolInstallerTaskId;

    private static JsonNode BuildDefinitionWithInstallerAndOtherStep()
    {
        return JsonNode.Parse($$"""
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
                    "inputs": { "solution": "**/*.sln" }
                  }
                ]
              }
            ],
            "type": 1
          }
        }
        """)!;
    }

    [TestMethod]
    public void Update_ChangesTaskVersionNuGetVersionAndDisplayName()
    {
        // arrange
        var definition = BuildDefinitionWithInstallerAndOtherStep();
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.Update(definition);

        // assert
        Assert.AreEqual<int>(1, actual.UpdatedStepCount, "updated step count");

        var change = actual.Changes[0];
        Assert.AreEqual<string>("Agent job 1", change.PhaseName, "phase name");
        Assert.AreEqual<int>(0, change.StepIndex, "step index");
        Assert.AreEqual<string>("0.*", change.OldTaskVersionSpec, "old task version");
        Assert.AreEqual<string>("1.*", change.NewTaskVersionSpec, "new task version");
        Assert.AreEqual<string>("4.4.1", change.OldNuGetVersionSpec, "old nuget version");
        Assert.AreEqual<string>("7.9.x", change.NewNuGetVersionSpec, "new nuget version");
        Assert.AreEqual<string>("Use NuGet 4.4.1", change.OldDisplayName, "old display name");
        Assert.AreEqual<string>("Use NuGet 7.9.x", change.NewDisplayName, "new display name");

        var step = definition["process"]!["phases"]![0]!["steps"]![0]!;
        Assert.AreEqual<string>("1.*", step["task"]!["versionSpec"]!.GetValue<string>(), "task versionSpec in json");
        Assert.AreEqual<string>("7.9.x", step["inputs"]!["versionSpec"]!.GetValue<string>(), "inputs versionSpec in json");
        Assert.AreEqual<string>("Use NuGet 7.9.x", step["displayName"]!.GetValue<string>(), "displayName in json");
        Assert.AreEqual<string>("false", step["inputs"]!["checkLatest"]!.GetValue<string>(),
            "checkLatest input is left alone");
    }

    [TestMethod]
    public void Update_LeavesOtherStepsAlone()
    {
        // arrange
        var definition = BuildDefinitionWithInstallerAndOtherStep();
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        systemUnderTest.Update(definition);

        // assert
        var otherStep = definition["process"]!["phases"]![0]!["steps"]![1]!;
        Assert.AreEqual<string>("Build solution", otherStep["displayName"]!.GetValue<string>(),
            "other step display name");
        Assert.AreEqual<string>("1.*", otherStep["task"]!["versionSpec"]!.GetValue<string>(),
            "other step task version");
        Assert.AreEqual<string>("**/*.sln", otherStep["inputs"]!["solution"]!.GetValue<string>(),
            "other step inputs");
    }

    [TestMethod]
    public void Update_NoInstallerSteps_ReturnsZeroChanges()
    {
        // arrange
        var definition = JsonNode.Parse("""
        {
          "id": 1,
          "process": {
            "phases": [
              { "name": "Agent job 1", "steps": [] }
            ]
          }
        }
        """)!;
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.Update(definition);

        // assert
        Assert.AreEqual<int>(0, actual.UpdatedStepCount, "updated step count");
    }

    [TestMethod]
    public void Update_StepWithoutInputs_CreatesInputsObject()
    {
        // arrange
        var definition = JsonNode.Parse($$"""
        {
          "id": 1,
          "process": {
            "phases": [
              {
                "name": "Agent job 1",
                "steps": [
                  {
                    "displayName": "Use NuGet",
                    "task": {
                      "id": "{{NuGetToolInstallerTaskId}}",
                      "versionSpec": "0.*",
                      "definitionType": "task"
                    }
                  }
                ]
              }
            ]
          }
        }
        """)!;
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "6.4.0");

        // act
        var actual = systemUnderTest.Update(definition);

        // assert
        Assert.AreEqual<int>(1, actual.UpdatedStepCount, "updated step count");

        var step = definition["process"]!["phases"]![0]!["steps"]![0]!;
        Assert.AreEqual<string>("6.4.0", step["inputs"]!["versionSpec"]!.GetValue<string>(),
            "inputs versionSpec was created");
        Assert.AreEqual<string>("Use NuGet 6.4.0", step["displayName"]!.GetValue<string>(),
            "display name");
    }

    private static NuGetToolInstallerReference Reference(
        string taskVersionSpec, string nugetVersionSpec, string displayName)
    {
        return new NuGetToolInstallerReference
        {
            TaskVersionSpec = taskVersionSpec,
            NuGetVersionSpec = nugetVersionSpec,
            StepDisplayName = displayName
        };
    }

    [TestMethod]
    public void IsOutOfSpec_EverythingAlreadyMatches_ReturnsFalse()
    {
        // arrange
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.IsOutOfSpec(Reference("1.*", "7.9.x", "Use NuGet 7.9.x"));

        // assert
        Assert.IsFalse(actual,
            "A step that already has every value the updater would write is in spec.");
    }

    [TestMethod]
    public void IsOutOfSpec_TaskVersionDiffers_ReturnsTrue()
    {
        // arrange
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.IsOutOfSpec(Reference("0.*", "7.9.x", "Use NuGet 7.9.x"));

        // assert
        Assert.IsTrue(actual, "task version spec differs");
    }

    [TestMethod]
    public void IsOutOfSpec_NuGetVersionDiffers_ReturnsTrue()
    {
        // arrange
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.IsOutOfSpec(Reference("1.*", "4.4.1", "Use NuGet 7.9.x"));

        // assert
        Assert.IsTrue(actual, "nuget version spec differs");
    }

    [TestMethod]
    public void IsOutOfSpec_DisplayNameDiffers_ReturnsTrue()
    {
        // arrange
        // the command's job includes setting the display name, so a step with the right
        // versions but a stale name is still out of spec
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.IsOutOfSpec(Reference("1.*", "7.9.x", "Use NuGet"));

        // assert
        Assert.IsTrue(actual, "display name differs");
    }

    [TestMethod]
    public void IsOutOfSpec_MissingNuGetVersion_ReturnsTrue()
    {
        // arrange
        // a step with no inputs.versionSpec scans as an empty string
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        // act
        var actual = systemUnderTest.IsOutOfSpec(Reference("1.*", "", "Use NuGet 7.9.x"));

        // assert
        Assert.IsTrue(actual, "nuget version spec is not set");
    }

    [TestMethod]
    public void IsOutOfSpec_ScannedStepThatUpdaterJustWrote_ReturnsFalse()
    {
        // arrange
        // the round trip that matters: update a definition, rescan it, and the steps
        // should now report as in spec so a second run writes nothing
        var definition = BuildDefinitionWithInstallerAndOtherStep();
        var systemUnderTest = new NuGetToolInstallerUpdater("1.*", "7.9.x");

        systemUnderTest.Update(definition);

        // act
        var references = NuGetToolInstallerScanner.FindReferences(definition.ToJsonString());

        // assert
        Assert.AreNotEqual<int>(0, references.Count, "scan should find the installer step");

        foreach (var reference in references)
        {
            Assert.IsFalse(systemUnderTest.IsOutOfSpec(reference),
                $"step '{reference.StepDisplayName}' should be in spec after an update");
        }
    }

    [TestMethod]
    public void Constructor_EmptyVersions_Throws()
    {
        // act & assert
        Assert.ThrowsExactly<ArgumentException>(() => new NuGetToolInstallerUpdater("", "7.9.x"));
        Assert.ThrowsExactly<ArgumentException>(() => new NuGetToolInstallerUpdater("1.*", ""));
    }
}
