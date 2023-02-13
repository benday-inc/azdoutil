using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.AzureDevOpsUtil.Api;
[Command(Name = Constants.CommandName_ListProcessTemplates,
        Description = "List process templates",
        IsAsync = true)]
public class ListProcessTemplatesCommand : AzureDevOpsCommandBase
{
    public ListProcessTemplatesCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);       

        return arguments;
    }

    protected override async Task OnExecute()
    {
        await GetProcessTypes();
    }

    private async Task GetProcessTypes()
    {
        // GET https://dev.azure.com/{organization}/_apis/process/processes?api-version=7.0

        var requestUrl = $"_apis/process/processes?api-version=7.0";

        var result = await CallEndpointViaGetAndGetResult<ListProcessTemplatesResponse>(requestUrl);

        LastResult = result;

        if (IsQuietMode == false)
        {
            if (result == null || result.Count == 0)
            {
                WriteLine($"No process templates found.");
            }
            else
            {
                foreach (var item in result.Values)
                {
                    WriteLine($"Name: {item.Name}");
                    WriteLine($"Description: {item.Description}");
                    WriteLine($"IsDefault: {item.IsDefault}");
                    WriteLine($"Id: {item.Id}");
                    WriteLine($"Url: {item.Url}");
                    WriteLine(string.Empty);
                }
            }
        }
    }   

    public ListProcessTemplatesResponse? LastResult { get; set; }
}

