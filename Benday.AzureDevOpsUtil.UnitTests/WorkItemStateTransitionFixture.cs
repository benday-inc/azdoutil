using Benday.AzureDevOpsUtil.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.AzureDevOpsUtil.UnitTests
{
    [TestClass]
    public class WorkItemStateTransitionFixture
    {
        public WorkItemStateTransitionFixture()
        {
        }

        [TestMethod]
        public void GetXmlRepresentationForTransition()
        {
            var systemUnderTest = new WorkItemStateTransition("Not Done", "New");

            var actual = systemUnderTest.ToXml();

            var expected =
                "<TRANSITION from=\"Not Done\" to=\"New\"><REASONS><DEFAULTREASON value=\"State updated\" /></REASONS></TRANSITION>";

            Assert.AreEqual<string>(expected, actual, "XML value for transition was wrong.");
        }
    }
}
