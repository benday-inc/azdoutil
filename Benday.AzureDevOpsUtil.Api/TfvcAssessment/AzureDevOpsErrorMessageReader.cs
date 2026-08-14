using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Pulls the human-readable message out of an Azure DevOps error response.
///
/// A failed call carries a "message" property that says what actually went
/// wrong ("VS403405: The item $/Foo does not exist on the server, or you do not
/// have permission to access it."), which is worth considerably more to
/// somebody running the tool than "404 Not Found".
/// </summary>
public static class AzureDevOpsErrorMessageReader
{
    /// <summary>
    /// Returns the message from the response body, or
    /// <paramref name="fallback"/> when the body is empty, is not json, or
    /// carries no message.
    /// </summary>
    public static string GetMessageOrDefault(string? responseBody, string fallback)
    {
        if (string.IsNullOrWhiteSpace(responseBody) == true)
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "message", StringComparison.OrdinalIgnoreCase) ==
                    false)
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var message = property.Value.GetString();

                if (string.IsNullOrWhiteSpace(message) == false)
                {
                    return message;
                }
            }

            return fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
