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
        ExcelPackage.LicenseContext = LicenseContext.Commercial;

        using (var excel = new OfficeOpenXml.ExcelPackage())
        {
            var worksheetScript = excel.Workbook.Worksheets.Add(ExcelConstants.SheetNameScript);
            var worksheetIterations = excel.Workbook.Worksheets.Add(ExcelConstants.SheetNameIterations);

            AddIterations(excel, worksheetIterations);
            AddActions(excel, worksheetScript, actions);

            var dir = Path.GetDirectoryName(filename) ?? throw new InvalidOperationException();

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            excel.SaveAs(filename);
        }
    }

    private void AddIterations(ExcelPackage excel, ExcelWorksheet worksheet)
    {
        worksheet.SetValue(1, 1, ExcelConstants.ColumnNameIterationName);
        worksheet.SetValue(1, 2, ExcelConstants.ColumnNameStartDay);
        worksheet.SetValue(1, 3, ExcelConstants.ColumnNameEndDay);
        
        AddIteration(worksheet, 1);
        AddIteration(worksheet, 2);
        AddIteration(worksheet, 3);
        AddIteration(worksheet, 4);
        AddIteration(worksheet, 5);
        AddIteration(worksheet, 6);
    }

    private static void AddIteration(
        ExcelWorksheet worksheet,
        int sprintNumber)
    {
        var rowIndex = sprintNumber + 1;

        var sprintStartDate = ((sprintNumber - 1) * 14);
        var sprintEndDate = (sprintNumber * 14) - 1;

        worksheet.SetValue(rowIndex, 1, $"Sprint {sprintNumber}");
        worksheet.SetValue(rowIndex, 2, sprintStartDate);
        worksheet.SetValue(rowIndex, 3, sprintEndDate);
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

        var rowIndex = 1;

        foreach ( var action in actions )
        {
            var row = new ExcelRowWriteWrapper(mappings, worksheet, ++rowIndex);

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

            if (action.Rows.Count > 1)
            {
                bool isFirst = true;

                foreach (var childRow in action.Rows)
                {
                    if (isFirst == true)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        var childRowWriter = new ExcelRowWriteWrapper(mappings, worksheet, ++rowIndex);

                        childRowWriter[ExcelConstants.ColumnNameRefname] = childRow.Refname;
                        childRowWriter[ExcelConstants.ColumnNameFieldValue] = childRow.FieldValue;
                    }
                }
            }
        }
    }

    private void AddColumnHeaders(ExcelWorksheet worksheet, Dictionary<string, int> mappings)
    {
        foreach (var key in mappings.Keys)
        {
            worksheet.SetValue(1, mappings[key], key);
        }
    }
}
