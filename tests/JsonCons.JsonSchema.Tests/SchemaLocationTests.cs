using System.Text.Json;
using JsonCons.JsonSchema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace JsonCons.JsonSchema.Tests
{
    [TestClass]
    public class SchemaLocationTests
	 {       
        [TestMethod]
        public void TestAbsoluteUriWithEmptyFragment()
        {
            var href = "http://json-schema.org/draft-07/schema#";
            var loc = new SchemaLocation(href);
            Assert.IsTrue(loc.IsAbsoluteUri);
            Assert.IsFalse(loc.HasIdentifier);
            Assert.IsTrue(loc.HasPointer);
            Assert.IsTrue(loc.ToString().Equals(href));
        }
        [TestMethod]
        public void TestAbsoluteUri()
        {
            var href = "http://json-schema.org/draft-07/schema";
            var loc = new SchemaLocation(href);
            Assert.IsTrue(loc.IsAbsoluteUri);
            Assert.IsFalse(loc.HasIdentifier);
            Assert.IsFalse(loc.HasPointer);
            Assert.IsTrue(loc.ToString().Equals(href));
        }
        [TestMethod]
        public void TestPointerToRoot()
        {
            var href = "#";
            var loc = new SchemaLocation(href);
            Assert.IsFalse(loc.IsAbsoluteUri);
            Assert.IsFalse(loc.HasIdentifier);
            Assert.IsTrue(loc.HasPointer);
            Assert.IsTrue(loc.ToString().Equals(href));
            Assert.IsTrue(loc.Pointer.ToUriFragment().Equals("#"));
        }
        [TestMethod]
        public void TestPointerToBar()
        {
            var href = "#/bar";
            var loc = new SchemaLocation(href);
            Assert.IsFalse(loc.IsAbsoluteUri);
            Assert.IsFalse(loc.HasIdentifier);
            Assert.IsTrue(loc.HasPointer);
            Assert.IsTrue(loc.ToString().Equals(href));
            Assert.IsTrue(loc.Pointer.ToUriFragment().Equals("#/bar"));
        }
        [TestMethod]
        public void TestIdentifier()
        {
            var href = "#foo";
            var loc = new SchemaLocation(href);
            Assert.IsFalse(loc.IsAbsoluteUri);
            Assert.IsTrue(loc.HasIdentifier);
            Assert.IsFalse(loc.HasPointer);
            Debug.WriteLine($"ToString: {loc}");
            Assert.IsTrue(loc.ToString().Equals(href));
            Assert.IsTrue(loc.Identifier.Equals("foo"));
        }
        [TestMethod]
        public void TestResolve()
        {
            var baseUriStr = "http://json-schema.org/draft-07/schema#";
            var relativeUriStr = "#";

            var baseUri = new SchemaLocation(baseUriStr);
            var relativeUri = new SchemaLocation(relativeUriStr);

            var resolvedUri = SchemaLocation.Resolve(baseUri, relativeUri);
            Assert.IsTrue(resolvedUri.ToString() == baseUriStr);
        } 
        
        [TestMethod]
        public void TestAppendNameToRelative()
        {
            var baseUriStr = "#";
            var name = "foo";

            var baseUri = new SchemaLocation(baseUriStr);

            var resolvedUri = SchemaLocation.Append(baseUri, name);

            Debug.WriteLine(resolvedUri.Pointer);
            Assert.IsTrue(resolvedUri.Pointer.ToUriFragment().Equals("#/foo"));
        }
        [TestMethod]
        public void TestAppendIndexToRelative()
        {
            var baseUriStr = "#";
            int index = 3;

            var baseUri = new SchemaLocation(baseUriStr);

            var resolvedUri = SchemaLocation.Append(baseUri, index);

            Debug.WriteLine(resolvedUri.Pointer);
            Assert.IsTrue(resolvedUri.Pointer.ToUriFragment().Equals("#/3"));
        }
    }
}
