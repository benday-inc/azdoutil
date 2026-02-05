namespace Benday.AzureDevOpsUtil.Api.BuildReadiness;

public class DelegateFileContentProvider : IFileContentProvider
{
    private readonly Func<string, Task<string?>> _fetchFileAsync;

    public DelegateFileContentProvider(Func<string, Task<string?>> fetchFileAsync)
    {
        _fetchFileAsync = fetchFileAsync ?? throw new ArgumentNullException(nameof(fetchFileAsync));
    }

    public Task<string?> GetFileContentAsync(string filePath)
    {
        return _fetchFileAsync(filePath);
    }
}
