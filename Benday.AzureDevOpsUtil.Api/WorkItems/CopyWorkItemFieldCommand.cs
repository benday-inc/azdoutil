using System.Text.Json;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.WorkItems;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameCopyWitdField,
    Description = "Copy work item field from one work item type definition to another.", IsAsync = true)]
public class CopyWorkItemFieldCommand : AzureDevOpsCommandBase
{

    public CopyWorkItemFieldCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameComparisonFile1).AsRequired().
            WithDescription("Path to the source work item type definition file.")
            .FromPositionalArgument(1);

        args.AddString(Constants.ArgumentNameComparisonFile2).AsRequired().
            WithDescription("Path to the target work item type definition file.")
            .FromPositionalArgument(2);

        args.AddString(Constants.ArgumentNameFieldRefName).AsRequired().
            WithDescription("Refname of the field to copy.");

        args.AddBoolean(Constants.ArgumentNameOverwrite).AsRequired().
            WithDescription("Overwrite the target field if it already exists.")
            .WithDefaultValue(false);

        return args;
    }


    protected override Task OnExecute()
    {
        var file1 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile1, true);
        var file2 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile2, true);

        var refname = Arguments.GetStringValue(Constants.ArgumentNameFieldRefName);

        var overwrite = Arguments.GetBooleanValue(Constants.ArgumentNameOverwrite);

        WriteLine("Loading files...");

        var witd1 = new WorkItemTypeDefinition(file1);
        var witd2 = new WorkItemTypeDefinition(file2);

        CopyField(witd1, witd2, refname, overwrite);

        WriteLine("Field copied.");

        witd2.Save(file2);

        WriteLine("Saved target file.");

        return Task.CompletedTask;
    }

    private void CopyField(WorkItemTypeDefinition sourceWitd, WorkItemTypeDefinition targetWitd, string refname, bool overwrite)
    {
        var fields1 = GetFieldDefinitions(sourceWitd.GetFields());
        var fields2 = GetFieldDefinitions(targetWitd.GetFields());

        var sourceField = fields1.Find(x => String.Equals(x.RefName, refname, StringComparison.OrdinalIgnoreCase));

        if (sourceField == null)
        {
            throw new KnownException($"Field with refname '{refname}' not found in source file.");
        }

        var targetField = fields2.Find(x => String.Equals(x.RefName, refname, StringComparison.OrdinalIgnoreCase));

        if (targetField != null && overwrite == false)
        {
            throw new KnownException($"Field with refname '{refname}' already exists in target file.");
        }
        else if (targetField != null && overwrite == true)
        {
            WriteLine($"Overwriting field '{refname}' in target file.");
        }
        
        if (targetField == null)
        {
            targetWitd.CopyField(sourceField);
        }
        else
        {
            targetWitd.CopyField(sourceField, targetField);
        }        
    }

    private List<WorkItemFieldDefinition> GetFieldDefinitions(List<XElement> fromValues)
    {
        var toValues = new List<WorkItemFieldDefinition>();

        foreach (var fromValue in fromValues)
        {
            var item = new WorkItemFieldDefinition(fromValue);
            toValues.Add(item);
        }

        return toValues;
    }
}
