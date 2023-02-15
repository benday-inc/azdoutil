namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

public class WorkItemScriptWorkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Iteration { get; set; } = string.Empty;
    public int RemainingWork { get; set; } = 0;
    public WorkItemScriptWorkItem? Parent { get; set; }

    public List<WorkItemScriptWorkItem> ChildItems { get; set; } = new List<WorkItemScriptWorkItem>();

    public int TotalRemainingWork
    {
        get
        {
            if (ChildItems.Count == 0)
            {
                return 0;
            }
            else
            {
                int total = 0;

                foreach (var child in ChildItems)
                {
                    total += child.RemainingWork;
                }

                return total;
            }
        }
    }
    public bool IsDone { get; set; }
    public string Effort { get; set; } = "0";
}