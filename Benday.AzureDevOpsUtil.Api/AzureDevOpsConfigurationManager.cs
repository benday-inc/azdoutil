using System.Text.Json;
using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.Api;

public class AzureDevOpsConfigurationManager
{
    public static AzureDevOpsConfigurationManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new AzureDevOpsConfigurationManager();
            }

            return _Instance;
        }
        set => _Instance = value;
    }

    public AzureDevOpsConfigurationManager()
    {

    }

    public AzureDevOpsConfigurationManager(string pathToConfigurationFile)
    {
        _PathToConfigurationFile = pathToConfigurationFile;
    }

    private string? _PathToConfigurationFile;
    private static AzureDevOpsConfigurationManager? _Instance;

    // Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    public string PathToConfigurationFile
    {
        get
        {
            if (_PathToConfigurationFile == null)
            {
                var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                _PathToConfigurationFile = Path.Combine(userProfilePath, Constants.ExeName, Constants.ConfigFileName);
            }

            return _PathToConfigurationFile;
        }

        private set => _PathToConfigurationFile = value;
    }

    public AzureDevOpsConfiguration? Get(string name = Constants.DefaultConfigurationName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
        }

        if (File.Exists(PathToConfigurationFile) == false)
        {
            return null;
        }
        else
        {
            var json = File.ReadAllText(PathToConfigurationFile);

            var configs = JsonSerializer.Deserialize<AzureDevOpsConfiguration[]>(json);

            if (configs == null || configs.Length == 0)
            {
                return null;
            }
            else
            {
                var match = configs.Where(x => x.Name == name).FirstOrDefault();

                return match;
            }
        }
    }

    public void Save(AzureDevOpsConfiguration config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }
        else if (string.IsNullOrEmpty( config.Name) ==true)
        {
            throw new ArgumentException(nameof(config), "Name value not set");
        }

        List<AzureDevOpsConfiguration> configurations;

        if (File.Exists(PathToConfigurationFile) == true)
        {
            var json = File.ReadAllText(PathToConfigurationFile);

            var configs = JsonSerializer.Deserialize<AzureDevOpsConfiguration[]>(json);

            if (configs == null || configs.Length == 0)
            {
                configurations = new List<AzureDevOpsConfiguration>();
            }
            else
            {
                configurations = configs.ToList();
            }
        }
        else
        {
            configurations = new List<AzureDevOpsConfiguration>();
        }

        var match = configurations.Where(x => x.Name == config.Name).FirstOrDefault();

        if (match != null)
        {
            configurations.Remove(match);
        }
        else
        {
            configurations.Add(config);
        }

        Save(configurations);
    }

    public void Remove(string configName)
    {
        if (string.IsNullOrEmpty(configName))
        {
            throw new ArgumentException($"'{nameof(configName)}' cannot be null or empty.", nameof(configName));
        }

        List<AzureDevOpsConfiguration> configurations;

        if (File.Exists(PathToConfigurationFile) == true)
        {
            var json = File.ReadAllText(PathToConfigurationFile);

            var configs = JsonSerializer.Deserialize<AzureDevOpsConfiguration[]>(json);

            if (configs == null || configs.Length == 0)
            {
                configurations = new List<AzureDevOpsConfiguration>();
            }
            else
            {
                configurations = configs.ToList();
            }
        }
        else
        {
            configurations = new List<AzureDevOpsConfiguration>();
        }

        var match = configurations.Where(x => x.Name == configName).FirstOrDefault();

        if (match != null)
        {
            configurations.Remove(match);
        }
        
        Save(configurations);
    }

    private void Save(List<AzureDevOpsConfiguration> configurations)
    {
        var dirName = Path.GetDirectoryName(PathToConfigurationFile);

        if (dirName == null)
        {
            throw new InvalidOperationException($"Could not establish directory.");
        }

        if (Directory.Exists(dirName)== false)
        {
            Directory.CreateDirectory(dirName);
        }

        var json = JsonSerializer.Serialize<AzureDevOpsConfiguration[]>(configurations.ToArray());

        File.WriteAllText(PathToConfigurationFile, json);
    }
}
