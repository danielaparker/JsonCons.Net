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
            string fragment;
            string location;

            location = "";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/foo";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/foo");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/foo/0";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/foo/0");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/a~1b";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            //Debug.WriteLine($"/a~1b");
            Assert.IsTrue(fragment == "#/a~1b");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/c%d";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/c%25d");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/e^f";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/e%5Ef");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/g|h";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/g%7Ch");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/i\\j";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/i%5Cj");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/k\"l";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/k%22l");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/ ";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            Assert.IsTrue(fragment == "#/%20");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));

            location = "/m~0n";
            Assert.IsTrue(JsonPointer.TryParse(location, out pointer));
            fragment = pointer.ToUriFragment();
            //Debug.WriteLine($"/m~0n {fragment}");
            Assert.IsTrue(fragment == "#/m~0n");
            Assert.IsTrue(JsonPointer.TryParse(fragment, out pointer));
            Assert.IsTrue(pointer.ToString().Equals(location));
        }

        [TestMethod]
        public void GetWithRefTest()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": [""bar"", ""baz""]}");
            
            var root = doc.RootElement;
            JsonPointer pointer; 
            Assert.IsTrue(JsonPointer.TryParse("/foo/0", out pointer));
            JsonElement value;
            Assert.IsTrue(pointer.TryGetValue(root, out value));

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
            Assert.IsFalse(pointer.TryGetValue(doc.RootElement, out value));
        }
    }
}
