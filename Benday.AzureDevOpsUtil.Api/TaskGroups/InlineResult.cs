namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public class InlineResult
{
    public int InlinedReferenceCount { get; set; }
    public List<string> InlinedTaskGroupIds { get; set; } = new();
}
