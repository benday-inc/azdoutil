using OfficeOpenXml;

using static OfficeOpenXml.ExcelErrorValue;

namespace Benday.AzureDevOpsUtil.Api.Excel;

public class ExcelRowWriteWrapper
{
    private readonly ExcelWorksheet _sheet;
    private readonly Dictionary<string, int> _mappings;
    private readonly int _rowIndex;

    public ExcelRowWriteWrapper(Dictionary<string, int> mappings, ExcelWorksheet sheet, int rowIndex) 
    {
        _rowIndex = rowIndex;
        _sheet = sheet;
        _mappings = mappings;
    }

    public string this[string columnName]
    {
        set
        {
            if (_mappings.ContainsKey(columnName) == false)
            {
                throw new InvalidOperationException($"Unknown column '{columnName}'");
            }
            else
            {
                _sheet.SetValue(_rowIndex, _mappings[columnName], value);
            }
        }
    }
}