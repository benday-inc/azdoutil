using Benday.AzureDevOpsUtil.Api.Commands.ProcessTemplates;
using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.ScriptGenerator;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class WorkItemScriptRefnamesFixture
{
    [TestMethod]
    [DataRow("Title", "Scrum", "System.Title")]
    [DataRow("title", "Scrum", "System.Title")]
    [DataRow("State", "Scrum", "System.State")]
    [DataRow("Status", "Scrum", "System.State")]
    [DataRow("Description", "Scrum", "System.Description")]
    [DataRow("IterationPath", "Scrum", "System.IterationPath")]
    [DataRow("AreaPath", "Scrum", "System.AreaPath")]
    [DataRow("Effort", "Scrum", "Microsoft.VSTS.Scheduling.Effort")]
    [DataRow("RemainingWork", "Scrum", "Microsoft.VSTS.Scheduling.RemainingWork")]
    [DataRow("BacklogPriority", "Scrum", "Microsoft.VSTS.Common.BacklogPriority")]
    [DataRow("BacklogPriority", "Scrum with Backlog Refinement", "Microsoft.VSTS.Common.BacklogPriority")]
    [DataRow("BacklogPriority", "My Inherited Scrum", "Microsoft.VSTS.Common.BacklogPriority")]
    [DataRow("BacklogPriority", "Agile", "Microsoft.VSTS.Common.StackRank")]
    [DataRow("BacklogPriority", "Agile with Backlog Refinement", "Microsoft.VSTS.Common.StackRank")]
    [DataRow("BacklogPriority", "CMMI", "Microsoft.VSTS.Common.StackRank")]
    [DataRow("StackRank", "Scrum", "Microsoft.VSTS.Common.BacklogPriority")]
    [DataRow("StackRank", "Agile", "Microsoft.VSTS.Common.StackRank")]
    [DataRow("System.Description", "Scrum", "System.Description")]
    [DataRow("Custom.MyField", "Scrum", "Custom.MyField")]
    [DataRow("  Effort  ", "Scrum", "Microsoft.VSTS.Scheduling.Effort")]
    public void GetFullRefname_MapsShortNamesAndPassesFullNamesThrough(
        string refname, string processTemplateName, string expected)
    {
        var actual = WorkItemScriptRefnames.GetFullRefname(refname, processTemplateName);

        Assert.AreEqual<string>(expected, actual);
    }

    [TestMethod]
    public void GetFullRefname_EmptyRefnameReturnsEmpty()
    {
        Assert.AreEqual<string>(string.Empty, WorkItemScriptRefnames.GetFullRefname(string.Empty, "Scrum"));
        Assert.AreEqual<string>(string.Empty, WorkItemScriptRefnames.GetFullRefname("   ", "Scrum"));
    }

    /// <summary>
    /// The bug this guards against: createfromgenerator --scriptonly wrote a
    /// script that createfromexcel could not read back, because the Excel command
    /// kept its own (shorter) refname list and BacklogPriority reached Azure
    /// DevOps unmapped (TF51535: Cannot find field BacklogPriority). Generate a
    /// script, write it to Excel, read it back through the same reader the Excel
    /// command uses, and check that every refname the generator wrote maps to a
    /// fully qualified field.
    /// </summary>
    [TestMethod]
    [DataRow("Scrum", "Microsoft.VSTS.Common.BacklogPriority")]
    [DataRow("Agile", "Microsoft.VSTS.Common.StackRank")]
    public void GeneratedScript_RoundTripsThroughExcelWithEveryRefnameMapped(
        string processTemplateName, string expectedBacklogOrderField)
    {
        // arrange
        var generator = new WorkItemScriptGenerator(GetTemplateInfo(processTemplateName));

        generator.GenerateScript(new List<WorkItemScriptSprint>
        {
            new WorkItemScriptSprint()
            {
                AverageNumberOfTasksPerPbi = 3,
                NewPbiCount = 15,
                RefinedPbiCountMeeting1 = 5,
                RefinedPbiCountMeeting2 = 5,
                SprintNumber = 1,
                SprintPbiCount = 4,
                SprintPbisToDoneCount = 5,
                DailyHoursPerTeamMember = 6,
                TeamMemberCount = 7
            }
        }, false);

        var path = Path.Combine(Utilities.GetTempFolder(), "workitem-script-roundtrip.xlsx");

        new ExcelWorkItemScriptWriter().WriteToExcel(path, generator.Actions);

        // act -- exactly what CreateWorkItemsFromExcelScriptCommand.PopulateActions does
        var rows = new ExcelWorkItemScriptRowReader(new ExcelReader(path)).GetRows();
        var actions = WorkItemScriptActionParser.GetActions(rows);

        // assert
        Assert.AreEqual<int>(generator.Actions.Count, actions.Count, "Action count changed in the round trip");

        // cell values survive the trip: same fields, same order, same text
        for (var i = 0; i < generator.Actions.Count; i++)
        {
            var expectedAction = generator.Actions[i];
            var actualAction = actions[i];

            Assert.AreEqual<string>(expectedAction.ActionId, actualAction.ActionId, $"ActionId for action {i}");
            Assert.AreEqual<string>(expectedAction.Definition.Operation, actualAction.Definition.Operation, $"Operation for action {i}");
            Assert.AreEqual<string>(expectedAction.Definition.WorkItemType, actualAction.Definition.WorkItemType, $"WorkItemType for action {i}");
            Assert.AreEqual<int>(expectedAction.Definition.ActionDay, actualAction.Definition.ActionDay, $"ActionDay for action {i}");
            Assert.AreEqual<int>(expectedAction.Rows.Count, actualAction.Rows.Count, $"Row count for action {i}");

            for (var j = 0; j < expectedAction.Rows.Count; j++)
            {
                Assert.AreEqual<string>(expectedAction.Rows[j].Refname, actualAction.Rows[j].Refname, $"Refname for action {i} row {j}");
                Assert.AreEqual<string>(expectedAction.Rows[j].FieldValue, actualAction.Rows[j].FieldValue, $"FieldValue for action {i} row {j}");
            }
        }

        // the Iterations sheet reads back too, with its numbers as numbers
        var iterations = new ExcelWorkItemIterationRowReader(new ExcelReader(path)).GetRows();

        Assert.AreEqual<int>(6, iterations.Count, "Iteration count");
        Assert.AreEqual<string>("Sprint 1", iterations[0].IterationName);
        Assert.AreEqual<int>(0, iterations[0].StartDay);
        Assert.AreEqual<int>(13, iterations[0].EndDay);
        Assert.AreEqual<string>("Sprint 6", iterations[5].IterationName);
        Assert.AreEqual<int>(70, iterations[5].StartDay);
        Assert.AreEqual<int>(83, iterations[5].EndDay);

        var refnamesInScript = actions
            .SelectMany(a => a.Rows)
            .Select(r => r.Refname)
            .Where(r => string.IsNullOrWhiteSpace(r) == false && r != WorkItemScriptRefnames.Parent)
            .Distinct()
            .ToList();

        Assert.IsTrue(refnamesInScript.Contains("BacklogPriority"),
            "The generator no longer writes BacklogPriority, so this test is not exercising the bug it was written for");

        foreach (var refname in refnamesInScript)
        {
            var mapped = WorkItemScriptRefnames.GetFullRefname(refname, processTemplateName);

            Assert.IsTrue(mapped.Contains('.'),
                $"Refname '{refname}' written by the generator is not mapped to a full reference name (got '{mapped}'). " +
                "Add it to WorkItemScriptRefnames or createfromexcel will fail with TF51535.");
        }

        Assert.AreEqual<string>(expectedBacklogOrderField,
            WorkItemScriptRefnames.GetFullRefname("BacklogPriority", processTemplateName));
    }

    private static ProcessTemplateCreationInfo GetTemplateInfo(string processTemplateName)
    {
        if (processTemplateName == "Scrum")
        {
            return new ProcessTemplateCreationInfo()
            {
                TemplateName = "Scrum",
                UseRefinement = false,
                RequirementWorkItemTypeFullName = "Product Backlog Item",
                RequirementWorkItemTypeAbbreviationName = "PBI",
                InProgressStateName = "Committed",
                RequirementDoneStateName = "Done",
                RefinementStateName = "Approved",
                ReadyForSprintStateName = "Approved",
                AcceptedOnSprintBacklogStateName = "Committed"
            };
        }
        else
        {
            return new ProcessTemplateCreationInfo()
            {
                TemplateName = "Agile",
                UseRefinement = false,
                RequirementWorkItemTypeFullName = "User Story",
                RequirementWorkItemTypeAbbreviationName = "story",
                InProgressStateName = "Active",
                RequirementDoneStateName = "Closed",
                RefinementStateName = "New",
                ReadyForSprintStateName = "New",
                AcceptedOnSprintBacklogStateName = "Active"
            };
        }
    }
}
