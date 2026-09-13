using System.Globalization;

namespace Benday.AzureDevOpsUtil.Api.Excel;

/// <summary>
/// One data row of a worksheet, addressable by header text.
/// </summary>
public class ExcelRowWrapper
{
    protected readonly Dictionary<string, string> _values = new();

    /// <param name="mappings">Header text to column letter</param>
    /// <param name="cells">Column letter to cell value for this row</param>
    /// <param name="rowIndex">1-based Excel row number</param>
    public ExcelRowWrapper(Dictionary<string, string> mappings, IDictionary<string, object?> cells, int rowIndex)
    {
        if (mappings == null)
        {
            throw new ArgumentNullException(nameof(mappings), "Argument cannot be null.");
        }

        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells), "Argument cannot be null.");
        }

        RowIndex = rowIndex;

        PopulateValues(cells, mappings);
    }

    private void PopulateValues(
        IDictionary<string, object?> cells,
        Dictionary<string, string> mappings)
    {
        var foundAValueInRow = false;

        foreach (var columnName in mappings.Keys)
        {
            var temp = GetValue(cells, mappings, columnName);
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

    /// <summary>
    /// The text of a cell the way the script readers expect it: numbers
    /// without a culture-specific decimal separator, booleans lower-cased,
    /// blanks as empty string.
    /// </summary>
    public static string CellToString(object? cellValue)
    {
        if (cellValue == null)
        {
            return string.Empty;
        }
        else if (cellValue is bool boolValue)
        {
            return boolValue.ToString().ToLower();
        }
        else if (cellValue is string stringValue)
        {
            return stringValue;
        }
        else
        {
            return Convert.ToString(cellValue, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    private static string GetValue(IDictionary<string, object?> cells,
        Dictionary<string, string> mappings, string columnName)
    {
        if (mappings.ContainsKey(columnName) == true)
        {
            var columnLetter = mappings[columnName];

            if (cells.TryGetValue(columnLetter, out var cellValue) == true)
            {
                return CellToString(cellValue);
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            throw new InvalidOperationException(string.Format("Unknown column name '{0}'", columnName));
        }
    }
}
