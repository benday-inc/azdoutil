using System;
using System.Linq;

namespace Benday.AzureDevOpsUtil.Api.BuildUpgraders;

public class TfvcSourceControlMapping
{
    public TfvcSourceControlMapping()
    {

    }

    public TfvcSourceControlMapping(string fromServerPath)
    {
        ServerPath = fromServerPath;

        IsCloaked = true;

        LocalPath = "\\";
    }

    public bool IsCloaked { get; set; }

    public TfvcSourceControlMapping(string fromServerPath, string toLocalPath)
    {
        if (string.IsNullOrEmpty(fromServerPath))
        {
            throw new ArgumentException($"{nameof(fromServerPath)} is null or empty.", nameof(fromServerPath));
        }

        if (string.IsNullOrEmpty(toLocalPath))
        {
            throw new ArgumentException($"{nameof(toLocalPath)} is null or empty.", nameof(toLocalPath));
        }

        LocalPath = CleanupPath(toLocalPath);
        ServerPath = fromServerPath;
        IsCloaked = false;
    }

    public string LocalPath { get; set; } = string.Empty;
    public string ServerPath { get; set; }

    private string CleanupPath(string localPath)
    {
        if (localPath == "$(SourceDir)\\" || localPath == "$(SourceDir)")
        {
            return "\\";
        }
        else
        {
            var leadingSourceDir = "$(SourceDir)\\";

            if (localPath.StartsWith(leadingSourceDir,
                StringComparison.CurrentCultureIgnoreCase) == true)
            {
                return localPath.Substring(leadingSourceDir.Length);
            }
            else
            {
                return localPath;
            }
        }
    }
}