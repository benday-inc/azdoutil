using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Benday.AzureDevOpsUtil.Api.McpTools;

/// <summary>
/// MCP tools for interrogating azdoutil's stored Azure DevOps connections.
/// Stored access tokens are never returned.
/// </summary>
[McpServerToolType]
public class ConfigurationTools
{
    [McpServerTool(Name = "list_configurations")]
    [Description(
        "List the Azure DevOps connections (configurations) that azdoutil is set up with, so " +
        "you know which organizations or collections you can report on and what name to pass " +
        "as configName. Use this when someone asks 'what are you connected to?' or 'which " +
        "projects can you see?', or when another tool reports that a configuration is missing. " +
        "Stored access tokens are never returned.")]
    public ConfigurationListResult ListConfigurations()
    {
        var configs = AzureDevOpsConfigurationManager.Instance.GetAll();

        var result = new ConfigurationListResult();

        foreach (var config in configs)
        {
            result.Configurations.Add(new ConfigurationSummary
            {
                Name = config.Name,
                CollectionUrl = config.CollectionUrl,
                AccountOrCollectionName = config.AccountNameOrCollectionName,
                AuthMethod = config.IsWindowsAuth ? "Windows authentication" : "Personal access token",
                IsDefault = config.Name == Constants.DefaultConfigurationName
            });
        }

        result.Count = result.Configurations.Count;

        result.Message = result.Count == 0
            ? "azdoutil has no configurations yet, so there is nothing to report on. Ask the user " +
              "to add one by running this in a terminal: " +
              "azdoutil addconfig --url <your Azure DevOps URL> --pat <personal access token> " +
              "(add --config <name> for a named configuration), then restart the MCP server."
            : $"{result.Count} configuration(s) available. Pass a configuration's name as the " +
              "configName parameter to any tool, or set the AZDO_CONFIG_NAME environment variable, " +
              "to choose which connection to use.";

        return result;
    }
}

public class ConfigurationSummary
{
    public string Name { get; set; } = string.Empty;
    public string CollectionUrl { get; set; } = string.Empty;
    public string AccountOrCollectionName { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class ConfigurationListResult
{
    public int Count { get; set; }
    public List<ConfigurationSummary> Configurations { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
