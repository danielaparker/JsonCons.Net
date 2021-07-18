using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPointerLib;
using JsonCons.JsonLib;

namespace JsonPointerLib.Tests
{
    [TestClass]
    public class JsonPointerTests
    {
        [TestMethod]
        public void GetWithRefTest()
        {
            using (var doc = JsonDocument.Parse(@"{""foo"": [""bar"", ""baz""]}"))
            {
                var root = doc.RootElement;
                JsonPointer pointer; 
                Assert.IsTrue(JsonPointer.TryParse("/foo/0", out pointer));
                JsonElement value;
                Assert.IsTrue(pointer.TryGet(root, out value));

                var comparer = JsonElementEqualityComparer.Instance;

                var expected = root.GetProperty("foo")[0];
                Assert.IsTrue(comparer.Equals(value, expected));
            }
        }
        [TestMethod]
        public void GetWithNonexistentTarget()
        {
            using (var doc = JsonDocument.Parse(@"{ ""foo"": ""bar"" }"))
            {
                JsonPointer pointer; 
                Assert.IsTrue(JsonPointer.TryParse("/baz", out pointer));
                JsonElement value;
                Assert.IsFalse(pointer.TryGet(doc.RootElement, out value));
            }
        }
    }
}
