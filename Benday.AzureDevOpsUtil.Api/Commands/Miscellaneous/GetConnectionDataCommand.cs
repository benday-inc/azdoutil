using System.Text.Json;
using System.Web;

using Benday.AzureDevOpsUtil.Api.ApiVersioning;
using Benday.AzureDevOpsUtil.Api.Messages;
using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Miscellaneous;

[Command(
    Category = Constants.Category_Misc,
    Name = Constants.CommandName_ConnectionData,
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

    protected override async Task OnExecute(CancellationToken cancellationToken)
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

            await PrintServerVersion(cancellationToken);

            await PrintApiVersionSupport(result, cancellationToken);
        }
    }

    /// <summary>
    /// The server's build, when it will say.  There is no endpoint for this, so
    /// it comes off the About page and may simply not be there.
    /// </summary>
    private async Task PrintServerVersion(CancellationToken cancellationToken)
    {
        var version = await GetServerVersion(cancellationToken);

        if (version.IsEmpty == true)
        {
            WriteLine("Server Version", "not reported by this server");

            return;
        }

        if (version.ServiceVersion.Length > 0)
        {
            WriteLine("Server Version", version.ServiceVersion);
        }

        if (version.BuildNumber.Length > 0)
        {
            WriteLine("Server Build", version.BuildNumber);

            var release = AzureDevOpsProductVersion.DescribeBuild(version.BuildNumber);

            if (release.Length > 0)
            {
                WriteLine("Release", release);
            }
        }
    }

    /// <summary>
    /// What this collection will actually accept as an api-version.
    ///
    /// Worth printing next to the connection details because it is the thing
    /// that decides whether a command works against an older server, and there
    /// is no other way to see it -- the response carries no product version of
    /// its own.
    /// </summary>
    private async Task PrintApiVersionSupport(
        ConnectionDataResponse result, CancellationToken cancellationToken)
    {
        var info = await GetServerApiVersionInfo(cancellationToken);

        var catalog = info?.Catalog;

        if (catalog == null)
        {
            WriteLine("Max REST api-version", "could not be determined");

            return;
        }

        var isHosted = result.DeploymentType.Contains(
            "hosted", StringComparison.OrdinalIgnoreCase);

        WriteLine("Max REST api-version (released)", catalog.MaxReleasedVersion.ToString());
        WriteLine("Max REST api-version (incl. preview)", catalog.MaxVersion.ToString());
        WriteLine("Product (inferred)",
            AzureDevOpsProductVersion.Describe(catalog.MaxReleasedVersion, isHosted));
        WriteLine("API resource count", catalog.Locations.Count.ToString());
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
