using System.Xml.Linq;

using Benday.XmlUtilities;

public class WorkItemFieldDefinition
{
    public string Name { get; set; }
    public string RefName { get; set; }
    public string Type { get; set; }
    public string Reportable { get; set; }
    public string HelpText { get; set; }
    public List<string> AllowedValues { get; set; }
    public XElement Element { get; private set; }

    public WorkItemFieldDefinition(XElement element)
    {
        Element = element;

        Name = element.AttributeValue("name");
        RefName = element.AttributeValue("refname");
        Type = element.AttributeValue("type");
        Reportable = element.AttributeValue("reportable");
        HelpText = element.ElementValue("HELPTEXT") ?? string.Empty;

        AllowedValues = new List<string>();

        PopulateAllowedValues(element);
    }

    private void PopulateAllowedValues(XElement element)
    {
        var listItems = element.ElementByLocalName("ALLOWEDVALUES")?
            .ElementsByLocalName("LISTITEM");

        if (listItems != null)
        {
            foreach (var listItem in listItems)
            {
                AllowedValues.Add(listItem.AttributeValue("value"));
            }
        }
    }
}
