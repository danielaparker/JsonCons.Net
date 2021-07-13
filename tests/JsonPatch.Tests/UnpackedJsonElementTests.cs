using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPatchLib;
using JsonCons.JsonPointerLib;
using JsonCons.JsonLib;

namespace JsonPatchLib.Tests
{
    [TestClass]
    public class UnpackedJsonElementTests
    {
        [TestMethod]
        public void Test1()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": ""bar""}");

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);
            var result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(doc.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void Test2()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": [""bar"", ""baz""]}");
            using var expected = JsonDocument.Parse(@"""bar""");

            var root = doc.RootElement;
            var unpacked = UnpackedJsonElement.Unpack(root);

            var pointer = JsonPointer.Parse("/foo/0");

            UnpackedJsonElement value;
            Assert.IsTrue(pointer.TryGet(unpacked, out value));
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

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);
            var unpackedValue = UnpackedJsonElement.Unpack(value.RootElement);

            var location = JsonPointer.Parse(@"/baz");

            Assert.IsTrue(location.TryAdd(ref unpacked, unpackedValue));
            JsonDocument result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestAddElementToArray()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""baz"" ] }");
            using var value = JsonDocument.Parse(@"""qux""");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }");

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);
            var unpackedValue = UnpackedJsonElement.Unpack(value.RootElement);

            var location = JsonPointer.Parse(@"/foo/1");

            Assert.IsTrue(location.TryAdd(ref unpacked, unpackedValue));
            JsonDocument result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestAddElementToArrayEnd()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [""bar""] }");
            using var value = JsonDocument.Parse(@"""qux""");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [""bar"", [""abc"", ""def""]] }");

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);
            var unpackedValue = UnpackedJsonElement.Unpack(value.RootElement);

            var location = JsonPointer.Parse(@"/foo/-");

            Assert.IsTrue(location.TryAdd(ref unpacked, unpackedValue));
            JsonDocument result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestRemovePropertyFromObject()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": ""bar"", ""baz"" : ""qux""}");

            using var expected = JsonDocument.Parse(@"{ ""foo"": ""bar""}");

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);

            var location = JsonPointer.Parse(@"/baz");

            Assert.IsTrue(location.TryRemove(ref unpacked));
            JsonDocument result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }

        [TestMethod]
        public void TestRemoveArrayElement()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }");

            using var expected = JsonDocument.Parse(@"{ ""foo"": [ ""bar"", ""baz"" ] }");

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);

            var location = JsonPointer.Parse(@"/foo/1");

            Assert.IsTrue(location.TryRemove(ref unpacked));
            JsonDocument result = unpacked.ToJsonDocument();

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

            var unpacked = UnpackedJsonElement.Unpack(doc.RootElement);
            var unpackedValue = UnpackedJsonElement.Unpack(value.RootElement);

            var location = JsonPointer.Parse(@"/baz");

            Assert.IsTrue(location.TryAdd(ref unpacked, unpackedValue));
            JsonDocument result = unpacked.ToJsonDocument();

            JsonElementEqualityComparer.Instance.Equals(expected.RootElement,
                                                        result.RootElement);
        }
    }
}
