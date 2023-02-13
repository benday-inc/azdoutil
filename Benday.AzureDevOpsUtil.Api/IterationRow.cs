using System.Text;

namespace Benday.AzureDevOpsUtil.Api;

public class IterationRow
{
    public string IterationName { get; set; } = string.Empty;
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public int ExcelRowId { get; set; }


    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("*****");

        builder.AppendLine($"Excel Row Id: {ExcelRowId}");
        builder.AppendLine($"Iteration Name: {IterationName}");
        builder.AppendLine($"Start Day: {StartDay}");
        builder.AppendLine($"End Day: {EndDay}");

        return builder.ToString();
    }

    public DateTime GetIterationStart(DateTime startDate)
    {
        return startDate.AddDays(StartDay);
    }

    public DateTime GetIterationEnd(DateTime startDate)
    {
        return startDate.AddDays(EndDay);
    }
}
