using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

using OfficeOpenXml.Utils;

namespace Benday.AzureDevOpsUtil.Api;

[Command(Name = Constants.CommandName_ExportWorkItemQuery,
        Description = "Export work item query results",
        IsAsync = true)]
public class ExportWorkItemQueryCommand : AzureDevOpsCommandBase
{
    private const int BATCH_SIZE = 200;

    public ExportWorkItemQueryCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    protected override ArgumentCollection GetAvailableArguments()
    {
        var arguments = new ArgumentCollection();

        AddCommonArguments(arguments);
        arguments.AddString(Constants.ArgumentNameTeamProjectName)
            .AsRequired()
            .WithDescription("Team project name to delete");

        arguments.AddString(Constants.ArgumentNameWorkItemQueryName)
          .AsRequired()
          .WithDescription("Work item query name");

        arguments.AddString(Constants.ArgumentNameExportToPath)
          .AsRequired()
          .WithDescription("Export to path");

        return arguments;
    }

    private string? _teamProjectName;
    private string? _workItemQueryName;
    private string? _exportToPath;

    protected override async Task OnExecute()
    {
        _teamProjectName = Arguments.GetStringValue(Constants.ArgumentNameTeamProjectName);
        _workItemQueryName = Arguments.GetStringValue(Constants.ArgumentNameWorkItemQueryName);
        _exportToPath = Arguments.GetStringValue(Constants.ArgumentNameExportToPath);

        var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandName_RunWorkItemQuery, true);

        var itemsToExportInfo = new RunWorkItemQueryCommand(args, _OutputProvider);

        await itemsToExportInfo.ExecuteAsync();

        if (itemsToExportInfo.WorkItemQueryInfo == null)
        {
            throw new InvalidOperationException($"No query info results.");
        }

        if (itemsToExportInfo.LastResult == null)
        {
            throw new InvalidOperationException($"No query results.");
        }

        await RunWorkItemQuery(itemsToExportInfo.WorkItemQueryInfo, itemsToExportInfo.LastResult);
    }

    private async Task RunWorkItemQuery(WorkItemQueryInfo queryInfo, WorkItemQueryExecutionResult exportThese)
    {
        if (queryInfo == null)
        {
            throw new ArgumentNullException(nameof(queryInfo), "Argument cannot be null.");
        }

        if (exportThese == null)
        {
            throw new ArgumentNullException(nameof(exportThese), "Argument cannot be null.");
        }

        WriteLine($"Number of work items to export: {exportThese.WorkItems.Length}");

        var batches = GetWorkItemIdBatches(exportThese);

        WriteLine($"Batch count: {batches.Count}");

        var count = 0;

        foreach (var batch in batches)
        {
            WriteLine($"Batch #{count} size: {batch.Count}");

            var response = await DownloadBatch(queryInfo, batch);

            AddWorkItemsToExportCollection(response);

            count++;
        }

        ExportDownloadedWorkItems();
    }

    private void ExportDownloadedWorkItems()
    {
        if (_exportToPath == null)
        {
            throw new InvalidOperationException($"Export path is null");
        }

        WriteLine($"Number of work items to export: {ExportThese.Count}");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(ExportThese, options);

        if (Directory.Exists(_exportToPath) == false)
        {
            Directory.CreateDirectory(_exportToPath);
        }

        var now = DateTime.Now.ToString("yyyyMMddHHmmss");

        var exportToFile = Path.Combine(_exportToPath, $"{_workItemQueryName}-{now}.json");

        WriteLine($"Exporting to {exportToFile}...");

        File.WriteAllText(exportToFile, json);

        WriteLine($"Exported.");
    }

    private List<GetWorkItemByIdResponse>? _exportThese;
    private List<GetWorkItemByIdResponse> ExportThese
    {
        get
        {
            if (_exportThese == null)
            {
                _exportThese = new List<GetWorkItemByIdResponse>();
            }

            return _exportThese;
        }
    }

    private void AddWorkItemsToExportCollection(WorkItemDetailResponse response)
    {
        foreach (var item in response.Value)
        {
            ExportThese.Add(item);
        }
    }

    public async Task<WorkItemDetailResponse> DownloadBatch(WorkItemQueryInfo queryInfo, List<long> batch)
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var requestUrl = $"_apis/wit/workitemsbatch?api-version=6.0";

        var body = new WorkItemDetailRequest();

        foreach (var col in queryInfo.Columns)
        {
            body.Fields.Add(col.ReferenceName);
        }

        foreach (var workItemId in batch)
        {
            body.Ids.Add(workItemId);
        }

        var requestAsJson = JsonSerializer.Serialize(body);

        var requestContent = new StringContent(
            requestAsJson,
            Encoding.UTF8, "application/json");

        var result = await client.PostAsync(requestUrl, requestContent);

        if (result.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Problem with server call to {requestUrl}. {result.StatusCode} {result.ReasonPhrase}");
        }

        var responseContent = await result.Content.ReadAsStringAsync();

        return JsonUtilities.GetJsonValueAsType<WorkItemDetailResponse>(responseContent);
    }


    private static List<List<long>> GetWorkItemIdBatches(WorkItemQueryExecutionResult exportThese)
    {
        var returnValue = new List<List<long>>();

        List<long> batch = new();

        foreach (var item in exportThese.WorkItems)
        {
            if (batch == null)
            {
                batch = new List<long>();
            }

            if (batch.Count == BATCH_SIZE)
            {
                returnValue.Add(batch);
                batch = new List<long>();
            }

            batch.Add(item.Id);
        }

        if (batch.Count > 0 && returnValue.Contains(batch) == false)
        {
            returnValue.Add(batch);
        }

        return returnValue;
    }

    public WorkItemDetailResponse? LastResult
    {
        get;
        set;
    }
}
