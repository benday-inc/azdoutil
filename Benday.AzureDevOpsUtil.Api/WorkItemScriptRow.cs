using System;
using System.Linq;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api;
public class WorkItemScriptRow
{
    public bool IsComment
    {
        get
        {
            if (ActionId == "COMMENT")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public string ActionId { get; set; }
    public string Description { get; set; }
    public string WorkItemId { get; set; }
    public string Operation { get; set; }
    public string WorkItemType { get; set; }
    public int ActionDay { get; set; }
    public int ActionHour { get; set; }
    public int ActionMinute { get; set; }
    public string Refname { get; set; }
    public string FieldValue { get; set; }
    public int ExcelRowId { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("*****");

        if (IsComment == true)
        {
            builder.AppendLine($"Excel Row Id: {ExcelRowId}");
            builder.AppendLine($"Action Id: {ActionId}");
            builder.AppendLine($"Description: {Description}");
        }
        else
        {
            builder.AppendLine($"Excel Row Id: {ExcelRowId}");
            builder.AppendLine($"Action Id: {ActionId}");
            builder.AppendLine($"Description: {Description}");
            builder.AppendLine($"Work Item Id: {WorkItemId}");
            builder.AppendLine($"Operation: {Operation}");
            builder.AppendLine($"Work Item Type: {WorkItemType}");
            builder.AppendLine($"Action Day: {ActionDay}");
            builder.AppendLine($"Action Hour: {ActionHour}");
            builder.AppendLine($"Action Minute: {ActionMinute}");
            builder.AppendLine($"Refname: {Refname}");
            builder.AppendLine($"Value: {FieldValue}");
        }

        return builder.ToString();
    }
}
