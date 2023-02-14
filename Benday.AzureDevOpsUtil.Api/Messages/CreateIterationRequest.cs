using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;
public class CreateIterationRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public ClassificationNodeAttributes Attributes { get; set; } = new ClassificationNodeAttributes();
}
