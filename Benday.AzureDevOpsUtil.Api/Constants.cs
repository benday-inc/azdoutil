using Microsoft.VisualBasic;

namespace Benday.AzureDevOpsUtil.Api;

public static class Constants
{
    public const string ExeName = "azdoutil";
    public const string ConfigFileName = "azdoutil-config.json";
    public const string DefaultConfigurationName = "(default)";
    
    public const string CommandArgumentNameShowConfig = "showconfig";
    public const string CommandArgumentNameAddUpdateConfig = "addconfig";
    public const string CommandArgumentNameRemoveConfig = "removeconfig";

    public const string ArgumentNameConfigurationName = "name";
    public const string ArgumentNameCollectionUrl = "url";
    public const string ArgumentNameToken = "pat";
}
