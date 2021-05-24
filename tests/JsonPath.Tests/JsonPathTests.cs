using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using JsonCons.JsonPathLib;
using NUnit.Framework;

namespace JsonCons.JsonPathLib.Tests
{
    [TestFixture]
    public class JsonPathTests
    {
        public void RunJsonPathTests(string path)
        {
            string text = System.IO.File.ReadAllText(path);
            JsonDocument doc = JsonDocument.Parse(text);
            var testsEnumeratable = doc.RootElement.EnumerateArray();

            foreach (var testGroup in testsEnumeratable)
            {
                var given = testGroup.GetProperty("given");
                var testCases = testGroup.GetProperty("cases");
                var testCasesEnumeratable = testCases.EnumerateArray();
                foreach (var testCase in testCasesEnumeratable)
                {
                    var exprElement = testCase.GetProperty("expression");
                    var expression = JsonPathExpression.CreateInstance(exprElement.ToString());
                    JsonElement expected;
                    if (testCase.TryGetProperty("result", out expected))
                    {
                        TestContext.WriteLine(expected.ToString());
                    }
                }
            }
        }

        [Test]
        public void Test()
        {
            //TestContext.WriteLine("Message...");
            var path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\identifiers.json");
            RunJsonPathTests(path);
        }
    }
}
