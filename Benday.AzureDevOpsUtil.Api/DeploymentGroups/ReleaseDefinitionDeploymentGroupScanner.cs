using System.Text.Json;

namespace Benday.AzureDevOpsUtil.Api.DeploymentGroups;

/// <summary>
/// Finds the deployment group phases in a release definition's JSON.  Pure
/// string-in, results-out so it unit tests against canned payloads.
/// </summary>
public static class ReleaseDefinitionDeploymentGroupScanner
{
    /// <summary>
    /// The phaseType value for a deployment group phase.  Agent phases are
    /// 'agentBasedDeployment' and server phases are 'runOnServer'.
    /// </summary>
    public const string MachineGroupPhaseType = "machineGroupBasedDeployment";

    public static List<DeploymentGroupPhaseReference> FindPhases(string? releaseDefinitionJson)
    {
        var results = new List<DeploymentGroupPhaseReference>();

        if (string.IsNullOrWhiteSpace(releaseDefinitionJson))
        {
            return results;
        }

        using var document = JsonDocument.Parse(releaseDefinitionJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        var definitionId = ReadInt(root, "id");
        var definitionName = ReadString(root, "name");

        if (!root.TryGetProperty("environments", out var environments) ||
            environments.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var environment in environments.EnumerateArray())
        {
            var environmentName = ReadString(environment, "name");

            if (environment.ValueKind != JsonValueKind.Object ||
                !environment.TryGetProperty("deployPhases", out var phases) ||
                phases.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var phase in phases.EnumerateArray())
            {
                var reference = TryReadPhase(phase, definitionId, definitionName, environmentName);

                if (reference != null)
                {
                    results.Add(reference);
                }
            }
        }

        return results;
    }

    private static DeploymentGroupPhaseReference? TryReadPhase(
        JsonElement phase, int definitionId, string definitionName, string environmentName)
    {
        if (phase.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var phaseType = ReadString(phase, "phaseType");

        if (string.Equals(phaseType, MachineGroupPhaseType, StringComparison.OrdinalIgnoreCase) == false)
        {
            return null;
        }

        var reference = new DeploymentGroupPhaseReference
        {
            ReleaseDefinitionId = definitionId,
            ReleaseDefinitionName = definitionName,
            EnvironmentName = environmentName,
            PhaseName = ReadString(phase, "name")
        };

        if (phase.TryGetProperty("deploymentInput", out var deploymentInput) &&
            deploymentInput.ValueKind == JsonValueKind.Object)
        {
            // queueId doubles as the deployment group id for this phase type
            reference.DeploymentGroupId = ReadInt(deploymentInput, "queueId");

            if (deploymentInput.TryGetProperty("tags", out var tags) &&
                tags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tags.EnumerateArray())
                {
                    if (tag.ValueKind == JsonValueKind.String)
                    {
                        var value = tag.GetString();

                        if (string.IsNullOrWhiteSpace(value) == false)
                        {
                            reference.Tags.Add(value);
                        }
                    }
                }
            }
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

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return 0;
        }

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }
}
