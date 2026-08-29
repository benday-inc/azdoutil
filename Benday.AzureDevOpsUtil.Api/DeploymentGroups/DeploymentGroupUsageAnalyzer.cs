namespace Benday.AzureDevOpsUtil.Api.DeploymentGroups;

/// <summary>
/// Correlates a project's deployment groups and their targets with the
/// deployment group phases found in the project's release definitions.
/// Does no I/O -- the command fetches, this does the arithmetic.
/// </summary>
public static class DeploymentGroupUsageAnalyzer
{
    public static DeploymentGroupUsageProject Analyze(
        string projectName,
        IReadOnlyList<DeploymentGroupInfo> groups,
        IReadOnlyDictionary<int, List<DeploymentTargetInfo>> targetsByGroupId,
        IReadOnlyList<DeploymentGroupPhaseReference> phases)
    {
        var result = new DeploymentGroupUsageProject
        {
            ProjectName = projectName
        };

        foreach (var group in groups.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var usage = new DeploymentGroupUsage
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description
            };

            if (targetsByGroupId.TryGetValue(group.Id, out var targets) == true)
            {
                usage.Targets.AddRange(targets);
            }

            foreach (var phase in phases.Where(x => x.DeploymentGroupId == group.Id))
            {
                usage.Consumers.Add(ToPhaseUsage(phase, usage.Targets));
            }

            result.Groups.Add(usage);
        }

        var knownGroupIds = groups.Select(x => x.Id).ToHashSet();

        result.PhasesWithUnknownGroup.AddRange(
            phases.Where(x => knownGroupIds.Contains(x.DeploymentGroupId) == false));

        return result;
    }

    private static DeploymentGroupPhaseUsage ToPhaseUsage(
        DeploymentGroupPhaseReference phase, IReadOnlyList<DeploymentTargetInfo> targets)
    {
        var usage = new DeploymentGroupPhaseUsage
        {
            ReleaseDefinitionId = phase.ReleaseDefinitionId,
            ReleaseDefinitionName = phase.ReleaseDefinitionName,
            EnvironmentName = phase.EnvironmentName,
            PhaseName = phase.PhaseName,
            DeploymentGroupId = phase.DeploymentGroupId,
            Tags = phase.Tags.ToList()
        };

        usage.MatchingTargetNames.AddRange(
            targets
                .Where(target => MatchesTags(target, phase.Tags))
                .Select(target => target.Agent?.Name ?? $"(target {target.Id})")
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

        return usage;
    }

    /// <summary>
    /// A phase deploys to the targets that carry every tag it names.  A phase
    /// with no tags deploys to every target in the group.
    /// </summary>
    public static bool MatchesTags(DeploymentTargetInfo target, IReadOnlyList<string> phaseTags)
    {
        if (phaseTags.Count == 0)
        {
            return true;
        }

        return phaseTags.All(tag =>
            target.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }
}
