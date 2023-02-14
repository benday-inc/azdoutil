using System.Text;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

public static class RandomCollectionExtensionMethods
{
    public static string Random(this List<string> strings)
    {
        if (strings == null)
        {
            return string.Empty;
        }
        else if (strings.Count == 0)
        {
            return string.Empty;
        }
        else
        {
            var rnd = new RandomNumGen();

            var randomIndex = rnd.GetNumberInRange(0, strings.Count - 1);

            return strings[randomIndex];
        }
    }

    public static string RandomPhrase(this List<string> strings, int wordCount)
    {
        if (strings == null)
        {
            return string.Empty;
        }
        else if (strings.Count == 0)
        {
            return string.Empty;
        }

        var alreadyUsedIndexes = new List<int>();

        var rnd = new RandomNumGen();

        var numberOfWords = rnd.GetNumberInRange(1, wordCount);

        var builder = new StringBuilder();

        for (var i = 0; i < numberOfWords; i++)
        {
            var randomIndex = rnd.GetNumberInRange(0, strings.Count - 1);

            if (alreadyUsedIndexes.Contains(randomIndex) == true)
            {
                continue;
            }
            else
            {
                alreadyUsedIndexes.Add(randomIndex);
                builder.Append(' ');
                builder.Append(strings[randomIndex]);
            }
        }

        return builder.ToString();
    }
}
