using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class JsonPointerTests
    {
        [TestMethod]
        public void ToUriFragmentTest()
        {
            JsonPointer pointer;
            string s;

            Assert.IsTrue(JsonPointer.TryParse("", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#");

            Assert.IsTrue(JsonPointer.TryParse("/foo", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/foo");

            Assert.IsTrue(JsonPointer.TryParse("/foo/0", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/foo/0");

            Assert.IsTrue(JsonPointer.TryParse("/", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/");

            Assert.IsTrue(JsonPointer.TryParse("/a~1b", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/a~1b");

            Assert.IsTrue(JsonPointer.TryParse("/c%d", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/c%25d");

            Assert.IsTrue(JsonPointer.TryParse("/e^f", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/e%5Ef");

            Assert.IsTrue(JsonPointer.TryParse("/g|h", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/g%7Ch");

            Assert.IsTrue(JsonPointer.TryParse("/i\\j", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/i%5Cj");

            Assert.IsTrue(JsonPointer.TryParse("/k\"l", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/k%22l");

            Assert.IsTrue(JsonPointer.TryParse("/ ", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/%20");

            Assert.IsTrue(JsonPointer.TryParse("/m~0n", out pointer));
            s = pointer.ToUriFragment();
            Assert.IsTrue(s == "#/m~0n");
        }

        [TestMethod]
        public void GetWithRefTest()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": [""bar"", ""baz""]}");
            
            var root = doc.RootElement;
            JsonPointer pointer; 
            Assert.IsTrue(JsonPointer.TryParse("/foo/0", out pointer));
            JsonElement value;
            Assert.IsTrue(pointer.TryGet(root, out value));

            var comparer = JsonElementEqualityComparer.Instance;

            var expected = root.GetProperty("foo")[0];
            Assert.IsTrue(comparer.Equals(value, expected));
        }
        [TestMethod]
        public void GetWithNonexistentTarget()
        {
            using var doc = JsonDocument.Parse(@"{ ""foo"": ""bar"" }");
            
            JsonPointer pointer; 
            Assert.IsTrue(JsonPointer.TryParse("/baz", out pointer));
            JsonElement value;
            Assert.IsFalse(pointer.TryGet(doc.RootElement, out value));
        }
    }
}
