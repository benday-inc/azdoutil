using Benday.AzureDevOpsUtil.Api;
using System.Xml.Linq;
public class BugWorkItemUpdaterForMissingFields
{
    private readonly WorkItemTypeDefinition _witd;
    public BugWorkItemUpdaterForMissingFields(WorkItemTypeDefinition witd)
    {
        _witd = witd ?? throw new ArgumentNullException(nameof(witd));
    }


    public void Fix()
    {
        var fieldAcceptanceCriteria = "<FIELD name=\"Acceptance Criteria\" refname=\"Microsoft.VSTS.Common.AcceptanceCriteria\" type=\"HTML\" />";
        var fieldReproSteps = "<FIELD name=\"Repro Steps\" refname=\"Microsoft.VSTS.TCM.ReproSteps\" type=\"HTML\" />";
        var fieldSystemInfo = "<FIELD name=\"System Info\" refname=\"Microsoft.VSTS.TCM.SystemInfo\" type=\"HTML\" />";

        var fields = _witd.GetFieldsElement();

        if (fields == null)
        {
            throw new InvalidOperationException($"Could not locate fields element");
        }

        var madeChange = false;

        if (_witd.HasField("Microsoft.VSTS.Common.AcceptanceCriteria") == false)
        {
            fields.Add(XElement.Parse(fieldAcceptanceCriteria));
            madeChange = true;
        }

        if (_witd.HasField("Microsoft.VSTS.Common.ReproSteps") == false)
        {
            fields.Add(XElement.Parse(fieldReproSteps));
            madeChange = true;
        }

        if (_witd.HasField("Microsoft.VSTS.Common.SystemInfo") == false)
        {
            fields.Add(XElement.Parse(fieldSystemInfo));
            madeChange = true;
        }

        if (madeChange == false)
        {
            return;
        }


        var page = @"<Page Label=""Misc"" LayoutMode=""FirstColumnWide"">
    <Section>
    <Group Label=""Repro Steps"">
        <Control Label=""Repro Steps"" Type=""HtmlFieldControl"" FieldName=""Microsoft.VSTS.TCM.ReproSteps"" />
    </Group>
    <Group Label=""System Info"">
        <Control Label=""System Info"" Type=""HtmlFieldControl"" FieldName=""Microsoft.VSTS.TCM.SystemInfo"" />
    </Group>
    <Group Label=""Acceptance Criteria"">
        <Control Label=""Acceptance Criteria"" Type=""HtmlFieldControl"" FieldName=""Microsoft.VSTS.Common.AcceptanceCriteria"" />
    </Group>
    </Section></Page>";

        if (_witd.HasPageInWebLayout("Misc") == false)
        {
            var webLayout = _witd.GetWebLayout();

            webLayout?.Add(XElement.Parse(page));
        }
    }
}