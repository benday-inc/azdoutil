using System.Text.Json;

using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

public static class JsonUtilities
{
    /// <summary>
    /// The deserialization settings this tool uses everywhere.  Azure DevOps
    /// sends camelCase and several of the message classes have no explicit
    /// property name attributes, so matching has to ignore case or those
    /// properties silently keep their default values.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T GetJsonValueAsType<T>(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json), "Argument cannot be null.");
        }

        try
        {
            var returnValue = JsonSerializer.Deserialize<T>(json, DefaultOptions);

            if (returnValue == null)
            {
                throw new InvalidOperationException($"Could not deserialize json to {nameof(T)}");
            }
            else
            {
                return returnValue;
            }
        }
        catch (JsonException ex)
        {
            json = json.Trim();

            var startsWithHtml = json.StartsWith("<!DOCTYPE html ");
            var containsSigninWarning = json.Contains("Azure DevOps Services | Sign In");

            if (startsWithHtml == true && containsSigninWarning == true)
            {
               throw new KnownException("Response from server indicates you are not signed in.  Did your token expire?");
            }
            else if (startsWithHtml == true)
            {
                throw new KnownException("Response from server is not json.");
            }
            else
            {
                throw new InvalidOperationException($"Failed to deserialize json.  {ex.Message}");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
