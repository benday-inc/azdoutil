using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.SecurityMigration;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;

[Command(
    Category = Constants.Category_ProjectAdmin,
    Name = Constants.CommandName_ExportLocalGroups,
    Description =
        "Export the permission grants held by Windows local groups on the app tier " +
        "machine: the groups, their memberships, and every security namespace ACL " +
        "that references them, serialized to JSON. Also writes a PowerShell script " +
        "that recreates the groups and memberships on a new machine, for a server " +
        "migration where the collection database moves to a new app tier.",
    IsAsync = true)]
public class ExportLocalGroupsCommand : AzureDevOpsCommandBase
{
    public const string ArgumentNameMachine = "machine";
    public const string DefaultOutputFileName = "local-groups-export.json";

    public LocalGroupExportResult? LastResult { get; private set; }

    public ExportLocalGroupsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(ArgumentNameMachine)
            .AsRequired()
            .WithDescription(
                "Name of the app tier machine whose local groups should be exported. " +
                "Grants are matched by the domain part of each Windows identity.");

        arguments.AddString(Constants.ArgumentNameOutputFile)
            .AsNotRequired()
            .WithDescription(
                $"Path for the export JSON file. Default is '{DefaultOutputFileName}' " +
                "in the current directory. The PowerShell script is written next to " +
                "it with a .ps1 extension.");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var machineName = Arguments.GetStringValue(ArgumentNameMachine);

        var outputPath = Arguments.HasValue(Constants.ArgumentNameOutputFile)
            ? Arguments.GetStringValue(Constants.ArgumentNameOutputFile)
            : Path.Combine(Directory.GetCurrentDirectory(), DefaultOutputFileName);

        outputPath = Path.GetFullPath(outputPath);

        var scriptPath = Path.ChangeExtension(outputPath, ".ps1");

        var client = new SecurityApiClient(
            url => GetStringAsync(url, false, false),
            PostJsonAsync);

        var service = new LocalGroupExportService(
            client,
            message =>
            {
                if (IsQuietMode == false)
                {
                    WriteLine(message);
                }
            });

        var result = await service.ExportAsync(machineName, Configuration.CollectionUrl);

        LastResult = result;

        if (result.Document.Groups.Count == 0)
        {
            WriteLine();
            WriteLine(
                $"No local groups on machine '{machineName}' hold any permission grants " +
                $"in this collection ({result.NamespacesScanned} namespaces, " +
                $"{result.AclsScanned} ACLs scanned).");

            if (result.WindowsDomainsSeen.Count > 0)
            {
                WriteLine(
                    "Windows identities in the ACLs belong to: " +
                    string.Join(", ", result.WindowsDomainsSeen) +
                    $". If one of these is the app tier machine, rerun with that value for /{ArgumentNameMachine}.");
            }

            return;
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (string.IsNullOrEmpty(outputDirectory) == false)
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, JsonSerializer.Serialize(
            result.Document, new JsonSerializerOptions { WriteIndented = true }));

        File.WriteAllText(scriptPath,
            LocalGroupPowerShellScriptGenerator.Generate(result.Document));

        WriteLine();
        WriteLine($"Exported {result.Document.Groups.Count} local group(s) with " +
            $"{result.Document.AccessControlEntries.Count} permission grant(s) " +
            $"from {result.NamespacesScanned} security namespaces.");
        WriteLine();

        foreach (var group in result.Document.Groups)
        {
            var grantCount = result.Document.AccessControlEntries
                .Count(x => string.Equals(
                    x.GroupAccountName, group.AccountName, StringComparison.OrdinalIgnoreCase));

            WriteLine($"   {group.AccountName} -- {group.Members.Count} member(s), {grantCount} grant(s)");

            if (group.Members.Count == 0)
            {
                WriteLine(
                    "      No members came back from the identity service. Verify this " +
                    "group's membership on the app tier machine itself.");
            }
        }

        WriteLine();
        WriteLine($"Export:            {outputPath}");
        WriteLine($"PowerShell script: {scriptPath}");
        WriteLine();
        WriteLine("Next steps on the new server: run the PowerShell script on the new app tier");
        WriteLine("machine, let the identity sync job notice the new groups (restarting the");
        WriteLine($"Azure DevOps Server services forces it), then run '{Constants.ExeName} " +
            $"{Constants.CommandName_ImportLocalGroups}' to reapply the grants.");
    }

    private async Task<string?> PostJsonAsync(string requestUrl, string bodyJson)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        var result = await client.SendAsync(request);

        if (result.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await result.Content.ReadAsStringAsync();
    }
}
