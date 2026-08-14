namespace Benday.AzureDevOpsUtil.Api.TfCommandLine;

/// <summary>
/// One folder mapping in a TFVC workspace.
/// </summary>
public class TfWorkspaceMapping
{
    public string ServerPath { get; set; } = string.Empty;

    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// Cloaked mappings exclude a folder from the workspace rather than
    /// bringing it in, so they never resolve a local directory.
    /// </summary>
    public bool IsCloaked { get; set; }
}

public class TfWorkfoldResult
{
    public string WorkspaceName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// The collection url, with a trailing separator so it compares directly
    /// against a stored configuration.
    /// </summary>
    public string CollectionUrl { get; set; } = string.Empty;

    public List<TfWorkspaceMapping> Mappings { get; set; } = new();
}

/// <summary>
/// Reads the output of "tf workfold".
///
/// The output looks like this, and lists every mapping in the workspace rather
/// than only the one holding the current directory:
///
///     ===========================================================
///     Workspace : dev-vm-20260325 (Ben Day)
///     Collection: https://dev.azure.com/benday
///      $/TfvcBuildCodeAnalysis: C:\code\tfvc\TfvcBuildCodeAnalysis
///      $/OtherProject: C:\code\azdo\OtherProject
/// </summary>
public static class TfWorkfoldParser
{
    public static TfWorkfoldResult? Parse(string? output)
    {
        if (string.IsNullOrWhiteSpace(output) == true)
        {
            return null;
        }

        var result = new TfWorkfoldResult();

        using var reader = new StringReader(output);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("=", StringComparison.Ordinal) == true)
            {
                continue;
            }

            if (TryReadLabelled(trimmed, "Workspace", out var workspace) == true)
            {
                ReadWorkspaceAndOwner(workspace, result);
                continue;
            }

            if (TryReadLabelled(trimmed, "Collection", out var collection) == true)
            {
                result.CollectionUrl = NormalizeCollectionUrl(collection);
                continue;
            }

            var mapping = ReadMapping(trimmed);

            if (mapping != null)
            {
                result.Mappings.Add(mapping);
            }
        }

        if (result.Mappings.Count == 0 && result.CollectionUrl.Length == 0)
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Reads a "Label: value" line.  The space before the colon varies between
    /// labels in the real output, so the label is matched and then the first
    /// colon is found.
    /// </summary>
    private static bool TryReadLabelled(string line, string label, out string value)
    {
        value = string.Empty;

        if (line.StartsWith(label, StringComparison.OrdinalIgnoreCase) == false)
        {
            return false;
        }

        var colonIndex = line.IndexOf(':');

        if (colonIndex < 0)
        {
            return false;
        }

        value = line.Substring(colonIndex + 1).Trim();

        return true;
    }

    private static void ReadWorkspaceAndOwner(string value, TfWorkfoldResult result)
    {
        // "dev-vm-20260325 (Ben Day)"
        var openIndex = value.LastIndexOf('(');
        var closeIndex = value.LastIndexOf(')');

        if (openIndex > 0 && closeIndex > openIndex)
        {
            result.WorkspaceName = value.Substring(0, openIndex).Trim();
            result.OwnerName = value.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();

            return;
        }

        result.WorkspaceName = value;
    }

    /// <summary>
    /// Reads a mapping line such as " $/Project/Main: C:\code\Main".
    ///
    /// The local path contains a colon of its own, so the split is on the first
    /// one.  A TFVC server path cannot contain a colon, which is what makes
    /// that safe.
    /// </summary>
    private static TfWorkspaceMapping? ReadMapping(string line)
    {
        var value = line;

        var isCloaked = false;

        if (value.StartsWith("(cloaked)", StringComparison.OrdinalIgnoreCase) == true)
        {
            isCloaked = true;
            value = value.Substring("(cloaked)".Length).Trim();
        }

        if (value.StartsWith("$/", StringComparison.Ordinal) == false)
        {
            return null;
        }

        var colonIndex = value.IndexOf(':');

        if (colonIndex < 0)
        {
            // A cloaked entry can appear without a local path.
            return isCloaked == true ?
                new TfWorkspaceMapping { ServerPath = value.Trim(), IsCloaked = true } :
                null;
        }

        var serverPath = value.Substring(0, colonIndex).Trim();
        var localPath = value.Substring(colonIndex + 1).Trim();

        if (serverPath.Length == 0)
        {
            return null;
        }

        return new TfWorkspaceMapping
        {
            ServerPath = serverPath,
            LocalPath = localPath,
            IsCloaked = isCloaked
        };
    }

    private static string NormalizeCollectionUrl(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        return value.EndsWith("/", StringComparison.Ordinal) == true ? value : value + "/";
    }
}
