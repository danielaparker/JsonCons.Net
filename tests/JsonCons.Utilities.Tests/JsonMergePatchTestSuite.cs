using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class JsonMergePatchTestSuite
    {
        static JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

        public void RunJsonMergePatchTests(string path)
        {
            var serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
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

                    try
                    {
                        JsonElement patch;
                        Assert.IsTrue(testCase.TryGetProperty("patch", out patch));
                        JsonElement expected;
                        if (testCase.TryGetProperty("result", out expected))
                        {
                            using JsonDocument result = JsonMergePatch.ApplyMergePatch(given, patch);
                            Assert.IsTrue(comparer.Equals(result.RootElement,expected));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("File: {0}", path);
                        Debug.WriteLine("Error: {0}", e.Message);
                        if (comment.Length > 0)
                        {
                            Debug.WriteLine($"Comment: {comment}");
                        }
                        Console.WriteLine($"{JsonSerializer.Serialize(given, serializerOptions)}\n");
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
                RunJsonMergePatchTests(@".\TestFiles\rfc7396-test-cases.json");
                //RunJsonMergePatchTests(@".\TestFiles\test.json");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
