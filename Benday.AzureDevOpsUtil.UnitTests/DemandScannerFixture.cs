using Benday.AzureDevOpsUtil.Api.Demands;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class DemandScannerFixture
{
    [TestMethod]
    public void Scan_BuildDefinition_RootDemandsAsStrings()
    {
        // arrange - the shape a classic build definition returns
        var json = """
        {
          "id": 12,
          "name": "Sample-CI",
          "demands": [ "msbuild", "visualstudio", "Agent.OS -equals Windows_NT" ],
          "process": { "phases": [ { "name": "Agent job 1", "steps": [] } ], "type": 1 }
        }
        """;

        // act
        var result = DemandScanner.Scan(json);

        // assert
        CollectionAssert.AreEqual(
            new[] { "msbuild", "visualstudio", "Agent.OS -equals Windows_NT" },
            result.ToArray());
    }

    [TestMethod]
    public void Scan_ReleaseDefinition_DemandsUnderDeploymentInput()
    {
        // arrange - release demands live down inside each deploy phase
        var json = """
        {
          "id": 5,
          "name": "Sample-CD",
          "environments": [
            {
              "name": "Prod",
              "deployPhases": [
                {
                  "deploymentInput": {
                    "queueId": 3,
                    "demands": [ "SpecialSoftware", "docker" ]
                  }
                }
              ]
            }
          ]
        }
        """;

        // act
        var result = DemandScanner.Scan(json);

        // assert
        CollectionAssert.AreEqual(new[] { "SpecialSoftware", "docker" }, result.ToArray());
    }

    [TestMethod]
    public void Scan_DeduplicatesAcrossPhases()
    {
        // arrange - the same demand appears on two phases
        var json = """
        {
          "process": {
            "phases": [
              { "name": "A", "demands": [ "msbuild" ] },
              { "name": "B", "demands": [ "msbuild", "npm" ] }
            ]
          }
        }
        """;

        // act
        var result = DemandScanner.Scan(json);

        // assert
        CollectionAssert.AreEqual(new[] { "msbuild", "npm" }, result.ToArray());
    }

    [TestMethod]
    public void Scan_ObjectFormDemand_RendersNameAndValue()
    {
        // arrange - the object model form { name, value }
        var json = """
        {
          "demands": [
            { "name": "Agent.OS", "value": "Linux" },
            { "name": "SpecialSoftware", "value": "" }
          ]
        }
        """;

        // act
        var result = DemandScanner.Scan(json);

        // assert
        CollectionAssert.AreEqual(
            new[] { "Agent.OS -equals Linux", "SpecialSoftware" },
            result.ToArray());
    }

    [TestMethod]
    public void Scan_NoDemands_ReturnsEmpty()
    {
        // arrange
        var json = """
        { "id": 1, "name": "No demands here", "process": { "phases": [] } }
        """;

        // act
        var result = DemandScanner.Scan(json);

        // assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Scan_InvalidJson_ReturnsEmpty()
    {
        // act
        var result = DemandScanner.Scan("this is not json");

        // assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Scan_NullOrEmpty_ReturnsEmpty()
    {
        Assert.AreEqual(0, DemandScanner.Scan(null).Count);
        Assert.AreEqual(0, DemandScanner.Scan(string.Empty).Count);
    }
}
