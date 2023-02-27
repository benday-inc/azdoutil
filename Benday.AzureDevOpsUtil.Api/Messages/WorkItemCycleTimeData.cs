namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemCycleTimeData
{
    public int WorkItemId { get; set; }
    public float CycleTimeDays { get; set; }
    public int CompletedDateSK { get; set; }
    public string Title { get; set; } = string.Empty;
}


