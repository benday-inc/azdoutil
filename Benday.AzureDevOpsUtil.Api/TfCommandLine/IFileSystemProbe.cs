namespace Benday.AzureDevOpsUtil.Api.TfCommandLine;

/// <summary>
/// The filesystem and environment questions the locator asks.  Behind an
/// interface so the search can be exercised against a made-up machine.
/// </summary>
public interface IFileSystemProbe
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    /// <summary>
    /// Immediate subdirectories, or an empty list when the directory cannot be
    /// read.  Never throws: a machine will always have folders this process
    /// cannot open.
    /// </summary>
    IReadOnlyList<string> GetDirectories(string path);

    string? GetEnvironmentVariable(string name);
}

public class FileSystemProbe : IFileSystemProbe
{
    public bool FileExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (IOException)
        {
            return false;
        }
    }

    public bool DirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch (IOException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> GetDirectories(string path)
    {
        try
        {
            return Directory.GetDirectories(path);
        }
        catch (Exception)
        {
            // Unreadable or missing folders are simply places tf is not.
            return Array.Empty<string>();
        }
    }

    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
