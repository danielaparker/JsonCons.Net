using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.Utilities;

namespace JsonCons.Utilities.Examples
{
    static class JsonPointerExamples
    {
        // Example from RFC 6901
        static void GetExamples()
        {
            using var doc = JsonDocument.Parse(@"
{
   ""foo"": [""bar"", ""baz""],
   """": 0,
   ""a/b"": 1,
   ""c%d"": 2,
   ""e^f"": 3,
   ""g|h"": 4,
   ""i\\j"": 5,
   ""k\""l"": 6,
   "" "": 7,
   ""m~n"": 8
}
            ");

            var options = new JsonSerializerOptions() { WriteIndented = true };

            JsonElement result;

            if (JsonPointer.TryGet(doc.RootElement, "", out result))
            {
                Console.WriteLine($"(1) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/foo", out result))
            {
                Console.WriteLine($"(2) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/foo/0", out result))
            {
                Console.WriteLine($"(3) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/", out result))
            {
                Console.WriteLine($"(4) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/a~1b", out result))
            {
                Console.WriteLine($"(5) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/c%d", out result))
            {
                Console.WriteLine($"(6) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/e^f", out result))
            {
                Console.WriteLine($"(7) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/g|h", out result))
            {
                Console.WriteLine($"(8) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/i\\j", out result))
            {
                Console.WriteLine($"(9) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/k\"l", out result))
            {
                Console.WriteLine($"(10) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/ ", out result))
            {
                Console.WriteLine($"(11) {JsonSerializer.Serialize(result, options)}\n");
            }
            if (JsonPointer.TryGet(doc.RootElement, "/m~0n", out result))
            {
                Console.WriteLine($"(12) {JsonSerializer.Serialize(result, options)}\n");
            }
        }

        static void FlattenAndUnflatten()
        {
            using var doc = JsonDocument.Parse(@"
{
   ""application"": ""hiking"",
   ""reputons"": [
       {
           ""rater"": ""HikingAsylum"",
           ""assertion"": ""advanced"",
           ""rated"": ""Marilyn C"",
           ""rating"": 0.90
        },
        {
           ""rater"": ""HikingAsylum"",
           ""assertion"": ""intermediate"",
           ""rated"": ""Hongmin"",
           ""rating"": 0.75
        }    
    ]
}
            ");

            JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);

            var options = new JsonSerializerOptions() { WriteIndented = true };

            Console.WriteLine($"{JsonSerializer.Serialize(flattened, options)}\n");

            JsonDocument unflattened = JsonFlattener.Unflatten(flattened.RootElement);

            var comparer = JsonElementEqualityComparer.Instance;
            Debug.Assert(comparer.Equals(unflattened.RootElement,doc.RootElement));
        }

        static void UnflattenAssumingObject()
        {
            using var doc = JsonDocument.Parse(@"
{
    ""discards"": {
        ""1000"": ""Record does not exist"",
        ""1004"": ""Queue limit exceeded"",
        ""1010"": ""Discarding timed-out partial msg""
    },
    ""warnings"": {
        ""0"": ""Phone number missing country code"",
        ""1"": ""State code missing"",
        ""2"": ""Zip code missing""
    }
}
            ");

            var options = new JsonSerializerOptions() { WriteIndented = true };

            JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);
            Console.WriteLine($"(1) {JsonSerializer.Serialize(flattened, options)}\n");

            JsonDocument unflattened1 = JsonFlattener.Unflatten(flattened.RootElement);
            Console.WriteLine($"(2) {JsonSerializer.Serialize(unflattened1, options)}\n");

            JsonDocument unflattened2 = JsonFlattener.Unflatten(flattened.RootElement,
                                                                IntTokenHandling.AssumeObject);
            Console.WriteLine($"(3) {JsonSerializer.Serialize(unflattened2, options)}\n");
        }

        static void Main(string[] args)
        {
            JsonPointerExamples.GetExamples();
            JsonPointerExamples.FlattenAndUnflatten();
            JsonPointerExamples.UnflattenAssumingObject();
        }
    }
}
