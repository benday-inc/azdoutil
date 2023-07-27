using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api;

[Command(
    Category = Constants.Category_Misc,
    Name = Constants.CommandName_ConnectionData,
    IsAsync = true,
    Description = "Get information about a connection to Azure DevOps.")]
public class GetConnectionDataCommand : AzureDevOpsCommandBase
{
    public ConnectionDataResponse? LastResult { get; private set; }

    public GetConnectionDataCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        AddCommonArguments(args);

        return args;
    }

    protected override async Task OnExecute()
    {
        var result = await GetConnectionData();

        LastResult = result;

        if (IsQuietMode)
        {
            return;
        }
        else if (result == null)
        {
            WriteLine("Result is null");
        }
        else
        {
            Print(result);
        }
    }

    public async Task<ConnectionDataResponse?> GetConnectionData()
    {
        using var client = GetHttpClientInstanceForAzureDevOps();

        var results = await client.GetAsync($"_apis/ConnectionData");

        if (results.IsSuccessStatusCode == false)
        {
            throw new InvalidOperationException($"Request failed -- {results.StatusCode} {results.ReasonPhrase}");
        }

        var content = await results.Content.ReadAsStringAsync();

        var objectResults = JsonSerializer.Deserialize<ConnectionDataResponse>(content);

        if (objectResults == null)
        {
            return null;
        }

        return objectResults;
    }

    private void WriteLine(string name, string value)
    {
        WriteLine($"{name}: {value}");
    }

    private void Print(ConnectionDataResponse result)
    {
        WriteLine("AuthenticatedUser Id", result.AuthenticatedUser.Id);
        WriteLine("AuthenticatedUser Display Name", result.AuthenticatedUser.ProviderDisplayName);
        WriteLine("AuthenticatedUser Account Name", result.AuthenticatedUser.Properties.Account.Value);

        WriteLine("AuthorizedUser Id", result.AuthorizedUser.Id);
        WriteLine("AuthorizedUser Display Name", result.AuthorizedUser.ProviderDisplayName);
        WriteLine("AuthorizedUser Account Name", result.AuthorizedUser.Properties.Account.Value);

        WriteLine("Deployment Id", result.DeploymentId);
        WriteLine("Deployment Type", result.DeploymentType);
        WriteLine("InstanceId", result.InstanceId);
        WriteLine("WebApplicationRelativeDirectory", result.WebApplicationRelativeDirectory);
    }
}
