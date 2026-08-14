using Benday.AzureDevOpsUtil.Api.AgentCapabilities;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

/// <summary>
/// Shared preview-and-apply reporting for the import and set capability
/// commands, so both describe a plan and write it the same way.  Output goes
/// through a <see cref="Action{String}"/> the command supplies, keeping this
/// class free of the console.
/// </summary>
internal static class AgentCapabilityCommandHelper
{
    public static async Task ApplyPlanAsync(
        AgentCapabilityService service,
        IReadOnlyList<CapabilityPlanItem> plan,
        bool previewOnly,
        Action<string> writeLine)
    {
        var changing = plan.Where(x => x.WillChange).ToList();
        var unchanged = plan.Count - changing.Count;

        writeLine(string.Empty);
        writeLine($"{changing.Count} agent(s) would change; {unchanged} already match.");

        foreach (var item in changing
            .OrderBy(x => x.PoolName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.AgentName, StringComparer.OrdinalIgnoreCase))
        {
            writeLine(string.Empty);
            writeLine($"   {item.PoolName} / {item.AgentName} (agent id: {item.AgentId})");

            foreach (var key in item.AddedKeys)
            {
                writeLine($"      + {key} = {item.Final[key]}");
            }

            foreach (var key in item.ChangedKeys)
            {
                writeLine($"      ~ {key}: {item.Existing[key]} -> {item.Final[key]}");
            }

            foreach (var key in item.RemovedKeys)
            {
                writeLine($"      - {key} (was {item.Existing[key]})");
            }
        }

        if (previewOnly == true)
        {
            writeLine(string.Empty);
            writeLine("PREVIEW ONLY: no changes were written.");
            return;
        }

        if (changing.Count == 0)
        {
            return;
        }

        writeLine(string.Empty);

        var applied = 0;

        foreach (var item in changing)
        {
            writeLine($"Updating {item.PoolName} / {item.AgentName}...");

            await service.ApplyAsync(item);

            applied++;
        }

        writeLine(string.Empty);
        writeLine($"Updated {applied} agent(s).");
    }
}
