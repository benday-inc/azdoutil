using System;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;
public class WorkItemScriptGenerator
{
    private const int SPRINT_DURATION = 14;
    public readonly List<string> FibonacciValues =
        new() {
        "1", "2", "3", "5", "8", "13", "21"
        };

    public readonly List<int> HourValues =
        new() {
            1, 2, 3, 4, 5, 6, 8, 12
        };

    private readonly List<string> _actionWords;
    private readonly List<string> _endingWords;
    private readonly List<string> _randomWords;
    private int _createdWorkItemNumber = 100;
    private int _createdActionNumber = 100;

    public WorkItemScriptGenerator()
    {
        _actionWords = GetActionWords();
        _randomWords = GetRandomWords();
        _endingWords = GetEndingWords();
    }

    public string GetRandomTitle()
    {
        var rnd = new RandomNumGen();
        var builder = new StringBuilder();

        builder.Append(Capitalize(_actionWords.Random()));
        builder.Append(_randomWords.RandomPhrase(4));

        builder.Append(" ");
        builder.Append(_endingWords.Random());

        return builder.ToString();
    }
    private string Capitalize(string word)
    {
        return $"{char.ToUpper(word[0])}{word[1..]}";
    }

    private List<string> GetActionWords()
    {
        return new List<string>()
        {
            "implement",
            "create",
            "fix",
            "replace",
            "conjure",
            "market",
            "advertise",
            "improve",
            "fabricate",
            "build",
            "compile",
            "collate",
            "prepare",
            "randomize",
            "utilize"
        };
    }

    private List<string> GetEndingWords()
    {
        return new List<string>()
        {
            "for luck",
            "when in doubt",
            "to experience neverending bliss",
            "to achieve nirvana",
            "because goals",
            "with sleep",
            "without sleep",
            "because my cat told me to",
            "since my cat told me to",
            "in order to party",
            "to boogie on down",
            "because bob in accounting wants it",
            "because priya in shipping asked",
            "because zeus in actuarial services demanded",
            "for no reason whatsoever",
            "to satisfy the fashion police",
            "for fuel",
            "after flipfloparoonie time",
            "for all the marbles",
            "without extra sugar",
            "for real this time"
        };
    }

    private List<string> GetRandomWords()
    {
        return new List<string>()
        {
            "pumpkin",
            "recovery",
            "device",
            "thingy",
            "hamster",
            "dumpster",
            "computer",
            "mobile",
            "repair",
            "happy time",
            "buttery",
            "paper",
            "display",
            "wifi",
            "database",
            "purchase",
            "elephant",
            "recovery",
            "donkey",
            "never",
            "baffle",
            "peace",
            "apples",
            "everyone",
            "olive",
            "tree",
            "overview",
            "finance",
            "nugget",
            "mayonnaise",
            "mustard",
            "pants",
            "membership",
            "closet",
            "range",
            "invitation",
            "nothing",
            "compact",
            "script",
            "luggage",
            "symbol",
            "lawn",
            "gumbo",
            "donuts",
            "pizza",
            "nonsense"
        };
    }

