using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
namespace Benday.AzureDevOpsUtil.Api;

public abstract class GetClassificationNodesCommandBase : AzureDevOpsCommandBase
{
    public GetClassificationNodesCommandBase(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    protected async Task GetNodes(string teamProjectName, string filterStructureType, bool verbose)
    {
        
        string requestUrl;

        if (filterStructureType == "area")
        {
            requestUrl = $"{teamProjectName.Replace(" ", "%20")}/_apis/wit/classificationnodes/Areas?api-version=7.0&$depth=5";
        }
        else if (filterStructureType == "iteration")
        {
            requestUrl = $"{teamProjectName.Replace(" ", "%20")}/_apis/wit/classificationnodes/Iterations?api-version=7.0&$depth=5";
        }
        else
        {
            throw new NotImplementedException($"Filter structure type '{filterStructureType}' not supported.");
        }

        var result = await CallEndpointViaGetAndGetResult<ClassificationNode>(requestUrl, false);

        if (result != null && IsQuietMode == false)
        {

            WriteClassificationNode(result, verbose);
        }
        else
        {
            WriteLine($"No {filterStructureType}s found.");
        }
    }

    private void WriteClassificationNode(ClassificationNode item, bool verbose)
    {
        WriteLine($"Name: {item.Name}");
        WriteLine($"Path: {item.Path}");
        WriteLine($"StructureType: {item.StructureType}");

        if (verbose)
        {
            WriteLine($"Id: {item.Id}");
            WriteLine($"Url: {item.Url}");
            WriteLine($"Identifier: {item.Identifier}");
            WriteLine($"HasChildren: {item.HasChildren}");
            WriteLine($"Url: {item.Url}");
        }

        WriteLine(string.Empty);

        WriteClassificationNodeChildren(item, verbose, 1);
    }

    private void WriteClassificationNodeChildren(
        ClassificationNode item, bool verbose, int indentLevel)
    {
        if (item.Children != null)
        {
            var indentString = new String('\t', indentLevel);

            foreach (var child in item.Children)
            {
                WriteLine($"{indentString}Name: {child.Name}");
                WriteLine($"{indentString}StructureType: {child.StructureType}");
                WriteLine($"{indentString}Path: {child.Path}");

                if (verbose)
                {
                    WriteLine($"{indentString}Id: {child.Id}");
                    WriteLine($"{indentString}Url: {child.Url}");
                    WriteLine($"{indentString}Identifier: {child.Identifier}");
                    WriteLine($"{indentString}HasChildren: {child.HasChildren}");
                    WriteLine($"{indentString}Url: {child.Url}");
                }

                if (child.Attributes != null)
                {
                    WriteLine($"{indentString}StartDate: {child.Attributes.StartDate}");
                    WriteLine($"{indentString}FinishDate: {child.Attributes.FinishDate}");
                }

                WriteLine(string.Empty);

                WriteClassificationNodeChildren(child, verbose, indentLevel + 1);
            }
        }
    }
}

