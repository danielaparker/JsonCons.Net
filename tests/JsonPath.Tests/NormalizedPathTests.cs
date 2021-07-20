using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.JsonPathLib;

namespace JsonPathLib.Tests
{
    [TestClass]
    public class PathNodeTests
    {
        [TestMethod]
        public void TestNormalizedPathEquals()
        {
            PathLink node1 = PathLink.Root;
            PathLink node2 = new PathLink(node1,"foo");
            PathLink node3 = new PathLink(node2,"bar");
            PathLink node4 = new PathLink(node3,0);

            PathLink node11 = PathLink.Root;
            PathLink node12 = new PathLink(node11,"foo");
            PathLink node13 = new PathLink(node12,"bar");
            PathLink node14 = new PathLink(node13,0);


            NormalizedPath path1 = new NormalizedPath(node4);
            NormalizedPath path2 = new NormalizedPath(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [TestMethod]
        public void TestNormalizedPathToString()
        {
            PathLink node1 = PathLink.Root;
            PathLink node2 = new PathLink(node1,"foo");
            PathLink node3 = new PathLink(node2,"bar");
            PathLink node4 = new PathLink(node3,0);

            NormalizedPath path1 = new NormalizedPath(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathWithSolidusToString()
        {
            PathLink node1 = PathLink.Root;
            PathLink node2 = new PathLink(node1,"foo's");
            PathLink node3 = new PathLink(node2,"bar");
            PathLink node4 = new PathLink(node3,0);

            NormalizedPath path = new NormalizedPath(node4);
            Assert.IsTrue(path.ToString().Equals(@"$['foo\'s']['bar'][0]"));
        }

        [TestMethod]
        public void TestNormalizedPathToJsonPointer()
        {
            PathLink node1 = PathLink.Root;
            PathLink node2 = new PathLink(node1,"a/b");

            NormalizedPath path = new NormalizedPath(node2);
            Assert.IsTrue(path.ToJsonPointer().Equals(@"/a~1b"));
        }
    }
}