    public Dictionary<string, WorkItemScriptWorkItem> ProductBacklogItems { get; set; } = new();
    public Dictionary<string, WorkItemScriptWorkItem> SprintTasks { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsThatNeedRefinementRound1 { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsThatNeedRefinementRound2 { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsReadyForSprint { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsInSprint { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsDone { get; set; } = new();
    public List<WorkItemScriptAction> Actions { get; set; } = new();

    private void Move(WorkItemScriptWorkItem moveThis,
        List<WorkItemScriptWorkItem> fromList,
        List<WorkItemScriptWorkItem> toList)
    {
        fromList.Remove(moveThis);
        toList.Add(moveThis);
    }

    public void GenerateScript(WorkItemScriptSprint sprint)
    {
        if (sprint == null)
        {
            throw new ArgumentNullException(nameof(sprint), $"{nameof(sprint)} is null.");
        }

        for (int i = 0; i < sprint.NewPbiCount; i++)
        {
            _createdWorkItemNumber += 100;

            var item = new WorkItemScriptWorkItem
            {
                Id = $"pbi-{_createdWorkItemNumber}",
                Title = GetRandomTitle(),
                WorkItemType = "PBI",
                State = "New",
                Iteration = string.Empty
            };

            ProductBacklogItems.Add(item.Id, item);
            ProductBacklogItemsThatNeedRefinementRound1.Add(item);

            Actions.Add(GetCreateAction(item, sprint, GetNextActionNumber()));
        }

        ScriptSprintPlanning(sprint);
        
        ScriptDailySprintActivities(sprint);

        var rnd = new RandomNumGen();

        var lotteryNumber = rnd.GetNumberInRange(0, 4);

        if (lotteryNumber == 1)
        {
            // have a great sprint...surprise...everything goes to done
            MoveUndonePbisToDone(sprint);
        }
        else
        {
            MoveUndonePbisToBacklogAndReestimate(sprint);
        }

        SprintTasks.Clear();
        ProductBacklogItemsInSprint.Clear();
        ProductBacklogItemsDone.Clear();
    }

    private int GetNextActionNumber()
    {
        return ((++_createdActionNumber) * 100);
    }

    private void ScriptRefinementMeeting1(WorkItemScriptSprint sprint, 
        bool randomizeNumber = false, int createDateOffset = 0)
    {
        var rnd = new RandomNumGen();

        int numberOfItemsToRefine = sprint.RefinedPbiCountMeeting1;

        if (randomizeNumber == true)
        {
            numberOfItemsToRefine =
                rnd.GetNumberInRange(0,
                    sprint.RefinedPbiCountMeeting1);
        }

        for (int i = 0; i < numberOfItemsToRefine; i++)
        {
            var item = this.ProductBacklogItemsThatNeedRefinementRound1.RandomItem();

            if (item != null)
            {
                Move(item, this.ProductBacklogItemsThatNeedRefinementRound1,
                    this.ProductBacklogItemsThatNeedRefinementRound2);
                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Update";
                action.Definition.Description = "Set PBI status to Needs Refinement";
                action.Definition.WorkItemId = item.Id;
                action.Definition.WorkItemType = item.WorkItemType;
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 3 + createDateOffset;
                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Needs Refinement";

                Actions.Add(action);
            }
        }
    }

    private void ScriptRefinementMeeting2(WorkItemScriptSprint sprint, 
        bool randomizeNumber = false, int createDateOffset = 0)
    {
        var rnd = new RandomNumGen();

        int numberOfItemsToRefine = sprint.RefinedPbiCountMeeting2;

        if (randomizeNumber == true)
        {
            numberOfItemsToRefine =
                rnd.GetNumberInRange(0,
                    sprint.RefinedPbiCountMeeting2);
        }

        for (int i = 0; i < numberOfItemsToRefine; i++)
        {
            var item = this.ProductBacklogItemsThatNeedRefinementRound2.RandomItem();

            if (item != null)
            {
                Move(item, this.ProductBacklogItemsThatNeedRefinementRound2,
                    this.ProductBacklogItemsReadyForSprint);
                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Update";
                action.Definition.Description = "PBI Got Refined and is ready for sprint";
                action.Definition.WorkItemId = item.Id;
                action.Definition.WorkItemType = item.WorkItemType;
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 10 + createDateOffset;
                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Ready for Sprint";

                item.Effort = FibonacciValues.Random();
                action.SetValue("Effort", item.Effort);

                Actions.Add(action);
            }
        }
    }

    private void BurndownTask(WorkItemScriptSprint sprint, WorkItemScriptWorkItem task,
        int sprintDayNumber, int remainingWork)
    {
        var action = new WorkItemScriptAction();

        action.ActionId = GetNextActionNumber().ToString();
        action.Definition.Operation = "Update";

        if (remainingWork == 0)
        {
            action.Definition.Description = "Task Done";
            action.Definition.Refname = "Status";
            action.Definition.FieldValue = "Done";

            action.SetValue("RemainingWork", remainingWork.ToString());
        }
        else
        {
            action.Definition.Description = "Burn down task";
            action.Definition.Refname = "RemainingWork";
            action.Definition.FieldValue = remainingWork.ToString();
        }

        action.Definition.WorkItemId = task.Id;
        action.Definition.WorkItemType = task.WorkItemType;
        action.Definition.ActionDay =
            ((sprint.SprintNumber - 1) * SPRINT_DURATION) + sprintDayNumber;

        Actions.Add(action);
    }

    private void MoveUndonePbisToDone(WorkItemScriptSprint sprint)
    {
        foreach (var pbi in this.ProductBacklogItemsInSprint)
        {
            if (pbi.IsDone == false)
            {
                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Update";
                action.Definition.Description = "PBI won the lottery and got done";
                action.Definition.WorkItemId = pbi.Id;
                action.Definition.WorkItemType = pbi.WorkItemType;

                // last day of sprint
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 13;

                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Done";                

                Actions.Add(action);
            }
        }
    }

    private void MoveUndonePbisToBacklogAndReestimate(WorkItemScriptSprint sprint)
    {
        foreach (var pbi in this.ProductBacklogItemsInSprint)
        {
            if (pbi.IsDone == false)
            {
                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Update";
                action.Definition.Description = "PBI not done in sprint";
                action.Definition.WorkItemId = pbi.Id;
                action.Definition.WorkItemType = pbi.WorkItemType;

                // last day of sprint
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 13;

                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Ready for Sprint";

                pbi.Effort = FibonacciValues.Random();

                action.SetValue("Effort", pbi.Effort);
                action.SetValue("IterationPath", string.Empty);

                Actions.Add(action);
            }
        }        
    }
    private void MarkPbiAsDone(WorkItemScriptSprint sprint, WorkItemScriptWorkItem pbi, int sprintDayNumber)
    {
        pbi.IsDone = true;

        var action = new WorkItemScriptAction();

        action.ActionId = GetNextActionNumber().ToString();
        action.Definition.Operation = "Update";
        action.Definition.Description = "PBI Done";
        action.Definition.WorkItemId = pbi.Id;
        action.Definition.WorkItemType = pbi.WorkItemType;
        action.Definition.ActionDay =
            ((sprint.SprintNumber - 1) * SPRINT_DURATION) + sprintDayNumber;
        action.Definition.Refname = "Status";
        action.Definition.FieldValue = "Done";

        Actions.Add(action);
    }

    private void ScriptDailyScrum(WorkItemScriptSprint sprint, int sprintDayNumber)
    {
        var dailyBurndownMax = sprint.DailyHoursPerTeamMember * sprint.TeamMemberCount;

        var todaysBurndownAmount = new RandomNumGen().GetNumberInRange(0, dailyBurndownMax);

        if (ProductBacklogItemsInSprint.Count == 0)
        {
            return;
        }

        int pbiTotalRemainingWork = -1;

        foreach (var pbi in ProductBacklogItemsInSprint)
        {
            if (todaysBurndownAmount <= 0)
            {
                // out of hours for today
                break;
            }
            else if (pbi.IsDone == true)
            {
                // pbi is done
                break;
            }

            pbiTotalRemainingWork = pbi.TotalRemainingWork;

            while (todaysBurndownAmount > 0 &&
                ProductBacklogItemsInSprint.Count > 0 &&
                pbiTotalRemainingWork > 0)
            {
                foreach (var task in pbi.ChildItems)
                {
                    if (task.RemainingWork <= 0)
                    {
                        // no changes
                    }
                    else if (task.RemainingWork >= todaysBurndownAmount)
                    {
                        task.RemainingWork -= todaysBurndownAmount;
                        todaysBurndownAmount = 0;
                        BurndownTask(sprint, task, sprintDayNumber, task.RemainingWork);
                    }
                    else
                    {
                        // less work than available to burn down
                        todaysBurndownAmount -= task.RemainingWork;
                        task.RemainingWork = 0;
                        BurndownTask(sprint, task, sprintDayNumber, task.RemainingWork);
                    }

                    pbiTotalRemainingWork = pbi.TotalRemainingWork;

                    if (pbiTotalRemainingWork == 0)
                    {
                        MarkPbiAsDone(sprint, pbi, sprintDayNumber);
                        break;
                    }
                }

                Console.WriteLine($"Sprint {sprint.SprintNumber}: pbi {pbi.Id} -- remaining burndown: {todaysBurndownAmount}");

            }
        }
    }

    private void ScriptDailySprintActivities(WorkItemScriptSprint sprint)
    {
        // week 1
        // no daily scrum on first day because sprint planning
        ScriptDailyScrum(sprint, 2);
        ScriptDailyScrum(sprint, 3);
        ScriptRefinementMeeting1(sprint);
        ScriptDailyScrum(sprint, 4);
        ScriptDailyScrum(sprint, 5);

        // weekend 1

        // week 2
        ScriptDailyScrum(sprint, 8);
        ScriptDailyScrum(sprint, 9);
        ScriptDailyScrum(sprint, 10);
        ScriptRefinementMeeting2(sprint); 
        ScriptDailyScrum(sprint, 11);
        ScriptDailyScrum(sprint, 12);

        // weekend 2
    }

    private void ScriptSprintPlanning(WorkItemScriptSprint sprint, bool randomizeNumber = false)
    {
        var rnd = new RandomNumGen();

        int numberOfItemsToSelect = sprint.SprintPbiCount;

        if (randomizeNumber == true)
        {
            numberOfItemsToSelect =
                rnd.GetNumberInRange(0,
                    sprint.SprintPbiCount);
        }

        for (int i = 0; i < numberOfItemsToSelect; i++)
        {
            var item = this.ProductBacklogItemsReadyForSprint.RandomItem();

            if (item != null)
            {
                Move(item, this.ProductBacklogItemsReadyForSprint,
                    this.ProductBacklogItemsInSprint);

                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Update";
                action.Definition.Description = "PBI selected for sprint\r\n";
                action.Definition.WorkItemId = item.Id;
                action.Definition.WorkItemType = item.WorkItemType;
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION);
                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Committed";

                action.SetValue("IterationPath", $"Sprint {sprint.SprintNumber}");
                action.SetValue("Effort", FibonacciValues.Random().ToString());

                Actions.Add(action);
            }
        }

        foreach (var parentPbi in this.ProductBacklogItemsInSprint)
        {
            for (int i = 0; i < sprint.AverageNumberOfTasksPerPbi; i++)
            {
                var task = new WorkItemScriptWorkItem()
                {
                    Id = $"task-{GetNextActionNumber()}",
                    Iteration = $"Sprint {sprint.SprintNumber}",
                    State = "New",
                    Title = $"{parentPbi.Id}: Task {GetRandomTitle()}",
                    WorkItemType = "Task",
                    Parent = parentPbi
                };

                parentPbi.ChildItems.Add(task);
                task.RemainingWork = HourValues.RandomItem();

                this.SprintTasks.Add(task.Id, task);

                var action = new WorkItemScriptAction();

                action.ActionId = GetNextActionNumber().ToString();
                action.Definition.Operation = "Create";
                action.Definition.Description = "Add task for PBI";
                action.Definition.WorkItemId = task.Id;
                action.Definition.WorkItemType = task.WorkItemType;
                action.Definition.ActionDay =
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION);
                action.Definition.Refname = "Title";
                action.Definition.FieldValue = task.Title;

                action.SetValue("IterationPath", $"Sprint {sprint.SprintNumber}");
                action.SetValue("PARENT", parentPbi.Id);

                action.SetValue("RemainingWork", task.RemainingWork.ToString());


                Actions.Add(action);
            }
        }
    }

    private WorkItemScriptAction GetCreateAction(
        WorkItemScriptWorkItem item,
        WorkItemScriptSprint sprint,
        int actionId, int createDateOffset = 0)
    {
        var returnValue = new WorkItemScriptAction();

        returnValue.ActionId = actionId.ToString();
        returnValue.Definition.Operation = "Create";
        returnValue.Definition.Description = "Create PBI";
        returnValue.Definition.WorkItemId = item.Id;
        returnValue.Definition.WorkItemType = item.WorkItemType;
        
        // always create the PBIs 1 week ahead of sprint
        // this avoids date problems in azdo
        returnValue.Definition.ActionDay =
            ((sprint.SprintNumber - 1) * SPRINT_DURATION) + createDateOffset;
        returnValue.Definition.Refname = "Title";
        returnValue.Definition.FieldValue = item.Title;

        returnValue.SetValue("System.Description",
            $"Created by action id '{actionId}'; Work item script id '{item.Id}';");

        return returnValue;
    }

    private void AddBasicStartingInfo(WorkItemScriptSprint sprint)
    {
        for (int i = 0; i < sprint.NewPbiCount; i++)
        {
            _createdWorkItemNumber += 100;

            var item = new WorkItemScriptWorkItem
            {
                Id = $"pbi-{_createdWorkItemNumber}",
                Title = GetRandomTitle(),
                WorkItemType = "PBI",
                State = "New",
                Iteration = string.Empty
            };

            ProductBacklogItems.Add(item.Id, item);
            ProductBacklogItemsThatNeedRefinementRound1.Add(item);

            Actions.Add(GetCreateAction(item, sprint, GetNextActionNumber(), -14));
        }

        ScriptRefinementMeeting1(sprint, createDateOffset: -14);
        ScriptRefinementMeeting2(sprint, createDateOffset: -14);
    }

    public void GenerateScript(List<WorkItemScriptSprint> sprints)
    {
        AddBasicStartingInfo(sprints[0]);

        foreach (var item in sprints)
        {
            GenerateScript(item);
        }
    }
}
