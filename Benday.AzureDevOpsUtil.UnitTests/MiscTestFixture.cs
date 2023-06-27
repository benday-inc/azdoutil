namespace Benday.AzureDevOpsUtil.UnitTests
{
    [TestClass]
    public class MiscTestFixture
    {
        [TestMethod]
        public void WhatCharIsThis()
        {
            var val = @"BUG - Planning a trip - Trip Assessment report lower part of the page Trip assessment Details/Actions to Undertake is displayed starting in the center of the screen.";

            val = val.Trim();

            foreach (var item in val)
            {
                Console.WriteLine($"IsControl:{char.IsControl(item)}\t{item}: {(int)item}");
            }
        }

        [TestMethod]
        public void RemoveControlCharsFromString()
        {
            var original = @"BUG - Planning a trip - Trip Assessment report lower part of the page Trip assessment Details/Actions to Undertake is displayed starting in the center of the screen.";
            var expected = @"BUG - Planning a trip - Trip Assessment report lower part of the page Trip assessment Details/Actions to Undertake is displayed starting in the center of the screen.";

            var actual = StringUtility.RemoveControlChars(original);

            Assert.AreEqual<string>(expected, actual, "Did not remove control chars");
        }

        [TestMethod]
        public void ContainsControlCharacters_true()
        {
            var original = @"BUG - Planning a trip - Trip Assessment report lower part of the page Trip assessment Details/Actions to Undertake is displayed starting in the center of the screen.";
            var expected = true;

            var actual = StringUtility.ContainsControlCharacters(original);

            Assert.AreEqual<bool>(expected, actual, "Wrong value");
        }

        [TestMethod]
        public void ContainsControlCharacters_false()
        {
            var original = @"BUG - Planning a trip - Trip Assessment report lower part of the page Trip assessment Details/Actions to Undertake is displayed starting in the center of the screen.";
            var expected = false;

            var actual = StringUtility.ContainsControlCharacters(original);

            Assert.AreEqual<bool>(expected, actual, "Wrong value");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetIndexForPercentForecast_ItemCountMustBePositiveNumber()
        {
            Api.Utilities.GetIndexForPercentForecast(-1, 85);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetIndexForPercentForecast_PercentMustBePositiveNumber()
        {
            Api.Utilities.GetIndexForPercentForecast(100, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetIndexForPercentForecast_PercentMaxValueIs100()
        {
            Api.Utilities.GetIndexForPercentForecast(100, 101);
        }

        [TestMethod]
        [DataRow(0, 85, -1)]
        [DataRow(1, 85, 0)]
        [DataRow(100, 0, 0)]
        [DataRow(2, 85, 1)]
        [DataRow(3, 85, 2)]
        [DataRow(4, 85, 2)]
        [DataRow(5, 85, 3)]
        [DataRow(6, 85, 4)]
        [DataRow(7, 85, 5)]
        [DataRow(10, 85, 8)]
        [DataRow(100, 85, 84)]
        public void GetIndexForPercentForecast_LowItemCountTests(int itemCount, int percent, int expectedIndex)
        {
            // arrange

            // act
            var actual = Api.Utilities.GetIndexForPercentForecast(itemCount, percent, true);

            // assert
            Assert.AreEqual<int>(expectedIndex, actual, "Wrong value for index.");
        }
    }
}