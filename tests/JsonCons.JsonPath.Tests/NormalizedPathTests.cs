using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPath;

namespace JsonPath.Tests
{
    [TestClass]
    public class PathNodeTests
    {
        [TestMethod]
        public void TestNormalizedPathEquals()
        {
            NormalizedPathNode node1 = NormalizedPathNode.Root;
            NormalizedPathNode node2 = new NormalizedPathNode(node1,"foo");
            NormalizedPathNode node3 = new NormalizedPathNode(node2,"bar");
            NormalizedPathNode node4 = new NormalizedPathNode(node3,0);

            NormalizedPathNode node11 = NormalizedPathNode.Root;
            NormalizedPathNode node12 = new NormalizedPathNode(node11,"foo");
            NormalizedPathNode node13 = new NormalizedPathNode(node12,"bar");
            NormalizedPathNode node14 = new NormalizedPathNode(node13,0);


            NormalizedPath path1 = new NormalizedPath(node4);
            NormalizedPath path2 = new NormalizedPath(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [TestMethod]
        public void TestNormalizedPathToString()
        {
            NormalizedPathNode node1 = NormalizedPathNode.Root;
            NormalizedPathNode node2 = new NormalizedPathNode(node1,"foo");
            NormalizedPathNode node3 = new NormalizedPathNode(node2,"bar");
            NormalizedPathNode node4 = new NormalizedPathNode(node3,0);

            NormalizedPath path1 = new NormalizedPath(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathWithSolidusToString()
        {
            NormalizedPathNode node1 = NormalizedPathNode.Root;
            NormalizedPathNode node2 = new NormalizedPathNode(node1,"foo's");
            NormalizedPathNode node3 = new NormalizedPathNode(node2,"bar");
            NormalizedPathNode node4 = new NormalizedPathNode(node3,0);

            NormalizedPath path = new NormalizedPath(node4);
            Assert.IsTrue(path.ToString().Equals(@"$['foo\'s']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathToJsonPointer()
        {
            NormalizedPathNode node1 = NormalizedPathNode.Root;
            NormalizedPathNode node2 = new NormalizedPathNode(node1,"a/b");

            NormalizedPath path = new NormalizedPath(node2);
            Assert.IsTrue(path.ToJsonPointer().Equals(@"/a~1b"));
        }
    }
}
