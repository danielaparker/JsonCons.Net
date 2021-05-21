using System;
using System.IO;
using System.Diagnostics;
using NUnit.Framework;

namespace Jsoncons.JsonPathLib.Tests
{
    [TestFixture]
    public class JsonPathTests
    {
        [Test]
        public void Test()
        {
            var path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\identifiers.json");
            Debug.WriteLine(path);
            string text = System.IO.File.ReadAllText(path);
            //Console.Write(text);
        }
    }
}
