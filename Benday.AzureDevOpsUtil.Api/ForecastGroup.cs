namespace Benday.AzureDevOpsUtil.Api;

public class ForecastGroup
{
    private readonly List<IterationForecast> _forecasts = new();

    public List<IterationForecast> Forecasts => _forecasts;

    public void Add(IterationForecast forecast)
    {
        Forecasts.Add(forecast);
    }

    public int TotalThroughput
    {
        get
        {
            return _forecasts.Sum(x => x.Throughput);
        }
    }
}