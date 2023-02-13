using System;
using System.Collections.Generic;
using System.Linq;

namespace Benday.AzureDevOpsUtil.Api;

public class WorkItemScriptAction
{
    public List<WorkItemScriptRow> Rows { get; set; } = new();
    public string ActionId { get; set; } = string.Empty;
    public WorkItemScriptRow Definition
    {
        get
        {
            if (Rows.Count == 0)
            {
                throw new InvalidOperationException("No definition row");
            }
            else
            {
                return Rows[0];
            }
        }
    }
    public DateTime GetActionDate(DateTime startDate)
    {
        startDate = startDate.AddDays(Definition.ActionDay);
        startDate = startDate.AddHours(Definition.ActionHour);
        startDate = startDate.AddMinutes(Definition.ActionMinute);

        return startDate;
    }

    public bool Skip { get; set; } = false;
}
