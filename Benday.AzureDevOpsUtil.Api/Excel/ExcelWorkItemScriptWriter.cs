using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Benday.AzureDevOpsUtil.Api.ScriptGenerator;

using OfficeOpenXml;

namespace Benday.AzureDevOpsUtil.Api.Excel;
public class ExcelWorkItemScriptWriter
{
    public void WriteToExcel(string filename, List<WorkItemScriptAction> actions)
    {
        using (var excel = new OfficeOpenXml.ExcelPackage())
        {
            var worksheetScript = excel.Workbook.Worksheets.Add(ExcelConstants.SheetNameScript);
            var worksheetIterations = excel.Workbook.Worksheets.Add(ExcelConstants.SheetNameIterations);

            AddActions(excel, worksheetScript, actions);

            excel.SaveAs(filename);
        }
    }
    private void AddActions(ExcelPackage excel, 
        ExcelWorksheet worksheet, 
        List<WorkItemScriptAction> actions)
    {
        Dictionary<string, int> mappings = new();

        var columnIndex = 0;

        mappings.Add(ExcelConstants.ColumnNameActionId, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameDescription, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameWorkItemId, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameOperation, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameWorkItemType, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameActionDay, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameActionHour, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameActionMinute, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameRefname, ++columnIndex);
        mappings.Add(ExcelConstants.ColumnNameFieldValue, ++columnIndex);

        AddColumnHeaders(worksheet, mappings);
    }

    private void AddColumnHeaders(ExcelWorksheet worksheet, Dictionary<string, int> mappings)
    {
        foreach (var key in mappings.Keys)
        {
            worksheet.SetValue(1, mappings[key], key);
        }
    }
}
