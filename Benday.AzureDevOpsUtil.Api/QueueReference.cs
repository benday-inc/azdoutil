namespace Benday.AzureDevOpsUtil.Api;


public class QueueReference
{
    public int QueueId { get; set; }
    public string QueueName { get; set; } = string.Empty;
    
    public int EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;

    public string AgentSpecification { get; set; } = string.Empty;
}
