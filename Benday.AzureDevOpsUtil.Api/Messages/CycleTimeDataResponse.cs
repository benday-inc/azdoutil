using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class CycleTimeDataResponse
{
    [JsonPropertyName("odataContext")]
    public string OdataContext { get; set; } = string.Empty;


    [JsonPropertyName("value")]
    public WorkItemCycleTimeData[] Items { get; set; } = new WorkItemCycleTimeData[0];
}

public class AgingWorkItemDataResponse
{
    [JsonPropertyName("odataContext")]
    public string OdataContext { get; set; } = string.Empty;


    [JsonPropertyName("value")]
    public AgingWorkItemData[] Items { get; set; } = new AgingWorkItemData[0];
}
