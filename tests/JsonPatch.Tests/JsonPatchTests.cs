using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPointerLib;
using JsonCons.JsonLib;

namespace JsonPatchLib.Tests
{
    [TestClass]
    public class JsonPointerTests
    {
        [TestMethod]
        public void TestAdd()
        {
            using var doc = JsonDocument.Parse(@"{""foo"": ""bar""}");
        }
    }
}
