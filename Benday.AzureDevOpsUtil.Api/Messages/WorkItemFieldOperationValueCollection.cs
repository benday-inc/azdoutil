namespace Benday.AzureDevOpsUtil.Api.Messages;

public class WorkItemFieldOperationValueCollection
{
    private readonly List<WorkItemFieldOperationValue> _values = new();
    private readonly Dictionary<string, WorkItemFieldOperationValue> _valuesByRefname = new();

    public List<WorkItemFieldOperationValue> Values => _values;

    public int Count => _values.Count;

    public void AddValue(WorkItemFieldInfo field, string value)
    {
        var temp = new WorkItemFieldOperationValue()
        {
            Operation = "add",
            Path = $"/fields/{field.ReferenceName}",
            Value = value,
            Refname = field.ReferenceName
        };

        CheckForDuplicateAndAddValue(temp);
    }

    private void CheckForDuplicateAndAddValue(WorkItemFieldOperationValue temp)
    {
        if (temp == null)
        {
            throw new ArgumentNullException(nameof(temp), "Argument cannot be null.");
        }

        // check for duplicate...remove if exists
        if (string.IsNullOrEmpty(temp.Refname) == false)
        {
            // always store by refname in lowercase
            // to make sure that case sensitivity doesn't matter
            var key = temp.Refname.ToLower();

            if (_valuesByRefname.ContainsKey(key) == true)
            {
                var itemToRemove = _valuesByRefname[key];

                _values.Remove(itemToRemove);
                _valuesByRefname.Remove(key);
            }

            _values.Add(temp);
            _valuesByRefname.Add(key, temp);
        }
        else
        {
            _values.Add(temp);
        }
    }

    public void AddValue(string referenceName, string value)
    {
        var temp = new WorkItemFieldOperationValue()
        {
            Operation = "add",
            Path = $"/fields/{referenceName}",
            Value = value,
            Refname = referenceName
        };

        CheckForDuplicateAndAddValue(temp);
    }

    public void AddValue(WorkItemFieldInfo field, string nowTicks, int index)
    {
        var value = $"{field.ReferenceName} {nowTicks} - #{index}";

        AddValue(field, value);
    }

    public void AddHtmlValue(WorkItemFieldInfo field, string nowTicks, int index)
    {
        var value = $"<div>{field.ReferenceName} {nowTicks} - #{index}</div>";

        AddValue(field, value);
    }

    public void AddValue(WorkItemFieldInfo field, DateTime value)
    {
        var temp = new WorkItemFieldOperationValue()
        {
            Operation = "add",
            Path = $"/fields/{field.ReferenceName}",
            Value = value.ToString("u").Replace(" ", "T"),
            Refname = field.ReferenceName
        };

        CheckForDuplicateAndAddValue(temp);
    }

    public void AddValue(WorkItemFieldOperationValue val)
    {
        if (val == null)
        {
            throw new ArgumentNullException(nameof(val), "Argument cannot be null.");
        }

        CheckForDuplicateAndAddValue(val);
    }
}
