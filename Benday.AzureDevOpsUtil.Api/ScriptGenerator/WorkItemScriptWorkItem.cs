namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

public class WorkItemScriptWorkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Iteration { get; set; } = string.Empty;
}