using Benday.AzureDevOpsUtil.Api.DeploymentGroups;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class DeploymentGroupUsageFixture
{
    private static string ReleaseDefinitionWithDeploymentGroupPhase()
    {
        return """
        {
          "id": 12,
          "name": "MyApp-Release",
          "environments": [
            {
              "name": "Dev",
              "deployPhases": [
                {
                  "name": "Agent phase",
                  "phaseType": "agentBasedDeployment",
                  "deploymentInput": { "queueId": 99 }
                }
              ]
            },
            {
              "name": "Prod",
              "deployPhases": [
                {
                  "name": "Deploy web servers",
                  "phaseType": "machineGroupBasedDeployment",
                  "deploymentInput": {
                    "queueId": 3,
                    "tags": [ "web", "iis" ],
                    "healthPercent": 50
                  }
                },
                {
                  "name": "Deploy db servers",
                  "phaseType": "machineGroupBasedDeployment",
                  "deploymentInput": {
                    "queueId": 4,
                    "tags": [ ]
                  }
                }
              ]
            }
          ]
        }
        """;
    }

    [TestMethod]
    public void FindPhases_FindsDeploymentGroupPhasesOnly()
    {
        // arrange
        var json = ReleaseDefinitionWithDeploymentGroupPhase();

        // act
        var actual = ReleaseDefinitionDeploymentGroupScanner.FindPhases(json);

        // assert
        Assert.AreEqual<int>(2, actual.Count, "phase count -- agent phase is not included");

        var first = actual[0];
        Assert.AreEqual<int>(12, first.ReleaseDefinitionId, "release id");
        Assert.AreEqual<string>("MyApp-Release", first.ReleaseDefinitionName, "release name");
        Assert.AreEqual<string>("Prod", first.EnvironmentName, "environment");
        Assert.AreEqual<string>("Deploy web servers", first.PhaseName, "phase name");
        Assert.AreEqual<int>(3, first.DeploymentGroupId, "deployment group id comes from queueId");
        CollectionAssert.AreEqual(new[] { "web", "iis" }, first.Tags, "tags");

        var second = actual[1];
        Assert.AreEqual<int>(4, second.DeploymentGroupId, "second group id");
        Assert.AreEqual<int>(0, second.Tags.Count, "second phase has no tags");
    }

    [TestMethod]
    public void FindPhases_NullOrEmptyJson_ReturnsEmpty()
    {
        // act & assert
        Assert.AreEqual<int>(0, ReleaseDefinitionDeploymentGroupScanner.FindPhases(null).Count, "null");
        Assert.AreEqual<int>(0, ReleaseDefinitionDeploymentGroupScanner.FindPhases("").Count, "empty");
    }

    private static DeploymentTargetInfo Target(int id, string name, params string[] tags)
    {
        return new DeploymentTargetInfo
        {
            Id = id,
            Tags = tags.ToList(),
            Agent = new DeploymentTargetAgentInfo { Id = id, Name = name, Status = "online" }
        };
    }

    [TestMethod]
    public void Analyze_MatchesPhaseTagsToTargets()
    {
        // arrange
        var groups = new List<DeploymentGroupInfo>
        {
            new() { Id = 3, Name = "WebServers" },
            new() { Id = 4, Name = "DbServers" }
        };

        var targetsByGroupId = new Dictionary<int, List<DeploymentTargetInfo>>
        {
            [3] = new()
            {
                Target(1, "WEB01", "web", "iis"),
                Target(2, "WEB02", "web"),
                Target(3, "STAGE01", "staging")
            },
            [4] = new()
            {
                Target(4, "DB01", "sql")
            }
        };

        var phases = new List<DeploymentGroupPhaseReference>
        {
            new()
            {
                ReleaseDefinitionId = 12,
                ReleaseDefinitionName = "MyApp-Release",
                EnvironmentName = "Prod",
                PhaseName = "Deploy web servers",
                DeploymentGroupId = 3,
                Tags = new List<string> { "Web", "IIS" }
            },
            new()
            {
                ReleaseDefinitionId = 12,
                ReleaseDefinitionName = "MyApp-Release",
                EnvironmentName = "Prod",
                PhaseName = "Deploy db servers",
                DeploymentGroupId = 4,
                Tags = new List<string>()
            },
            new()
            {
                ReleaseDefinitionId = 44,
                ReleaseDefinitionName = "Orphaned-Release",
                EnvironmentName = "Prod",
                PhaseName = "Deploy",
                DeploymentGroupId = 999,
                Tags = new List<string>()
            }
        };

        // act
        var actual = DeploymentGroupUsageAnalyzer.Analyze(
            "MyProject", groups, targetsByGroupId, phases);

        // assert
        Assert.AreEqual<string>("MyProject", actual.ProjectName, "project name");
        Assert.AreEqual<int>(2, actual.Groups.Count, "group count");

        var dbGroup = actual.Groups[0];
        Assert.AreEqual<string>("DbServers", dbGroup.Name, "groups are ordered by name");
        Assert.AreEqual<int>(1, dbGroup.Consumers.Count, "db consumer count");
        CollectionAssert.AreEqual(new[] { "DB01" }, dbGroup.Consumers[0].MatchingTargetNames,
            "no tag filter matches every target");

        var webGroup = actual.Groups[1];
        Assert.AreEqual<string>("WebServers", webGroup.Name, "web group name");
        Assert.AreEqual<int>(3, webGroup.Targets.Count, "web target count");
        Assert.AreEqual<int>(1, webGroup.Consumers.Count, "web consumer count");

        // Requires BOTH tags, matched case-insensitively: WEB01 has web+iis, WEB02 only web
        CollectionAssert.AreEqual(new[] { "WEB01" }, webGroup.Consumers[0].MatchingTargetNames,
            "tag filter requires all tags");

        Assert.AreEqual<int>(1, actual.PhasesWithUnknownGroup.Count, "unknown group phase count");
        Assert.AreEqual<int>(999, actual.PhasesWithUnknownGroup[0].DeploymentGroupId,
            "unknown group id");
    }

    [TestMethod]
    public void MatchesTags_EmptyPhaseTags_MatchesEverything()
    {
        // arrange
        var target = Target(1, "ANY01");

        // act & assert
        Assert.AreEqual<bool>(true,
            DeploymentGroupUsageAnalyzer.MatchesTags(target, new List<string>()),
            "no phase tags matches a target with no tags");
    }

    [TestMethod]
    public void MatchesTags_TargetMissingOneTag_DoesNotMatch()
    {
        // arrange
        var target = Target(1, "WEB02", "web");

        // act & assert
        Assert.AreEqual<bool>(false,
            DeploymentGroupUsageAnalyzer.MatchesTags(target, new List<string> { "web", "iis" }),
            "target must carry every phase tag");
    }
}
