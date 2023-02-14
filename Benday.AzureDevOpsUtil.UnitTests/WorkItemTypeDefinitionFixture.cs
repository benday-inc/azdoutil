using Benday.AzureDevOpsUtil.Api;
using System.Xml.Linq;

namespace Benday.AzureDevOpsUtil.UnitTests
{
    [TestClass]
    [Ignore("Need to migrate test files into solution")]
    public class WorkItemTypeDefinitionFixture
    {
        private WorkItemTypeDefinition? _systemUnderTest;
        public WorkItemTypeDefinition SystemUnderTest
        {
            get
            {
                if (_systemUnderTest == null)
                {
                    _systemUnderTest = new WorkItemTypeDefinition(
                        UnitTestUtility.GetPathToTestFile());
                }

                return _systemUnderTest;
            }
        }

        [TestInitialize]
        public void OnTestInitialize()
        {
            _systemUnderTest = null;
        }

        [TestMethod]
        public void WorkItemTypeNameIsPopulated()
        {
            Assert.AreEqual<string>("Product Backlog Item", SystemUnderTest.WorkItemType, "Wrong value.");
        }

        [TestMethod]
        public void GetFields()
        {
            var actual = SystemUnderTest.GetFields();

            Assert.IsNotNull(actual, "Actual was null.");
            Assert.AreNotEqual<int>(0, actual.Count, "Count was 0");

            foreach (var item in actual)
            {
                Assert.AreEqual<string>("FIELD", item.Name.ToString(), "Item's name was not expected.");
            }
        }

        [TestMethod]
        public void GetStates()
        {
            var expected = new List<string>
            {
                "Approved",
                "Committed",
                "Done",
                "New",
                "Removed",
                "Refined",
                "In QA",
                "In Progress",
                "Dev Complete"
            };

            var actual = SystemUnderTest.GetStates();

            Assert.AreEqual<int>(expected.Count, actual.Count, "Count");

            CollectionAssert.AreEquivalent(expected, actual, "States didn't match.");
        }

