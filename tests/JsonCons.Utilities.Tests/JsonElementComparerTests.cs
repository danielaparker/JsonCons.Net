using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Tests
{
    [TestClass]
    public class JsonElementComparerTests
    {
        [TestMethod]
        public void CompareUnequalKindTest()
        {
            using var doc1 = JsonDocument.Parse(@"
    {
       ""foo"": ""bar""
    }
            ");

            using var doc2 = JsonDocument.Parse(@"
       [""bar"", ""baz""]
            ");

            using var doc3 = JsonDocument.Parse(@"""foo""");

            using var doc4 = JsonDocument.Parse(@"10.123456789");

            using var doc5 = JsonDocument.Parse(@"true");

            using var doc6 = JsonDocument.Parse(@"false");

            using var doc7 = JsonDocument.Parse(@"null");

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc1.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc2.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc3.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc4.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc5.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc6.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc2.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc3.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc4.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc5.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc6.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc3.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc4.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc5.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc6.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc3.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc4.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc5.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc6.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc4.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc3.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc4.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc5.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc6.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc5.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc3.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc4.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc5.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc6.RootElement) == 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc6.RootElement,doc7.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc3.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc4.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc5.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc6.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc7.RootElement,doc7.RootElement) == 0);
        }

        [TestMethod]
        public void CompareObjectTest()
        {
            using var doc1 = JsonDocument.Parse(@"
    {
       ""foo"": ""abc"",
       ""bar"" : 10.123456789
    }
            ");

            using var doc2 = JsonDocument.Parse(@"
    {
       ""bar"" : 10.123456789,
       ""foo"": ""abc"",
       ""baz"" : ""abc""
    }
            ");

            using var doc3 = JsonDocument.Parse(@"
    {
       ""bar"" : 10.123456789,
       ""foo"": ""abc"",
       ""g"" : ""abc""
    }
            ");

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc2.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc1.RootElement) < 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc3.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc1.RootElement) > 0);
        }

        [TestMethod]
        public void CompareArrayTest()
        {
            using var doc1 = JsonDocument.Parse(@"[""foo"", ""bar"",""baz""]");

            using var doc2 = JsonDocument.Parse(@"[""foo"", ""bar"",""baz"", ""qux""]");

            using var doc3 = JsonDocument.Parse(@"[""foo"", ""baz"",""bar""]");

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc2.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc1.RootElement) > 0);

            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc1.RootElement,doc3.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc2.RootElement,doc3.RootElement) < 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc1.RootElement) > 0);
            Assert.IsTrue(JsonElementComparer.Instance.Compare(doc3.RootElement,doc2.RootElement) > 0);
        }
    }
}
