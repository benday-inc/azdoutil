using Benday.AzureDevOpsUtil.Api;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class WorkItemScriptActionParserFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private WorkItemScriptActionParser _SystemUnderTest;

    private WorkItemScriptActionParser SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new WorkItemScriptActionParser();
            }

            return _SystemUnderTest;
        }
    }


    [TestMethod]
    public void GetActions_Simple()
    {
        // arrange

        var rows = new List<WorkItemScriptRow>()
        {
            CreateRow("1", "Action row 1"),
            CreateRow("2", "Action row 2"),
            CreateRow("3", "Action row 3")
        };

        // act
        var actions = SystemUnderTest.GetActions(rows);

        // assert
        Assert.AreEqual<int>(3, actions.Count, "Action count was wrong.");
    }

    [TestMethod]
    public void GetActions_IgnoreComments()
    {
        // arrange

        var rows = new List<WorkItemScriptRow>()
        {
            CreateRow("COMMENT", "comment 1"),
            CreateRow("1", "Action row 1"),
            CreateRow("COMMENT", "comment 2"),
            CreateRow("2", "Action row 2"),
            CreateRow("COMMENT", "comment 3"),
            CreateRow("3", "Action row 3"),
            CreateRow("COMMENT", "comment 4")
        };

        // act
        var actions = SystemUnderTest.GetActions(rows);

        // assert
        Assert.AreEqual<int>(3, actions.Count, "Action count was wrong.");
    }

    [TestMethod]
    public void GetActions_GroupMultipleRowsIntoAction_SameActionId()
    {
        // arrange

        var rows = new List<WorkItemScriptRow>()
        {
            CreateRow("1", "Action row 1"),
            CreateRow("1", "Action row 2"),
        };

        // act
        var actions = SystemUnderTest.GetActions(rows);

        // assert
        Assert.AreEqual<int>(1, actions.Count, "Action count was wrong.");
        Assert.AreEqual<int>(2, actions[0].Rows.Count, "Row count was wrong");
    }

    [TestMethod]
    public void GetActions_GroupMultipleRowsIntoAction_SecondActionIdIsBlank()
    {
        // arrange

        var rows = new List<WorkItemScriptRow>()
        {
            CreateRow("1", "Action row 1"),
            CreateRow(string.Empty, "Action row 2")
        };

        // act
        var actions = SystemUnderTest.GetActions(rows);

        // assert
        Assert.AreEqual<int>(1, actions.Count, "Action count was wrong.");
        Assert.AreEqual<int>(2, actions[0].Rows.Count, "Rows count was wrong");
    }

    [TestMethod]
    public void GetActions_GroupMultipleRowsIntoAction_SecondActionIdIsBlank_Variation2()
    {
        // arrange

        var rows = new List<WorkItemScriptRow>()
        {
            CreateRow("1", "Action row 1"),
            CreateRow(string.Empty, "Action row 2"),
            CreateRow("COMMENT", "comment 2"),
            CreateRow("2", "Action row 2"),
        };

        // act
        var actions = SystemUnderTest.GetActions(rows);

        // assert
        Assert.AreEqual<int>(2, actions.Count, "Action count was wrong.");
        Assert.AreEqual<int>(2, actions[0].Rows.Count, "Row count was wrong for action 0");
        Assert.AreEqual<int>(1, actions[1].Rows.Count, "Row count was wrong for action 1");
    }

    private WorkItemScriptRow CreateRow(string actionId, string desc)
    {
        var row = new WorkItemScriptRow();
        row.ActionId = actionId.ToString();
        row.Description = desc;
        return row;
    }




}
