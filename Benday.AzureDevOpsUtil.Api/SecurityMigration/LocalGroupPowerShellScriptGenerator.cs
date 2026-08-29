using System.Text;

namespace Benday.AzureDevOpsUtil.Api.SecurityMigration;

/// <summary>
/// Generates the PowerShell script that recreates the exported local groups
/// and their memberships on the new app tier machine.  The OS-level work has
/// to happen in Windows itself -- the Azure DevOps API cannot create machine
/// local groups -- so this script is the import side's first step.
/// </summary>
public static class LocalGroupPowerShellScriptGenerator
{
    public static string Generate(LocalGroupsExportDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var builder = new StringBuilder();

        builder.AppendLine("# Recreates the Windows local groups exported from " +
            $"{document.MachineName} ({document.CollectionUrl}).");
        builder.AppendLine($"# Exported {document.ExportedAtUtc:yyyy-MM-dd HH:mm} UTC by azdoutil {Constants.CommandName_ExportLocalGroups}.");
        builder.AppendLine("# Run in an elevated PowerShell session on the NEW app tier machine,");
        builder.AppendLine($"# then run 'azdoutil {Constants.CommandName_ImportLocalGroups}' to reapply the permission grants.");
        builder.AppendLine("# The server picks up new local groups when its identity sync job runs;");
        builder.AppendLine("# restarting the Azure DevOps Server services forces it.");
        builder.AppendLine();
        builder.AppendLine("$ErrorActionPreference = 'Stop'");

        foreach (var group in document.Groups)
        {
            builder.AppendLine();
            AppendGroup(builder, group);
        }

        return builder.ToString();
    }

    private static void AppendGroup(StringBuilder builder, ExportedLocalGroup group)
    {
        var groupName = EscapeSingleQuotes(group.GroupName);

        builder.AppendLine($"# -- {group.AccountName} --");
        builder.AppendLine($"if ($null -eq (Get-LocalGroup -Name '{groupName}' -ErrorAction SilentlyContinue)) {{");
        builder.AppendLine($"    New-LocalGroup -Name '{groupName}' | Out-Null");
        builder.AppendLine($"    Write-Host \"Created local group '{groupName}'\"");
        builder.AppendLine("} else {");
        builder.AppendLine($"    Write-Host \"Local group '{groupName}' already exists\"");
        builder.AppendLine("}");

        foreach (var member in group.Members)
        {
            if (member.IsMachineLocal == true)
            {
                // A member account that lived on the old machine cannot be
                // added by name on the new one -- the account itself has to be
                // recreated first, so this stays a decision for a human.
                builder.AppendLine(
                    $"# NOT ADDED: '{member.AccountName}' was local to the old machine. " +
                    "Recreate that account or pick its replacement, then add it manually.");

                continue;
            }

            var memberName = EscapeSingleQuotes(member.AccountName);

            builder.AppendLine("try {");
            builder.AppendLine($"    Add-LocalGroupMember -Group '{groupName}' -Member '{memberName}' -ErrorAction Stop");
            builder.AppendLine($"    Write-Host \"  added {memberName}\"");
            builder.AppendLine("} catch {");
            builder.AppendLine($"    Write-Host \"  skipped {memberName} ($($_.Exception.Message))\"");
            builder.AppendLine("}");
        }
    }

    private static string EscapeSingleQuotes(string value)
    {
        return value.Replace("'", "''");
    }
}
