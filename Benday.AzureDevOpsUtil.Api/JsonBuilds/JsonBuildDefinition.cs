using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Benday.AzureDevOpsUtil.Api.JsonBuilds;

namespace Benday.AzureDevOpsUtil.Api.JsonBuilds;

public class JsonBuildDefinition
{
    [JsonPropertyName("options")]
    public Option[] Options { get; set; } = new Option[0];

    [JsonPropertyName("variables")]
    public Variables Variables { get; set; } = new();

    [JsonPropertyName("properties")]
    public JsonBuildProperties Properties { get; set; } = new();

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("buildNumberFormat")]
    public string BuildNumberFormat { get; set; } = string.Empty;

    [JsonPropertyName("jobAuthorizationScope")]
    public int JobAuthorizationScope { get; set; }

    [JsonPropertyName("jobTimeoutInMinutes")]
    public int JobTimeoutInMinutes { get; set; }

    [JsonPropertyName("jobCancelTimeoutInMinutes")]
    public int JobCancelTimeoutInMinutes { get; set; }

    [JsonPropertyName("process")]
    public Process Process { get; set; } = new();

    [JsonPropertyName("repository")]
    public Repository Repository { get; set; } = new();

    [JsonPropertyName("processParameters")]
    public ProcessParameters ProcessParameters { get; set; } = new();

    [JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonPropertyName("authoredBy")]
    public AuthoredBy AuthoredBy { get; set; } = new();

    [JsonPropertyName("queue")]
    public Queue Queue { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("queueStatus")]
    public int QueueStatus { get; set; }

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("createdDate")]
    public string CreatedDate { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public Project Project { get; set; } = new();

}


public class Option
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("definition")]
    public Definition Definition { get; set; } = new();

    [JsonPropertyName("inputs")]
    public Inputs Inputs { get; set; } = new();

}


public class Definition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

}


public class Inputs
{
    [JsonPropertyName("branchFilters")]
    public string BranchFilters { get; set; } = string.Empty;

    [JsonPropertyName("additionalFields")]
    public string AdditionalFields { get; set; } = string.Empty;

    [JsonPropertyName("workItemType")]
    public string WorkItemType { get; set; } = string.Empty;

    [JsonPropertyName("assignToRequestor")]
    public string AssignToRequestor { get; set; } = string.Empty;

    [JsonPropertyName("versionSpec")]
    public string VersionSpec { get; set; } = string.Empty;

    [JsonPropertyName("checkLatest")]
    public string CheckLatest { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("solution")]
    public string Solution { get; set; } = string.Empty;

    [JsonPropertyName("selectOrConfig")]
    public string SelectOrConfig { get; set; } = string.Empty;

    [JsonPropertyName("feedRestore")]
    public string FeedRestore { get; set; } = string.Empty;

    [JsonPropertyName("includeNuGetOrg")]
    public string IncludeNuGetOrg { get; set; } = string.Empty;

    [JsonPropertyName("nugetConfigPath")]
    public string NugetConfigPath { get; set; } = string.Empty;

    [JsonPropertyName("externalEndpoints")]
    public string ExternalEndpoints { get; set; } = string.Empty;

    [JsonPropertyName("noCache")]
    public string NoCache { get; set; } = string.Empty;

    [JsonPropertyName("disableParallelProcessing")]
    public string DisableParallelProcessing { get; set; } = string.Empty;

    [JsonPropertyName("packagesDirectory")]
    public string PackagesDirectory { get; set; } = string.Empty;

    [JsonPropertyName("verbosityRestore")]
    public string VerbosityRestore { get; set; } = string.Empty;

    [JsonPropertyName("searchPatternPush")]
    public string SearchPatternPush { get; set; } = string.Empty;

    [JsonPropertyName("nuGetFeedType")]
    public string NuGetFeedType { get; set; } = string.Empty;

    [JsonPropertyName("feedPublish")]
    public string FeedPublish { get; set; } = string.Empty;

    [JsonPropertyName("publishPackageMetadata")]
    public string PublishPackageMetadata { get; set; } = string.Empty;

