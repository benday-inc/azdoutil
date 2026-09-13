using MiniExcelLibs;

namespace Benday.AzureDevOpsUtil.Api.Excel;

/// <summary>
/// Reads a worksheet as a header row followed by data rows, keyed by the
/// header text. Built on MiniExcel (Apache-2.0). This used to sit on EPPlus,
/// whose Polyform Noncommercial license has to be acknowledged in code before
/// the first workbook is opened -- and only the write path did, so a fresh
/// process could write a script but not read one back.
/// </summary>
public class ExcelReader
{
    private readonly string _PathToExcelFile;
    private readonly List<string> _SheetNames;

    public ExcelReader(string pathToExcelFile)
    {
        _PathToExcelFile = pathToExcelFile;

        _SheetNames = PopulateSheetNames();
    }

    public List<string> SheetNames
    {
        get
        {
            return _SheetNames;
        }
    }

    private List<string> PopulateSheetNames()
    {
        using var stream = OpenForRead();

        return MiniExcel.GetSheetNames(stream);
    }

    private FileStream OpenForRead()
    {
        // NOTE: open the file and ignore whether any other process has it open
        return File.Open(_PathToExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public List<ExcelRowWrapper> GetRows(int sheetIndex)
    {
        var name = SheetNames[sheetIndex];

        return GetRows(name);
    }

    public List<ExcelRowWrapper> GetRows(string sheetName)
    {
        AssertSheetExists(sheetName);

        var returnValue = new List<ExcelRowWrapper>();

        Dictionary<string, string>? mappings = null;

        // row numbers are 1-based like Excel's, so they line up with what the
        // user sees when a script step is reported as failing
        var rowIndex = 0;

        foreach (var row in ReadRawRows(sheetName))
        {
            rowIndex++;

            if (mappings == null)
            {
                // first row is the header row
                mappings = GetColumnMappings(row);
            }
            else
            {
                var wrapper = new ExcelRowWrapper(mappings, row, rowIndex);

                if (wrapper.IsRowEmpty == false)
                {
                    returnValue.Add(wrapper);
                }
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Header text to 1-based column index for the named sheet.
    /// </summary>
    public Dictionary<string, int> GetColumnMappings(string sheetName)
    {
        AssertSheetExists(sheetName);

        var headerRow = ReadRawRows(sheetName).FirstOrDefault();

        if (headerRow == null)
        {
            return new Dictionary<string, int>();
        }

        return GetColumnMappings(headerRow)
            .ToDictionary(kv => kv.Key, kv => ColumnLetterToIndex(kv.Value));
    }

    private void AssertSheetExists(string sheetName)
    {
        if (SheetNames.Contains(sheetName) == false)
        {
            throw new InvalidOperationException($"Invalid sheet name '{sheetName}' in file '{_PathToExcelFile}'.");
        }
    }

    /// <summary>
    /// Every row of the sheet, header included, as column letter to cell value.
    /// The enumeration owns the file handle, so it has to be consumed inside
    /// the caller's loop rather than returned to someone else.
    /// </summary>
    private IEnumerable<IDictionary<string, object?>> ReadRawRows(string sheetName)
    {
        using var stream = OpenForRead();

        foreach (var row in MiniExcel.Query(stream, useHeaderRow: false, sheetName: sheetName))
        {
            yield return (IDictionary<string, object?>)row;
        }
    }

    /// <summary>
    /// Header text to column letter, read from the header row.
    /// </summary>
    private static Dictionary<string, string> GetColumnMappings(IDictionary<string, object?> headerRow)
    {
        var mappings = new Dictionary<string, string>();

        foreach (var cell in headerRow)
        {
            var value = ExcelRowWrapper.CellToString(cell.Value);

            if (string.IsNullOrWhiteSpace(value) == false)
            {
                if (mappings.ContainsKey(value) == true)
                {
                    throw new InvalidOperationException(string.Format("Duplicate column name '{0}'", value));
                }

                mappings.Add(value, cell.Key);
            }
        }

        return mappings;
    }

    private static int ColumnLetterToIndex(string columnLetters)
    {
        var index = 0;

        foreach (var letter in columnLetters.ToUpperInvariant())
        {
            index = (index * 26) + (letter - 'A' + 1);
        }

        return index;
    }
}
