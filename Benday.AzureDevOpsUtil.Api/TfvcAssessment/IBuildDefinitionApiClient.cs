using Benday.AzureDevOpsUtil.Api.Messages;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// The read-only build definition calls the assessment needs.
/// </summary>
public interface IBuildDefinitionApiClient
{
    /// <summary>
    /// Every build definition in the project.  These come back shallow: no
    /// repository, and no workspace mappings.
    /// </summary>
    Task<IReadOnlyList<BuildDefinitionInfo>> GetDefinitionsAsync(string projectName);

    /// <summary>
    /// One definition with its repository and its most recent completed build.
    /// Returns null when the definition could not be read.
    /// </summary>
    Task<BuildDefinitionDetail?> GetDefinitionAsync(string projectName, int definitionId);
}
