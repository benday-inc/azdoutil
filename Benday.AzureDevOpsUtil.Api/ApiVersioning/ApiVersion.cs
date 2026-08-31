namespace Benday.AzureDevOpsUtil.Api.ApiVersioning;

/// <summary>
/// An Azure DevOps REST api-version value: "7.0", "5.0-preview.1", "7.1-preview".
///
/// These are ordered rather than compared as strings, because "10.0" sorts
/// before "7.0" as text and a clamp that got that backwards would raise the
/// version instead of lowering it.  A released version outranks a preview of
/// the same number -- "7.1-preview.2" is what shipped on the way to "7.1", not
/// something newer than it.
/// </summary>
public readonly struct ApiVersion : IComparable<ApiVersion>, IEquatable<ApiVersion>
{
    /// <summary>
    /// The suffix on a preview version that names no number at all
    /// ("5.0-preview").  Real preview numbers start at 1, so zero cannot
    /// collide with one.
    /// </summary>
    public const int UnnumberedPreview = 0;

    public ApiVersion(int major, int minor, bool isPreview = false, int previewNumber = UnnumberedPreview)
    {
        Major = major;
        Minor = minor;
        IsPreview = isPreview;
        PreviewNumber = isPreview == true ? previewNumber : UnnumberedPreview;
    }

    public int Major { get; }
    public int Minor { get; }
    public bool IsPreview { get; }
    public int PreviewNumber { get; }

    public bool IsEmpty => Major == 0 && Minor == 0 && IsPreview == false;

    /// <summary>
    /// Parses an api-version value.  Returns false for anything that is not one,
    /// which is the signal to leave a request alone rather than to guess.
    /// </summary>
    public static bool TryParse(string? value, out ApiVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(value) == true)
        {
            return false;
        }

        var trimmed = value.Trim();

        var separatorIndex = trimmed.IndexOf('-');

        var numberPart = separatorIndex < 0 ? trimmed : trimmed[..separatorIndex];
        var suffixPart = separatorIndex < 0 ? string.Empty : trimmed[(separatorIndex + 1)..];

        var numberSegments = numberPart.Split('.');

        if (numberSegments.Length > 2)
        {
            return false;
        }

        if (int.TryParse(numberSegments[0], out var major) == false || major < 0)
        {
            return false;
        }

        var minor = 0;

        if (numberSegments.Length == 2 &&
            (int.TryParse(numberSegments[1], out minor) == false || minor < 0))
        {
            return false;
        }

        if (suffixPart.Length == 0)
        {
            version = new ApiVersion(major, minor);

            return true;
        }

        // the only suffix Azure DevOps uses is "preview", optionally numbered
        if (suffixPart.StartsWith("preview", StringComparison.OrdinalIgnoreCase) == false)
        {
            return false;
        }

        var previewRemainder = suffixPart["preview".Length..];

        if (previewRemainder.Length == 0)
        {
            version = new ApiVersion(major, minor, true);

            return true;
        }

        if (previewRemainder[0] != '.' ||
            int.TryParse(previewRemainder[1..], out var previewNumber) == false ||
            previewNumber < 0)
        {
            return false;
        }

        version = new ApiVersion(major, minor, true, previewNumber);

        return true;
    }

    /// <summary>
    /// This version renumbered onto <paramref name="target"/>, keeping whatever
    /// preview suffix it carried.  A command that asked for "7.1-preview.1"
    /// wants the preview form of the resource, and on an older server that is
    /// "5.0-preview.1" rather than "5.0" -- plenty of resources are preview-only
    /// and reject the released form outright.
    /// </summary>
    public ApiVersion WithNumberOf(ApiVersion target)
    {
        return new ApiVersion(target.Major, target.Minor, IsPreview, PreviewNumber);
    }

    public int CompareTo(ApiVersion other)
    {
        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        if (IsPreview != other.IsPreview)
        {
            // released outranks the previews that led up to it
            return IsPreview == true ? -1 : 1;
        }

        return PreviewNumber.CompareTo(other.PreviewNumber);
    }

    public bool Equals(ApiVersion other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is ApiVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, IsPreview, PreviewNumber);

    public override string ToString()
    {
        var number = $"{Major}.{Minor}";

        if (IsPreview == false)
        {
            return number;
        }

        return PreviewNumber == UnnumberedPreview ? $"{number}-preview" : $"{number}-preview.{PreviewNumber}";
    }

    public static bool operator <(ApiVersion left, ApiVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(ApiVersion left, ApiVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(ApiVersion left, ApiVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ApiVersion left, ApiVersion right) => left.CompareTo(right) >= 0;
    public static bool operator ==(ApiVersion left, ApiVersion right) => left.Equals(right);
    public static bool operator !=(ApiVersion left, ApiVersion right) => left.Equals(right) == false;
}
