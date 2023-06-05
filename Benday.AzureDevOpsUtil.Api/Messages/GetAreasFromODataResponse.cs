using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class GetAreasFromODataResponse
{
    [JsonPropertyName("odataContext")]
    public string OdataContext { get; set; } = string.Empty;


    [JsonPropertyName("value")]
    public AreaData[] Items { get; set; } = new AreaData[0];
}
