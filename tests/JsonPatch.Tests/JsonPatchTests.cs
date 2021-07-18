using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPatchLib;
using JsonCons.JsonLib;

namespace JsonPatchLib.Tests
{
    [TestClass]
    public class JsonPatchTests
    {
        static JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

        [TestMethod]
        public void TestTest()
        {
            using var doc = JsonDocument.Parse(@"
{
    ""baz"": ""qux"",
    ""foo"": [ ""a"", 2, ""c"" ]
}
            ");
            using var patch = JsonDocument.Parse(@"
[
   { ""op"": ""test"", ""path"": ""/baz"", ""value"": ""qux"" },
   { ""op"": ""test"", ""path"": ""/foo/1"", ""value"": 2 }
]
            ");

            var result = JsonPatch.ApplyPatch(doc.RootElement, patch.RootElement);
            Assert.IsTrue(comparer.Equals(doc.RootElement,result.RootElement));

        }

        [TestMethod]
        public void TestAdd()
        {
            using var doc = JsonDocument.Parse(@"
    { ""foo"": ""bar""}
            ");
            using var patch = JsonDocument.Parse(@"
    [
        { ""op"": ""add"", ""path"": ""/baz"", ""value"": ""qux"" },
        { ""op"": ""add"", ""path"": ""/foo"", ""value"": [ ""bar"", ""baz"" ] }
    ]
            ");
            using var expected = JsonDocument.Parse(@"
    { ""baz"":""qux"", ""foo"": [ ""bar"", ""baz"" ]}
            ");

            var result = JsonPatch.ApplyPatch(doc.RootElement, patch.RootElement);
            Assert.IsTrue(comparer.Equals(expected.RootElement,result.RootElement));

        }

        [TestMethod]
        public void TestDiff()
        {
            using var source = JsonDocument.Parse(@"
{""/"": 9, ""~1"": 10, ""foo"": ""bar""}
            ");
            using var target = JsonDocument.Parse(@"
{ ""baz"":""qux"", ""foo"": [ ""bar"", ""baz"" ]}
            ");

            var patch = JsonPatch.FromDiff(source.RootElement, target.RootElement);

            var result = JsonPatch.ApplyPatch(source.RootElement, patch.RootElement);

            Assert.IsTrue(comparer.Equals(target.RootElement,result.RootElement));
        }

        [TestMethod]
        public void TestDiff2()
        {
            using var source = JsonDocument.Parse(@"
{ 
    ""/"": 3,
    ""foo"": ""bar""
}
            ");
            using var target = JsonDocument.Parse(@"
{
    ""/"": 9,
    ""~1"": 10
}
            ");

            var patch = JsonPatch.FromDiff(source.RootElement, target.RootElement);

            var result = JsonPatch.ApplyPatch(source.RootElement, patch.RootElement);

            Assert.IsTrue(comparer.Equals(target.RootElement,result.RootElement));
        }

        [TestMethod]
        public void TestDiffWithAddedItemsInTarget()
        {
            using var source = JsonDocument.Parse(@"
{""/"": 9, ""foo"": [ ""bar""]}
            ");
            using var target = JsonDocument.Parse(@"
{ ""baz"":""qux"", ""foo"": [ ""bar"", ""baz"" ]}
            ");

            var patch = JsonPatch.FromDiff(source.RootElement, target.RootElement);

            var result = JsonPatch.ApplyPatch(source.RootElement, patch.RootElement);

            Assert.IsTrue(comparer.Equals(target.RootElement,result.RootElement));
        }

        [TestMethod]
        public void TestDiffWithAddedItemsInTarget2()
        {
            using var source = JsonDocument.Parse(@"
{""/"": 9, ""foo"": [ ""bar"", ""bar""]}
            ");
            using var target = JsonDocument.Parse(@"
{ ""baz"":""qux"", ""foo"": [ ""bar"", ""baz"" ]}
            ");

            var patch = JsonPatch.FromDiff(source.RootElement, target.RootElement);

            var result = JsonPatch.ApplyPatch(source.RootElement, patch.RootElement);

            Debug.WriteLine($"source: {source.RootElement}");
            Debug.WriteLine($"target: {target.RootElement}");
            Debug.WriteLine($"patch: {patch.RootElement}");
            Debug.WriteLine($"result: {result.RootElement}");

            Assert.IsTrue(comparer.Equals(target.RootElement,result.RootElement));
        }

    }
}
