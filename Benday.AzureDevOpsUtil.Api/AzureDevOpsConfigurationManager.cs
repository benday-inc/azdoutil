namespace Benday.AzureDevOpsUtil.Api;

public class AzureDevOpsConfigurationManager
{
    public AzureDevOpsConfigurationManager()
    {

    }

    public AzureDevOpsConfigurationManager(string pathToConfigurationFile)
    {
        _PathToConfigurationFile = pathToConfigurationFile;
    }

    private string? _PathToConfigurationFile;

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
}
