namespace Benday.AzureDevOpsUtil.Api;


public class ServerCallGotDeadlockMessageException : Exception
{
    public ServerCallGotDeadlockMessageException(string message) : base(message) { }
}