    [JsonPropertyName("allowPackageConflicts")]
    public string AllowPackageConflicts { get; set; } = string.Empty;

    [JsonPropertyName("externalEndpoint")]
    public string ExternalEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("verbosityPush")]
    public string VerbosityPush { get; set; } = string.Empty;

    [JsonPropertyName("searchPatternPack")]
    public string SearchPatternPack { get; set; } = string.Empty;

    [JsonPropertyName("configurationToPack")]
    public string ConfigurationToPack { get; set; } = string.Empty;

    [JsonPropertyName("outputDir")]
    public string OutputDir { get; set; } = string.Empty;

    [JsonPropertyName("versioningScheme")]
    public string VersioningScheme { get; set; } = string.Empty;

    [JsonPropertyName("includeReferencedProjects")]
    public string IncludeReferencedProjects { get; set; } = string.Empty;

    [JsonPropertyName("versionEnvVar")]
    public string VersionEnvVar { get; set; } = string.Empty;

    [JsonPropertyName("requestedMajorVersion")]
    public string RequestedMajorVersion { get; set; } = string.Empty;

    [JsonPropertyName("requestedMinorVersion")]
    public string RequestedMinorVersion { get; set; } = string.Empty;

    [JsonPropertyName("requestedPatchVersion")]
    public string RequestedPatchVersion { get; set; } = string.Empty;

    [JsonPropertyName("packTimezone")]
    public string PackTimezone { get; set; } = string.Empty;

    [JsonPropertyName("includeSymbols")]
    public string IncludeSymbols { get; set; } = string.Empty;

    [JsonPropertyName("toolPackage")]
    public string ToolPackage { get; set; } = string.Empty;

    [JsonPropertyName("buildProperties")]
    public string BuildProperties { get; set; } = string.Empty;

    [JsonPropertyName("basePath")]
    public string BasePath { get; set; } = string.Empty;

    [JsonPropertyName("verbosityPack")]
    public string VerbosityPack { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    [JsonPropertyName("vsVersion")]
    public string VsVersion { get; set; } = string.Empty;

    [JsonPropertyName("msbuildArgs")]
    public string MsbuildArgs { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("configuration")]
    public string Configuration { get; set; } = string.Empty;

    [JsonPropertyName("clean")]
    public string Clean { get; set; } = string.Empty;

    [JsonPropertyName("maximumCpuCount")]
    public string MaximumCpuCount { get; set; } = string.Empty;

    [JsonPropertyName("restoreNugetPackages")]
    public string RestoreNugetPackages { get; set; } = string.Empty;

    [JsonPropertyName("msbuildArchitecture")]
    public string MsbuildArchitecture { get; set; } = string.Empty;

    [JsonPropertyName("logProjectEvents")]
    public string LogProjectEvents { get; set; } = string.Empty;

    [JsonPropertyName("createLogFile")]
    public string CreateLogFile { get; set; } = string.Empty;

    [JsonPropertyName("logFileVerbosity")]
    public string LogFileVerbosity { get; set; } = string.Empty;

    [JsonPropertyName("enableDefaultLogger")]
    public string EnableDefaultLogger { get; set; } = string.Empty;

    [JsonPropertyName("customVersion")]
    public string CustomVersion { get; set; } = string.Empty;

    [JsonPropertyName("testSelector")]
    public string TestSelector { get; set; } = string.Empty;

    [JsonPropertyName("testAssemblyVer2")]
    public string TestAssemblyVer2 { get; set; } = string.Empty;

    [JsonPropertyName("testPlan")]
    public string TestPlan { get; set; } = string.Empty;

    [JsonPropertyName("testSuite")]
    public string TestSuite { get; set; } = string.Empty;

    [JsonPropertyName("testConfiguration")]
    public string TestConfiguration { get; set; } = string.Empty;

    [JsonPropertyName("tcmTestRun")]
    public string TcmTestRun { get; set; } = string.Empty;

    [JsonPropertyName("searchFolder")]
    public string SearchFolder { get; set; } = string.Empty;

    [JsonPropertyName("resultsFolder")]
    public string ResultsFolder { get; set; } = string.Empty;

    [JsonPropertyName("testFiltercriteria")]
    public string TestFiltercriteria { get; set; } = string.Empty;

    [JsonPropertyName("runOnlyImpactedTests")]
    public string RunOnlyImpactedTests { get; set; } = string.Empty;

    [JsonPropertyName("runAllTestsAfterXBuilds")]
    public string RunAllTestsAfterXBuilds { get; set; } = string.Empty;

    [JsonPropertyName("uiTests")]
    public string UiTests { get; set; } = string.Empty;

    [JsonPropertyName("vstestLocationMethod")]
    public string VstestLocationMethod { get; set; } = string.Empty;

    [JsonPropertyName("vsTestVersion")]
    public string VsTestVersion { get; set; } = string.Empty;

    [JsonPropertyName("vstestLocation")]
    public string VstestLocation { get; set; } = string.Empty;

    [JsonPropertyName("runSettingsFile")]
    public string RunSettingsFile { get; set; } = string.Empty;

    [JsonPropertyName("overrideTestrunParameters")]
    public string OverrideTestrunParameters { get; set; } = string.Empty;

    [JsonPropertyName("pathtoCustomTestAdapters")]
    public string PathtoCustomTestAdapters { get; set; } = string.Empty;

    [JsonPropertyName("runInParallel")]
    public string RunInParallel { get; set; } = string.Empty;

    [JsonPropertyName("runTestsInIsolation")]
    public string RunTestsInIsolation { get; set; } = string.Empty;

    [JsonPropertyName("codeCoverageEnabled")]
    public string CodeCoverageEnabled { get; set; } = string.Empty;

    [JsonPropertyName("otherConsoleOptions")]
    public string OtherConsoleOptions { get; set; } = string.Empty;

    [JsonPropertyName("distributionBatchType")]
    public string DistributionBatchType { get; set; } = string.Empty;

    [JsonPropertyName("batchingBasedOnAgentsOption")]
    public string BatchingBasedOnAgentsOption { get; set; } = string.Empty;

    [JsonPropertyName("customBatchSizeValue")]
    public string CustomBatchSizeValue { get; set; } = string.Empty;

    [JsonPropertyName("batchingBasedOnExecutionTimeOption")]
    public string BatchingBasedOnExecutionTimeOption { get; set; } = string.Empty;

    [JsonPropertyName("customRunTimePerBatchValue")]
    public string CustomRunTimePerBatchValue { get; set; } = string.Empty;

    [JsonPropertyName("dontDistribute")]
    public string DontDistribute { get; set; } = string.Empty;

    [JsonPropertyName("testRunTitle")]
    public string TestRunTitle { get; set; } = string.Empty;

    [JsonPropertyName("publishRunAttachments")]
    public string PublishRunAttachments { get; set; } = string.Empty;

    [JsonPropertyName("failOnMinTestsNotRun")]
    public string FailOnMinTestsNotRun { get; set; } = string.Empty;

    [JsonPropertyName("minimumExpectedTests")]
    public string MinimumExpectedTests { get; set; } = string.Empty;

    [JsonPropertyName("diagnosticsEnabled")]
    public string DiagnosticsEnabled { get; set; } = string.Empty;

    [JsonPropertyName("collectDumpOn")]
    public string CollectDumpOn { get; set; } = string.Empty;

    [JsonPropertyName("rerunFailedTests")]
    public string RerunFailedTests { get; set; } = string.Empty;

    [JsonPropertyName("rerunType")]
    public string RerunType { get; set; } = string.Empty;

    [JsonPropertyName("rerunFailedThreshold")]
    public string RerunFailedThreshold { get; set; } = string.Empty;

    [JsonPropertyName("rerunFailedTestCasesMaxLimit")]
    public string RerunFailedTestCasesMaxLimit { get; set; } = string.Empty;

    [JsonPropertyName("rerunMaxAttempts")]
    public string RerunMaxAttempts { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsFolder")]
    public string SymbolsFolder { get; set; } = string.Empty;

    [JsonPropertyName("SearchPattern")]
    public string SearchPattern { get; set; } = string.Empty;

    [JsonPropertyName("IndexSources")]
    public string IndexSources { get; set; } = string.Empty;

    [JsonPropertyName("PublishSymbols")]
    public string PublishSymbols { get; set; } = string.Empty;

    [JsonPropertyName("SymbolServerType")]
    public string SymbolServerType { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsPath")]
    public string SymbolsPath { get; set; } = string.Empty;

    [JsonPropertyName("CompressSymbols")]
    public string CompressSymbols { get; set; } = string.Empty;

    [JsonPropertyName("SymbolExpirationInDays")]
    public string SymbolExpirationInDays { get; set; } = string.Empty;

    [JsonPropertyName("IndexableFileFormats")]
    public string IndexableFileFormats { get; set; } = string.Empty;

    [JsonPropertyName("DetailedLog")]
    public string DetailedLog { get; set; } = string.Empty;

    [JsonPropertyName("TreatNotIndexedAsWarning")]
    public string TreatNotIndexedAsWarning { get; set; } = string.Empty;

    [JsonPropertyName("UseNetCoreClientTool")]
    public string UseNetCoreClientTool { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsMaximumWaitTime")]
    public string SymbolsMaximumWaitTime { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsProduct")]
    public string SymbolsProduct { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsVersion")]
    public string SymbolsVersion { get; set; } = string.Empty;

    [JsonPropertyName("SymbolsArtifactName")]
    public string SymbolsArtifactName { get; set; } = string.Empty;

    [JsonPropertyName("SourceFolder")]
    public string SourceFolder { get; set; } = string.Empty;

    [JsonPropertyName("Contents")]
    public string Contents { get; set; } = string.Empty;

    [JsonPropertyName("TargetFolder")]
    public string TargetFolder { get; set; } = string.Empty;

    [JsonPropertyName("CleanTargetFolder")]
    public string CleanTargetFolder { get; set; } = string.Empty;

    [JsonPropertyName("OverWrite")]
    public string OverWrite { get; set; } = string.Empty;

    [JsonPropertyName("flattenFolders")]
    public string FlattenFolders { get; set; } = string.Empty;

    [JsonPropertyName("preserveTimestamp")]
    public string PreserveTimestamp { get; set; } = string.Empty;

    [JsonPropertyName("retryCount")]
    public string RetryCount { get; set; } = string.Empty;

    [JsonPropertyName("delayBetweenRetries")]
    public string DelayBetweenRetries { get; set; } = string.Empty;

    [JsonPropertyName("ignoreMakeDirErrors")]
    public string IgnoreMakeDirErrors { get; set; } = string.Empty;

    [JsonPropertyName("PathtoPublish")]
    public string PathtoPublish { get; set; } = string.Empty;

    [JsonPropertyName("ArtifactName")]
    public string ArtifactName { get; set; } = string.Empty;

    [JsonPropertyName("ArtifactType")]
    public string ArtifactType { get; set; } = string.Empty;

    [JsonPropertyName("TargetPath")]
    public string TargetPath { get; set; } = string.Empty;

    [JsonPropertyName("Parallel")]
    public string Parallel { get; set; } = string.Empty;

    [JsonPropertyName("ParallelCount")]
    public string ParallelCount { get; set; } = string.Empty;

    [JsonPropertyName("FileCopyOptions")]
    public string FileCopyOptions { get; set; } = string.Empty;

    [JsonPropertyName("StoreAsTar")]
    public string StoreAsTar { get; set; } = string.Empty;

}


public class Variables
{
    [JsonPropertyName("BuildConfiguration")]
    public BuildConfiguration BuildConfiguration { get; set; } = new();

    [JsonPropertyName("BuildPlatform")]
    public BuildPlatform BuildPlatform { get; set; } = new();

    [JsonPropertyName("system.debug")]
    public SystemDebug SystemDebug { get; set; } = new();

}


public class BuildConfiguration
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("allowOverride")]
    public bool AllowOverride { get; set; }

}


public class BuildPlatform
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("allowOverride")]
    public bool AllowOverride { get; set; }

}


public class SystemDebug
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("allowOverride")]
    public bool AllowOverride { get; set; }

}


