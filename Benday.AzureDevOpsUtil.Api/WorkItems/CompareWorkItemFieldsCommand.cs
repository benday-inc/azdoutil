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
    Name = Constants.CommandArgumentNameCompareWitdFields,
    Description = "Compare work item fields between two work item type definition files.", IsAsync = true)]
public class CompareWorkItemFieldsCommand : AzureDevOpsCommandBase
{

    public CompareWorkItemFieldsCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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
            WithDescription("Path to the source work item type definition file.")
            .FromPositionalArgument(2);

        args.AddBoolean(Constants.ArgumentNameReverseSourceAndTarget)
            .WithDescription("Reverse the source and target files.")
            .WithDefaultValue(false)
            .AsNotRequired().AllowEmptyValue();

        return args;
    }


    protected override Task OnExecute()
    {
        var file1 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile1, true);
        var file2 = Arguments.GetPathToFile(Constants.ArgumentNameComparisonFile2, true);

        var flipSourceAndTarget = Arguments.GetBooleanValue(Constants.ArgumentNameReverseSourceAndTarget);

        if (flipSourceAndTarget == true)
        {
            var temp = file1;
            file1 = file2;
            file2 = temp;
        }

        WriteLine($"Comparing '{file1}' to '{file2}'.");
        WriteLine("Loading files...");

        var witd1 = new WorkItemTypeDefinition(file1);
        var witd2 = new WorkItemTypeDefinition(file2);

        WriteLine("Comparing files...");

        CompareFields(witd1, witd2);

        return Task.CompletedTask;
    }

    private void CompareFields(WorkItemTypeDefinition witd1, WorkItemTypeDefinition witd2)
    {
        var fields1 = GetFieldDefinitions(witd1.GetFields());
        var fields2 = GetFieldDefinitions(witd2.GetFields());

        var fieldNames1 = fields1.Select(x => x.Name).OrderBy(x => x).ToList();
        var fieldNames2 = fields2.Select(x => x.Name).OrderBy(x => x).ToList();

        var in1Not2 = fieldNames1.Except(fieldNames2).ToList();
        var in2Not1 = fieldNames2.Except(fieldNames1).ToList();

        WriteLine();
        WriteLine("Fields in 1 but not in 2:");
        foreach (var item in in1Not2)
        {
            WriteLine(item);
        }

        WriteLine();
        WriteLine("Fields in 2 but not in 1:");
        foreach (var item in in2Not1)
        {
            WriteLine(item);
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
