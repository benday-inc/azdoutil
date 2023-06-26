using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;


public class AgingWorkItemData
{
    public int WorkItemId { get; set; }
    public DateTime InProgressDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AreaSK { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;

    [JsonIgnore]
    public double AgeInDays
    {
        get
        {
            return (float)(DateTime.UtcNow - InProgressDate).TotalDays;
        }
    }
}