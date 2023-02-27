namespace Benday.AzureDevOpsUtil.Api;

public class IterationForecast
{
    public IterationForecast(int throughput)
    {
        Throughput = throughput;
    }

    public int Throughput { get; }
}