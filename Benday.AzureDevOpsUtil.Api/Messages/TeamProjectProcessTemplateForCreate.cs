using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class TeamProjectProcessTemplateForCreate
{
    [JsonPropertyName("templateTypeId")]
    public string TemplateTypeId { get; set; } = string.Empty;
}