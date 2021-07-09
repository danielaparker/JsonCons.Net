using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPathLib;

namespace JsonPath.Tests
{
    [TestClass]
    public class PathNodeTests
    {
        [TestMethod]
        public void TestNormalizedPathEquals()
        {
            PathComponent node1 = new PathComponent("$");
            PathComponent node2 = new PathComponent(node1,"foo");
            PathComponent node3 = new PathComponent(node2,"bar");
            PathComponent node4 = new PathComponent(node3,0);

            PathComponent node11 = new PathComponent("$");
            PathComponent node12 = new PathComponent(node11,"foo");
            PathComponent node13 = new PathComponent(node12,"bar");
            PathComponent node14 = new PathComponent(node13,0);


            NormalizedPath path1 = new NormalizedPath(node4);
            NormalizedPath path2 = new NormalizedPath(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [TestMethod]
        public void TestNormalizedPathToString()
        {
            PathComponent node1 = new PathComponent("$");
            PathComponent node2 = new PathComponent(node1,"foo");
            PathComponent node3 = new PathComponent(node2,"bar");
            PathComponent node4 = new PathComponent(node3,0);

            NormalizedPath path1 = new NormalizedPath(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathWithSolidusToString()
        {
            PathComponent node1 = new PathComponent("$");
            PathComponent node2 = new PathComponent(node1,"foo's");
            PathComponent node3 = new PathComponent(node2,"bar");
            PathComponent node4 = new PathComponent(node3,0);

            NormalizedPath path = new NormalizedPath(node4);
            Assert.IsTrue(path.ToString().Equals(@"$['foo\'s']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathToJsonPointer()
        {
            PathComponent node1 = new PathComponent("$");
            PathComponent node2 = new PathComponent(node1,"a/b");

            NormalizedPath path = new NormalizedPath(node2);
            Assert.IsTrue(path.ToJsonPointer().Equals(@"/a~1b"));
        }
    }
}
