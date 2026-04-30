using System.Net.Http;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages.TaskGroups;

namespace Benday.AzureDevOpsUtil.Api.TaskGroups;

public class TaskGroupClient
{
    private const string ApiVersion = "7.1-preview.1";

    private readonly HttpClient _httpClient;

    public TaskGroupClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<TaskGroupInfo>> ListAsync(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);
        var requestUrl =
            $"{projectEscaped}/_apis/distributedtask/taskgroups?api-version={ApiVersion}";

        var response = await GetStringAsync(requestUrl);
        var parsed = JsonUtilities.GetJsonValueAsType<TaskGroupListResponse>(response);

        return parsed?.Values ?? new List<TaskGroupInfo>();
    }

    public async Task<TaskGroupInfo?> GetByIdAsync(string projectName, string taskGroupId)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }
        if (string.IsNullOrWhiteSpace(taskGroupId))
        {
            throw new ArgumentException("Task group id is required.", nameof(taskGroupId));
        }

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);
        var idEscaped = HttpUtility.UrlPathEncode(taskGroupId);
        var requestUrl =
            $"{projectEscaped}/_apis/distributedtask/taskgroups/{idEscaped}?api-version={ApiVersion}";

        var response = await GetStringAsync(requestUrl);
        var parsed = JsonUtilities.GetJsonValueAsType<TaskGroupListResponse>(response);

        return parsed?.Values.FirstOrDefault();
    }

    public async Task<string> GetRawJsonByIdAsync(string projectName, string taskGroupId)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }
        if (string.IsNullOrWhiteSpace(taskGroupId))
        {
            throw new ArgumentException("Task group id is required.", nameof(taskGroupId));
        }

        var projectEscaped = HttpUtility.UrlPathEncode(projectName);
        var idEscaped = HttpUtility.UrlPathEncode(taskGroupId);
        var requestUrl =
            $"{projectEscaped}/_apis/distributedtask/taskgroups/{idEscaped}?api-version={ApiVersion}";

        return await GetStringAsync(requestUrl);
    }

    private async Task<string> GetStringAsync(string requestUrl)
    {
        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Problem with server call to {requestUrl}. {response.StatusCode} {response.ReasonPhrase} - {content}");
        }

        return await response.Content.ReadAsStringAsync();
    }
}
