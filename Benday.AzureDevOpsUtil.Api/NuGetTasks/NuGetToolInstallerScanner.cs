using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

/// <summary>
/// Finds NuGetToolInstaller steps in a classic (designer) build definition's
/// JSON.  Pure string-in, results-out so it unit tests against canned payloads.
/// </summary>
public static class NuGetToolInstallerScanner
{
    /// <summary>
    /// The task id is the same across every major version of the task -- the
    /// major version lives in the step's task.versionSpec, not in the id.
    /// </summary>
    public const string NuGetToolInstallerTaskId = "2c65196a-54fd-4a02-9be8-d9d1837b7c5d";

    public static List<NuGetToolInstallerReference> FindReferences(string? buildDefinitionJson)
    {
        var results = new List<NuGetToolInstallerReference>();

        if (string.IsNullOrWhiteSpace(buildDefinitionJson))
        {
            return results;
        }

        using var document = JsonDocument.Parse(buildDefinitionJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("process", out var process) ||
            process.ValueKind != JsonValueKind.Object ||
            !process.TryGetProperty("phases", out var phases) ||
            phases.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        var phaseIndex = 0;
        foreach (var phase in phases.EnumerateArray())
        {
            var phaseName = ReadString(phase, "name", ReadString(phase, "refName"));

            if (phase.TryGetProperty("steps", out var steps) &&
                steps.ValueKind == JsonValueKind.Array)
            {
                var stepIndex = 0;
                foreach (var step in steps.EnumerateArray())
                {
                    var reference = TryReadReference(step, phaseIndex, phaseName, stepIndex);
                    if (reference != null)
                    {
                        results.Add(reference);
                    }
                    stepIndex++;
                }
            }

            phaseIndex++;
        }

        return results;
    }

    public static bool IsNuGetToolInstallerStep(JsonElement step)
    {
        if (step.ValueKind != JsonValueKind.Object ||
            !step.TryGetProperty("task", out var task) ||
            task.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var taskId = ReadString(task, "id");

        return string.Equals(taskId, NuGetToolInstallerTaskId, StringComparison.OrdinalIgnoreCase);
    }

    private static NuGetToolInstallerReference? TryReadReference(
        JsonElement step, int phaseIndex, string phaseName, int stepIndex)
    {
        if (IsNuGetToolInstallerStep(step) == false)
        {
            return null;
        }

        step.TryGetProperty("task", out var task);

        var reference = new NuGetToolInstallerReference
        {
            PhaseIndex = phaseIndex,
            PhaseName = phaseName,
            StepIndex = stepIndex,
            StepDisplayName = ReadString(step, "displayName"),
            Enabled = ReadBool(step, "enabled", defaultValue: true),
            TaskVersionSpec = ReadString(task, "versionSpec")
        };

        if (step.TryGetProperty("inputs", out var inputs) &&
            inputs.ValueKind == JsonValueKind.Object)
        {
            reference.NuGetVersionSpec = ReadString(inputs, "versionSpec");
            reference.CheckLatest = ReadString(inputs, "checkLatest");
        }

        return reference;
    }

    private static string ReadString(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return defaultValue;
        }

        return prop.ValueKind == JsonValueKind.String ? (prop.GetString() ?? defaultValue) : defaultValue;
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return defaultValue;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => defaultValue
        };
    }
}
