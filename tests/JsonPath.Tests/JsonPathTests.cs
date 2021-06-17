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
                JsonElement given = testGroup.GetProperty("given");
                var testCases = testGroup.GetProperty("cases");
                var testCasesEnumeratable = testCases.EnumerateArray();
                foreach (var testCase in testCasesEnumeratable)
                {
                    var exprElement = testCase.GetProperty("expression");
                    var expr = JsonPathExpression.Compile(exprElement.ToString());


                    JsonElement expected;
                    if (testCase.TryGetProperty("result", out expected))
                    {
                        TestContext.WriteLine(@"result: " + expected.ToString());
                        var items = expr.Evaluate(given);

                        var comparer = new JsonElementComparer();

                        TestContext.WriteLine("Results " + items.Count);
                        Assert.AreEqual(items.Count, expected.GetArrayLength());
                        for (Int32 i = 0; i < items.Count; ++i)
                        {
                            TestContext.WriteLine("Result: " + items[i].ToString());

                            Assert.IsTrue(comparer.Equals(items[i],expected[i]));
                        }
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
