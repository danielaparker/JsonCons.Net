using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;
using JsonCons.JsonPath;

namespace JsonCons.JsonPath.Tests
{
    [TestClass]
    public class JsonPathTests
    {
        public void RunJsonPathTests(string path)
        {
            Debug.WriteLine($"Test {path}");
            string text = System.IO.File.ReadAllText(path);
            var jsonOptions = new JsonDocumentOptions();
            jsonOptions.CommentHandling = JsonCommentHandling.Skip; 
            using JsonDocument doc = JsonDocument.Parse(text, jsonOptions);
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

                    var options = new JsonSelectorOptions();

                    JsonElement element;
                    if (testCase.TryGetProperty("nodups", out element) && element.ValueKind == JsonValueKind.True)
                    {
                        options.NoDuplicates = true;
                    }
                    if (testCase.TryGetProperty("sort", out element) && element.ValueKind == JsonValueKind.True)
                    {
                        options.Sort = true;
                    }

                    var exprElement = testCase.GetProperty("expression");

                    try
                    {
                        JsonElement expected;
                        if (testCase.TryGetProperty("error", out expected))
                        {
                            Assert.ThrowsException<JsonPathParseException>(() => JsonSelector.Parse(exprElement.ToString()));
                        }
                        else if (testCase.TryGetProperty("result", out expected))
                        {
                            var expr = JsonSelector.Parse(exprElement.ToString());
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
                                Debug.WriteLine("File: {0}", path);
                                Debug.WriteLine(comment);
                                Debug.WriteLine("Document: " + given.ToString());
                                Debug.WriteLine("Path: " + exprElement.ToString());
                                if (options.NoDuplicates)
                                {
                                    Debug.WriteLine("nodups");
                                }
                                if (options.Sort)
                                {
                                    Debug.WriteLine("sort");
                                }
                                Debug.WriteLine("Expected: " + expected.ToString());
                                Debug.WriteLine("Results: ");
                                foreach (var item in items)
                                {
                                    Debug.WriteLine(item.ToString());
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
                        Debug.WriteLine("File: {0}", path);
                        Debug.WriteLine(comment);
                        Debug.WriteLine("Error: {0}", e.Message);
                        throw e;
                    }
                }
            }
        }

        [TestMethod]
        [DeploymentItem("test_files")]
        public void Test()
        {
            try
            {
                /*RunJsonPathTests(@".\test_files\dot-notation.json");
                RunJsonPathTests(@".\test_files\filters.json");
                RunJsonPathTests(@".\test_files\functions.json");              
                RunJsonPathTests(@".\test_files\identifiers.json");
                RunJsonPathTests(@".\test_files\indices.json");
                RunJsonPathTests(@".\test_files\regex.json");
                RunJsonPathTests(@".\test_files\slice.json");*/
                //RunJsonPathTests(@".\test_files\syntax.json");
                /*RunJsonPathTests(@".\test_files\union.json");
                RunJsonPathTests(@".\test_files\wildcard.json");
                RunJsonPathTests(@".\test_files\parent-operator.json");
                RunJsonPathTests(@".\test_files\recursive-descent.json");*/
                
                RunJsonPathTests(@".\test_files\test.json");              
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
