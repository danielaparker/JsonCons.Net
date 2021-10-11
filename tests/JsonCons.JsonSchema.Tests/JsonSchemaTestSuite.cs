using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;
using JsonCons.JsonSchema;

namespace JsonCons.JsonSchema.Tests
{
    [TestClass]
    public class JsonPathTests
    {
        public void RunJsonSchemaTests(string path)
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
                JsonElement schema = testGroup.GetProperty("schema");
                var validator = JsonValidator.Create(schema);

                var testCases = testGroup.GetProperty("tests");
                var testCasesEnumeratable = testCases.EnumerateArray();
                foreach (var testCase in testCasesEnumeratable)
                {
                    string description;
                    JsonElement commentElement;
                    if (testCase.TryGetProperty("description", out commentElement) && commentElement.ValueKind == JsonValueKind.String)
                    {
                        description = commentElement.GetString();
                    }
                    else
                    {
                        description = "";
                    }

                    var dataElement = testCase.GetProperty("data");

                    try
                    {
                        JsonElement expectedElement;
                        if (testCase.TryGetProperty("valid", out expectedElement))
                        {
                            bool expected = expectedElement.ValueKind == JsonValueKind.False ? false : true;
                            bool result = validator.TryValidate(dataElement);
                            Assert.AreEqual(expected, result);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("File: {0}", path);
                        Debug.WriteLine(description);
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

                //RunJsonSchemaTests(@".\test_files\draft7\propertyNames.json");
                RunJsonSchemaTests(@".\test_files\draft7\additionalItems.json");
                /*RunJsonSchemaTests(@".\test_files\draft7\additionalProperties.json");
                RunJsonSchemaTests(@".\test_files\draft7\allOf.json");
                RunJsonSchemaTests(@".\test_files\draft7\anyOf.json");
                RunJsonSchemaTests(@".\test_files\draft7\boolean_schema.json");
                RunJsonSchemaTests(@".\test_files\draft7\const.json");
                RunJsonSchemaTests(@".\test_files\draft7\contains.json");
                RunJsonSchemaTests(@".\test_files\draft7\default.json");
                RunJsonSchemaTests(@".\test_files\draft7\definitions.json"); 

                RunJsonSchemaTests(@".\test_files\draft7\dependencies.json");

                RunJsonSchemaTests(@".\test_files\draft7\enum.json");
                RunJsonSchemaTests(@".\test_files\draft7\exclusiveMaximum.json");
                RunJsonSchemaTests(@".\test_files\draft7\exclusiveMinimum.json");

                RunJsonSchemaTests(@".\test_files\draft7\format.json");
                RunJsonSchemaTests(@".\test_files\draft7\if-then-else.json");
                RunJsonSchemaTests(@".\test_files\draft7\items.json");
                RunJsonSchemaTests(@".\test_files\draft7\maximum.json");
                RunJsonSchemaTests(@".\test_files\draft7\maxItems.json");
                RunJsonSchemaTests(@".\test_files\draft7\maxLength.json");
                RunJsonSchemaTests(@".\test_files\draft7\maxProperties.json");

                RunJsonSchemaTests(@".\test_files\draft7\minimum.json");
                RunJsonSchemaTests(@".\test_files\draft7\minItems.json");
                RunJsonSchemaTests(@".\test_files\draft7\minLength.json");
                RunJsonSchemaTests(@".\test_files\draft7\minProperties.json");
                RunJsonSchemaTests(@".\test_files\draft7\multipleOf.json");
                RunJsonSchemaTests(@".\test_files\draft7\not.json");
                RunJsonSchemaTests(@".\test_files\draft7\oneOf.json");
                RunJsonSchemaTests(@".\test_files\draft7\pattern.json");
                RunJsonSchemaTests(@".\test_files\draft7\patternProperties.json");
                RunJsonSchemaTests(@".\test_files\draft7\properties.json");

                RunJsonSchemaTests(@".\test_files\draft7\ref.json"); // *
                RunJsonSchemaTests(@".\test_files\draft7\refRemote.json");

                RunJsonSchemaTests(@".\test_files\draft7\required.json");

                RunJsonSchemaTests(@".\test_files\draft7\type.json");

                RunJsonSchemaTests(@".\test_files\draft7\uniqueItems.json"); 

                // format tests
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\date.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\date-time.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\ecmascript-regex.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\email.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\hostname.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\idn-email.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\idn-hostname.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\ipv4.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\ipv6.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\iri.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\iri-reference.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\json-pointer.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\regex.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\relative-json-pointer.json");
                RunJsonSchemaTests(@".\test_files\draft7\optional\format\time.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\uri.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\uri-reference.json");
                //RunJsonSchemaTests(@".\test_files\draft7\optional\format\uri-template.json");

                RunJsonSchemaTests(@".\test_files\draft7\optional\content.json");
                */
            }
            catch (Exception e)
            {
                Debug.WriteLine(@"Error: {0}", e.Message);
                throw e;
            }
        }
    }
}
