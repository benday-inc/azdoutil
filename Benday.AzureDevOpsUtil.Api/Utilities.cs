namespace Benday.AzureDevOpsUtil.Api;

public static class Utilities
{
    public static void AssertNotNull<T>(T value, string valueName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Value '{valueName}' was null.");
        }
    }
}
