using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonHelpersLib;
using JsonCons.JsonPatchLib;

namespace JsonCons.JsonPatchLib.Tests
{
    [TestClass]
    public class JsonPatchTests
    {
        static JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

        public void RunJsonPatchTests(string path)
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

                    try
                    {
                        JsonElement patch;
                        Assert.IsTrue(testCase.TryGetProperty("patch", out patch) && patch.ValueKind == JsonValueKind.Array);
                        JsonElement expected;
                        if (testCase.TryGetProperty("error", out expected))
                        {
                           // Assert.ThrowsException<JsonPatchParseException>(() => JsonPatch.Parse(exprElement.ToString()));
                        }
                        else if (testCase.TryGetProperty("result", out expected))
                        {
                            JsonDocument result = JsonPatch.ApplyPatch(given, patch);
                            Assert.IsTrue(comparer.Equals(result.RootElement,expected));
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
        [DeploymentItem("TestFiles")]
        public void Test()
        {
            try
            {
                RunJsonPatchTests(@".\TestFiles\rfc6902-examples.json");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
