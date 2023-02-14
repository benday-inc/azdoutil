using System;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;
public class WorkItemScriptGenerator
{
    private const int SPRINT_DURATION = 14;
    public readonly List<string> FibonnaciValues =
        new() {
        "1", "2", "3", "5", "8", "13", "21"
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
    public List<WorkItemScriptWorkItem> ProductBacklogItemsThatNeedRefinementRound1 { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsThatNeedRefinementRound2 { get; set; } = new();
    public List<WorkItemScriptWorkItem> ProductBacklogItemsReadyForSprint { get; set; } = new();
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

        ScriptRefinementMeeting1(sprint);
        ScriptRefinementMeeting2(sprint);
    }

    private int GetNextActionNumber()
    {
        return ((++_createdActionNumber) * 100);
    }

    private void ScriptRefinementMeeting1(WorkItemScriptSprint sprint, bool randomizeNumber = false)
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
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 3;
                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Needs Refinement";

                Actions.Add(action);
            }
        }
    }

    private void ScriptRefinementMeeting2(WorkItemScriptSprint sprint, bool randomizeNumber = false)
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
                    ((sprint.SprintNumber - 1) * SPRINT_DURATION) + 10;
                action.Definition.Refname = "Status";
                action.Definition.FieldValue = "Ready for Sprint";

                action.AddSetValue("Effort", FibonnaciValues.Random());

                Actions.Add(action);
            }
        }
    }

    private WorkItemScriptAction GetCreateAction(
        WorkItemScriptWorkItem item,
        WorkItemScriptSprint sprint,
        int actionId)
    {
        var returnValue = new WorkItemScriptAction();

        returnValue.ActionId = actionId.ToString();
        returnValue.Definition.Operation = "Create";
        returnValue.Definition.Description = "Create PBI";
        returnValue.Definition.WorkItemId = item.Id;
        returnValue.Definition.WorkItemType = item.WorkItemType;
        returnValue.Definition.ActionDay = 
            ((sprint.SprintNumber - 1)  * SPRINT_DURATION);
        returnValue.Definition.Refname = "Title";
        returnValue.Definition.FieldValue = item.Title;

        return returnValue;
    }
   
    public void GenerateScript(List<WorkItemScriptSprint> sprints)
    {
        foreach (var item in sprints)
        {
            GenerateScript(item);
        }
    }
}
