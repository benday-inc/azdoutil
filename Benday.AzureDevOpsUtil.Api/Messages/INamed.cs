namespace Benday.AzureDevOpsUtil.Api.Messages;

public interface INamed
{
    string Id { get; set; }
    string Name { get; set; }
}