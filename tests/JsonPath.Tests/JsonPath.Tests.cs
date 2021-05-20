using System;
using System.IO;
using NUnit.Framework;

namespace Jsoncons.JsonPathLib.Tests
{
    public class JsonPathTests
    {
        public static void Test(string j)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, @"test_data\identifiers.json");
            Console.Write(path);
        }
    }
}
