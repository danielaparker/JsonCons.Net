using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;
using JsonCons.JmesPath;

namespace JsonCons.JmesPath.Tests
{
    [TestClass]
    public class JmesPathTests
    {
        public void RunJmesPathTests(string path)
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

                    var exprElement = testCase.GetProperty("expression");

                    try
                    {
                        JsonElement expected;
                        if (testCase.TryGetProperty("error", out expected))
                        {
                            Assert.ThrowsException<JmesPathParseException>(() => JsonSearcher.Parse(exprElement.ToString()));
                        }
                        else if (testCase.TryGetProperty("result", out expected))
                        {
                            var expr = JsonSearcher.Parse(exprElement.ToString());
                            using JsonDocument result = expr.Search(given);
                            bool success = comparer.Equals(result.RootElement, expected);
                            if (!success)
                            {
                                Debug.WriteLine("File: {0}", path);
                                Debug.WriteLine(comment);
                                Debug.WriteLine($"Document: {given}");
                                Debug.WriteLine($"Path: {exprElement}");
                                Debug.WriteLine($"Expected: {JsonSerializer.Serialize(expected)}");
                                Debug.WriteLine($"Result: {JsonSerializer.Serialize(result)}");
                            }
                            Assert.IsTrue(comparer.Equals(result.RootElement,expected));
                            
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("File: {0}", path);
                        Debug.WriteLine($"Document: {given}");
                        Debug.WriteLine($"Path: {exprElement}");
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
/*                RunJmesPathTests(@".\test_files\basic.json");
                RunJmesPathTests(@".\test_files\benchmarks.json");
                RunJmesPathTests(@".\test_files\boolean.json");
                RunJmesPathTests(@".\test_files\current.json");
                RunJmesPathTests(@".\test_files\escape.json");
                RunJmesPathTests(@".\test_files\filters.json");
                RunJmesPathTests(@".\test_files\identifiers.json");
                RunJmesPathTests(@".\test_files\indices.json");
                RunJmesPathTests(@".\test_files\literal.json");
                RunJmesPathTests(@".\test_files\multiselect.json");
                RunJmesPathTests(@".\test_files\pipe.json");
                RunJmesPathTests(@".\test_files\slice.json");
                RunJmesPathTests(@".\test_files\unicode.json");
                RunJmesPathTests(@".\test_files\syntax.json");
                RunJmesPathTests(@".\test_files\wildcard.json");
*/
                RunJmesPathTests(@".\test_files\functions.json");              
                
               //RunJmesPathTests(@".\test_files\test.json");              
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
