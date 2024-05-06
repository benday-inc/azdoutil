using System;
using System.Linq;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class XamlBuildSettingsPlatformConfiguration
{

    public XamlBuildSettingsPlatformConfiguration()
    {

    }

    public string Configuration { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}