using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ConnectionDataUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

    [JsonPropertyName("subjectDescriptor")]
    public string SubjectDescriptor { get; set; } = string.Empty;

    [JsonPropertyName("providerDisplayName")]
    public string ProviderDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("properties")]
    public ConnectionDataUserProperties Properties { get; set; } = new();

    [JsonPropertyName("resourceVersion")]
    public int ResourceVersion { get; set; }

    [JsonPropertyName("metaTypeId")]
    public int MetaTypeId { get; set; }
}