        [TestMethod]
        public void CreateAllToAllStateTransitions()
        {
            // arrange
            var expectedAllToAll = new List<WorkItemStateTransition>() {
                new WorkItemStateTransition(string.Empty, "New"),

                new WorkItemStateTransition("Approved", "Committed"),
                new WorkItemStateTransition("Approved", "Done"),
                new WorkItemStateTransition("Approved", "New"),
                new WorkItemStateTransition("Approved", "Removed"),
                new WorkItemStateTransition("Approved", "Refined"),
                new WorkItemStateTransition("Approved", "In QA"),
                new WorkItemStateTransition("Approved", "In Progress"),
                new WorkItemStateTransition("Approved", "Dev Complete"),

                new WorkItemStateTransition("Committed", "Approved"),
                new WorkItemStateTransition("Committed", "Done"),
                new WorkItemStateTransition("Committed", "New"),
                new WorkItemStateTransition("Committed", "Removed"),
                new WorkItemStateTransition("Committed", "Refined"),
                new WorkItemStateTransition("Committed", "In QA"),
                new WorkItemStateTransition("Committed", "In Progress"),
                new WorkItemStateTransition("Committed", "Dev Complete"),

                new WorkItemStateTransition("Done", "Approved"),
                new WorkItemStateTransition("Done", "Committed"),
                new WorkItemStateTransition("Done", "New"),
                new WorkItemStateTransition("Done", "Removed"),
                new WorkItemStateTransition("Done", "Refined"),
                new WorkItemStateTransition("Done", "In QA"),
                new WorkItemStateTransition("Done", "In Progress"),
                new WorkItemStateTransition("Done", "Dev Complete"),

                new WorkItemStateTransition("New", "Approved"),
                new WorkItemStateTransition("New", "Committed"),
                new WorkItemStateTransition("New", "Done"),
                new WorkItemStateTransition("New", "Removed"),
                new WorkItemStateTransition("New", "Refined"),
                new WorkItemStateTransition("New", "In QA"),
                new WorkItemStateTransition("New", "In Progress"),
                new WorkItemStateTransition("New", "Dev Complete"),

                new WorkItemStateTransition("Removed", "Approved"),
                new WorkItemStateTransition("Removed", "Committed"),
                new WorkItemStateTransition("Removed", "Done"),
                new WorkItemStateTransition("Removed", "New"),
                new WorkItemStateTransition("Removed", "Refined"),
                new WorkItemStateTransition("Removed", "In QA"),
                new WorkItemStateTransition("Removed", "In Progress"),
                new WorkItemStateTransition("Removed", "Dev Complete"),

                new WorkItemStateTransition("Refined", "Approved"),
                new WorkItemStateTransition("Refined", "Committed"),
                new WorkItemStateTransition("Refined", "Done"),
                new WorkItemStateTransition("Refined", "New"),
                new WorkItemStateTransition("Refined", "Removed"),
                new WorkItemStateTransition("Refined", "In QA"),
                new WorkItemStateTransition("Refined", "In Progress"),
                new WorkItemStateTransition("Refined", "Dev Complete"),

                new WorkItemStateTransition("In QA", "Approved"),
                new WorkItemStateTransition("In QA", "Committed"),
                new WorkItemStateTransition("In QA", "Done"),
                new WorkItemStateTransition("In QA", "New"),
                new WorkItemStateTransition("In QA", "Refined"),
                new WorkItemStateTransition("In QA", "Removed"),
                new WorkItemStateTransition("In QA", "In Progress"),
                new WorkItemStateTransition("In QA", "Dev Complete"),

                new WorkItemStateTransition("In Progress", "Approved"),
                new WorkItemStateTransition("In Progress", "Committed"),
                new WorkItemStateTransition("In Progress", "Done"),
                new WorkItemStateTransition("In Progress", "New"),
                new WorkItemStateTransition("In Progress", "Refined"),
                new WorkItemStateTransition("In Progress", "Removed"),
                new WorkItemStateTransition("In Progress", "In QA"),
                new WorkItemStateTransition("In Progress", "Dev Complete"),

                new WorkItemStateTransition("Dev Complete", "Approved"),
                new WorkItemStateTransition("Dev Complete", "Committed"),
                new WorkItemStateTransition("Dev Complete", "Done"),
                new WorkItemStateTransition("Dev Complete", "New"),
                new WorkItemStateTransition("Dev Complete", "Removed"),
                new WorkItemStateTransition("Dev Complete", "Refined"),
                new WorkItemStateTransition("Dev Complete", "In QA"),
                new WorkItemStateTransition("Dev Complete", "In Progress"),
            };

            // act
            SystemUnderTest.CreateAllToAllStateTransitions();

            // assert
            var actual = SystemUnderTest.GetTransitions();

            AssertAreEquivalent(expectedAllToAll, actual);

            /*
                        var xml = SystemUnderTest.Element.ToString();

                        File.WriteAllText("/Users/benday/Pbi-Extra-States-all-to-all-transitions.xml", xml);
                        */
        }

        private void AssertAreEquivalent(List<WorkItemStateTransition> expected,
            WorkItemStateTransitionCollection actual)
        {
            Assert.AreEqual<int>(expected.Count, actual.Count, "Count");

            foreach (var item in expected)
            {
                Assert.IsTrue(actual.Contains(item.From, item.To),
                    $"Actual did not contain transition for '{item.From}' --> '{item.To}'");
            }
        }

        [TestMethod]
        public void GetInitialState()
        {
            // arrange
            var expected = "New";

            // act
            var actual = SystemUnderTest.GetInitialState();

            // assert
            Assert.AreEqual<string>(expected, actual, "Initial state was wrong.");
        }

