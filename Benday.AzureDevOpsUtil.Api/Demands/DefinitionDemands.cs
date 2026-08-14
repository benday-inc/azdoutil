using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.Demands;

/// <summary>
/// One build or release definition that has at least one demand, plus the
/// demands it carries.  This is what <c>finddemands</c> reports.
/// </summary>
public class DefinitionDemands
{
    /// <summary>"Build" or "Release".</summary>
    [JsonPropertyName("definitionType")]
    public string DefinitionType { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The pool or queue the definition targets, when it could be read from the
    /// list response.  Informational, so the report can say which agents the
    /// demands will be matched against.
    /// </summary>
    [JsonPropertyName("poolOrQueue")]
    public string PoolOrQueue { get; set; } = string.Empty;

    [JsonPropertyName("demands")]
    public List<string> Demands { get; set; } = new();
}
