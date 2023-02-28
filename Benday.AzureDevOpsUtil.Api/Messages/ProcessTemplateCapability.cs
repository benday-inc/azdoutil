using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ProcessTemplateCapability
{
    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("templateTypeId")]
    public string TemplateTypeId { get; set; } = string.Empty;
}

