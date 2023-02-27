using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api;

public class ThroughputIteration
{
    private readonly int _weekOfYear;
    private readonly DateTime _startOfWeek;

    public ThroughputIteration(int weekOfYear, DateTime startOfWeek)
    {
        _weekOfYear = weekOfYear;
        _startOfWeek = startOfWeek;
    }

    public string WeekOfYearAsString
    {
        get
        {
            return _weekOfYear.ToString("00");
        }
    }

    public DateTime StartOfWeek
    {
        get
        {
            return _startOfWeek;
        }
    }

    public int WeekOfYear
    {
        get
        {
            return _weekOfYear;
        }
    }

    internal void Add(WorkItemCycleTimeData item)
    {
        Items.Add(item);
    }

    public List<WorkItemCycleTimeData> Items { get; set; } = new();

    public float AverageCycleTime
    {
        get
        {
            if (Items.Count < 1) 
            {             
                return 0;
            }
            else
            {
                return Items.Average(x => x.CycleTimeDays);
            }
        }
    }
}