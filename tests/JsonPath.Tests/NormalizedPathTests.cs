using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using JsonCons.JsonPathLib;
using NUnit.Framework;

namespace JsonCons.JsonPathLib.Tests
{
    [TestFixture]
    public class PathNodeTests
    {
        [Test]
        public void TestNormalizedPathEquals()
        {
            PathNode node1 = new PathNode("$");
            PathNode node2 = new PathNode(node1,"foo");
            PathNode node3 = new PathNode(node2,"bar");
            PathNode node4 = new PathNode(node3,0);

            PathNode node11 = new PathNode("$");
            PathNode node12 = new PathNode(node11,"foo");
            PathNode node13 = new PathNode(node12,"bar");
            PathNode node14 = new PathNode(node13,0);


            NormalizedPath path1 = new NormalizedPath(node4);
            NormalizedPath path2 = new NormalizedPath(node14);

            Assert.IsTrue(path1.Equals(path2));
        }

        [Test]
        public void TestNormalizedPathToString()
        {
            PathNode node1 = new PathNode("$");
            PathNode node2 = new PathNode(node1,"foo");
            PathNode node3 = new PathNode(node2,"bar");
            PathNode node4 = new PathNode(node3,0);

            NormalizedPath path1 = new NormalizedPath(node4);
            Assert.IsTrue(path1.ToString().Equals(@"$['foo']['bar'][0]"));
        }
    }
}
