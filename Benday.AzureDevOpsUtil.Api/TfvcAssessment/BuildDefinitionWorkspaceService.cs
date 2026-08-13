using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

public class BuildDefinitionScanResult
{
    public List<TfvcBuildDefinitionInfo> Definitions { get; set; } = new();

    public List<MappedPathUsage> MappedPathUsages { get; set; } = new();

    /// <summary>
    /// How many definitions in the project were looked at, TFVC or not.  Lets
    /// the report distinguish "no TFVC builds" from "no builds at all".
    /// </summary>
    public int TotalDefinitionsExamined { get; set; }

    /// <summary>
    /// Definitions whose detail could not be read.  These are named in the
    /// report rather than silently dropped.
    /// </summary>
    public List<string> UnreadableDefinitions { get; set; } = new();
}

/// <summary>
/// Finds the build definitions that pull source from TFVC and works out what
/// their workspace mappings would mean in Git.
///
/// This runs against the whole team project regardless of the path being
/// assessed: a build defined elsewhere can still map a folder inside the
/// assessed path, and that dependency is the point.
/// </summary>
public class BuildDefinitionWorkspaceService
{
    public const int InactiveAfterDays = 365;

    public Action<string>? ProgressCallback { get; set; }

    public async Task<BuildDefinitionScanResult> ScanAsync(
        IBuildDefinitionApiClient client, string projectName, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(client);

        var result = new BuildDefinitionScanResult();

        var definitions = await client.GetDefinitionsAsync(projectName);

        result.TotalDefinitionsExamined = definitions.Count;

        foreach (var definition in definitions)
        {
            ProgressCallback?.Invoke($"Reading build definition '{definition.Name}'...");

            var detail = await client.GetDefinitionAsync(projectName, definition.Id);

            if (detail == null)
            {
                result.UnreadableDefinitions.Add(definition.Name);
                continue;
            }

            if (detail.Repository == null || detail.Repository.IsTfvc == false)
            {
                // Git-backed and YAML definitions are not affected by TFVC
                // being retired, so they are not part of this section.
                continue;
            }

            result.Definitions.Add(CreateDefinitionInfo(detail, utcNow));
        }

        result.MappedPathUsages = BuildMappedPathUsages(result.Definitions);

        return result;
    }

    private TfvcBuildDefinitionInfo CreateDefinitionInfo(
        BuildDefinitionDetail detail, DateTime utcNow)
    {
        var mappings = TfvcWorkspaceMappingParser.Parse(detail.Repository?.GetTfvcMappingJson());

        var info = new TfvcBuildDefinitionInfo
        {
            Id = detail.Id,
            Name = detail.Name,
            Mappings = mappings,
            MappedPaths = mappings
                .Where(x => x.IsMap == true)
                .Select(x => TfvcPath.Normalize(x.ServerPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CloakedPaths = mappings
                .Where(x => x.IsCloak == true)
                .Select(x => TfvcPath.Normalize(x.ServerPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            LastRunDate = GetLastRunDate(detail)
        };

        info.IsInactive = info.LastRunDate.HasValue == false ||
            info.LastRunDate.Value < utcNow.AddDays(-InactiveAfterDays);

        return info;
    }

    private DateTime? GetLastRunDate(BuildDefinitionDetail detail)
    {
        var candidates = new[] { detail.LatestCompletedBuild, detail.LatestBuild };

        foreach (var build in candidates)
        {
            if (build == null)
            {
                continue;
            }

            // A build that is still running has no finish time, and an absent
            // date deserializes to the default value rather than to null.
            if (build.FinishTime > DateTime.MinValue)
            {
                return build.FinishTime;
            }

            if (build.QueueTime > DateTime.MinValue)
            {
                return build.QueueTime;
            }
        }

        return null;
    }

    private List<MappedPathUsage> BuildMappedPathUsages(List<TfvcBuildDefinitionInfo> definitions)
    {
        var usagesByPath = new Dictionary<string, MappedPathUsage>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            foreach (var path in definition.MappedPaths)
            {
                if (usagesByPath.TryGetValue(path, out var usage) == false)
                {
                    usage = new MappedPathUsage { Path = path };
                    usagesByPath[path] = usage;
                }

                usage.DefinitionNames.Add(definition.Name);
            }
        }

        return usagesByPath.Values
            .OrderByDescending(x => x.DefinitionCount)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
