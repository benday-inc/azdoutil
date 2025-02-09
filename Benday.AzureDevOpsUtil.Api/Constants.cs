namespace Benday.AzureDevOpsUtil.Api;

public static class Constants
{
    public const string ExeName = "azdoutil";
    public const string ConfigFileName = "azdoutil-config.json";
    public const string DefaultConfigurationName = "(default)";

    public const string Category_AzdoUtilConfig = "AzdoUtil Configuration";
    public const string Category_Builds = "Builds";
    public const string Category_FlowMetrics = "Flow Metrics";
    public const string Category_Misc = "Miscellaneous";
    public const string Category_ProcessTemplates = "Process Templates";
    public const string Category_ProjectAdmin = "Project Administration";
    public const string Category_TestData = "Test Data";
    public const string Category_VersionControl = "Version Control";
    public const string Category_WorkItems = "Work Items";


    public const string CommandArgumentNameShowConfig = "showconfig";
    public const string CommandArgumentNameWithAgents = "agents";
    public const string CommandArgumentNameToJson = "json";
    public const string CommandArgumentNameAddUpdateConfig = "addconfig";
    public const string CommandArgumentNameRemoveConfig = "removeconfig";
    public const string CommandArgumentNameListConfig = "listconfig";
    public const string CommandArgumentName_ListGitRepos = "listgitrepos";
    public const string CommandArgumentNameListBuildDefinitions = "listbuilddefs";
    public const string CommandArgumentNameRepairBuildDefAgentPool = "repairbuilddefagentpool";
    public const string CommandArgumentNameListReleaseDefinitions = "listreleasedefs";
    public const string CommandArgumentNameListAgentPools = "listagentpools";
    public const string CommandArgumentNameExportBuildDefinition = "exportbuilddef";
    public const string CommandArgumentNameCreateGitRepository = "creategitrepo";
    public const string CommandArgumentNameCompareWitdFields = "comparewitdfields";
    public const string CommandArgumentNameCopyWitdField = "copywitdfield";
    public const string CommandArgumentNameCopyCategory = "copycategory";
    public const string CommandArgumentNameUpdateProcessConfig = "updateprocessconfig";
    public const string CommandArgumentNameImportTfvcToGit = "tfvc-to-git";
    public const string CommandArgumentNameListWorkItemQueries = "listworkitemqueries";
    public const string CommandArgumentNameGetIterations = "getiterations";
    public const string CommandArgumentNameGetAreas = "getareas";
    public const string CommandArgumentNameGetWorkItemFields = "getfields";
    public const string CommandArgumentNameGetWorkItemTypes = "getworkitemtypes";
    public const string CommandArgumentNameCreateBacklogRefinementProcessTemplate = "addrefinementprocess";
    public const string CommandArgumentName_ListTeams = "listteams";
    public const string CommandArgumentName_CreateTeam = "createteam";
    public const string CommandName_ConnectionData = "connectiondata";
    public const string CommandArgumentNameExportProjectProcessTemplate = "exportprocesstemplate";

    public const string CommandName_ListProjects = "listprojects";
    public const string CommandName_GetProject = "getproject";
    public const string CommandName_CreateWorkItemsFromExcelScript = "createfromexcel";
    public const string CommandName_CreateWorkItemsFromDataGenerator = "createfromgenerator";
    public const string CommandName_CreateRandomTitles = "createrandomtitles";
    public const string CommandName_CreateProject = "createproject";
    public const string CommandName_ListProcessTemplates = "listprocesstemplates";
    public const string CommandName_SetIteration = "setiteration";
    public const string CommandName_GetWorkItemById = "getworkitem";
    public const string CommandName_DeleteProject = "deleteproject";
    public const string CommandName_ShowWorkItemQuery = "showworkitemquery";
    public const string CommandName_RunWorkItemQuery = "runworkitemquery";
    public const string CommandName_ExportWorkItemQuery = "exportworkitemquery";
    public const string CommandName_SetWorkItemState = "setworkitemstate";
    public const string CommandArgumentNameGetWorkItemStates = "getworkitemstates";
    public const string CommandName_ChangeProjectProcess = "changeprocess";

    public const string CommandArgumentNameGetCycleTimeAndThroughput = "throughputcycletime";
    public const string CommandArgumentNameGetAgingWorkItemsCommand = "agingwork";
    public const string CommandArgumentNameForecastWorkItemDelivery = "forecastworkitem";
    public const string CommandArgumentNameGetForecastItemCountInWeeks = "forecastitemsinweeks";
    public const string CommandArgumentNameSuggestServiceLevelExpectation = "suggest-sle";
    public const string CommandArgumentNameCycleTimeConfidenceRangesCommand = "cycletimeconfidence";

