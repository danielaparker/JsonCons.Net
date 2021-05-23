using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib.Tests
{
    [TestFixture]
    public class JsonPathTests
    {
        public void RunJsonTests(string path)
        {
            string text = System.IO.File.ReadAllText(path);
            JsonDocument tests = JsonDocument.Parse(text);
        }

        [Test]
        public void Test()
        {
            var path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\identifiers.json");
            RunJsonTests(path);
        }
    }
}
