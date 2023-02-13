using System;
using Benday.WorkItemUtility.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.WorkItemUtility.UnitTests
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
    }
}
