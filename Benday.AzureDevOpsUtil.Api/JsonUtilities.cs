using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api;

public static class JsonUtilities
{
    public static T GetJsonValueAsType<T>(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json), "Argument cannot be null.");
        }

        try
        {
            var returnValue = JsonSerializer.Deserialize<T>(json);

            if (returnValue == null)
            {
                throw new InvalidOperationException($"Could not deserialize json to {nameof(T)}");
            }
            else
            {
                return returnValue;
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
