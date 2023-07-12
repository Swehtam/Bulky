using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Bulky.Utility;

namespace BulkyTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test_AddMethod()
        {
            Calc cal = new Calc();
            double res = cal.Add(10, 10);
            Assert.AreEqual(res, 20);
        }
    }
}