public class JsonBuildProperties
{
    [JsonPropertyName("cleanOptions")]
    public string CleanOptions { get; set; } = string.Empty;

    [JsonPropertyName("tfvcMapping")]
    public string TfvcMapping { get; set; } = string.Empty;

    [JsonPropertyName("labelSources")]
    public string LabelSources { get; set; } = string.Empty;

    [JsonPropertyName("labelSourcesFormat")]
    public string LabelSourcesFormat { get; set; } = string.Empty;

}


public class Links
{
    [JsonPropertyName("self")]
    public Self Self { get; set; } = new();

    [JsonPropertyName("web")]
    public Web Web { get; set; } = new();

    [JsonPropertyName("editor")]
    public Editor Editor { get; set; } = new();

    [JsonPropertyName("badge")]
    public Badge Badge { get; set; } = new();

    [JsonPropertyName("avatar")]
    public Avatar Avatar { get; set; } = new();

}


public class Self
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Web
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Editor
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Badge
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Process
{
    [JsonPropertyName("phases")]
    public Phase[] Phases { get; set; } = new Phase[0];

    [JsonPropertyName("type")]
    public int Type { get; set; }

}


public class Phase
{
    [JsonPropertyName("steps")]
    public Step[] Steps { get; set; } = new Step[0];

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("refName")]
    public string RefName { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public Target Target { get; set; } = new();

    [JsonPropertyName("jobAuthorizationScope")]
    public int JobAuthorizationScope { get; set; }

}


