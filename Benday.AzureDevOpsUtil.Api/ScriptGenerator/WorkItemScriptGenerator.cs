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

    //public List<WorkItemScriptAction> GetWorkItemScript()
    //{

    //}

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

    public List<WorkItemScriptWorkItem> ProductBacklogItems { get; set; } = new();
    public List<WorkItemScriptAction> Actions { get; set; } = new();

    public void GenerateScript(WorkItemScriptSprint sprint)
    {
        if (sprint == null)
        {
            throw new ArgumentNullException(nameof(sprint), $"{nameof(sprint)} is null.");
        }

        var createdWorkItemNumber = 100;

        for (int i = 0; i < sprint.NewPbiCount; i++)
        {
            createdWorkItemNumber += 100;

            var item = new WorkItemScriptWorkItem
            {
                Id = $"pbi-{createdWorkItemNumber}",
                Title = GetRandomTitle(),
                WorkItemType = "PBI",
                State = "New",
                Iteration= string.Empty
            };

            ProductBacklogItems.Add(item);

            Actions.Add(GetCreateAction(item, sprint, ((i + 1) * 100)));
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
}
