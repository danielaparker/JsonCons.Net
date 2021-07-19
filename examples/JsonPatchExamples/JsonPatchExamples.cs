using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPatchLib;

namespace JsonCons.JsonPatchLib.Examples
{
    class JsonPatchExamples
    {
        static void AddOperation()
        {
            using var doc = JsonDocument.Parse(@"
{
    ""baz"": ""qux"",
    ""foo"": [ ""a"", 2, ""c"" ]
}
            ");
            using var patch = JsonDocument.Parse(@"
[
   { ""op"": ""test"", ""path"": ""/baz"", ""value"": ""qux"" },
   { ""op"": ""test"", ""path"": ""/foo/1"", ""value"": 2 }
]
            ");

            JsonDocument result = JsonPatch.ApplyPatch(doc.RootElement, patch.RootElement);

            Console.WriteLine($"{JsonSerializer.Serialize(result)}\n\n");
        }

        static void Main(string[] args)
        {
            JsonPatchExamples.AddOperation();
        }
    }
}
