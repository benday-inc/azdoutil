namespace Benday.AzureDevOpsUtil.Api.Excel;
public class ExcelWorkItemIterationRowReader
{
    public ExcelWorkItemIterationRowReader(ExcelReader reader)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public ExcelReader Reader { get; }

    public List<IterationRow> GetRows()
    {
        var returnValue = new List<IterationRow>();

        PopulateRows(returnValue);

        return returnValue;
    }

    private void PopulateRows(List<IterationRow> toValues)
    {
        var sheetName = ExcelConstants.SheetNameIterations;

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

    private IterationRow GetRow(ExcelRowWrapper row)
    {
        var temp = new IterationRow
        {
            ExcelRowId = row.RowIndex,

            IterationName = row[
            ExcelConstants.ColumnNameIterationName],

            StartDay = ToInt32(row[
            ExcelConstants.ColumnNameStartDay],
            ExcelConstants.ColumnNameStartDay,
            row.RowIndex),

            EndDay = ToInt32(row[
            ExcelConstants.ColumnNameEndDay],
            ExcelConstants.ColumnNameEndDay,
            row.RowIndex),
        };

        return temp;
    }
}
