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
        public static void SelectExamples()
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
            ""author"" : ""Phillips, David Atlee""
        }
    ]
}
            ";

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                using (JsonPath jsonPath = JsonPath.Parse("$.books[1,1,3].title"))
                {
                    Console.WriteLine("Select values");
                    IReadOnlyList<JsonElement> values = jsonPath.Select(doc.RootElement);
                    foreach (var value in values)
                    {
                        Console.WriteLine(value);
                    }
                    Console.WriteLine();

                    Console.WriteLine("Select paths");
                    IReadOnlyList<NormalizedPath> paths = jsonPath.SelectPaths(doc.RootElement);
                    foreach (var path in paths)
                    {
                        Console.WriteLine(path);
                    }
                }
            }

        }
        public static void Main(string[] args)
        {
            SelectExamples();
        }
    }
}