        [TestMethod]
        public void GetStateTransitions()
        {
            // arrange
            var expectedCount = 17;

            var expected = new List<WorkItemStateTransition>() {
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

            // double-check that the expected collection is properly initialized
            Assert.AreEqual<int>(expectedCount, expected.Count,
                "Expected collection count was wrong");

            // act
            var actual = SystemUnderTest.GetTransitions();

            // assert
            Assert.IsNotNull(actual, "actual was null");
            Assert.AreEqual<int>(expectedCount, actual.Count, "Actual transition count was wrong.");
        }






        [TestMethod]
        public void GetStatesWithReadOnlyFlags()
        {
            /*
             * 
             *  <STATE value="Done">
                  <FIELDS>
                    <FIELD refname="Microsoft.VSTS.Scheduling.Effort">
                      <READONLY />
                    </FIELD>
                  </FIELDS>
                </STATE>
            */

            IList<XElement> states = SystemUnderTest.GetStatesWithReadOnlyFlags();

            Assert.IsNotNull(states, "Result was null.");
            Assert.AreNotEqual<int>(0, states.Count, "Number of states with read only flags should not be zero.");
            Assert.AreEqual<int>(1, states.Count, "Number of states with read only flags should only be one.");

            var actual = states[0];

            Assert.AreEqual<string>("STATE", actual.Name.LocalName, "Expected element to be named STATE.");

            Assert.AreEqual<string>(XmlUtility.GetAttributeValue(actual, "value"), "Done", "Expected the value attribute to be Done.");

            var readonlyElements = actual.Descendants("READONLY");

            Assert.AreNotEqual<int>(0, readonlyElements.Count(),
                "State transition didn't have a read-only flag under it");
            Assert.AreEqual<int>(2, readonlyElements.Count(), "Number of readonly elements was wrong.");

            var readonlyElement0 = readonlyElements.ToArray()[0];
            var readonlyElement1 = readonlyElements.ToArray()[1];

            Assert.IsNotNull(readonlyElement0.Parent, "Parent node was null.");
            Assert.AreEqual<string>(readonlyElement0.Parent.Name.LocalName, "FIELD", "Expected node should be a field.");
            Assert.AreEqual<string>(
                "Microsoft.VSTS.Common.BusinessValue",
                XmlUtility.GetAttributeValue(readonlyElement0.Parent, "refname"),
                "Field element with readonly didn't have the expected refname attribute.");

            Assert.IsNotNull(readonlyElement1.Parent, "Parent node was null.");
            Assert.AreEqual<string>(readonlyElement1.Parent.Name.LocalName, "FIELD", "Expected node should be a field.");
            Assert.AreEqual<string>(
                "Microsoft.VSTS.Scheduling.Effort",
                XmlUtility.GetAttributeValue(readonlyElement1.Parent, "refname"),
                "Field element with readonly didn't have the expected refname attribute.");
        }

        [TestMethod]
        public void RemoveReadOnlyFlagFromStates()
        {
            /*
             * 
             *  <STATE value="Done">
                  <FIELDS>
                    <FIELD refname="Microsoft.VSTS.Scheduling.Effort">
                      <READONLY />
                    </FIELD>
                  </FIELDS>
                </STATE>
            */

            var stateWithReadOnly = SystemUnderTest.GetStatesWithReadOnlyFlags()[0];

            var theReadOnlyFlag = stateWithReadOnly.Descendants("READONLY").FirstOrDefault();

            Assert.IsNotNull(theReadOnlyFlag, "State transition didn't have a read-only flag under it.");

            SystemUnderTest.RemoveReadOnlyFlagFromStates();

            var theReadOnlyFlagShouldNotExist = stateWithReadOnly.Descendants("READONLY").FirstOrDefault();

            Assert.IsNull(theReadOnlyFlagShouldNotExist, "The READONLY element shouldn't exist.");
        }

        [TestMethod]
        public void ConvertFieldFromAllowedValuesToSuggestedValues()
        {
            var refname = "Microsoft.VSTS.Common.Priority";

            var fieldElement = SystemUnderTest.GetFieldByRefname(refname);

            Assert.IsNotNull(fieldElement,
                $"Could not find field for {refname}.");
            Assert.AreEqual<string>(refname,
                XmlUtility.GetAttributeValue(fieldElement, "refname"), "Wrong element.");


            var allowedValuesElement = fieldElement.Element("ALLOWEDVALUES");
            Assert.IsNotNull(allowedValuesElement, "Did not find the allowed values element.");

            var allowedValuesElementAsString = allowedValuesElement.ToString();

            SystemUnderTest.ConvertFieldFromAllowedValuesToSuggestedValues(refname);

            Console.WriteLine(fieldElement.ToString());

            // re-retrieve the element
            allowedValuesElement = fieldElement.Element("ALLOWEDVALUES");

            Assert.IsNull(allowedValuesElement, "Allowed values should not exist.");

            var suggestedValuesElement = fieldElement.Element("SUGGESTEDVALUES");
            Assert.IsNotNull(suggestedValuesElement, "Suggested values element could not be found.");

            var suggestedValuesElementToString = suggestedValuesElement.ToString();

            Console.WriteLine(suggestedValuesElementToString);

            Assert.AreEqual<string>(allowedValuesElementAsString.Replace("ALLOWEDVALUES", "SUGGESTEDVALUES"),
                suggestedValuesElementToString,
                "Suggested values element wasn't what was expected.");
        }

        [TestMethod]
        public void AddState()
        {
            var states = SystemUnderTest.GetStates();

            Assert.IsFalse(states.Contains("Deleted"), "States should not contain value yet.");

            SystemUnderTest.AddState("Deleted");

            Console.WriteLine(SystemUnderTest.Element.ToString());

            states = SystemUnderTest.GetStates();

            Assert.IsTrue(states.Contains("Deleted"), "States didn't contain new value.");
        }

        [TestMethod]
        public void WhenStateExistsContainsStateShouldReturnTrue()
        {
            Assert.IsTrue(SystemUnderTest.ContainsState("Committed"), "Should contain the state.");
        }

        [TestMethod]
        public void RemoveState()
        {
            Assert.IsTrue(SystemUnderTest.ContainsState("Committed"), "Should contain the state.");

            SystemUnderTest.RemoveState("Committed");

            Assert.IsFalse(SystemUnderTest.ContainsState("Committed"), "Should not contain the state.");
        }

        [TestMethod]
        public void RemoveTransition()
        {
            Assert.IsTrue(SystemUnderTest.Contains(
                new WorkItemStateTransition("New", "Approved")), "Should contain the transition.");
            Assert.IsTrue(SystemUnderTest.Contains(
                new WorkItemStateTransition("Approved", "Committed")), "Should contain the transition.");

            SystemUnderTest.RemoveTransition("Approved");

            Assert.IsFalse(SystemUnderTest.Contains(
                new WorkItemStateTransition("New", "Approved")), "Should not contain the transition.");
            Assert.IsFalse(SystemUnderTest.Contains(
                new WorkItemStateTransition("Approved", "Committed")), "Should not contain the transition.");
        }

        [TestMethod]
        public void WhenStateDoesNotExistContainsStateShouldReturnFalse()
        {
            Assert.IsFalse(SystemUnderTest.ContainsState("Junk State That Does Not Exist"), "Should contain the state.");
        }

        [TestMethod]
        public void AddTransition()
        {
            var transition = new WorkItemStateTransition("Not Done", "New");

            Assert.IsFalse(SystemUnderTest.Contains(transition), "Should not contain the transition yet.");

            SystemUnderTest.AddTransition(transition);

            Console.WriteLine(SystemUnderTest.Element.ToString());

            Assert.IsTrue(SystemUnderTest.Contains(transition), "Should contain the transition.");
        }

        [TestMethod]
        public void WhenTransitionExistsContainsShouldReturnTrue()
        {
            var transition = new WorkItemStateTransition("New", "Approved");

            Assert.IsTrue(SystemUnderTest.Contains(transition), "Should contain the transition.");
        }

        [TestMethod]
        public void WhenTransitionDoesNotExistContainsShouldReturnFalse()
        {
            var transition = new WorkItemStateTransition("Thursday", "Friday");

            Assert.IsFalse(SystemUnderTest.Contains(transition), "Should not contain the transition.");
        }

        [TestMethod]
        public void CreateTemporaryDoneState()
        {
            Assert.IsFalse(SystemUnderTest.ContainsState("TemporaryDone"), "Should not have the TemporaryDone state.");

            Assert.IsFalse(SystemUnderTest.Contains(new WorkItemStateTransition("Done", "TemporaryDone")),
                "Should not have a transition from Done to TemporaryDone.");

            Assert.IsFalse(SystemUnderTest.Contains(new WorkItemStateTransition("TemporaryDone", "Done")),
                "Should not have a transition from Done to TemporaryDone.");

            SystemUnderTest.CreateTemporaryDoneState();

            Assert.IsTrue(SystemUnderTest.ContainsState("TemporaryDone"), "Should have the TemporaryDone state.");

            Assert.IsTrue(SystemUnderTest.Contains(new WorkItemStateTransition("Done", "TemporaryDone")),
                "Should have a transition from Done to TemporaryDone.");

            Assert.IsTrue(SystemUnderTest.Contains(new WorkItemStateTransition("TemporaryDone", "Done")),
                "Should have a transition from Done to TemporaryDone.");
        }
    }
}
