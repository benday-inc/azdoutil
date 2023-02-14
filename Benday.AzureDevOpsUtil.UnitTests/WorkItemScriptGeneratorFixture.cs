using Benday.AzureDevOpsUtil.Api.ScriptGenerator;

namespace Benday.AzureDevOpsUtil.UnitTests;
[TestClass]
public class WorkItemScriptGeneratorFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _systemUnderTest = null;
    }

    private WorkItemScriptGenerator? _systemUnderTest;

    private WorkItemScriptGenerator SystemUnderTest
    {
        get
        {
            if (_systemUnderTest == null)
            {
                _systemUnderTest = new WorkItemScriptGenerator();
            }

            return _systemUnderTest;
        }
    }

    [TestMethod]
    public void GetRandomTitles()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(SystemUnderTest.GetRandomTitle());
            Console.WriteLine();
        }
    }

    [TestMethod]
    public void GeneratorStartsWithZeroPbis()
    {
        // arrange
        var expected = 0;

        // act
        var actual = SystemUnderTest.ProductBacklogItems.Count;

        // assert
        Assert.AreEqual<int>(expected, actual, $"PBI Count was wrong");
    }



    [TestMethod]
    public void GenerateScriptForOneSprint()
    {
        // arrange

        var sprint = new WorkItemScriptSprint()
        {
            AverageNumberOfTasksPerPbi = 5,
            NewPbiCount = 15,
            RefinedPbiCount = 10,
            SprintNumber = 1,
            SprintPbiCount = 7,
            SprintPbisToDoneCount = 5
        };

        // act
        SystemUnderTest.GenerateScript(sprint);

        // assert
        Assert.AreEqual<int>(sprint.NewPbiCount, 
            SystemUnderTest.ProductBacklogItems.Count, 
            $"PBI Count was wrong");

        Assert.AreEqual<int>(sprint.NewPbiCount,
            SystemUnderTest.Actions.Count,
            $"Action count was wrong");

    }
}