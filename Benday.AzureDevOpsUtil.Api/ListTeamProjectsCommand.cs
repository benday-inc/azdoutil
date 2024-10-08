﻿using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_ProjectAdmin,
    Name = Constants.CommandName_ListProjects,
        Description = "List team projects",
        IsAsync = true)]
public class ListTeamProjectsCommand : AzureDevOpsCommandBase
{
    public ListTeamProjectsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public ListProjectsResponse? LastResult { get; private set; }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);

        return arguments;
    }

    protected override async Task OnExecute()
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl = $"_apis/projects?$top=10000&api-version=7.0";

        var temp = await client.GetAsync(requestUrl);

        var result = await client.GetStringAsync(requestUrl);

        if (temp.IsSuccessStatusCode == false)
        {
            throw new KnownException($"Failed to get projects.  Status code: {temp.StatusCode}.  Content: {result}");
        }

        var resultAsJson = JsonUtilities.GetJsonValueAsType<ListProjectsResponse>(result);

        LastResult = resultAsJson;

        if (IsQuietMode == false)
        {
            WriteLine($"Project count: {LastResult!.Count}");

            foreach (var item in LastResult.Projects.OrderBy(p => p.Name))
            {
                WriteLine($"{item.Name}");
            }
        }
    }
}
