using System.Xml.Linq;
public static class XmlUtility
{
    public static string GetAttributeValue(XElement fromElement, string attributeName)
    {
        if (fromElement == null || fromElement.HasAttributes == false || fromElement.Attribute(attributeName) == null)
        {
            return string.Empty;
        }
        else
        {
            return fromElement.Attribute(attributeName)?.Value ?? string.Empty;
        }
    }

    public static void EmbedInXmlComment(XElement elementToCommentOut)
    {
        if (elementToCommentOut == null)
        {
            throw new ArgumentNullException(nameof(elementToCommentOut), "elementToCommentOut is null.");
        }

        var parent = elementToCommentOut.Parent;

        if (parent == null)
        {
            throw new InvalidOperationException("Cannot comment out the root.");
        }

        var comment = new XComment(elementToCommentOut.ToString());

        parent.Add(comment);
        elementToCommentOut.Remove();
    }
}
