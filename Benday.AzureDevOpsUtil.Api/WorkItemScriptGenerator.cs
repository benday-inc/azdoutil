using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Benday.WorkItemUtility.Api;
public class WorkItemScriptGenerator
{
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


}
