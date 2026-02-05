namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class ExternalReference
{
    public string ReferenceType { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
