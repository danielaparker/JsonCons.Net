using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class JsonMergeTests
    {
        static JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

        [TestMethod]
        public void TestMergePatch()
        {
            using var doc = JsonDocument.Parse(@"
{
         ""a"": ""b"",
         ""c"": {
       ""d"": ""e"",
       ""f"": ""g""
         }
}
            ");
            using var patch = JsonDocument.Parse(@"
{
         ""a"":""z"",
         ""c"": {
       ""f"": null
         }
}            
            ");

            using var expected = JsonDocument.Parse(@"
{
         ""a"": ""z"",
         ""c"": {
       ""d"": ""e""
         }
}
            ");

            using JsonDocument result = JsonMergePatch.ApplyMergePatch(doc.RootElement, patch.RootElement);
            Assert.IsTrue(comparer.Equals(result.RootElement,expected.RootElement));
        }

        [TestMethod]
        public void TestMergePatch2()
        {
            using var doc = JsonDocument.Parse(@"
    {
             ""title"": ""Goodbye!"",
             ""author"" : {
           ""givenName"" : ""John"",
           ""familyName"" : ""Doe""
             },
             ""tags"":[ ""example"", ""sample"" ],
             ""content"": ""This will be unchanged""
    }
            ");
            using var patch = JsonDocument.Parse(@"
    {
             ""title"": ""Hello!"",
             ""phoneNumber"": ""+01-123-456-7890"",
             ""author"": {
           ""familyName"": null
             },
             ""tags"": [ ""example"" ]
    }
                ");

            using var expected = JsonDocument.Parse(@"
    {
             ""title"": ""Hello!"",
             ""author"" : {
           ""givenName"" : ""John""
             },
             ""tags"": [ ""example"" ],
             ""content"": ""This will be unchanged"",
             ""phoneNumber"": ""+01-123-456-7890""
    }
            ");

            using JsonDocument result = JsonMergePatch.ApplyMergePatch(doc.RootElement, patch.RootElement);
            Assert.IsTrue(comparer.Equals(result.RootElement,expected.RootElement));
        }
    }
}
