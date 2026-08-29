using System.Text.Json.Nodes;

namespace Benday.AzureDevOpsUtil.Api.NuGetTasks;

/// <summary>
/// Updates every NuGetToolInstaller step in a classic build definition to a
/// chosen task major version and NuGet version.  Mutates the JsonNode it is
/// given and reports what changed; the caller decides whether to save.
/// </summary>
public class NuGetToolInstallerUpdater
{
    private readonly string _taskVersionSpec;
    private readonly string _nugetVersionSpec;

    public NuGetToolInstallerUpdater(string taskVersionSpec, string nugetVersionSpec)
    {
        if (string.IsNullOrWhiteSpace(taskVersionSpec))
        {
            throw new ArgumentException(
                $"{nameof(taskVersionSpec)} is null or empty.", nameof(taskVersionSpec));
        }

        if (string.IsNullOrWhiteSpace(nugetVersionSpec))
        {
            throw new ArgumentException(
                $"{nameof(nugetVersionSpec)} is null or empty.", nameof(nugetVersionSpec));
        }

        _taskVersionSpec = taskVersionSpec;
        _nugetVersionSpec = nugetVersionSpec;
    }

    /// <summary>
    /// The display name mirrors what the pipeline designer generates for this
    /// task, so the build's step list shows which NuGet version it installs.
    /// </summary>
    public string GetDisplayName() => $"Use NuGet {_nugetVersionSpec}";

    public NuGetToolInstallerUpdateResult Update(JsonNode buildDefinition)
    {
        if (buildDefinition == null)
        {
            throw new ArgumentNullException(nameof(buildDefinition));
        }

        var result = new NuGetToolInstallerUpdateResult();

        if (buildDefinition["process"] is not JsonObject process ||
            process["phases"] is not JsonArray phases)
        {
            return result;
        }

        foreach (var phase in phases)
        {
            if (phase is not JsonObject phaseObj ||
                phaseObj["steps"] is not JsonArray steps)
            {
                continue;
            }

            var phaseName =
                phaseObj["name"]?.GetValue<string>() ??
                phaseObj["refName"]?.GetValue<string>() ?? string.Empty;

            var stepIndex = 0;
            foreach (var step in steps)
            {
                if (step is JsonObject stepObj)
                {
                    var change = TryUpdateStep(stepObj, phaseName, stepIndex);

                    if (change != null)
                    {
                        result.Changes.Add(change);
                    }
                }

                stepIndex++;
            }
        }

        return result;
    }

    private NuGetToolInstallerStepChange? TryUpdateStep(
        JsonObject step, string phaseName, int stepIndex)
    {
        if (step["task"] is not JsonObject task)
        {
            return null;
        }

        var taskId = task["id"]?.GetValue<string>();

        if (string.Equals(taskId, NuGetToolInstallerScanner.NuGetToolInstallerTaskId,
                StringComparison.OrdinalIgnoreCase) == false)
        {
            return null;
        }

        var change = new NuGetToolInstallerStepChange
        {
            PhaseName = phaseName,
            StepIndex = stepIndex,
            OldTaskVersionSpec = task["versionSpec"]?.GetValue<string>() ?? string.Empty,
            NewTaskVersionSpec = _taskVersionSpec,
            OldDisplayName = step["displayName"]?.GetValue<string>() ?? string.Empty,
            NewDisplayName = GetDisplayName()
        };

        task["versionSpec"] = _taskVersionSpec;
        step["displayName"] = GetDisplayName();

        if (step["inputs"] is not JsonObject inputs)
        {
            inputs = new JsonObject();
            step["inputs"] = inputs;
        }

        change.OldNuGetVersionSpec = inputs["versionSpec"]?.GetValue<string>() ?? string.Empty;
        change.NewNuGetVersionSpec = _nugetVersionSpec;

        inputs["versionSpec"] = _nugetVersionSpec;

        return change;
    }
}
