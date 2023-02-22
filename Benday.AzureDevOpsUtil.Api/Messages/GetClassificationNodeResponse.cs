using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GetClassificationNodeResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public ClassificationNode[] Value { get; set; } = new ClassificationNode[0];
}                                                              