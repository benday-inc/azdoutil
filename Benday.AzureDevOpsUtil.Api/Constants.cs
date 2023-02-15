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
    public const string CommandName_CreateWorkItemsFromExcelScript = "createfromexcel";
    public const string CommandName_CreateWorkItemsFromDataGenerator = "createfromgenerator";
    public const string CommandName_CreateProject = "createproject";
    public const string CommandName_ListProcessTemplates = "listprocesstemplates";
    public const string CommandName_SetIteration = "setiteration";
    public const string CommandName_GetWorkItemById = "getworkitem";
    public const string CommandName_DeleteProject = "deleteproject";

    public const string ArgumentNameTeamProjectName = "name";
    public const string ArgumentNameConfirm = "confirm";
    public const string ArgumentNameConfigurationName = "config";
    public const string ArgumentNameCollectionUrl = "url";
    public const string ArgumentNameToken = "pat";
    public const string ArgumentNameQuietMode = "quiet";
    public const int RetryDelayInMillisecs = 100;

    public const string CommandArg_Comment = "comment";
    public const string CommandArg_SkipFutureDates = "skipfuturedates";
    public const string CommandArg_PathToExcel = "pathtoexcel";
    public const string CommandArg_SaveScriptFileTo = "savescriptto";
    public const string CommandArg_StartDate = "startdate";
    public const string CommandArg_TeamProjectName = "teamproject";
    public const string CommandArg_CreateProjectIfNotExists = "createproject";
    public const string CommandArg_ProcessTemplateName = "processname";
    public const string CommandArg_EndDate = "enddate";
    public const string CommandArg_IterationName = "name";
    public const string CommandArg_SprintCount = "numberofsprints";
    public const string CommandArg_WorkItemId = "id";
    public const string CommandArg_AllPbisGoToDone = "alldone";
    public const string CommandArg_AddSessionTag = "addsessiontag";

}
