using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

/// <summary>
/// In-memory stand-in for the build definition API.
/// </summary>
public class FakeBuildDefinitionApiClient : IBuildDefinitionApiClient
{
    public List<BuildDefinitionInfo> Definitions { get; } = new();

    public Dictionary<int, BuildDefinitionDetail?> DetailsById { get; } = new();

    public List<int> DetailRequests { get; } = new();

    /// <summary>
    /// Wraps a string so it can live in the untyped repository properties
    /// dictionary the way a real payload does.
    /// </summary>
    public static JsonElement StringElement(string value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));

        return document.RootElement.Clone();
    }

    public static string BuildMappingJson(params (string ServerPath, string MappingType)[] mappings)
    {
        var entries = mappings.Select(x =>
            $"{{\"serverPath\":\"{x.ServerPath}\",\"mappingType\":\"{x.MappingType}\"," +
            "\"localPath\":\"\\\\\"}");

        return "{\"mappings\":[" + string.Join(",", entries) + "]}";
    }

    public static BuildDefinitionDetail TfvcDefinition(
        int id,
        string name,
        DateTime? lastRun,
        params (string ServerPath, string MappingType)[] mappings)
    {
        var repository = new BuildRepositoryInfo
        {
            Id = "$/Project",
            Name = "$/Project",
            Type = BuildRepositoryInfo.TypeTfvc
        };

        repository.Properties["tfvcMapping"] = StringElement(BuildMappingJson(mappings));

        var detail = new BuildDefinitionDetail
        {
            Id = id,
            Name = name,
            Repository = repository
        };

        if (lastRun.HasValue == true)
        {
            detail.LatestCompletedBuild = new BuildRunInfo
            {
                Id = id * 100,
                FinishTime = lastRun.Value
            };
        }

        return detail;
    }

    public static BuildDefinitionDetail GitDefinition(int id, string name)
    {
        return new BuildDefinitionDetail
        {
            Id = id,
            Name = name,
            Repository = new BuildRepositoryInfo
            {
                Id = "some-guid",
                Name = "MyRepo",
                Type = "TfsGit"
            }
        };
    }

    public void Add(BuildDefinitionDetail detail)
    {
        Definitions.Add(new BuildDefinitionInfo { Id = detail.Id, Name = detail.Name });

        DetailsById[detail.Id] = detail;
    }

    /// <summary>
    /// A definition that shows up in the list but whose detail cannot be read.
    /// </summary>
    public void AddUnreadable(int id, string name)
    {
        Definitions.Add(new BuildDefinitionInfo { Id = id, Name = name });

        DetailsById[id] = null;
    }

    public Task<IReadOnlyList<BuildDefinitionInfo>> GetDefinitionsAsync(string projectName)
    {
        return Task.FromResult<IReadOnlyList<BuildDefinitionInfo>>(Definitions);
    }

    public Task<BuildDefinitionDetail?> GetDefinitionAsync(string projectName, int definitionId)
    {
        DetailRequests.Add(definitionId);

        if (DetailsById.TryGetValue(definitionId, out var detail) == true)
        {
            return Task.FromResult(detail);
        }

        return Task.FromResult<BuildDefinitionDetail?>(null);
    }
}
