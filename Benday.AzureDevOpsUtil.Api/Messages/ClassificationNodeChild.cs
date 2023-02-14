using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ClassificationNodeChild : ClassificationNode
{
    [JsonPropertyName("attributes")]
    public ClassificationNodeAttributes Attributes { get; set; } = new();
}

