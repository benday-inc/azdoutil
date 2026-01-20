using System.Net;
using System.Text;
using System.Text.Json;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Builds;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameImportBuildDefinition,
    Description = "Import build definition from JSON file",
    IsAsync = true)]
public class ImportBuildDefinitionCommand : AzureDevOpsCommandBase
{
    private string _teamProjectName = string.Empty;
    private string _inputFilePath = string.Empty;
    private int? _definitionToCloneId = null;
    private int? _definitionToCloneRevision = null;

    public BuildDefinitionInfo? LastResult { get; private set; }

    public ImportBuildDefinitionCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name")
            .AsRequired();

        arguments.AddFile(Constants.ArgumentNameInputFile)
            .MustExist()
            .WithDescription("Path to JSON file containing build definition")
            .FromPositionalArgument(1)
            .AsRequired();

        arguments.AddInt32("cloneid")
            .WithDescription("ID of the definition to clone (optional)")
            .AsNotRequired();

        arguments.AddInt32("clonerev")
            .WithDescription("Revision of the definition to clone (optional)")
            .AsNotRequired();

        return arguments;
    }

    public string? LastResultRawJson { get; private set; }

    protected override async Task OnExecute()
    {
        _teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _inputFilePath = Arguments.GetStringValue(Constants.ArgumentNameInputFile);

        if (Arguments.HasValue("cloneid"))
        {
            _definitionToCloneId = Arguments.GetInt32Value("cloneid");
        }

        if (Arguments.HasValue("clonerev"))
        {
            _definitionToCloneRevision = Arguments.GetInt32Value("clonerev");
        }

        // Validate file exists
        if (!System.IO.File.Exists(_inputFilePath))
        {
            throw new KnownException($"Input file does not exist: {_inputFilePath}");
        }

        // Read the JSON from file
        WriteLine($"Reading build definition from: {_inputFilePath}");
        var json = await System.IO.File.ReadAllTextAsync(_inputFilePath, Encoding.UTF8);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new KnownException("Input file is empty or contains only whitespace");
        }

        // Validate it's valid JSON
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new KnownException($"Invalid JSON in input file: {ex.Message}");
        }

        // Build the request URL
        var requestUrl = $"{_teamProjectName}/_apis/build/definitions?api-version=7.1";

        // Add optional clone parameters if provided
        if (_definitionToCloneId.HasValue)
        {
            requestUrl += $"&definitionToCloneId={_definitionToCloneId.Value}";

            if (_definitionToCloneRevision.HasValue)
            {
                requestUrl += $"&definitionToCloneRevision={_definitionToCloneRevision.Value}";
            }
        }

        WriteLine($"Importing build definition to team project: {_teamProjectName}");
        WriteLine($"API Endpoint: {requestUrl}");

        try
        {
            // Parse the JSON to get the definition object
            var definition = JsonSerializer.Deserialize<JsonElement>(json);

            // Remove read-only properties that shouldn't be sent in POST
            var modifiableDefinition = RemoveReadOnlyProperties(definition);

            // Convert back to JSON string
            var modifiedJson = JsonSerializer.Serialize(modifiableDefinition, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Send POST request to create the build definition
            var response = await SendPostForBodyAndGetTypedResponseSingleAttempt<BuildDefinitionInfo, JsonElement>(
                requestUrl,
                modifiableDefinition,
                writeStringContentToInfo: false,
                optionalDebuggingMessageInfo: "Creating build definition"
            );

            if (response != null)
            {
                LastResult = response;
                LastResultRawJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                WriteLine();
                WriteLine("Build definition imported successfully!");
                WriteLine($"  ID: {response.Id}");
                WriteLine($"  Name: {response.Name}");
                WriteLine($"  URL: {response.Url}");

                if (!string.IsNullOrEmpty(response.Quality))
                {
                    WriteLine($"  Quality: {response.Quality}");
                }

                if (response.Revision > 0)
                {
                    WriteLine($"  Revision: {response.Revision}");
                }

                if (response.Project != null && !string.IsNullOrEmpty(response.Project.Name))
                {
                    WriteLine($"  Project: {response.Project.Name}");
                }
            }
            else
            {
                throw new KnownException("Failed to import build definition - no response received");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Check if this is a duplicate name error
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                throw new KnownException($"A build definition with the same name already exists in project '{_teamProjectName}'. " +
                    "Please rename the definition in the JSON file or delete the existing definition first.");
            }

            throw new KnownException($"Failed to import build definition: {ex.Message}");
        }
        catch (Exception ex) when (!(ex is KnownException))
        {
            throw new KnownException($"Unexpected error importing build definition: {ex.Message}");
        }
    }

    private static JsonElement RemoveReadOnlyProperties(JsonElement definition)
    {
        // Create a dictionary to build the modified definition
        var modifiedDef = new Dictionary<string, object?>();

        foreach (var property in definition.EnumerateObject())
        {
            // Skip read-only properties that shouldn't be sent in POST
            var propertyName = property.Name;
            if (IsReadOnlyProperty(propertyName))
            {
                continue;
            }

            // Special handling for nested objects
            if (propertyName == "_links" || propertyName == "links")
            {
                continue; // Skip links as they're generated by server
            }

            // Add the property to modified definition
            modifiedDef[propertyName] = property.Value;
        }

        // Convert dictionary back to JsonElement
        var jsonString = JsonSerializer.Serialize(modifiedDef);
        return JsonSerializer.Deserialize<JsonElement>(jsonString);
    }

    private static bool IsReadOnlyProperty(string propertyName)
    {
        // List of read-only properties that should not be sent in POST
        var readOnlyProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "revision",
            "createdDate",
            "createdBy",
            "changedDate",
            "changedBy",
            "uri",
            "url",
            "_links",
            "links",
            "latestBuild",
            "latestCompletedBuild",
            "metrics",
            "project" // Remove project info - it will be inferred from the URL
        };

        return readOnlyProperties.Contains(propertyName);
    }
}