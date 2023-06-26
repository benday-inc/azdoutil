using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class AgingWorkItemDataResponse
{
    [JsonPropertyName("odataContext")]
    public string OdataContext { get; set; } = string.Empty;


    [JsonPropertyName("value")]
    public AgingWorkItemData[] Items { get; set; } = new AgingWorkItemData[0];
}