    public const string CommandArgumentNameGetForecastDurationForItemCount = "forecastdurationforitemcount";

    public const string ArgumentNameVerbose = "verbose";
    public const string ArgumentNameTeamProjectName = "teamproject";
    public const string ArgumentNameAllProjects = "all";
    public const string ArgumentNameAgentPoolInfo = "poolinfofile";
    public const string ArgumentNamePreviewOnly = "preview";
    public const string ArgumentNameProcessName = "processname";
    public const string ArgumentNameRepositoryName = "reponame";
    public const string ArgumentNameTfvcFolder = "tfvc-path";
    public const string ArgumentNameWorkItemQueryName = "queryname";
    public const string ArgumentNameConfirm = "confirm";
    public const string ArgumentNameConfigurationName = "config";
    public const string ArgumentNameCollectionUrl = "url";
    public const string ArgumentNameWindowsAuth = "windowsauth";
    public const string ArgumentNameToken = "pat";
    public const string ArgumentNameQuietMode = "quiet";
    public const string ArgumentNameNameOnly = "nameonly";
    public const string ArgumentNameXaml = "xaml";
    public const string ArgumentNameOutputRaw = "raw";
    public const string ArgumentNameBuildDefinitionName = "name";
    public const string ArgumentNameTeamName = "teamname";
    public const string ArgumentNamePercent = "percent";
    public const string ArgumentNameTeamDescription = "description";
    public const string ArgumentNameShowLastRunInfo = "showlastruninfo";
    public const string ArgumentNameOutputCsv = "csv";
    public const string ArgumentNameNoCsvHeader = "csv-noheader";
    public const string ArgumentNameComparisonFile1 = "file1";
    public const string ArgumentNameComparisonFile2 = "file2";
    public const string ArgumentNameReverseSourceAndTarget = "flip";
    public const string ArgumentNameFieldRefName = "refname";
    public const string ArgumentNameOverwrite = "overwrite";

    public const string ArgumentNameCycleTimeNumberOfDays = "numberofdays";
    public const string ArgumentNameForecastNumberOfWeeks = "forecastweeks";
    public const string ArgumentNameForecastNumberOfItems = "forecastitemcount";

    public const int RetryDelayInMillisecs = 100;
    public const int ForecastNumberOfSimulations = 1000;
    public const int ForecastNumberOfSimulationsFiftyPercent = 500;
    public const int ForecastNumberOfSimulationsEightyPercent = 800;
    public const int ForecastNumberOfSimulationsNinetyPercent = 900;
    public const int ForecastNumberOfSimulationsHundredPercent = 999;
    public const string ProcessTemplateName_ScrumWithBacklogRefinement = "Scrum with Backlog Refinement";
    public const string ProcessTemplateRefName_ScrumWithBacklogRefinement = "Inherited.ScrumWithBacklogRefinement";
    public const string ProcessTemplateName_Scrum = "Scrum";

    public const string CommandArg_Comment = "comment";
    public const string CommandArg_SkipFutureDates = "skipfuturedates";
    public const string CommandArg_PathToExcel = "pathtoexcel";
    public const string CommandArg_SaveScriptFileTo = "output";
    public const string CommandArg_ScriptOnly = "scriptonly";
    public const string CommandArg_StartDate = "startdate";
    public const string CommandArg_TeamProjectName = "teamproject";
    public const string CommandArg_CreateProjectIfNotExists = "createproject";
    public const string CommandArg_TeamCount = "teamcount";
    public const string CommandArg_ProcessTemplateName = "processname";
    public const string CommandArg_EndDate = "enddate";
    public const string CommandArg_IterationName = "name";
    public const string CommandArg_SprintCount = "numberofsprints";
    public const string CommandArg_TitlesOnly = "titlesonly";
    public const string CommandArg_WorkItemId = "id";
    public const string CommandArg_AllPbisGoToDone = "alldone";
    public const string CommandArg_AddSessionTag = "addsessiontag";
    public const string ArgumentNameExportToPath = "exporttopath";
    public const string ArgumentNamePathToWitAdminExe = "witadminpath";
    public const string CommandArg_State = "state";
    public const string CommandArg_StateTransitionDate = "date";
    public const string ArgumentNameWorkItemTypeName = "workitemtypename";
    public const string CommandArgumentNameOverride = "override";




}
