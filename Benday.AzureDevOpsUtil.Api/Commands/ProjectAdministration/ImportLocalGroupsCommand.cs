using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.SecurityMigration;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.ProjectAdministration;

[Command(
    Category = Constants.Category_ProjectAdmin,
    Name = Constants.CommandName_ImportLocalGroups,
    Description =
        "Reapply the permission grants from an export-local-groups JSON file on the " +
        "new server. Each old local group is re-resolved by name under the new app " +
        "tier machine, and its grants are merged into the same security namespace " +
        "tokens they came from. Run the generated PowerShell script on the new " +
        "machine first so the groups exist and the server has synced them.",
    IsAsync = true)]
public class ImportLocalGroupsCommand : AzureDevOpsCommandBase
{
    public const string ArgumentNameMachine = "machine";

    public LocalGroupImportResult? LastResult { get; private set; }

    public ImportLocalGroupsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        arguments.AddString(Constants.ArgumentNameInputFile)
            .AsRequired()
            .WithDescription("Path to the JSON file written by " +
                Constants.CommandName_ExportLocalGroups);

        arguments.AddString(ArgumentNameMachine)
            .AsRequired()
            .WithDescription(
                "Name of the NEW app tier machine. Groups are resolved as " +
                "MACHINE\\GroupName under this name.");

        arguments.AddBoolean(Constants.ArgumentNamePreviewOnly)
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .AsNotRequired()
            .WithDescription("Resolve the groups and show what would be applied without changing anything");

        return arguments;
    }

    protected override async Task OnExecute()
    {
        var inputPath = Arguments.GetStringValue(Constants.ArgumentNameInputFile);
        var machineName = Arguments.GetStringValue(ArgumentNameMachine);
        var preview = Arguments.GetBooleanValue(Constants.ArgumentNamePreviewOnly);

        AssertFileExists(inputPath, Constants.ArgumentNameInputFile);

        var document = JsonSerializer.Deserialize<LocalGroupsExportDocument>(
            File.ReadAllText(inputPath));

        if (document == null || document.Groups.Count == 0)
        {
            throw new KnownException(
                $"'{inputPath}' does not contain a local groups export with any groups in it.");
        }

        var client = new SecurityApiClient(
            url => GetStringAsync(url, false, false),
            PostJsonAsync);

        var service = new LocalGroupImportService(
            client,
            message =>
            {
                if (IsQuietMode == false)
                {
                    WriteLine(message);
                }
            });

        var result = await service.ImportAsync(document, machineName, preview);

        LastResult = result;

        WriteLine();

        if (preview == true)
        {
            WriteLine($"Preview: nothing was changed on the server.");
        }

        WriteLine($"Groups resolved on '{machineName}': {result.ResolvedGroups.Count} of {document.Groups.Count}");

        foreach (var pair in result.ResolvedGroups
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            WriteLine($"   {pair.Key} -> {pair.Value.AccountName}");
        }

        if (result.UnresolvedGroups.Count > 0)
        {
            WriteLine();
            WriteLine("Groups that do not exist on the new server yet:");

            foreach (var group in result.UnresolvedGroups)
            {
                WriteLine($"   {group}");
            }

            WriteLine(
                "Run the PowerShell script from the export on the new app tier machine, " +
                "let the identity sync job pick the groups up (restarting the Azure DevOps " +
                "Server services forces it), and run this command again. Grants for groups " +
                "that already resolved are safe to apply now and re-running is safe -- " +
                "grants merge rather than replace.");
        }

        WriteLine();

        var verb = preview == true ? "Would apply" : "Applied";

        WriteLine($"{verb} {result.AppliedAceCount} permission grant(s) across {result.TokenCount} token(s).");

        if (result.FailedTokens.Count > 0)
        {
            WriteLine();
            WriteLine("Tokens the server refused:");

            foreach (var token in result.FailedTokens)
            {
                WriteLine($"   {token}");
            }
        }
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
