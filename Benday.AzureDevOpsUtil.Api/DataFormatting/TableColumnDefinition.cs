namespace Benday.AzureDevOpsUtil.Api.DataFormatting;

public class TableColumnDefinition
{
    public required string Name { get; set; }

    public string NamePadded
    {
        get
        {
            return Name.PadRight(Width);
        }
    }

    public int WidthOfLongestValue { get; set; }

    public int Width
    {
        get
        {
            return Math.Max(Name.Length, WidthOfLongestValue);
        }
    }

    public bool IsColumnNameLongerThanLongestValue
    {
        get
        {
            return Name.Length > WidthOfLongestValue;
        }
    }

    /// <summary>
    /// Check the length of the value to see if it is longer than the current longest value
    /// </summary>
    /// <param name="newValue"></param>
    public void CheckValueLength(string newValue)
    {
        if (newValue == null)
        {
            return;
        }

        if (WidthOfLongestValue < newValue.Length)
        {
            WidthOfLongestValue = newValue.Length;
        }
    }

}