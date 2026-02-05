namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public interface IFileContentProvider
{
    Task<string?> GetFileContentAsync(string filePath);
}
