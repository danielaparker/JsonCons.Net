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
                var pointer = JsonPointer.Parse("/foo/0");
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
                var pointer = JsonPointer.Parse("/baz");
                JsonElement value;
                Assert.IsFalse(pointer.TryGet(doc.RootElement, out value));
            }
        }
    }
}
