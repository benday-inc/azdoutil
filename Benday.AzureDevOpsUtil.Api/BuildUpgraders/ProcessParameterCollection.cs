using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

using Benday.XmlUtilities;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class ProcessParameterCollection
{
    private readonly string _ProcessParametersXml;
    private readonly XDocument _Doc;
    private readonly XElement _Root;
    public ProcessParameterCollection(string processParametersXml)
    {
        _ProcessParametersXml = processParametersXml;

        if (string.IsNullOrWhiteSpace(_ProcessParametersXml) == true)
        {
            throw new ArgumentException(
                                   "Value is null or whitespace.", nameof(processParametersXml));
        }

        _Doc = XDocument.Parse(processParametersXml);        

        if (_Doc.Root == null)
        {
            throw new InvalidOperationException("Root element is null.");
        }
        else
        {
            _Root = _Doc.Root;
        }

        BuildVerbosity = GetElementValue("BuildVerbosity");

        PopulateSettingsValues();

        if (Settings.ContainsKey("MSBuildArguments") == true)
        {
            MsBuildArguments = Settings["MSBuildArguments"];
        }

    }

    public string ProjectsToBuild
    {
        get
        {
            if (BuildSettingsElement == null)
            {
                return string.Empty;
            }
            else
            {
                return BuildSettingsElement.AttributeValueByLocalName("ProjectsToBuild");
            }
        }
    }

    private XElement BuildSettingsElement
    {
        get
        {
            return _Root.DescendantsByLocalName("BuildSettings").FirstOrDefault();
        }
    }

    public List<XamlBuildSettingsPlatformConfiguration> BuildConfigurations
    {
        get
        {
            var results = new List<XamlBuildSettingsPlatformConfiguration>();

            if (BuildSettingsElement != null)
            {
                var configurationElements = BuildSettingsElement.DescendantsByLocalName("PlatformConfiguration");

                foreach (var item in configurationElements)
                {
                    results.Add(new XamlBuildSettingsPlatformConfiguration()
                    {
                        Configuration = item.AttributeValueByLocalName("Configuration"),
                        Platform = item.AttributeValueByLocalName("Platform")
                    });
                }
            }

            return results;
        }
    }

    public Dictionary<string, string> Settings { get; private set; } = new();

    public string BuildVerbosity { get; set; } = string.Empty;

    public string MsBuildArguments { get; set; } = string.Empty;

    private string GetElementValue(string elementName)
    {
        return _Root.DescendantByLocalName(elementName)?.Value ?? string.Empty;
    }
    private void PopulateSettingsValues()
    {
        var stringElements = _Root.DescendantsByLocalName("String");
        var intElements = _Root.DescendantsByLocalName("Int32");
        var boolElements = _Root.DescendantsByLocalName("Boolean");

        var settingsElements = stringElements
            .Concat(intElements)
            .Concat(boolElements);

        foreach (var item in settingsElements)
        {
            var key = item.AttributeValueByLocalName("Key");
            var value = item.Value.Trim();

            if (Settings.ContainsKey(key) == false)
            {
                Settings.Add(key, value);
            }
        }
    }
}
