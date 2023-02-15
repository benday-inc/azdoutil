using System.Text;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;
public class WorkItemScriptRow
{
    private string _refname = string.Empty;
    private string _fieldValue = string.Empty;

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

    public string ActionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WorkItemId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public int ActionDay { get; set; }
    public int ActionHour { get; set; }
    public int ActionMinute { get; set; }
    public string Refname
    {
        get => _refname;
        set
        {
            if (value == null)
            {
                _refname = string.Empty;
            }
            else
            {
                _refname = value.Trim();
            }
        }

    }
    public string FieldValue { get => _fieldValue;

        set
        {
            if (value == null)
            {
                _fieldValue = string.Empty;
            }
            else
            {
                _fieldValue = value.Trim();
            }
        }

    }
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
