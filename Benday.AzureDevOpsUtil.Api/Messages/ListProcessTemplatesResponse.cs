using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ListProcessTemplatesResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("value")]
    public ProcessTemplateInfo[] Values { get; set; } = new ProcessTemplateInfo[0];
}
