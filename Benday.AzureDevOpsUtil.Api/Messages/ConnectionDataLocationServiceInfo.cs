using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class ConnectionDataLocationServiceInfo
{
    [JsonPropertyName("clientCacheTimeToLive")]
    public int ClientCacheTimeToLive { get; set; }

    [JsonPropertyName("defaultAccessMappingMoniker")]
    public string EefaultAccessMappingMoniker { get; set; } = string.Empty;

    [JsonPropertyName("lastChangeId")]
    public int LastChangeId { get; set; }

    [JsonPropertyName("lastChangeId64")]
    public int LastChangeId64 { get; set; }
}
