using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public partial class WorkItemDetailRequest
{
    public WorkItemDetailRequest()
    {
        Ids = new List<long>();
        Fields = new List<string>();
    }

    [JsonPropertyName("ids")]
    public List<long> Ids { get; set; }

    [JsonPropertyName("fields")]
    public List<string> Fields { get; set; }

    [JsonIgnore]
    public bool ExpandAll { get; set; }

    [JsonPropertyName("$expand")]
    public string ExpandValue
    {
        get
        {
            if (ExpandAll == false)
            {
                return "None";
            }
            else
            {
                return "All";
            }
        }
    }
}
