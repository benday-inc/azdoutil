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
    public const string CommandArgumentNameListConfig = "listconfig";

    public const string CommandName_ListProjects = "listprojects";
    public const string CommandName_GetProject = "getproject";

    public const string ArgumentNameTeamProjectName = "name";
    public const string ArgumentNameConfigurationName = "config";
    public const string ArgumentNameCollectionUrl = "url";
    public const string ArgumentNameToken = "pat";
    public const string ArgumentNameQuietMode = "quiet";
    public const int RetryDelayInMillisecs = 100;
}
