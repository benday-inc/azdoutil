// Decompiled with JetBrains decompiler
// Type: Benday.AzureDevOpsUtil.Api.ImportReleaseDefinitionCommand
// Assembly: Benday.AzureDevOpsUtil.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 3BCB724F-BB08-4AFE-A058-EBC3A1C3B6B5
// Assembly location: C:\code\temp\20260120_090816\azdoutil.2.14.0\tools\net9.0\any\Benday.AzureDevOpsUtil.Api.dll

using Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails;
using Benday.CommandsFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable
namespace Benday.AzureDevOpsUtil.Api.Commands.Builds.Releases;

[Command(Category = "Builds", Name = "importreleasedef", Description = "Import release definition from JSON file", IsAsync = true)]
public class ImportReleaseDefinitionCommand(
  CommandExecutionInfo info,
  ITextOutputProvider outputProvider) : AzureDevOpsCommandBase(info, outputProvider)
{
  private string _teamProjectName = string.Empty;
  private string _inputFilePath = string.Empty;
  private int? _definitionToCloneId = new int?();
  private int? _definitionToCloneRevision = new int?();

  public GetReleaseDefinitionDetailResponse? LastResult { get; private set; }

  public override ArgumentCollection GetArguments()
  {
    ArgumentCollection arguments = new ArgumentCollection();
    this.AddCommonArguments(arguments);
    arguments.AddString("teamproject").WithDescription<string>("Team project name").AsRequired<string>();
    arguments.AddFile("input").MustExist<string>().WithDescription<string>("Path to JSON file containing release definition").FromPositionalArgument<string>(1).AsRequired<string>();
    arguments.AddInt32("cloneid").WithDescription<int>("ID of the definition to clone (optional)").AsNotRequired<int>();
    arguments.AddInt32("clonerev").WithDescription<int>("Revision of the definition to clone (optional)").AsNotRequired<int>();
    return arguments;
  }

  public string? LastResultRawJson { get; private set; }

  protected override async Task OnExecute()
  {
    this._teamProjectName = this.Arguments.GetStringValue("teamproject");
    this._inputFilePath = this.Arguments.GetStringValue("input");
    if (this.Arguments.HasValue("cloneid"))
      this._definitionToCloneId = new int?(this.Arguments.GetInt32Value("cloneid"));
    if (this.Arguments.HasValue("clonerev"))
      this._definitionToCloneRevision = new int?(this.Arguments.GetInt32Value("clonerev"));
    if (!File.Exists(this._inputFilePath))
      throw new KnownException("Input file does not exist: " + this._inputFilePath);
    this.WriteLine("Reading release definition from: " + this._inputFilePath);
    string json = await File.ReadAllTextAsync(this._inputFilePath, Encoding.UTF8);
    if (string.IsNullOrWhiteSpace(json))
      throw new KnownException("Input file is empty or contains only whitespace");
    try
    {
      using (JsonDocument.Parse(json))
        ;
    }
    catch (JsonException ex)
    {
      throw new KnownException("Invalid JSON in input file: " + ex.Message);
    }
    string requestUrl = this._teamProjectName + "/_apis/release/definitions?api-version=7.1";
    if (this._definitionToCloneId.HasValue)
    {
      requestUrl += $"&definitionToCloneId={this._definitionToCloneId.Value}";
      if (this._definitionToCloneRevision.HasValue)
        requestUrl += $"&definitionToCloneRevision={this._definitionToCloneRevision.Value}";
    }
    this.WriteLine("Importing release definition to team project: " + this._teamProjectName);
    this.WriteLine("API Endpoint: " + requestUrl);
    try
    {
      JsonElement definition = JsonSerializer.Deserialize<JsonElement>(json);
      JsonElement modifiableDefinition = ImportReleaseDefinitionCommand.RemoveReadOnlyProperties(definition);
      string modifiedJson = JsonSerializer.Serialize<JsonElement>(modifiableDefinition, new JsonSerializerOptions()
      {
        WriteIndented = true
      });
      HttpClient client = this.GetHttpClientInstanceForAzureDevOps(AzureDevOpsUrlTargetType.Release);
      string requestAsJson;
      HttpRequestMessage request;
      HttpResponseMessage result;
      string responseContent;
      GetReleaseDefinitionDetailResponse response;
      try
      {
        requestAsJson = JsonSerializer.Serialize<JsonElement>(modifiableDefinition);
        request = new HttpRequestMessage(new HttpMethod("POST"), requestUrl)
        {
          Content = (HttpContent) new StringContent(requestAsJson, Encoding.UTF8, "application/json")
        };
        result = await client.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
          string errorContent = await result.Content.ReadAsStringAsync();
          if (errorContent.Contains("RouteIdConflictException", StringComparison.OrdinalIgnoreCase))
            throw new KnownException($"Project ID mismatch. The release definition contains a project ID that doesn't match the target project '{this._teamProjectName}'. The definition has been updated to remove the project reference.");
          if (errorContent.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            throw new KnownException($"A release definition with the same name already exists in project '{this._teamProjectName}'. Please rename the definition in the JSON file or delete the existing definition first.");
          throw new KnownException($"Failed to import release definition: {result.StatusCode} {result.ReasonPhrase} - {errorContent}");
        }
        responseContent = await result.Content.ReadAsStringAsync();
        response = JsonSerializer.Deserialize<GetReleaseDefinitionDetailResponse>(responseContent);
        this.LastResult = response != null ? response : throw new KnownException("Failed to import release definition - no response received");
        this.LastResult.RawJson = responseContent;
        this.LastResultRawJson = responseContent;
        this.WriteLine();
        this.WriteLine("Release definition imported successfully!");
        this.WriteLine($"  ID: {response.Id}");
        this.WriteLine("  Name: " + response.Name);
        this.WriteLine("  URL: " + response.Url);
        this.WriteLine("  Path: " + response.Path);
        if (response.Revision > 0)
          this.WriteLine($"  Revision: {response.Revision}");
        if (!string.IsNullOrEmpty(response.ReleaseNameFormat))
          this.WriteLine("  Release Name Format: " + response.ReleaseNameFormat);
        if (response.Environments != null && response.Environments.Length != 0)
        {
          this.WriteLine($"  Environments: {response.Environments.Length}");
          Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails.Environment[] environmentArray = response.Environments;
          for (int index = 0; index < environmentArray.Length; ++index)
          {
            Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails.Environment env = environmentArray[index];
            this.WriteLine($"    - {env.Name} (ID: {env.Id})");
            env = (Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails.Environment) null;
          }
          environmentArray = (Benday.AzureDevOpsUtil.Api.Messages.ReleaseDefinitionDetails.Environment[]) null;
        }
      }
      finally
      {
        client?.Dispose();
      }
      definition = new JsonElement();
      modifiableDefinition = new JsonElement();
      modifiedJson = (string) null;
      client = (HttpClient) null;
      requestAsJson = (string) null;
      request = (HttpRequestMessage) null;
      result = (HttpResponseMessage) null;
      responseContent = (string) null;
      response = (GetReleaseDefinitionDetailResponse) null;
    }
    catch (InvalidOperationException ex)
    {
      throw new KnownException("Failed to import release definition: " + ex.Message);
    }
    catch (Exception ex) when (!(ex is KnownException))
    {
      throw new KnownException("Unexpected error importing release definition: " + ex.Message);
    }
    json = (string) null;
    requestUrl = (string) null;
  }

  private static JsonElement RemoveReadOnlyProperties(JsonElement definition)
  {
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    foreach (JsonProperty jsonProperty in definition.EnumerateObject())
    {
      string name = jsonProperty.Name;
      if (!ImportReleaseDefinitionCommand.IsReadOnlyProperty(name))
      {
        JsonElement jsonElement;
        int num1;
        if (name == "environments")
        {
          jsonElement = jsonProperty.Value;
          num1 = jsonElement.ValueKind == JsonValueKind.Array ? 1 : 0;
        }
        else
          num1 = 0;
        if (num1 != 0)
        {
          List<object> objectList = new List<object>();
          jsonElement = jsonProperty.Value;
          foreach (JsonElement enumerate in jsonElement.EnumerateArray())
            objectList.Add(ImportReleaseDefinitionCommand.CleanEnvironmentObject(enumerate));
          dictionary[name] = (object) objectList;
        }
        else
        {
          int num2;
          if (name == "artifacts")
          {
            jsonElement = jsonProperty.Value;
            num2 = jsonElement.ValueKind == JsonValueKind.Array ? 1 : 0;
          }
          else
            num2 = 0;
          if (num2 != 0)
          {
            List<object> objectList = new List<object>();
            jsonElement = jsonProperty.Value;
            foreach (JsonElement enumerate in jsonElement.EnumerateArray())
              objectList.Add(ImportReleaseDefinitionCommand.CleanArtifactObject(enumerate));
            dictionary[name] = (object) objectList;
          }
          else
            dictionary[name] = (object) jsonProperty.Value;
        }
      }
    }
    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize<Dictionary<string, object>>(dictionary));
  }

  private static object? CleanEnvironmentObject(JsonElement environment)
  {
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    foreach (JsonProperty jsonProperty in environment.EnumerateObject())
    {
      string name = jsonProperty.Name;
      if (!(name == "id") && !(name == "currentRelease") && !(name == "badgeUrl") && !(name == "environmentOptions"))
      {
        JsonElement jsonElement;
        int num;
        if (name == "deployPhases")
        {
          jsonElement = jsonProperty.Value;
          num = jsonElement.ValueKind == JsonValueKind.Array ? 1 : 0;
        }
        else
          num = 0;
        if (num != 0)
        {
          List<object> objectList = new List<object>();
          jsonElement = jsonProperty.Value;
          foreach (JsonElement enumerate in jsonElement.EnumerateArray())
            objectList.Add(ImportReleaseDefinitionCommand.CleanDeployPhaseObject(enumerate));
          dictionary[name] = (object) objectList;
        }
        else
          dictionary[name] = (object) jsonProperty.Value;
      }
    }
    return (object) dictionary;
  }

  private static object? CleanDeployPhaseObject(JsonElement deployPhase)
  {
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    foreach (JsonProperty jsonProperty in deployPhase.EnumerateObject())
      dictionary[jsonProperty.Name] = (object) jsonProperty.Value;
    return (object) dictionary;
  }

  private static object? CleanArtifactObject(JsonElement artifact)
  {
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    foreach (JsonProperty jsonProperty in artifact.EnumerateObject())
    {
      string name = jsonProperty.Name;
      if (!(name == "id"))
        dictionary[name] = (object) jsonProperty.Value;
    }
    return (object) dictionary;
  }

  private static bool IsReadOnlyProperty(string propertyName)
  {
    return new HashSet<string>((IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase)
    {
      "id",
      "revision",
      "createdDate",
      "createdOn",
      "createdBy",
      "modifiedDate",
      "modifiedOn",
      "modifiedBy",
      "uri",
      "url",
      "_links",
      "links",
      "lastRelease",
      "projectReference",
      "isDeleted",
      "properties"
    }.Contains(propertyName);
  }
}
