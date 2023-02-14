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

    public static T? RandomItem<T>(this List<T> items) where T : class
    {
        if (items.Count == 0)
        {
            return null;
        }

        var rnd = new RandomNumGen();

        var topOfRange = items.Count - 1;

        if (topOfRange < 0)
        {
            topOfRange = 0;
        }

        var randomIndex = rnd.GetNumberInRange(0, topOfRange);

        if (randomIndex < items.Count)
        {
            return items[randomIndex];
        }
        else
        {
            return null;
        }       
    }

    public static int RandomItem(this List<int> items)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        var rnd = new RandomNumGen();

        var topOfRange = items.Count - 1;

        if (topOfRange < 0)
        {
            topOfRange = 0;
        }

        var randomIndex = rnd.GetNumberInRange(0, topOfRange);

        if (randomIndex < items.Count)
        {
            return items[randomIndex];
        }
        else
        {
            return 0;
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
