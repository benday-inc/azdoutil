namespace Benday.AzureDevOpsUtil.Api;

public static class Utilities
{
    public static void AssertNotNull<T>(T value, string valueName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Value '{valueName}' was null.");
        }
    }

    public static int GetIndexForPercentForecast(
        int totalItems, int percent, bool debug = false)
    {
        if (totalItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalItems), totalItems, "Value must be greater than or equal to zero.");
        }

        if (percent < 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Value must be between 0 and 100.");
        }

        if (totalItems == 0)
        {
            return -1;
        }
        else if (percent == 0)
        {
            return 0;
        }
        else
        {
            var percentAsDouble = (percent / 100.0);

            var indexAsDouble = totalItems * percentAsDouble;

            var indexForPercentForecastAsDouble = Math.Round(
                indexAsDouble,
                MidpointRounding.AwayFromZero);

            var indexAsInt = Convert.ToInt32(indexForPercentForecastAsDouble);

            var indexForPercentForecast = indexAsInt - 1;

            if (indexForPercentForecast < 0)
            {
                indexForPercentForecast = -1;
            }

            if (debug == true)
            {
                Console.WriteLine($"totalItems={totalItems}");
                Console.WriteLine($"percent={percent}");
                Console.WriteLine($"indexAsDouble={indexAsDouble}");
                Console.WriteLine($"indexForPercentForecastAsDouble={indexForPercentForecastAsDouble}");
                Console.WriteLine($"indexAsInt={indexAsInt}");
                Console.WriteLine($"indexForPercentForecast={indexForPercentForecast}");
            }

            return indexForPercentForecast;
        }
    }
}
