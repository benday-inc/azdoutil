namespace Benday.AzureDevOpsUtil.Api;
public class WorkItemScriptActionParser
{
    public List<WorkItemScriptAction> GetActions(List<WorkItemScriptRow> rows)
    {
        var returnValue = new List<WorkItemScriptAction>();

        WorkItemScriptAction current = null;

        foreach (var row in rows)
        {
            if (row.IsComment == true)
            {
                continue;
            }
            else
            {
                if (current == null)
                {
                    if (string.IsNullOrEmpty(row.ActionId) == true)
                    {
                        throw new InvalidOperationException($"Expected non-empty action id for excel row {row.ExcelRowId}");
                    }

                    current = new WorkItemScriptAction();

                    current.ActionId = row.ActionId;

                    current.Rows.Add(row);

                    returnValue.Add(current);
                }
                else
                {
                    if (string.IsNullOrEmpty(row.ActionId) == true || current.ActionId == row.ActionId)
                    {
                        current.Rows.Add(row);
                    }
                    else
                    {
                        current = new WorkItemScriptAction();

                        current.ActionId = row.ActionId;

                        current.Rows.Add(row);

                        returnValue.Add(current);
                    }
                }
            }
        }

        return returnValue;
    }
}
