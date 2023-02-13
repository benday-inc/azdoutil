using Benday.AzureDevOpsUtil.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Benday.AzureDevOpsUtil.UnitTests;
[TestClass]
public class WorkItemScriptGeneratorFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private WorkItemScriptGenerator _SystemUnderTest;

    private WorkItemScriptGenerator SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new WorkItemScriptGenerator();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void GetRandomTitles()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(SystemUnderTest.GetRandomTitle());
            Console.WriteLine();
        }
    }


}
