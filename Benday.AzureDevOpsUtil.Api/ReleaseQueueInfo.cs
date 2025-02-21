using System.Diagnostics;

namespace Benday.AzureDevOpsUtil.Api;

[DebuggerDisplay("ReleaseId: {ReleaseId}, Name: {ReleaseName}, Project: {TeamProjectName}")]
public class ReleaseQueueInfo
{
    public int ReleaseId { get; set; }
    public string ReleaseName { get; set; } = string.Empty;
    public string TeamProjectName { get; set; } = string.Empty;
    public string TeamProjectId { get; set; } = string.Empty;

    public List<QueueReference> QueueReferences { get; set; } = new();

    public void AddQueue(QueueReference queue)
    {
        QueueReferences.Add(queue);
    }

    public void AddQueue(
        int queueId, string queueName, int environmentId,
        string environmentName, string agentSpecification)
    {
        var queue = new QueueReference();
        queue.QueueId = queueId;
        queue.QueueName = queueName;
        queue.EnvironmentId = environmentId;
        queue.EnvironmentName = environmentName;
        queue.AgentSpecification = agentSpecification;
        AddQueue(queue);
    }    
}