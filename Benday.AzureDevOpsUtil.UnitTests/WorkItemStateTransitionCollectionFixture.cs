using Benday.AzureDevOpsUtil.Api;

namespace Benday.AzureDevOpsUtil.UnitTests
{

    [TestClass]
    public class WorkItemStateTransitionCollectionFixture
    {

        [TestInitialize]
        public void OnTestInitialize()
        {
            _systemUnderTest = null;
        }

        private WorkItemStateTransitionCollection? _systemUnderTest;

        private WorkItemStateTransitionCollection SystemUnderTest
        {
            get
            {
                if (_systemUnderTest == null)
                {
                    _systemUnderTest = new WorkItemStateTransitionCollection();
                }

                return _systemUnderTest;
            }
        }

        [TestMethod]
        public void Count_Initialized()
        {
            // arrange
            var expectedCount = 17;

            // act
            Initialize();

            // assert
            Assert.AreEqual<int>(expectedCount, SystemUnderTest.Count,
                "count was wrong");
        }


        [TestMethod]
        public void Count_Default()
        {
            // arrange
            var expectedCount = 0;

            // act


            // assert
            Assert.AreEqual<int>(expectedCount, SystemUnderTest.Count,
                "count was wrong");
        }

        [DataTestMethod]
        [DataRow("Approved", "Committed", true)]
        [DataRow("Done", "Approved", true)]
        [DataRow("", "New", true)]
        [DataRow("", "bogus", false)]
        [DataRow("Approved", "bogus", false)]
        public void Contains(string from, string to, bool expected)
        {
            // arrange
            Initialize();

            // act
            var actual = SystemUnderTest.Contains(from, to);

            // assert
            Assert.AreEqual<bool>(expected, actual, "Wrong result");
        }

        private void Initialize()
        {
            _systemUnderTest = new WorkItemStateTransitionCollection() {
                new WorkItemStateTransition("Approved", "Committed"),
                new WorkItemStateTransition("New", "Committed"),
                new WorkItemStateTransition("Done", "Committed"),
                new WorkItemStateTransition("Committed", "Approved"),
                new WorkItemStateTransition("New", "Approved"),
                new WorkItemStateTransition("Done", "Approved"),
                new WorkItemStateTransition("Committed", "New"),
                new WorkItemStateTransition("Approved", "New"),
                new WorkItemStateTransition("Removed", "New"),
                new WorkItemStateTransition("Done", "New"),
                new WorkItemStateTransition(string.Empty, "New"),
                new WorkItemStateTransition("Committed", "Removed"),
                new WorkItemStateTransition("Approved", "Removed"),
                new WorkItemStateTransition("New", "Removed"),
                new WorkItemStateTransition("Committed", "Done"),
                new WorkItemStateTransition("Approved", "Done"),
                new WorkItemStateTransition("New", "Done")
            };
        }
    }


}
