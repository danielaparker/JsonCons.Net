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
            PathNode node1 = PathNode.Root;
            PathNode node2 = new PathNode(node1,"foo");
            PathNode node3 = new PathNode(node2,"bar");
            PathNode node4 = new PathNode(node3,0);

            PathNode node11 = PathNode.Root;
            PathNode node12 = new PathNode(node11,"foo");
            PathNode node13 = new PathNode(node12,"bar");
            PathNode node14 = new PathNode(node13,0);


            NormalizedPath path1 = new NormalizedPath(node4);
            NormalizedPath path2 = new NormalizedPath(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [TestMethod]
        public void TestNormalizedPathToString()
        {
            PathNode node1 = PathNode.Root;
            PathNode node2 = new PathNode(node1,"foo");
            PathNode node3 = new PathNode(node2,"bar");
            PathNode node4 = new PathNode(node3,0);

            NormalizedPath path1 = new NormalizedPath(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathWithSolidusToString()
        {
            PathNode node1 = PathNode.Root;
            PathNode node2 = new PathNode(node1,"foo's");
            PathNode node3 = new PathNode(node2,"bar");
            PathNode node4 = new PathNode(node3,0);

            NormalizedPath path = new NormalizedPath(node4);
            Assert.IsTrue(path.ToString().Equals(@"$['foo\'s']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathToJsonPointer()
        {
            PathNode node1 = PathNode.Root;
            PathNode node2 = new PathNode(node1,"a/b");

            NormalizedPath path = new NormalizedPath(node2);
            Assert.IsTrue(path.ToJsonPointer().Equals(@"/a~1b"));
        }
    }
}