public class Step
{
    [JsonPropertyName("environment")]
    public JsonBuildEnvironment Environment { get; set; } = new();

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("continueOnError")]
    public bool ContinueOnError { get; set; }

    [JsonPropertyName("alwaysRun")]
    public bool AlwaysRun { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("timeoutInMinutes")]
    public int TimeoutInMinutes { get; set; }

    [JsonPropertyName("retryCountOnTaskFailure")]
    public int RetryCountOnTaskFailure { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("task")]
    public Task Task { get; set; } = new();

    [JsonPropertyName("inputs")]
    public Inputs Inputs { get; set; } = new();

}


public class JsonBuildEnvironment
{
}


public class Task
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("versionSpec")]
    public string VersionSpec { get; set; } = string.Empty;

    [JsonPropertyName("definitionType")]
    public string DefinitionType { get; set; } = string.Empty;

}


public class Target
{
    [JsonPropertyName("executionOptions")]
    public ExecutionOptions ExecutionOptions { get; set; } = new();

    [JsonPropertyName("allowScriptsAuthAccessOption")]
    public bool AllowScriptsAuthAccessOption { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

}


public class ExecutionOptions
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

}


public class Repository
{
    [JsonPropertyName("properties")]
    public JsonBuildProperties Properties { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("defaultBranch")]
    public string DefaultBranch { get; set; } = string.Empty;

    [JsonPropertyName("rootFolder")]
    public string RootFolder { get; set; } = string.Empty;

    [JsonPropertyName("clean")]
    public string Clean { get; set; } = string.Empty;

    [JsonPropertyName("checkoutSubmodules")]
    public bool CheckoutSubmodules { get; set; }

}


public class ProcessParameters
{
    [JsonPropertyName("inputs")]
    public Input[] Inputs { get; set; } = new Input[0];

}


public class Input
{
    [JsonPropertyName("options")]
    public Options Options { get; set; } = new();

    [JsonPropertyName("properties")]
    public JsonBuildProperties Properties { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("helpMarkDown")]
    public string HelpMarkDown { get; set; } = string.Empty;

}


public class Options
{
}


public class AuthoredBy
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

}


public class Avatar
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

}


public class Queue
{
    [JsonPropertyName("_links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("pool")]
    public Pool Pool { get; set; } = new();

}


public class Pool
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

}


public class Project
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public int State { get; set; }

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("lastUpdateTime")]
    public string LastUpdateTime { get; set; } = string.Empty;

}



