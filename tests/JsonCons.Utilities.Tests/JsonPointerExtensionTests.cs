using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class UnpackedJsonElementTests
    {
        [TestMethod]
        public void Test1()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": ""bar""}");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);
            var result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(doc.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void Test2()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": [""bar"", ""baz""]}");
            using var expected = JsonDocument.Parse(@"""bar""");

            var root = doc.RootElement;
            var documentBuilder = new JsonDocumentBuilder(root);

            JsonPointer pointer;
            Assert.IsTrue(JsonPointer.TryParse("/foo/0", out pointer));

            JsonDocumentBuilder value;
            Assert.IsTrue(pointer.TryGet(documentBuilder, out value));
            var result = value.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestAddObjectMember()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": ""bar""}");
            using var value = JsonDocument.Parse(@"""qux""");

            using var expected = JsonDocument.Parse(@"{ ""foo"": ""bar"", ""baz"" : ""qux""}");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);
            var valueBuilder = new JsonDocumentBuilder(value.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/baz", out location));

            Assert.IsTrue(location.TryAdd(ref documentBuilder, valueBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestAddElementToArray()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""baz"" ] }");
            using var value = JsonDocument.Parse(@"""qux""");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);
            var valueBuilder = new JsonDocumentBuilder(value.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/foo/1", out location));

            Assert.IsTrue(location.TryAdd(ref documentBuilder, valueBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestAddElementToArrayEnd()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [""bar""] }");
            using var value = JsonDocument.Parse(@"""qux""");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [""bar"", [""abc"", ""def""]] }");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);
            var valueBuilder = new JsonDocumentBuilder(value.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/foo/-", out location));

            Assert.IsTrue(location.TryAdd(ref documentBuilder, valueBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestRemovePropertyFromObject()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": ""bar"", ""baz"" : ""qux""}");

            using var expected = JsonDocument.Parse(@"{ ""foo"": ""bar""}");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/baz", out location));

            Assert.IsTrue(location.TryRemove(ref documentBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestRemoveArrayElement()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""baz"" ] }");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/foo/1", out location));

            Assert.IsTrue(location.TryRemove(ref documentBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestReplaceObjectValue()
        {
            using var doc = JsonDocument.Parse(@"{
              ""baz"": ""qux"",
              ""foo"": ""bar""
            }");
            using var value = JsonDocument.Parse(@"""boo""");

            using var expected = JsonDocument.Parse(@"
            {
              ""baz"": ""boo"",
              ""foo"": ""bar""
            }
            ");

            var documentBuilder = new JsonDocumentBuilder(doc.RootElement);
            var valueBuilder = new JsonDocumentBuilder(value.RootElement);

            JsonPointer location;
            Assert.IsTrue(JsonPointer.TryParse(@"/baz", out location));

            Assert.IsTrue(location.TryAdd(ref documentBuilder, valueBuilder));
            JsonDocument result = documentBuilder.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }
    }
}
