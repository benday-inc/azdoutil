using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemRelationAttributes
{
    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("authorizedDate")]
    public DateTime AuthorizedDate { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("resourceCreatedDate")]
    public DateTime ResourceCreatedDate { get; set; }
    [JsonPropertyName("resourceModifiedDate")]
    public DateTime ResourceModifiedDate { get; set; }

    [JsonPropertyName("revisedDate")]
    public DateTime RevisedDate { get; set; }

    [JsonPropertyName("resourceSize")]
    public long ResourceSize { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}