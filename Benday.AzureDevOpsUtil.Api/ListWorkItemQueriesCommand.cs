using System.Text.Json;
using System.Text;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Excel;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_WorkItems,
    Name = Constants.CommandArgumentNameListWorkItemQueries, 
    Description = "Gets a list of all work item queries in an Azure DevOps Team Project.", 
    IsAsync = true)]
public class ListWorkItemQueriesCommand : AzureDevOpsCommandBase
{

    public ListWorkItemQueriesCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();


        AddCommonArguments(args);
        args.AddString(Constants.ArgumentNameTeamProjectName).AsRequired().
            WithDescription("Team project name that contains the work item queries");
        
        return args;
    }


    protected override async Task OnExecute()
    {
        var projectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        
        var result = await ListWorkItemQueries(projectName);

        if (result == null)
        {
            WriteLine("Result is null");
        }
        else if (result.Count == 0)
        {
            WriteLine("No queries found");
        }
        else
        {            
            foreach (var query in result.Value)
            {
                WriteLine(query.Name);

                if (query.HasChildren == true)
                {
                    foreach (var child in query.Children)
                    {
                        WriteLine($"\t{child.Name}");
                    }
                }                
            }
        }
    }

    private async Task<WorkItemQuerySearchResponse> ListWorkItemQueries(string projectName)
    {        
        var queryString = 
            $"{projectName.Replace(" ", "%20")}/_apis/wit/queries?api-version=7.0&$expand=1&$depth=2";

        using var client = GetHttpClientInstanceForAzureDevOps();

        //WriteLine(client.BaseAddress.ToString());
        //WriteLine($"Calling {queryString}");

        var result = await client.GetStringAsync(queryString);

        var returnValue = JsonSerializer.Deserialize<WorkItemQuerySearchResponse>(result);

        return returnValue!;
    }
}

