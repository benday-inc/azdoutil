using Benday.AzureDevOpsUtil.Api;
using System;
using System.Linq;
using System.Xml.Linq;
public class BugWorkItemUpdaterForMissingFields
{
    private readonly WorkItemTypeDefinition _Witd;
    public BugWorkItemUpdaterForMissingFields(WorkItemTypeDefinition witd)
    {
        _Witd = witd ?? throw new ArgumentNullException(nameof(witd));
    }


    public void Fix()
    {
        var fieldAcceptanceCriteria = "<FIELD name=\"Acceptance Criteria\" refname=\"Microsoft.VSTS.Common.AcceptanceCriteria\" type=\"HTML\" />";
        var fieldReproSteps = "<FIELD name=\"Repro Steps\" refname=\"Microsoft.VSTS.TCM.ReproSteps\" type=\"HTML\" />";
        var fieldSystemInfo = "<FIELD name=\"System Info\" refname=\"Microsoft.VSTS.TCM.SystemInfo\" type=\"HTML\" />";

        var fields = _Witd.GetFieldsElement();

        var madeChange = false;

        if (_Witd.HasField("Microsoft.VSTS.Common.AcceptanceCriteria") == false)
        {
            fields.Add(XElement.Parse(fieldAcceptanceCriteria));
            madeChange = true;
        }

        if (_Witd.HasField("Microsoft.VSTS.Common.ReproSteps") == false)
        {
            fields.Add(XElement.Parse(fieldReproSteps));
            madeChange = true;
        }

        if (_Witd.HasField("Microsoft.VSTS.Common.SystemInfo") == false)
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

        if (_Witd.HasPageInWebLayout("Misc") == false)
        {
            var webLayout = _Witd.GetWebLayout();

            webLayout.Add(XElement.Parse(page));
        }
    }
}