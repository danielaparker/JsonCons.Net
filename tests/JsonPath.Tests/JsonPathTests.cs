using System;
using System.IO;
using NUnit.Framework;

namespace Jsoncons.JsonPathLib.Tests
{
    [TestFixture]
    public class JsonPathTests
    {
        [Test]
        public void Test()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, @"test_data\identifiers.json");
            Console.Write(path);
        }
        [TestCase(12, 3, 4)]
        [TestCase(12, 2, 6)]
        [TestCase(12, 4, 3)]
        public void DivideTest(int n, int d, int q)
        {
            Assert.AreEqual(q, n / d);
        }
    }
}
