using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class XamlBuildDumpInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TeamProject { get; set; } = string.Empty;
    public string ServerPath { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string TemplateDescription { get; set; } = string.Empty;
    public List<string> ProjectsToBuild { get; set; } = new List<string>();

    public List<string> ConfigurationsToBuild { get; set; } = new List<string>();

    public XamlBuildParameterTestSettingsInfo TestSettings { get; set; } = new();
    public XamlBuildParameterBuildSettingsInfo BuildSettings { get; set; } = new();
    public string ProcessXaml { get; set; } = string.Empty;
    public string ProcessParameters { get; set; } = string.Empty;
    public string DefaultDropLocation { get; set; } = string.Empty;
    public List<TfvcSourceControlMapping> SourceControlMappings { get; set; } = new();

    private ProcessParameterCollection? _Parameters = null;

    [JsonIgnore]
    public ProcessParameterCollection? Parameters
    {
        get
        {
            if (_Parameters == null && string.IsNullOrWhiteSpace(ProcessParameters) == false)
            {
                _Parameters = new ProcessParameterCollection(ProcessParameters);
            }

            return _Parameters;
        }
    }
}
