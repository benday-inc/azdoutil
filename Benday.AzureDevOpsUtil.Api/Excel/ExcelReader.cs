using OfficeOpenXml;
using System.ComponentModel;

namespace Benday.AzureDevOpsUtil.Api.Excel;

public class ExcelReader
{
    private readonly string _PathToExcelFile;
    private List<string> _SheetNames;

    public List<string> SheetNames
    {
        get
        {
            if (_SheetNames == null)
            {
                PopulateSheetNames();
            }

            return _SheetNames;
        }
    }

    public ExcelReader(string pathToExcelFile)
    {
        _PathToExcelFile = pathToExcelFile;
    }

    private void PopulateSheetNames()
    {
        var returnValue = new List<string>();

        using (var excel = new OfficeOpenXml.ExcelPackage())
        {
            // NOTE: open the file and ignore whether any other process has it open
            using (var stream = File.Open(_PathToExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                excel.Load(stream);
            }

            foreach (var sheet in excel.Workbook.Worksheets)
            {
                returnValue.Add(sheet.Name);
            }
        }

        _SheetNames = returnValue;
    }

    public List<ExcelRowWrapper> GetRows(int sheetIndex)
    {
        var name = SheetNames[sheetIndex];

        return GetRows(name);
    }

    public List<ExcelRowWrapper> GetRows(string sheetName)
    {
        var returnValue = new List<ExcelRowWrapper>();

        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;

        using (var excel = new OfficeOpenXml.ExcelPackage())
        {
            // NOTE: open the file and ignore whether any other process has it open
            using (var stream = File.Open(_PathToExcelFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                excel.Load(stream);
            }

            var mappings = GetColumnMappings(excel, sheetName);

            PopulateRows(returnValue, mappings, excel, sheetName);
        }

        return returnValue;
    }

    private void PopulateRows(List<ExcelRowWrapper> rows,
        Dictionary<string, int> mappings, ExcelPackage excel, string sheetName)
    {
        if (SheetNames.Contains(sheetName) == false)
        {
            throw new InvalidOperationException("Invalid sheet name.");
        }

        var sheetIndex = SheetNames.IndexOf(sheetName);

        var sheet = excel.Workbook.Worksheets[sheetIndex];

        var start = sheet.Dimension.Start;
        var end = sheet.Dimension.End;

        var rowCount = end.Row;

        // skip the first row because it's the header row
        var startingRow = start.Row + 1;
        _ = end.Column;
        _ = start.Column;
        for (var rowIndex = startingRow;
            rowIndex < rowCount + 1; rowIndex++)
        {
            var wrapper = new ExcelRowWrapper(mappings, sheet, rowIndex);
            if (wrapper.IsRowEmpty == false)
            {
                rows.Add(wrapper);
            }
        }
    }

    public Dictionary<string, int> GetColumnMappings(string sheetName)
    {
        using var excel = new OfficeOpenXml.ExcelPackage();
        // NOTE: open the file and ignore whether any other process has it open
        using (var stream = File.Open(_PathToExcelFile,
                                      FileMode.Open,
                                      FileAccess.Read,
                                      FileShare.ReadWrite))
        {
            excel.Load(stream);
        }

        return GetColumnMappings(excel, sheetName);
    }

    public Dictionary<string, int> GetColumnMappings()
    {
        throw new NotImplementedException();
    }

    private Dictionary<string, int> GetColumnMappings(
        OfficeOpenXml.ExcelPackage excel, string sheetName)
    {
        if (SheetNames.Contains(sheetName) == false)
        {
            throw new InvalidOperationException($"Invalid sheet name '{sheetName}' in file '{_PathToExcelFile}'.");
        }

        var sheetIndex = SheetNames.IndexOf(sheetName);

        var mappings = new Dictionary<string, int>();

        // NOTE: open the file and ignore whether any other process has it open
        using (var stream = File.Open(_PathToExcelFile,
                                      FileMode.Open,
                                      FileAccess.Read,
                                      FileShare.ReadWrite))
        {
            excel.Load(stream);
        }

        var sheet = excel.Workbook.Worksheets[sheetIndex];

        var start = sheet.Dimension.Start;
        var end = sheet.Dimension.End;
        _ = end.Row;
        var startingRow = start.Row;
        var columnCount = end.Column;
        var startingColumn = start.Column;

        for (var columnIndex = startingColumn; columnIndex < columnCount + 1; columnIndex++)
        {
            AddColumnMapping(mappings, sheet, startingRow, columnIndex);
        }

        return mappings;
    }

    private void AddColumnMapping(Dictionary<string, int> mappings,
        ExcelWorksheet sheet, int rowIndex, int columnIndex)
    {
        var value = SafeToString(sheet.Cells[rowIndex, columnIndex]);

        if (string.IsNullOrWhiteSpace(value) == false)
        {
            if (mappings.ContainsKey(value) == true)
            {
                throw new InvalidOperationException(string.Format("Duplicate column name '{0}'", value));
            }

            mappings.Add(value, columnIndex);
        }
    }

    private static string SafeToString(ExcelRange excelRange)
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
}
