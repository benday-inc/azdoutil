namespace Benday.AzureDevOpsUtil.Api.AgentCapabilities;

/// <summary>
/// The handful of agent pool fields the capability operations need: enough to
/// name a pool and address its agents.
/// </summary>
public class AgentPoolSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHosted { get; set; }
}
