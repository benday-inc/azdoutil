using Benday.AzureDevOpsUtil.Api.ScriptGenerator;

namespace Benday.AzureDevOpsUtil.Api.Excel;
public class ExcelWorkItemScriptRowReader
{
    public ExcelWorkItemScriptRowReader(ExcelReader reader)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public ExcelReader Reader { get; }

    public List<WorkItemScriptRow> GetRows()
    {
        var returnValue = new List<WorkItemScriptRow>();

        PopulateRows(returnValue);

        return returnValue;
    }

    private void PopulateRows(List<WorkItemScriptRow> toValues)
    {
        var sheetName = ExcelConstants.SheetNameScript;

        var rows = Reader.GetRows(sheetName);
        foreach (var row in rows)
        {
            var tempRow = GetRow(row);
            toValues.Add(tempRow);
        }
    }

    private int ToInt32(string fromValue, string fieldName, int rowIndex)
    {
        if (string.IsNullOrEmpty(fromValue) == true)
        {
            return -99999;
        }
        else
        {
            if (int.TryParse(fromValue, out var returnValue) == true)
            {
                return returnValue;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Could not convert row {rowIndex} value for {fieldName} to int: {fromValue}");
            }
        }
    }

    private WorkItemScriptRow GetRow(ExcelRowWrapper row)
    {
        var temp = new WorkItemScriptRow
        {
            ExcelRowId = row.RowIndex,

            ActionId = row[
            ExcelConstants.ColumnNameActionId],

            Description = row[
            ExcelConstants.ColumnNameDescription],

            WorkItemId = row[
            ExcelConstants.ColumnNameWorkItemId],

            Operation = row[
            ExcelConstants.ColumnNameOperation],

            WorkItemType = row[
            ExcelConstants.ColumnNameWorkItemType],

            ActionDay = ToInt32(row[
            ExcelConstants.ColumnNameActionDay],
            ExcelConstants.ColumnNameActionDay,
            row.RowIndex),

            ActionHour = ToInt32(row[
            ExcelConstants.ColumnNameActionHour],
            ExcelConstants.ColumnNameActionHour,
            row.RowIndex),

            ActionMinute = ToInt32(row[
            ExcelConstants.ColumnNameActionMinute],
            ExcelConstants.ColumnNameActionMinute,
            row.RowIndex),

            Refname = row[
            ExcelConstants.ColumnNameRefname],

            FieldValue = row[ExcelConstants.ColumnNameFieldValue]
        };

        return temp;
    }
}
