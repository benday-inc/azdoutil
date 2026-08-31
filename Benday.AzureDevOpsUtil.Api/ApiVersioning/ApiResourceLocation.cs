namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// One entry from the resource catalog an Azure DevOps collection returns for
/// OPTIONS on its _apis root.
///
/// <see cref="ReleasedVersion"/> is the highest version of this resource that
/// shipped out of preview, and reads "0.0" when the resource has never left
/// preview -- roughly half of them have not, so a caller asking for the
/// released form of one of those needs the preview suffix instead.
/// </summary>
public sealed class ApiResourceLocation
{
    public string Area { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
    public string RouteTemplate { get; init; } = string.Empty;
    public ApiVersion MinVersion { get; init; }
    public ApiVersion MaxVersion { get; init; }
    public ApiVersion ReleasedVersion { get; init; }

    /// <summary>
    /// How many fixed segments the route template has.  Several locations can
    /// match the same request once the templated segments are treated as
    /// optional, and the one that spells out the most of the path is the one
    /// that actually describes it.
    /// </summary>
    public int Specificity { get; init; }

    public override string ToString() => $"{Area}/{ResourceName} (released {ReleasedVersion}, max {MaxVersion})";
}
