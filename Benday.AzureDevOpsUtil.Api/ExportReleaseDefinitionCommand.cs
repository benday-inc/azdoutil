using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.BuildQueues;
using Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameExportReleaseDefinition,
        Description = "Export release definition",
        IsAsync = true)]
public class ExportReleaseDefinitionCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;
    private string _ReleaseDefinitionName = string.Empty;

    public GetReleaseDefinitionDetailResponse? LastResult { get; private set; }

    public ExportReleaseDefinitionCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name");

        arguments.AddString(Constants.ArgumentNameReleaseDefinitionName)
            .WithDescription("Release definition name");

        arguments.AddBoolean(Constants.CommandArgumentNameQueueInfo)
            .WithDescription("Only display queue info")
            .AllowEmptyValue()
            .AsNotRequired();

        arguments.AddBoolean(Constants.CommandArgumentNameToJson).WithDescription("Export to JSON")
            .AllowEmptyValue()
            .AsNotRequired();

        return arguments;
    }

    public ReleaseQueueInfo? QueueInfo { get; set; }
    public string? LastResultRawJson { get; private set; }

    protected override async Task OnExecute()
    {
        _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _ReleaseDefinitionName = Arguments.GetStringValue(Constants.ArgumentNameReleaseDefinitionName);
        
        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);
        var queueInfoOnly = Arguments.GetBooleanValue(Constants.CommandArgumentNameQueueInfo);

        var teamProject = await GetTeamProject(_TeamProjectName);

        if (teamProject == null)
        {
            throw new KnownException(
                $"Team project '{_TeamProjectName}' was not found.");
        }

        var releaseInfo = await GetReleaseInfoByName(_ReleaseDefinitionName);

        if (releaseInfo == null)
        {
            throw new KnownException(
                $"Release name '{_ReleaseDefinitionName}' was not found.");
        }

        var releaseDefinition = await GetReleaseDefinitionById(
            teamProject.Name, releaseInfo.Id);

        if (releaseDefinition == null)
        {
            throw new KnownException(
                $"Release definition '{_ReleaseDefinitionName}' was not found.");
        }


        LastResultRawJson = releaseDefinition.RawJson;
        LastResult = releaseDefinition;

        if (string.IsNullOrEmpty(LastResultRawJson) == true)
        {
            throw new KnownException("Raw JSON is empty.");
        }
        else if (IsQuietMode == false && toJson == true && queueInfoOnly == false)
        {
            WriteLine(LastResultRawJson);
        }
        else if (queueInfoOnly == true)
        {
            var queues = await GetBuildQueues(teamProject.Name);

            var info = new ReleaseQueueInfo()
            {
                ReleaseId = releaseDefinition.Id,
                ReleaseName = releaseDefinition.Name,
                TeamProjectName = teamProject.Name,
                TeamProjectId = teamProject.Id
            };

            QueueInfo = info;

            foreach (var environment in releaseDefinition.Environments)
            {
                foreach (var deployPhase in environment.DeployPhases)
                {
                    var queueId = deployPhase.DeploymentInput.QueueId;

                    var queue = queues?.Value.FirstOrDefault(x => x.Id == queueId);

                    var agentIdentifier =
                        deployPhase?.DeploymentInput?.AgentSpecification?.Identifier ??
                        string.Empty;

                    info.AddQueue(
                        queueId, queue?.Name ?? string.Empty,
                        environment.Id, environment.Name,
                        agentIdentifier);
                }
            }


            if (IsQuietMode == true)
            {
                return;
            }
            else if (toJson == false)
            {
                foreach (var queueRef in info.QueueReferences)
                {
                    WriteLine($"QueueId: {queueRef.QueueId}");
                    WriteLine($"EnvironmentId: {queueRef.EnvironmentId}");
                    WriteLine($"EnvironmentName: {queueRef.EnvironmentName}");
                    WriteLine($"ReleaseName: {info.ReleaseName}");
                    WriteLine($"ReleaseId: {info.ReleaseId}");
                    WriteLine($"TeamProjectName: {info.TeamProjectName}");
                    WriteLine($"TeamProjectId: {info.TeamProjectId}");
                    WriteLine($"QueueName: {queueRef.QueueName}");
                    WriteLine($"AgentSpecification: {queueRef.AgentSpecification}");
                    WriteLine(String.Empty);
                }
            }
            else
            {
                var json = JsonSerializer.Serialize(
                    info, new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });

                WriteLine(json);
            }

        }
    }

    private async Task<TeamProjectInfo?> GetTeamProject(string teamProjectName)
    {
        var teamProjectNameEncoded = HttpUtility.UrlPathEncode(teamProjectName);

        var requestUrl = $"_apis/projects/{teamProjectNameEncoded}?api-version=7.0";

        try
        {
            var result = await CallEndpointViaGetAndGetResult<TeamProjectInfo>(requestUrl, false, false);

            return result;
        }
        catch (Exception)
        {
            LastResult = null;
            return null;
        }
    }

    private async Task<GetBuildQueuesResponse?> GetBuildQueues(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/distributedtask/queues?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetBuildQueuesResponse>(requestUrl);

        return result;
    }

    private async Task<GetReleaseDefinitionDetailResponse?> GetReleaseDefinitionById(string teamProjectName, int releaseId)
    {
        var requestUrl = $"{teamProjectName}/_apis/release/definitions/{releaseId}?api-version=7.1";

        using var client = GetHttpClientInstanceForAzureDevOps(AzureDevOpsUrlTargetType.Release);

        var result = await client.GetAsync(requestUrl);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }
        else
        {
            var responseContent = await result.Content.ReadAsStringAsync();

            var typedResponse = JsonUtilities.GetJsonValueAsType<GetReleaseDefinitionDetailResponse>(responseContent);

            if (typedResponse != null)
            {
                typedResponse.RawJson = responseContent;
            }

            return typedResponse;
        }
    }

    private async Task<ReleaseInfo?> GetReleaseInfoByName(string name)
    {
        string requestUrl;

        requestUrl = $"{_TeamProjectName}/_apis/release/definitions?api-version=7.1";

        var result = await CallEndpointViaGetAndGetResult<GetReleasesForProjectResponse>(
            requestUrl, azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);

        if (result == null || result.Count == 0)
        {
            return null;
        }
        else
        {
            // find release by name case insensitive
            var release = result.Releases.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return release;
        }
    }
}
