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
        var requestUrl = $"{teamProjectName}/_apis/wit/classificationnodes?api-version=5.0&$depth=5";

        var result = await CallEndpointViaGetAndGetResult<GetClassificationNodeResponse>(requestUrl, false);

        if (result != null && IsQuietMode == false)
        {
            foreach (var item in result.Value)
            {
                if (filterStructureType != null && item.StructureType != filterStructureType)
                {
                    continue;
                }

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

                if (item.Children != null)
                {
                    foreach (var child in item.Children)
                    {
                        if (filterStructureType != null && child.StructureType != filterStructureType)
                        {
                            continue;
                        }

                        WriteLine($"\tName: {child.Name}");
                        WriteLine($"\tStructureType: {child.StructureType}");
                        WriteLine($"\tPath: {child.Path}");

                        if (verbose)
                        {
                            WriteLine($"\tId: {child.Id}");
                            WriteLine($"\tUrl: {child.Url}");
                            WriteLine($"\tIdentifier: {child.Identifier}");
                            WriteLine($"\tHasChildren: {child.HasChildren}");
                            WriteLine($"\tUrl: {child.Url}");
                        }

                        if (child.Attributes != null)
                        {
                            WriteLine($"\tStartDate: {child.Attributes.StartDate}");
                            WriteLine($"\tFinishDate: {child.Attributes.FinishDate}");
                        }

                        WriteLine(string.Empty);
                    }
                }
            }
        }
        else
        {
            WriteLine($"No iterations");
        }
    }
}

