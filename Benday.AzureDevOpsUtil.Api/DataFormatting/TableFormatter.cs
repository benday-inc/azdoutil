using System;
using System.Text;

namespace Benday.AzureDevOpsUtil.Api.DataFormatting;

public class TableFormatter
{
    public TableFormatter()
    {
        Columns = new List<TableColumnDefinition>();
    }

    public List<TableColumnDefinition> Columns { get; }
    public List<string[]> Data { get; } = new List<string[]>();

    public TableColumnDefinition AddColumn(string columnName)
    {
        var newItem = new TableColumnDefinition()
        {
            Name = columnName
        };

        Columns.Add(newItem);

        return newItem;
    }

    public void AddData(params string[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "data is null.");
        }

        if (data.Length == 0)
        {
            throw new ArgumentException("data is empty.", nameof(data));
        }

        if (data.Length != Columns.Count)
        {
            throw new ArgumentException(
                $"Expected {Columns.Count} columns but received {data.Length} columns.",
                nameof(data));
        }

        Data.Add(data);

        for (int index = 0; index < data.Length; index++)
        {
            var column = Columns[index];

            if (data[index] == null)
            {
                data[index] = string.Empty;
            }

            column.CheckValueLength(data[index]);            
        }
    }

    public string FormatTable()
    {
        var builder = new StringBuilder();

        foreach (var column in Columns)
        {
            builder.Append(column.NamePadded);

            if (column != Columns.Last())
            {
                builder.Append(" ");
            }
        }

        builder.AppendLine();

        foreach (var row in Data)
        {
            var needsSeparator = false;

            for (int index = 0; index < row.Length; index++)
            {
                if (needsSeparator == true)
                {
                    builder.Append(" ");
                }

                var column = Columns[index];

                var columnValue = row[index].PadRight(column.Width);

                builder.Append(columnValue);

                needsSeparator = true;
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Add data to the table if any of the values contain the filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="data"></param>    
    public void AddDataWithFilter(string filter, params string[] data)
    {
        if (data != null && data.Length > 0)
        {
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }
                
                if (item.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    AddData(data);
                    break;
                }
            }
        }
    }
}
