using System.Text.Json;

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
}
