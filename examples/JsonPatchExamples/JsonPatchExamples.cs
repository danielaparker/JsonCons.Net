﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPatchLib;

namespace JsonCons.JsonPatchLib.Examples
{
    class JsonPatchExamples
    {
        // Source: http://jsonpatch.com/
        static void PatchExample()
        {
            using var doc = JsonDocument.Parse(@"
{
  ""baz"": ""qux"",
  ""foo"": ""bar""
}
            ");

            using var patch = JsonDocument.Parse(@"
[
  { ""op"": ""replace"", ""path"": ""/baz"", ""value"": ""boo"" },
  { ""op"": ""add"", ""path"": ""/hello"", ""value"": [""world""] },
  { ""op"": ""remove"", ""path"": ""/foo"" }
]
            ");

            var options = new JsonSerializerOptions();
            options.WriteIndented = true;

            JsonDocument result = JsonPatch.ApplyPatch(doc.RootElement, patch.RootElement);

            Console.WriteLine("The original document:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(doc.RootElement, options)}\n");
            Console.WriteLine("The patch:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(patch.RootElement, options)}\n");
            Console.WriteLine("The result:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(result, options)}\n");
        }

        static void FromDiffExample()
        {
            using var sourceDoc = JsonDocument.Parse(@"
{
  ""baz"": ""qux"",
  ""foo"": ""bar""
}
            ");

            using var targetDoc = JsonDocument.Parse(@"
{
  ""baz"": ""boo"",
  ""hello"": [
    ""world""
  ]
}
            ");

            var options = new JsonSerializerOptions();
            options.WriteIndented = true;

            JsonDocument patch = JsonPatch.FromDiff(sourceDoc.RootElement, targetDoc.RootElement);

            Console.WriteLine("The source document:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(sourceDoc.RootElement, options)}\n");
            Console.WriteLine("The target document:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(targetDoc.RootElement, options)}\n");
            Console.WriteLine("Patch to be applied to source:\n");
            Console.WriteLine($"{JsonSerializer.Serialize(patch, options)}\n");
        }

        static void Main(string[] args)
        {
            JsonPatchExamples.PatchExample();
            JsonPatchExamples.FromDiffExample();
        }
    }
}