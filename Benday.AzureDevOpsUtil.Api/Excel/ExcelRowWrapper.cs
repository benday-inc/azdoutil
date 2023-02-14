using OfficeOpenXml;

namespace Benday.AzureDevOpsUtil.Api.Excel;

public class ExcelRowWrapper
{
    private readonly Dictionary<string, string> _values = new();

    public ExcelRowWrapper(Dictionary<string, int> mappings, ExcelWorksheet sheet, int rowIndex)
    {
        if (mappings == null)
        {
            throw new ArgumentNullException(nameof(mappings), "Argument cannot be null.");
        }

        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet), "Argument cannot be null.");
        }

        RowIndex = rowIndex;

        PopulateValues(sheet, mappings, RowIndex);
    }

    private void PopulateValues(
        ExcelWorksheet sheet,
        Dictionary<string, int> mappings,
        int rowIndex)
    {
        var foundAValueInRow = false;

        foreach (var columnName in mappings.Keys)
        {
            var temp = GetValue(sheet, mappings, rowIndex, columnName);
            if (foundAValueInRow == false &&
                string.IsNullOrWhiteSpace(temp) == false)
            {
                foundAValueInRow = true;
            }

            _values.Add(columnName, temp);
        }

        IsRowEmpty = !foundAValueInRow;
    }

    public bool IsRowEmpty { get; private set; }

    public int RowIndex { get; }

    public string this[string columnName]
    {
        get
        {
            if (_values.ContainsKey(columnName) == false)
            {
                return string.Empty;
            }
            else
            {
                return _values[columnName];
            }
        }
    }

    private string SafeToString(ExcelRange excelRange)
    {
        if (excelRange == null || excelRange.Text == null)
        {
            return string.Empty;
        }
        else
        {
            return excelRange.Text;
        }
    }

    private string GetValue(ExcelWorksheet sheet,
        Dictionary<string, int> mappings, int rowIndex, string columnName)
    {
        if (mappings.ContainsKey(columnName) == true)
        {
            var columnIndex = mappings[columnName];

            var range = sheet.Cells[rowIndex, columnIndex];

            if (range.Value is bool)
            {
                return range.GetValue<bool>().ToString().ToLower();
            }
            else
            {
                var value = SafeToString(range);

                return value;
            }
        }
        else
        {
            throw new InvalidOperationException(string.Format("Unknown column name '{0}'", columnName));
        }
    }
}
