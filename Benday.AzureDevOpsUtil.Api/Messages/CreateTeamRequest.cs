using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CreateTeamRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}