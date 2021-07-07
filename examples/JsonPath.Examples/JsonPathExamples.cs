using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPathLib;

namespace JsonCons.Examples
{
    public static class JsonPathExamples
    {
        public static void SelectWithAndWithoutDuplicates()
        {
            string jsonString = @"
{
    ""books"":
    [
        {
            ""category"": ""fiction"",
            ""title"" : ""A Wild Sheep Chase"",
            ""author"" : ""Haruki Murakami"",
            ""price"" : 22.72
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""Sergei Lukyanenko"",
            ""price"" : 23.58
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Comedians"",
            ""author"" : ""Graham Greene"",
            ""price"" : 21.99
        },
        {
            ""category"": ""memoir"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""David Atlee Phillips""
        }
    ]
}
            ";

            JsonDocument doc = null;
            JsonPath jsonPath = null;

            try
            {
                doc = JsonDocument.Parse(jsonString);
                jsonPath = JsonPath.Parse("$.books[3,1,1].title");

                Console.WriteLine("Allow duplicates");
                IReadOnlyList<JsonPathNode> nodes = jsonPath.SelectNodes(doc.RootElement);
                foreach (var node in nodes)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Allow duplicates and sort by path");
                IReadOnlyList<JsonPathNode> nodesSort = jsonPath.SelectNodes(doc.RootElement,
                                                                             ResultOptions.Sort);
                foreach (var node in nodesSort)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicates");
                IReadOnlyList<JsonPathNode> nodesNoDups = jsonPath.SelectNodes(doc.RootElement, 
                                                                               ResultOptions.NoDups);
                foreach (var node in nodesNoDups)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicates and sort by paths");
                IReadOnlyList<JsonPathNode> nodesNoDupsSort = jsonPath.SelectNodes(doc.RootElement, 
                                                                                   ResultOptions.NoDups | ResultOptions.Sort);
                foreach (var node in nodesNoDupsSort)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();
            }
            finally
            {
                if (!Object.ReferenceEquals(null, doc)) 
                    doc.Dispose();
                if (!Object.ReferenceEquals(null, jsonPath)) 
                    jsonPath.Dispose();
            }

        }
        public static void Main(string[] args)
        {
            SelectWithAndWithoutDuplicates();
        }
    }
}
