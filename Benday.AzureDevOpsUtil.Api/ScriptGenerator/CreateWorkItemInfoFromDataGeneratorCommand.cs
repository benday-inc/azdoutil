using System.Runtime.InteropServices;

using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.ScriptGenerator;

[Command(
    Category = Constants.Category_TestData,
    Name = Constants.CommandName_CreateWorkItemsRandom,
        Description = "Create work items data using random data generator",
        IsAsync = true)]
public class CreateWorkItemInfoFromDataGeneratorCommand : AzureDevOpsCommandBase
{
    public CreateWorkItemInfoFromDataGeneratorCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        return arguments;
    }

    protected override Task OnExecute()
    {
        GenerateTitlesOnly();

        return Task.CompletedTask;
    }

    private void GenerateTitlesOnly()
    {
        WriteLine("Running in titles only mode. Skipping write to Azure DevOps.");
        WriteLine();

        var generator = new WorkItemScriptGenerator(false);

        var numberOfTitles = 40;

        var items = new List<string>();

        for (int i = 0; i < numberOfTitles; i++)
        {
            items.Add(generator.GetRandomTitle());
        }

        foreach (var item in items)
        {
            WriteLine(item);
        }
    }
}