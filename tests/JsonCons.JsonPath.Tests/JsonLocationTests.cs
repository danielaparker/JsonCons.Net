using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPath;

namespace JsonPath.Tests
{
    [TestClass]
    public class JsonLocationNodeTests
    {
        [TestMethod]
        public void TestJsonLocationEquals()
        {
            JsonLocationNode node1 = JsonLocationNode.Root;
            JsonLocationNode node2 = new JsonLocationNode(node1,"foo");
            JsonLocationNode node3 = new JsonLocationNode(node2,"bar");
            JsonLocationNode node4 = new JsonLocationNode(node3,0);

            JsonLocationNode node11 = JsonLocationNode.Root;
            JsonLocationNode node12 = new JsonLocationNode(node11,"foo");
            JsonLocationNode node13 = new JsonLocationNode(node12,"bar");
            JsonLocationNode node14 = new JsonLocationNode(node13,0);


            JsonLocation path1 = new JsonLocation(node4);
            JsonLocation path2 = new JsonLocation(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [TestMethod]
        public void TestJsonLocationToString()
        {
            JsonLocationNode node1 = JsonLocationNode.Root;
            JsonLocationNode node2 = new JsonLocationNode(node1,"foo");
            JsonLocationNode node3 = new JsonLocationNode(node2,"bar");
            JsonLocationNode node4 = new JsonLocationNode(node3,0);

            JsonLocation path1 = new JsonLocation(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }

        [TestMethod]
        public void TestJsonLocationWithSolidusToString()
        {
            JsonLocationNode node1 = JsonLocationNode.Root;
            JsonLocationNode node2 = new JsonLocationNode(node1,"foo's");
            JsonLocationNode node3 = new JsonLocationNode(node2,"bar");
            JsonLocationNode node4 = new JsonLocationNode(node3,0);

            JsonLocation path = new JsonLocation(node4);
            Assert.IsTrue(path.ToString().Equals(@"$['foo\'s']['bar'][0]"));
        }

        [TestMethod]
        public void TestJsonLocationToJsonPointer()
        {
            JsonLocationNode node1 = JsonLocationNode.Root;
            JsonLocationNode node2 = new JsonLocationNode(node1,"a/b");

            JsonLocation path = new JsonLocation(node2);
            Assert.IsTrue(path.ToJsonPointer().Equals(@"/a~1b"));
        }
    }
}
