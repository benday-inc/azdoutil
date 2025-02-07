﻿using System.Net;
using System.Text;
using System.Text.Json;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.AzureDevOpsUtil.Api.Messages.Releases;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Builds,
    Name = Constants.CommandArgumentNameListReleaseDefinitions,
        Description = "List release definitions",
        IsAsync = true)]
public class ListReleaseDefinitionsCommand : AzureDevOpsCommandBase
{
    private string _TeamProjectName = string.Empty;

    public GetReleasesForProjectResponse? LastResult { get; private set; }

    public ListReleaseDefinitionsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .WithDescription("Team project name").
            AsNotRequired();

        arguments.AddBoolean(Constants.ArgumentNameAllProjects)
            .AllowEmptyValue()
            .WithDescription("All releases in all projects in this collection")
            .AsNotRequired();

        arguments.AddBoolean(Constants.CommandArgumentNameToJson)
            .AllowEmptyValue()
            .WithDescription("Export to JSON")
            .AsNotRequired();

        return arguments;
    }

    private string SerializeObjectToJson(List<ReleaseInfo> results)
    {
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return json;
    }

    protected override async Task OnExecute()
    {
        if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == false &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == false)
        {
            throw new KnownException(
                $"You must specify either /{Constants.ArgumentNameAllProjects} or supply a value for /{Constants.ArgumentNameTeamProjectName}.");
        }
        else if (Arguments.HasValue(Constants.ArgumentNameAllProjects) == true &&
            Arguments.HasValue(Constants.ArgumentNameTeamProjectName) == true)
        {
            throw new KnownException(
                $"You cannot specify both /{Constants.ArgumentNameAllProjects} and /{Constants.ArgumentNameTeamProjectName} at the same time.");
        }

        bool allProjects;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameAllProjects) == true)
        {
            _TeamProjectName = string.Empty;
            allProjects = true;
        }
        else
        {
            _TeamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
            allProjects = false;
        }

        var toJson = Arguments.GetBooleanValue(Constants.CommandArgumentNameToJson);

        if (allProjects == false)
        {
            await ListForSingleProject(toJson, _TeamProjectName);
        }
        else
        {
            await ListForAllProjects(toJson);
        }
    }
    

    private async Task ListForAllProjects(bool json)
    {
        // call ListTeamProjectsCommand
        var command = new ListTeamProjectsCommand(
            ExecutionInfo.GetCloneOfArguments(
               Constants.CommandName_ListProcessTemplates, true), _OutputProvider);

        await command.ExecuteAsync();

        if (command.LastResult == null)
        {
            throw new KnownException("No team projects found.");
        }
        else
        {
            var teamProjects = command.LastResult.Projects;

            var results = new List<ReleaseInfo>();

            foreach (var teamProject in teamProjects)
            {
                var result = await GetResult(teamProject.Name);

                if (result != null && result.Count > 0)
                {
                    results.AddRange(result.Releases);

                    await PopulateReleaseDetails(result.Releases);
                }
            }

            if (json == true)
            {
                WriteLine(SerializeObjectToJson(results));
            }
            else
            {
                WriteLine(String.Empty);
                WriteLine($"Result count: {results.Count}");

                var groupedResults = results.GroupBy(x => x.ProjectReference.Name);

                // order by team project name
                groupedResults = groupedResults.OrderBy(x => x.Key);

                foreach (var groupedResult in groupedResults)
                {
                    WriteLine();
                    WriteLine("** Team Project: " + groupedResult.Key);

                    foreach (var item in groupedResult)
                    {
                        WriteLine(ToString(item));
                    }
                }
            }
        }

    }

    private async Task ListForSingleProject(bool toJson, string teamProjectName)
    {
        var results = await GetResult(teamProjectName);

        if (results != null)
        {
            await PopulateReleaseDetails(results.Releases);
        }

        if (results == null)
        {
            WriteLine(String.Empty);
            WriteLine("No build definitions found");
        }
        else if (toJson == true)
        {
            WriteLine(SerializeObjectToJson(results.Releases.ToList()));
        }
        else
        {
            WriteLine(String.Empty);

            WriteLine($"Result count: {results.Count}");

            results.Releases.ToList().ForEach(x => WriteLine(ToString(x)));
        }
    }

    private async Task PopulateReleaseDetails(ReleaseInfo[] releases)
    {
        foreach (var release in releases)
        {
            
            var teamProjectName = release.ProjectReference.Name;

            var releaseId = release.ReleaseDefinition.Id;

            var requestUrl = $"{teamProjectName}/_apis/release/definitions/{releaseId}?api-version=7.1";

            var result = await CallEndpointViaGetAndGetResult<GetReleaseDetailResponse>(
                requestUrl, azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);

            release.Details = result;
        }
    }

    private async Task<GetReleasesForProjectResponse?> GetResult(string teamProjectName)
    {
        var requestUrl = $"{teamProjectName}/_apis/release/releases?api-version=7.1";
        
        var result = await CallEndpointViaGetAndGetResult<GetReleasesForProjectResponse>(
            requestUrl, azureDevOpsUrlTargetType: AzureDevOpsUrlTargetType.Release);

        LastResult = result;

        return result;
    }

    private string ToString(ReleaseInfo definition)
    {
        var builder = new StringBuilder();
        builder.Append(definition.Name);

        if (Arguments.GetBooleanValue(Constants.ArgumentNameNameOnly) == false)
        {
            builder.Append(" (");
            builder.Append(definition.Id);
            builder.Append(")");

            if (definition.Details != null)
            {
                var usesAgents = new List<string>();

                foreach (var environment in definition.Details.Environments)
                {
                    foreach (var phase in environment.DeployPhases)
                    {
                        // usesAgents.Add(phase.DeploymentInput.AgentSpecification.Identifier);
                        usesAgents.Add(phase.DeploymentInput.QueueId.ToString());
                    }
                }

                usesAgents = usesAgents.Distinct().ToList();

                if (usesAgents.Count > 0)
                {
                    builder.Append(" - Uses agent queue ids: ");
                    builder.Append(string.Join(", ", usesAgents));
                }
            }
            
        }

        return builder.ToString();
    }
}
