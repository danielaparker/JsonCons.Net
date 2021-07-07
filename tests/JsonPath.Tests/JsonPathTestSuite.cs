using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPathLib;

namespace JsonCons.JsonPathLib.Tests
{
    [TestClass]
    public class JsonPathTests
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }
        public void RunJsonPathTests(string path)
        {

            TestContext.WriteLine($"Test {path}");
            string text = System.IO.File.ReadAllText(path);
            var jsonOptions = new JsonDocumentOptions();
            jsonOptions.CommentHandling = JsonCommentHandling.Skip; 
            JsonDocument doc = JsonDocument.Parse(text, jsonOptions);
            var testsEnumeratable = doc.RootElement.EnumerateArray();
            var comparer = JsonElementEqualityComparer.Instance;

            foreach (var testGroup in testsEnumeratable)
            {
                JsonElement given = testGroup.GetProperty("given");
                var testCases = testGroup.GetProperty("cases");
                var testCasesEnumeratable = testCases.EnumerateArray();
                foreach (var testCase in testCasesEnumeratable)
                {
                    string comment;
                    JsonElement commentElement;
                    if (testCase.TryGetProperty("comment", out commentElement) && commentElement.ValueKind == JsonValueKind.String)
                    {
                        comment = commentElement.GetString();
                    }
                    else
                    {
                        comment = "";
                    }

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

                    try
                    {
                        JsonElement expected;
                        if (testCase.TryGetProperty("error", out expected))
                        {
                            //Assert.Throws<JsonException>(() => JsonPathExpression.Parse(exprElement.ToString()));
                        }
                        else if (testCase.TryGetProperty("result", out expected))
                        {
                            var expr = JsonPathExpression.Parse(exprElement.ToString());
                            var items = JsonPath.Select(given, expr, options);

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
                                TestContext.WriteLine("File: {0}", path);
                                TestContext.WriteLine(comment);
                                TestContext.WriteLine("Document: " + given.ToString());
                                TestContext.WriteLine("Path: " + exprElement.ToString());
                                if ((options & ResultOptions.NoDups) != 0)
                                {
                                    TestContext.WriteLine("nodups");
                                }
                                if ((options & ResultOptions.Sort) != 0)
                                {
                                    TestContext.WriteLine("sort");
                                }
                                TestContext.WriteLine("Expected: " + expected.ToString());
                                TestContext.WriteLine("Results: ");
                                foreach (var item in items)
                                {
                                    TestContext.WriteLine(item.ToString());
                                }
                            }
                            Assert.AreEqual(expected.GetArrayLength(),items.Count);
                            for (Int32 i = 0; i < items.Count && i < expected.GetArrayLength(); ++i)
                            {
                                Assert.IsTrue(comparer.Equals(items[i],expected[i]));
                            }
                            
                        }
                    }
                    catch (Exception e)
                    {
                        TestContext.WriteLine("File: {0}", path);
                        TestContext.WriteLine(comment);
                        TestContext.WriteLine("Error: {0}", e.Message);
                        throw e;
                    }
                }
            }
        }

        [TestMethod]
        [DeploymentItem("TestFiles")]
        public void Test()
        {
            try
            {
                RunJsonPathTests(@".\TestFiles\dot-notation.json");
                RunJsonPathTests(@".\TestFiles\filters.json");
                //RunJsonPathTests(@".\TestFiles\functions.json");              
                RunJsonPathTests(@".\TestFiles\identifiers.json");
                RunJsonPathTests(@".\TestFiles\indices.json");
                RunJsonPathTests(@".\TestFiles\regex.json");
                RunJsonPathTests(@".\TestFiles\slice.json");
                RunJsonPathTests(@".\TestFiles\syntax.json");
                RunJsonPathTests(@".\TestFiles\union.json");
                RunJsonPathTests(@".\TestFiles\wildcard.json");
                RunJsonPathTests(@".\TestFiles\parent-operator.json");
                
                //RunJsonPathTests(@".\TestFiles\test.json");              
            }
            catch (Exception e)
            {
                TestContext.WriteLine("Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
