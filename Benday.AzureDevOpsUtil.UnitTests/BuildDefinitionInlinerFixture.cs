using System.Text.Json.Nodes;

using Benday.AzureDevOpsUtil.Api.TaskGroups;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class BuildDefinitionInlinerFixture
{
    private const string TaskGroupId = "b6300ba1-3b53-4b97-9f16-c7cc790c723e";

    private static JsonNode BuildDefinitionWithOneMetaTask()
    {
        return JsonNode.Parse($$"""
        {
          "id": 100,
          "name": "Sample-CI",
          "revision": 14,
          "process": {
            "phases": [
              {
                "name": "Agent job 1",
                "steps": [
                  {
                    "displayName": "Task group: Sample - $(BuildConfiguration)",
                    "enabled": true,
                    "condition": "succeededOrFailed()",
                    "task": {
                      "id": "{{TaskGroupId}}",
                      "versionSpec": "1.*",
                      "definitionType": "metaTask"
                    },
                    "inputs": {
                      "BuildConfiguration": "Debug",
                      "BuildPlatform": "",
                      "WebConfigLocation": "$(Build.SourcesDirectory)\\QIP"
                    }
                  }
                ]
              }
            ],
            "type": 1
          }
        }
        """)!;
    }

    private static JsonNode TaskGroupWithThreeSteps()
    {
        return JsonNode.Parse($$"""
        {
          "id": "{{TaskGroupId}}",
          "name": "Sample Task Group",
          "tasks": [
            {
              "displayName": "Use NuGet",
              "enabled": true,
              "task": { "id": "abc", "versionSpec": "1.*", "definitionType": "task" },
              "inputs": { "versionSpec": "7.x" }
            },
            {
              "displayName": "Build $(BuildConfiguration) on $(BuildPlatform)",
              "enabled": true,
              "condition": "succeeded()",
              "task": { "id": "def", "versionSpec": "1.*", "definitionType": "task" },
              "inputs": {
                "configuration": "$(BuildConfiguration)",
                "platform": "$(BuildPlatform)",
                "sourcesDir": "$(Build.SourcesDirectory)"
              },
              "environment": {
                "MSBUILD_CONFIG": "$(BuildConfiguration)"
              }
            },
            {
              "displayName": "Copy to $(WebConfigLocation)",
              "enabled": true,
              "task": { "id": "ghi", "versionSpec": "1.*", "definitionType": "task" },
              "inputs": {
                "TargetFolder": "$(WebConfigLocation)"
              }
            }
          ],
          "inputs": [
            { "name": "BuildConfiguration", "defaultValue": "release", "type": "string" },
            { "name": "BuildPlatform",      "defaultValue": "any cpu",  "type": "string" },
            { "name": "WebConfigLocation",  "defaultValue": "$(Build.SourcesDirectory)\\", "type": "string" }
          ]
        }
        """)!;
    }

    private static BuildDefinitionInliner CreateInliner(JsonNode taskGroup)
    {
        var dict = new Dictionary<string, JsonNode> { [TaskGroupId] = taskGroup };
        return new BuildDefinitionInliner(dict);
    }

    [TestMethod]
    public void Inline_DisablesOriginalAndAppendsInlinedSteps()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();
        var taskGroup = TaskGroupWithThreeSteps();
        var sut = CreateInliner(taskGroup);

        // act
        var result = sut.Inline(buildDef);

        // assert
        Assert.AreEqual(1, result.InlinedReferenceCount, "should have inlined exactly one reference");

        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        Assert.AreEqual(4, steps.Count, "expected 1 disabled original + 3 inlined steps");

        var original = steps[0]!.AsObject();
        Assert.IsFalse(original["enabled"]!.GetValue<bool>(), "original metaTask step should be disabled");
        Assert.IsTrue(
            original["displayName"]!.GetValue<string>().StartsWith(BuildDefinitionInliner.InlinedDisplayNamePrefix),
            "original displayName should be prefixed");
    }

    [TestMethod]
    public void Inline_SubstitutesCallerProvidedValuesIntoInlinedSteps()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();
        var taskGroup = TaskGroupWithThreeSteps();
        var sut = CreateInliner(taskGroup);

        // act
        sut.Inline(buildDef);

        // assert
        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        var buildStep = steps[2]!.AsObject();

        Assert.AreEqual("Build Debug on any cpu",
            buildStep["displayName"]!.GetValue<string>(),
            "caller-supplied BuildConfiguration substituted; empty BuildPlatform fell back to default");

        var inputs = buildStep["inputs"]!.AsObject();
        Assert.AreEqual("Debug", inputs["configuration"]!.GetValue<string>(),
            "caller value should win over default");
        Assert.AreEqual("any cpu", inputs["platform"]!.GetValue<string>(),
            "empty caller value should fall back to default");

        var env = buildStep["environment"]!.AsObject();
        Assert.AreEqual("Debug", env["MSBUILD_CONFIG"]!.GetValue<string>(),
            "macros in environment dict should also substitute");
    }

    [TestMethod]
    public void Inline_LeavesBuiltInMacrosAlone()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();
        var taskGroup = TaskGroupWithThreeSteps();
        var sut = CreateInliner(taskGroup);

        // act
        sut.Inline(buildDef);

        // assert
        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        var buildStep = steps[2]!.AsObject();

        Assert.AreEqual("$(Build.SourcesDirectory)",
            buildStep["inputs"]!["sourcesDir"]!.GetValue<string>(),
            "Built-in macros (not in task group params) must not be touched");
    }

    [TestMethod]
    public void Inline_FallsBackToTaskGroupDefault_WhenCallerValueEmpty()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();
        var taskGroup = TaskGroupWithThreeSteps();
        var sut = CreateInliner(taskGroup);

        // act
        sut.Inline(buildDef);

        // assert
        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        var copyStep = steps[3]!.AsObject();

        Assert.AreEqual("Copy to $(Build.SourcesDirectory)\\QIP",
            copyStep["displayName"]!.GetValue<string>(),
            "caller passed WebConfigLocation explicitly; should win over default");
    }

    [TestMethod]
    public void Inline_FilterToSpecificTaskGroupId_OnlyInlinesMatching()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();
        var taskGroup = TaskGroupWithThreeSteps();
        var sut = CreateInliner(taskGroup);

        // act
        var result = sut.Inline(buildDef, taskGroupIdFilter: "different-id-here");

        // assert
        Assert.AreEqual(0, result.InlinedReferenceCount, "filter excludes this task group; no inlining");

        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        Assert.AreEqual(1, steps.Count, "no inlining; original step preserved unchanged");
        Assert.IsTrue(steps[0]!["enabled"]!.GetValue<bool>(),
            "original step should remain enabled when filter doesn't match");
    }

    [TestMethod]
    public void Inline_ThrowsOnNestedTaskGroup()
    {
        // arrange
        var buildDef = BuildDefinitionWithOneMetaTask();

        var nestedTaskGroup = JsonNode.Parse($$"""
        {
          "id": "{{TaskGroupId}}",
          "name": "Nested",
          "tasks": [
            {
              "displayName": "Calls another task group",
              "task": { "id": "inner", "versionSpec": "1.*", "definitionType": "metaTask" },
              "inputs": {}
            }
          ],
          "inputs": []
        }
        """)!;

        var sut = CreateInliner(nestedTaskGroup);

        // act + assert
        var ex = Assert.ThrowsExactly<KnownException>(() => sut.Inline(buildDef));
        Assert.IsTrue(ex.Message.Contains("nested", StringComparison.OrdinalIgnoreCase),
            "exception message should mention nested task groups");
    }

    [TestMethod]
    public void Inline_NoMetaTaskReferences_NoChanges()
    {
        // arrange
        var buildDef = JsonNode.Parse("""
        {
          "id": 200,
          "revision": 1,
          "process": {
            "phases": [
              {
                "name": "Agent job 1",
                "steps": [
                  {
                    "displayName": "Plain task",
                    "enabled": true,
                    "task": { "id": "abc", "versionSpec": "1.*", "definitionType": "task" },
                    "inputs": {}
                  }
                ]
              }
            ],
            "type": 1
          }
        }
        """)!;
        var sut = new BuildDefinitionInliner(new Dictionary<string, JsonNode>());

        // act
        var result = sut.Inline(buildDef);

        // assert
        Assert.AreEqual(0, result.InlinedReferenceCount);
        var steps = (JsonArray)buildDef["process"]!["phases"]![0]!["steps"]!;
        Assert.AreEqual(1, steps.Count, "step count unchanged");
    }
}
