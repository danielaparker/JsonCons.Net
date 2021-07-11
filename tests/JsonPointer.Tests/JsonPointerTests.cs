using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPointerLib;

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
                var pointer = JsonPointer.Parse("/foo/0");
                JsonElement value;
                Assert.IsTrue(pointer.TryGet(doc.RootElement, out value));
            }
        }
    }
}
