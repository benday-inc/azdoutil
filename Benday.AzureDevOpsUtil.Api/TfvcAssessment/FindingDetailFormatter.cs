using System.Text;

namespace Benday.AzureDevOpsUtil.Api.TfvcAssessment;

/// <summary>
/// Builds the <see cref="AssessmentFinding.Detail"/> string for a finding that
/// lists things -- branch paths, solution paths, build definition names.
///
/// A real repository produced a dead-branch finding listing 1,521 branch paths,
/// which came to 94,016 characters in one CSV field.  Excel holds at most
/// 32,767 characters in a cell; past that it fills the cell, loses track of the
/// quoted field, and spills the rest into following rows split on every comma,
/// which misaligns everything below it.
///
/// So the list is capped.  It is cut on a whole-item boundary -- a half a branch
/// path is worse than no branch path -- and it says how many were left out, so
/// the number in the cell can never quietly disagree with the count in the fact.
/// </summary>
public static class FindingDetailFormatter
{
    /// <summary>
    /// The separator between items in a detail list.
    /// </summary>
    public const string Separator = ", ";

    /// <summary>
    /// The maximum number of characters Excel will hold in a single cell.
    /// Stated here rather than taken from the CSV writer so that this does not
    /// depend on which version of the commands framework is restored.
    /// </summary>
    public const int ExcelMaxCellLength = 32767;

    /// <summary>
    /// The most characters a detail list is allowed to occupy.  Deliberately
    /// under Excel's per-cell maximum of 32,767, leaving room for the writer to
    /// quote and escape the value without crossing the line.
    /// </summary>
    public const int MaxDetailLength = 30000;

    /// <summary>
    /// Joins the items into a detail string of at most
    /// <see cref="MaxDetailLength"/> characters.
    /// </summary>
    /// <param name="items">The items to list.</param>
    /// <returns>
    /// The joined list, with a count of what was left out when it did not fit.
    /// </returns>
    public static string FormatList(IEnumerable<string> items)
    {
        return FormatList(items, MaxDetailLength);
    }

    /// <summary>
    /// Joins the items into a detail string of at most
    /// <paramref name="maxLength"/> characters.
    /// </summary>
    /// <param name="items">The items to list.</param>
    /// <param name="maxLength">The maximum length of the result.</param>
    /// <returns>
    /// The joined list, with a count of what was left out when it did not fit.
    /// The result is never longer than <paramref name="maxLength"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when maxLength is less than 1.
    /// </exception>
    public static string FormatList(IEnumerable<string> items, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (maxLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength), $"Value {maxLength} must be at least 1.");
        }

        var values = items.ToList();

        if (values.Count == 0)
        {
            return string.Empty;
        }

        // Reserve room for the longest suffix this list could produce, so that
        // appending it can never push the result back over the limit.
        var suffixAllowance = BuildSuffix(values.Count, values.Count).Length;
        var budget = Math.Max(0, maxLength - suffixAllowance);

        var builder = new StringBuilder();
        var shown = 0;

        foreach (var item in values)
        {
            var value = item ?? string.Empty;

            var addedLength = builder.Length == 0
                ? value.Length
                : Separator.Length + value.Length;

            // Always take the first item, even when it alone busts the budget.
            // The clamp below is what keeps the contract in that case.
            if (shown > 0 && builder.Length + addedLength > budget)
            {
                break;
            }

            if (builder.Length > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(value);
            shown++;
        }

        if (shown == values.Count && builder.Length <= maxLength)
        {
            return builder.ToString();
        }

        if (shown < values.Count)
        {
            builder.Append(BuildSuffix(values.Count - shown, values.Count));
        }

        return Clamp(builder.ToString(), maxLength);
    }

    /// <summary>
    /// Cuts a value that is already too long to fit, whatever produced it.
    /// This is the last line of defence rather than the normal path: a list is
    /// cut on an item boundary by <see cref="FormatList(IEnumerable{string}, int)"/>
    /// long before this is reached.
    /// </summary>
    /// <param name="value">The value to shorten.</param>
    /// <param name="maxLength">The maximum length of the result.</param>
    /// <returns>
    /// The value, shortened to <paramref name="maxLength"/> characters with a
    /// marker saying so.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when maxLength is less than 1.
    /// </exception>
    public static string Clamp(string? value, int maxLength)
    {
        if (maxLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength), $"Value {maxLength} must be at least 1.");
        }

        if (value == null)
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        const string Marker = " ... (truncated)";

        if (maxLength <= Marker.Length)
        {
            return value.Substring(0, maxLength);
        }

        return value.Substring(0, maxLength - Marker.Length) + Marker;
    }

    private static string BuildSuffix(int omitted, int total)
    {
        return $"{Separator}... plus {omitted:n0} more not shown ({total:n0} total)";
    }
}
