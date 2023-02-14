using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.Api
{
    public class WorkItemTypeDefinition
    {
        private const string ELEMENT_NAME_FIELD = "FIELD";
        public WorkItemTypeDefinition(string path)
        {
            LoadFromFile(path);

            if (_document is null)
            {
                throw new InvalidOperationException($"_document is null at end of ctor");
            }
        }

        public WorkItemTypeDefinition(XDocument doc)
        {
            Initialize(doc);
            if (_document is null)
            {
                throw new InvalidOperationException($"_document is null at end of ctor");
            }
        }

        private void Initialize(XDocument doc)
        {
            _document = doc ?? throw new ArgumentNullException(nameof(doc), "doc is null.");
        }

        private void LoadFromFile(string path)
        {
            var contents = File.ReadAllText(path);

            Initialize(XDocument.Parse(contents));
        }

        private XDocument _document;

        public XElement Element => _document.Root;

        private string _workItemType;

        public string WorkItemType
        {
            get
            {
                if (_workItemType == null)
                {
                    var match = Element.Element("WORKITEMTYPE");

                    if (match == null)
                    {
                        throw new InvalidOperationException("Could not locate WORKITEMTYPE element.");
                    }

                    _workItemType = XmlUtility.GetAttributeValue(match, "name");
                }

                return _workItemType;
            }
            set
            {
                var match = Element.Element("WORKITEMTYPE");

                if (match == null)
                {
                    throw new InvalidOperationException("Could not locate WORKITEMTYPE element.");
                }

                match.SetAttributeValue("name", value);
            }
        }

        public bool HasForAndNotAttributes()
        {
            var matches = from temp in Element.DescendantsAndSelf()
                          where temp.HasAttributes == true &&
                              (temp.Attributes("for").Any() == true ||
                              temp.Attributes("not").Any() == true)
                          select temp;

            return matches.Any();
        }

        public void RemoveForAndNotAttributes()
        {
            var matches = from temp in Element.DescendantsAndSelf()
                          where temp.HasAttributes == true &&
                              (temp.Attributes("for").Any() == true ||
                              temp.Attributes("not").Any() == true)
                          select temp;

            foreach (var item in matches)
            {
                item.Attribute("not")?.Remove();
                item.Attribute("for")?.Remove();
            }
        }

        public XElement GetFieldByRefname(string refname)
        {
            if (string.IsNullOrEmpty(refname))
                throw new ArgumentException("refname is null or empty.", nameof(refname));

            var fields = GetFields();

            var match = (from temp in fields
                         where XmlUtility.GetAttributeValue(temp, "refname") == refname
                         select temp).FirstOrDefault();

            return match;
        }

        public XElement GetWebLayout()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var match = _document.Root.Element("WORKITEMTYPE").Element("FORM").Element("WebLayout");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return match;
        }

        public bool HasFieldInWebLayout(string refname)
        {
            var webLayout = GetWebLayout();

            if (webLayout == null)
            {
                return false;
            }

            var match = webLayout.Descendants("Control").Where(x =>
                x.HasAttributes == true &&
                x.Attribute("FieldName") != null &&
                x.Attribute("FieldName").Value == refname
            ).FirstOrDefault();

            return (match != null);
        }

        public bool HasField(string refname)
        {
            return (GetFieldByRefname(refname) != null);
        }

        public bool HasPageInWebLayout(string pageName)
        {
            var webLayout = GetWebLayout();

            if (webLayout == null)
            {
                return false;
            }

            var match = webLayout.Elements("Page").Where(x =>
                x.HasAttributes == true &&
                x.Attribute("Label") != null &&
                x.Attribute("Label").Value == pageName
            ).FirstOrDefault();

            return (match != null);
        }

        public List<XElement> GetFields()
        {
            /*
             *<?xml version="1.0" encoding="utf-8"?>
<WITD application="Work item type editor" version="1.0">
  <WORKITEMTYPE name="Bug">
    <DESCRIPTION>Includes information about a Bug.</DESCRIPTION>
    <FIELDS> 
             */

            var matches = _document.Root.Element("WORKITEMTYPE").Element("FIELDS").Elements(ELEMENT_NAME_FIELD).ToList();

            return matches.ToList();
        }

        public XElement GetFieldsElement()
        {
            /*
             *<?xml version="1.0" encoding="utf-8"?>
<WITD application="Work item type editor" version="1.0">
  <WORKITEMTYPE name="Bug">
    <DESCRIPTION>Includes information about a Bug.</DESCRIPTION>
    <FIELDS> 
             */

            var match = _document.Root.Element("WORKITEMTYPE").Element("FIELDS");

            return match;
        }

        public string GetInitialState()
        {
            var transitions = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("TRANSITIONS");

            var match = (from temp in transitions.Elements("TRANSITION")
                         where
                         XmlUtility.GetAttributeValue(temp, "from") == string.Empty
                         select temp).FirstOrDefault();

            if (match == null)
            {
                return null;
            }
            else
            {
                return XmlUtility.GetAttributeValue(match, "to");
            }
        }

        public List<string> GetStates()
        {
            var matches = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Elements("STATES").Elements("STATE");

            var returnValue = new List<string>();

            foreach (var item in matches)
            {
                returnValue.Add(XmlUtility.GetAttributeValue(item, "value"));
            }

            return returnValue;
        }

        public List<XElement> GetStatesWithReadOnlyFlags()
        {
            var allStates = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Elements("STATES").Elements("STATE");

            var matches = (from temp in allStates
                           where temp.Descendants("READONLY").FirstOrDefault() != null
                           select temp);

            return matches.ToList();
        }

        public void RemoveReadOnlyFlagFromStates()
        {
            var statesWithReadOnlys = GetStatesWithReadOnlyFlags();

            foreach (var stateWithReadOnly in statesWithReadOnlys)
            {
                RemoveReadOnlyFromState(stateWithReadOnly);
            }
        }

        public WorkItemStateTransitionCollection GetTransitions()
        {
            var transitions = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("TRANSITIONS").Elements("TRANSITION");

            string from;
            string to;
            var returnValue = new WorkItemStateTransitionCollection();

            foreach (var item in transitions)
            {
                from = XmlUtility.GetAttributeValue(item, "from");
                to = XmlUtility.GetAttributeValue(item, "to");

                returnValue.Add(new WorkItemStateTransition(from, to));
            }

            return returnValue;
        }

        public void Save(string outputPath)
        {
            _document.Save(outputPath);
        }

        public void RelaxRestrictionsOnField(string refname)
        {
            /*
             *   <FIELD name="Closed Date" refname="Microsoft.VSTS.Common.ClosedDate" type="DateTime" reportable="dimension">
        <WHENNOTCHANGED field="System.State">
          <READONLY />
        </WHENNOTCHANGED>
      </FIELD>
             */
            if (string.IsNullOrEmpty(refname))
                throw new ArgumentException("refname is null or empty.", nameof(refname));

            var fieldElement = GetFieldByRefname(refname);

            if (fieldElement == null)
            {
                return;
            }
            else
            {
                var whenNotChangedElement = fieldElement.Element("WHENNOTCHANGED");

                if (whenNotChangedElement != null)
                {
                    whenNotChangedElement.Remove();
                }
            }
        }

        public void ConvertFieldFromAllowedValuesToSuggestedValues(string refname)
        {
            if (string.IsNullOrEmpty(refname))
                throw new ArgumentException("refname is null or empty.", nameof(refname));

            var fieldElement = GetFieldByRefname(refname);

            if (fieldElement == null)
            {
                return;
            }
            else
            {
                var allowedValuesElement = fieldElement.Element("ALLOWEDVALUES");

                if (allowedValuesElement != null)
                {
                    var xml = allowedValuesElement.ToString();

                    xml = xml.Replace("ALLOWEDVALUES", "SUGGESTEDVALUES");

                    var suggestedValuesElement = XElement.Parse(xml);

                    allowedValuesElement.Remove();

                    fieldElement.Add(suggestedValuesElement);
                }

                var requiredElement = fieldElement.Element("REQUIRED");

                if (requiredElement != null)
                {
                    requiredElement.Remove();
                }
            }
        }

        private void RemoveReadOnlyFromState(XElement stateWithReadOnly)
        {
            var fieldsElement = stateWithReadOnly.Element("FIELDS");

            if (fieldsElement == null)
            {
                // no fields element then there aren't any readonly's to deal with
                return;
            }
            else
            {
                var readOnlyElements = stateWithReadOnly.Descendants("READONLY");

                if (readOnlyElements == null || !readOnlyElements.Any())
                {
                    return;
                }
                else
                {
                    var commentTheseOut = new List<XElement>();

                    foreach (var readOnlyElement in readOnlyElements)
                    {
                        if (readOnlyElement.Parent != null &&
                            readOnlyElement.Parent.Name.LocalName == "FIELD")
                        {
                            commentTheseOut.Add(readOnlyElement.Parent);
                        }
                    }

                    if (commentTheseOut.Count == fieldsElement.Elements().Count())
                    {
                        // comment out the fields elements
                        XmlUtility.EmbedInXmlComment(fieldsElement);
                    }
                    else
                    {
                        foreach (var commentThisOut in commentTheseOut)
                        {
                            XmlUtility.EmbedInXmlComment(commentThisOut);
                        }
                    }
                }
            }
        }

        public void CreateAllToAllStateTransitions()
        {
            var states = GetStates();
            var initialState = GetInitialState();

            var allToAllTransitions = new WorkItemStateTransitionCollection
            {
                { string.Empty, initialState }
            };

            foreach (var fromState in states)
            {
                foreach (var toState in states)
                {
                    if (fromState == toState)
                    {
                        continue;
                    }
                    else
                    {
                        allToAllTransitions.Add(fromState, toState);
                    }
                }
            }

            var currentTransitions = GetTransitions();


            foreach (var item in allToAllTransitions)
            {
                if (currentTransitions.Contains(item.From, item.To) == false)
                {
                    AddTransition(item);
                }
            }
        }

        public void AddTransition(WorkItemStateTransition transition)
        {
            if (transition == null)
                throw new ArgumentNullException(nameof(transition), "transition is null.");

            if (Contains(transition) == false)
            {
                var transitions = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("TRANSITIONS");

                var newTransition = XElement.Parse(transition.ToXml());

                transitions.Add(newTransition);
            }
        }

        public void AddState(string state)
        {
            if (string.IsNullOrEmpty(state))
                throw new ArgumentException("state is null or empty.", nameof(state));

            if (ContainsState(state) == false)
            {
                var states = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("STATES");

                var newState = XElement.Parse(string.Format("<STATE value=\"{0}\" />", state));

                states.Add(newState);
            }
        }

        public bool Contains(WorkItemStateTransition transition)
        {
            if (transition == null)
                throw new ArgumentNullException(nameof(transition), "transition is null.");

            var transitions = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("TRANSITIONS");

            var match = (from temp in transitions.Elements("TRANSITION")
                         where XmlUtility.GetAttributeValue(temp, "from") == transition.From &&
                         XmlUtility.GetAttributeValue(temp, "to") == transition.To
                         select temp).FirstOrDefault();

            if (match == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void RemoveState(string state)
        {
            if (string.IsNullOrEmpty(state))
                throw new ArgumentException("state is null or empty.", nameof(state));

            if (ContainsState(state) == true)
            {
                var removeThis = GetStateElement(state);

                removeThis.Remove();
            }
        }

        private XElement GetStateElement(string state)
        {
            var states = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("STATES");

            var match = (from temp in states.Elements("STATE")
                         where XmlUtility.GetAttributeValue(temp, "value") == state
                         select temp).FirstOrDefault();
            return match;
        }

        public bool ContainsState(string state)
        {
            if (string.IsNullOrEmpty(state))
                throw new ArgumentException("state is null or empty.", nameof(state));

            var match = GetStateElement(state);
            if (match == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void RemoveTransition(string state)
        {
            if (string.IsNullOrEmpty(state))
                throw new ArgumentException("state is null or empty.", nameof(state));

            var transitions = _document.Root.Element("WORKITEMTYPE").Element("WORKFLOW").Element("TRANSITIONS");

            var matches = (from temp in transitions.Elements("TRANSITION")
                           where
                           (XmlUtility.GetAttributeValue(temp, "from") == state ||
                           XmlUtility.GetAttributeValue(temp, "to") == state)
                           select temp).ToList();

            foreach (var removeThis in matches)
            {
                removeThis.Remove();
            }
        }

        public void CreateTemporaryDoneState()
        {
            AddState("TemporaryDone");
            AddTransition(new WorkItemStateTransition("Done", "TemporaryDone"));
            AddTransition(new WorkItemStateTransition("TemporaryDone", "Done"));
        }
    }

}
