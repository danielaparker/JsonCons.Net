using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class JsonFlattenerTests
    {
        [TestMethod]
        public void FlattenTest()
        {
            using var doc = JsonDocument.Parse(@"
{
   ""application"": ""hiking"",
   ""reputons"": [
       {
           ""rater"": ""HikingAsylum"",
           ""assertion"": ""advanced"",
           ""rated"": ""Marilyn C"",
           ""rating"": 0.90
        },
        {
           ""rater"": ""HikingAsylum"",
           ""assertion"": ""intermediate"",
           ""rated"": ""Hongmin"",
           ""rating"": 0.75
        }    
    ]
}
             ");

            using var expected = JsonDocument.Parse(@"
{
  ""/application"": ""hiking"",
  ""/reputons/0/rater"": ""HikingAsylum"",
  ""/reputons/0/assertion"": ""advanced"",
  ""/reputons/0/rated"": ""Marilyn C"",
  ""/reputons/0/rating"": 0.90,
  ""/reputons/1/rater"": ""HikingAsylum"",
  ""/reputons/1/assertion"": ""intermediate"",
  ""/reputons/1/rated"": ""Hongmin"",
  ""/reputons/1/rating"": 0.75
}
            ");

            JsonDocument flattenedDoc = JsonFlattener.Flatten(doc.RootElement);
            Assert.IsTrue(JsonElementEqualityComparer.Instance.Equals(flattenedDoc.RootElement,expected.RootElement));

            JsonDocument unflattenedDoc = JsonFlattener.Unflatten(flattenedDoc.RootElement);

            var options = new JsonSerializerOptions() { WriteIndented = true };

            Assert.IsTrue(JsonElementEqualityComparer.Instance.Equals(unflattenedDoc.RootElement,doc.RootElement));
        }
    }
}
