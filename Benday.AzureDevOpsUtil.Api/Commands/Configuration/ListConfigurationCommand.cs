using Benday.CommandsFramework;

namespace Benday.AzureDevOpsUtil.Api.Commands.Configuration;

[Command(
    Category = Constants.Category_AzdoUtilConfig,
    Name = Constants.CommandArgumentNameListConfig,
        Description = "List an Azure DevOps configuration. For example, which server or account plus auth information.")]
public class ListConfigurationCommand : Command
{
    public ListConfigurationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddString(Constants.ArgumentNameConfigurationName)
            .WithDescription("Name of the configuration")
            .AsNotRequired();

        return arguments;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        if (Arguments[Constants.ArgumentNameConfigurationName].HasValue)
        {
            var configName = Arguments[Constants.ArgumentNameConfigurationName].Value;

            var config = AzureDevOpsConfigurationManager.Instance.Get(configName);

            Print(config, configName);
        }
        else
        {
            var configs = AzureDevOpsConfigurationManager.Instance.GetAll();

            Print(configs);
        }

        return Task.CompletedTask;
    }

    private void Print(AzureDevOpsConfiguration[] configs)
    {
        if (configs.Length == 0)
        {
            WriteLine("No configurations");
        }
        else
        {
            foreach (var config in configs)
            {
                Print(config, config.Name);
            }
        }
    }

    private void Print(AzureDevOpsConfiguration? config, string configName)
    {
        if (config == null)
        {
            WriteLine($"Configuration name '{configName}' not found");
        }
        else
        {
            WriteLine("***");
            WriteLine($"Name: {config.Name}");
            WriteLine($"Collection Url: {config.CollectionUrl}");
            WriteLine($"Account Name / TPC Name: {config.AccountNameOrCollectionName}");
            WriteLine($"Token: {config.Token}");
            WriteLine($"Use Windows Auth: {config.IsWindowsAuth}");

            if (string.IsNullOrWhiteSpace(config.MaxApiVersion) == false)
            {
                WriteLine($"Max API Version: {config.MaxApiVersion}");
            }
        }
    }
}
