using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

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

    public override ArgumentCollection GetArguments()
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

        var requestUrl = $"_apis/work/processes?api-version=7.0";

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
                var parentProcessInfo = new Dictionary<string, ProcessTemplateDetailInfo>();

                foreach (var processType in result.Values)
                {
                    parentProcessInfo.TryAdd(processType.Id, processType);
                }

                foreach (var processType in result.Values)
                {
                    if (processType.CustomizationType == "inherited" &&
                        parentProcessInfo.ContainsKey(processType.ParentProcessTypeId) == true)
                    {
                        processType.ParentProcessName =
                            parentProcessInfo[processType.ParentProcessTypeId].Name;

                        processType.Parent = parentProcessInfo[processType.ParentProcessTypeId];
                    }
                }

                foreach (var item in result.Values)
                {
                    WriteLine($"Name: {item.Name}");
                    WriteLine($"Description: {item.Description}");
                    WriteLine($"Is Default: {item.IsDefault}");
                    WriteLine($"Id: {item.Id}");
                    WriteLine($"Customization Type: {item.CustomizationType}");

                    if (item.CustomizationType == "inherited")
                    {
                        WriteLine($"Reference Name: {item.ReferenceName}");
                        WriteLine($"Parent Process Id: {item.ParentProcessTypeId}");
                        WriteLine($"Parent Process Name: {item.ParentProcessName}");
                    }

                    WriteLine(string.Empty);
                }
            }
        }
    }

    public ListProcessTemplatesResponse? LastResult { get; set; }
}

