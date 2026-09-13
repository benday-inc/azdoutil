using Benday.AzureDevOpsUtil.Api.ScriptGenerator;

using MiniExcelLibs;

namespace Benday.AzureDevOpsUtil.Api.Excel;

/// <summary>
/// Writes a generated work item script to Excel in the layout
/// <see cref="ExcelReader"/> reads back: a Script sheet and an Iterations
/// sheet, each a header row followed by data rows.
/// </summary>
public class ExcelWorkItemScriptWriter
{
    public void WriteToExcel(string filename, List<WorkItemScriptAction> actions)
    {
        var dir = Path.GetDirectoryName(filename) ?? throw new InvalidOperationException();

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // a dictionary of sheet name to rows writes one sheet per entry, in
        // this order; the first row's keys become that sheet's header row
        var sheets = new Dictionary<string, object>
        {
            { ExcelConstants.SheetNameScript, GetActionRows(actions) },
            { ExcelConstants.SheetNameIterations, GetIterationRows() }
        };

        MiniExcel.SaveAs(filename, sheets, overwriteFile: true);
    }

    private static List<Dictionary<string, object?>> GetIterationRows()
    {
        var rows = new List<Dictionary<string, object?>>();

        for (var sprintNumber = 1; sprintNumber <= 6; sprintNumber++)
        {
            var sprintStartDate = ((sprintNumber - 1) * 14);
            var sprintEndDate = (sprintNumber * 14) - 1;

            rows.Add(new Dictionary<string, object?>
            {
                { ExcelConstants.ColumnNameIterationName, $"Sprint {sprintNumber}" },
                { ExcelConstants.ColumnNameStartDay, sprintStartDate },
                { ExcelConstants.ColumnNameEndDay, sprintEndDate }
            });
        }

        return rows;
    }

    private static List<Dictionary<string, object?>> GetActionRows(List<WorkItemScriptAction> actions)
    {
        var rows = new List<Dictionary<string, object?>>();

        foreach (var action in actions)
        {
            var row = NewActionRow();

            row[ExcelConstants.ColumnNameActionId] = action.ActionId;
            row[ExcelConstants.ColumnNameDescription] = action.Definition.Description;
            row[ExcelConstants.ColumnNameWorkItemId] = action.Definition.WorkItemId;
            row[ExcelConstants.ColumnNameOperation] = action.Definition.Operation;
            row[ExcelConstants.ColumnNameWorkItemType] = action.Definition.WorkItemType;
            row[ExcelConstants.ColumnNameActionDay] = action.Definition.ActionDay.ToString();
            row[ExcelConstants.ColumnNameActionHour] = action.Definition.ActionHour.ToString();
            row[ExcelConstants.ColumnNameActionMinute] = action.Definition.ActionMinute.ToString();
            row[ExcelConstants.ColumnNameRefname] = action.Definition.Refname;
            row[ExcelConstants.ColumnNameFieldValue] = action.Definition.FieldValue;

            rows.Add(row);

            // the remaining rows of an action carry only a field and a value;
            // the reader attaches them to the action above by the blank ActionId
            foreach (var childRow in action.Rows.Skip(1))
            {
                var childRowValues = NewActionRow();

                childRowValues[ExcelConstants.ColumnNameRefname] = childRow.Refname;
                childRowValues[ExcelConstants.ColumnNameFieldValue] = childRow.FieldValue;

                rows.Add(childRowValues);
            }
        }

        return rows;
    }

    /// <summary>
    /// Every column present, in header order, so the first row defines the
    /// full header and a child row keeps its values in the right columns.
    /// </summary>
    private static Dictionary<string, object?> NewActionRow()
    {
        return new Dictionary<string, object?>
        {
            { ExcelConstants.ColumnNameActionId, null },
            { ExcelConstants.ColumnNameDescription, null },
            { ExcelConstants.ColumnNameWorkItemId, null },
            { ExcelConstants.ColumnNameOperation, null },
            { ExcelConstants.ColumnNameWorkItemType, null },
            { ExcelConstants.ColumnNameActionDay, null },
            { ExcelConstants.ColumnNameActionHour, null },
            { ExcelConstants.ColumnNameActionMinute, null },
            { ExcelConstants.ColumnNameRefname, null },
            { ExcelConstants.ColumnNameFieldValue, null }
        };
    }
}
