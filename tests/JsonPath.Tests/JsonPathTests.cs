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
            var comparer = new JsonElementEqualityComparer();

            foreach (var testGroup in testsEnumeratable)
            {
                JsonElement given = testGroup.GetProperty("given");
                var testCases = testGroup.GetProperty("cases");
                var testCasesEnumeratable = testCases.EnumerateArray();
                foreach (var testCase in testCasesEnumeratable)
                {
                    ResultOptions options = 0;

                    JsonElement element;
                    if (testCase.TryGetProperty("nodups", out element) && element.ValueKind == JsonValueKind.True)
                    {
                        options |= ResultOptions.NoDups;
                    }
                    if (testCase.TryGetProperty("sort", out element) && element.ValueKind == JsonValueKind.True)
                    {
                        options |= ResultOptions.Sort;
                    }

                    var exprElement = testCase.GetProperty("expression");
                    var expr = JsonPath.Compile(exprElement.ToString());


                    JsonElement expected;
                    if (testCase.TryGetProperty("result", out expected))
                    {
                        var items = expr.Select(given, options);

                        bool success = items.Count == expected.GetArrayLength();
                        for (Int32 i = 0; success && i < items.Count; ++i)
                        {
                            if (!comparer.Equals(items[i],expected[i]))
                            {
                                success = false;
                            }
                        }
                        if (!success)
                        {
                            TestContext.WriteLine("Document: " + given.ToString());
                            TestContext.WriteLine("Path: " + exprElement.ToString());
                            TestContext.WriteLine("Expected: " + expected.ToString());
                            TestContext.WriteLine("Results: ");
                            foreach (var item in items)
                            {
                                TestContext.WriteLine(item.ToString());
                            }
                        }
                        Assert.AreEqual(items.Count, expected.GetArrayLength());
                        for (Int32 i = 0; i < items.Count && i < expected.GetArrayLength(); ++i)
                        {
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
            //RunJsonPathTests(System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\identifiers.json"));
            //RunJsonPathTests(System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\indices.json"));
            RunJsonPathTests(System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, @"..\..\test_data\union.json"));
        }
    }
}
