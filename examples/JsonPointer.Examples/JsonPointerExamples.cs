using System;
using System.Diagnostics;
using System.Text.Json;
using JsonCons.Utilities;
using System.Collections.Generic;

static class JsonPointerExamples
{
    static void CreateJsonPointer()
    {
        using var doc = JsonDocument.Parse(@"
[
  {
    ""key"": true,
    ""other~key"": [""foo""]
  },
  {
    ""key"": false,
    ""other~key"": [""bar"", ""baz""]
  },
  {
    ""key"": true,
    ""other~key"": [""qux""]
  }
]
        ");

        var options = new JsonSerializerOptions() { WriteIndented = true };

        var tokens = new List<string>{"1", "other~key"};
        JsonPointer aPointer = new JsonPointer(tokens);
        Console.WriteLine($"(1) {aPointer}");
        //(1) /1/other~0key
        Console.WriteLine();

        JsonPointer anotherPointer = JsonPointer.Append(aPointer, 1);
        Console.WriteLine($"(2) {anotherPointer}");
        //(2) /1/other~0key/1
        Console.WriteLine();

        JsonElement element;
        if (anotherPointer.TryGetValue(doc.RootElement, out element))
        {
            Console.WriteLine($"(3) {JsonSerializer.Serialize(element, options)}\n");
        }
        //(3) "baz"
        Console.WriteLine();
    }

    // Examples from RFC 6901
    static void GetValueExamples()
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

        JsonElement element;

        if (JsonPointer.TryGetValue(doc.RootElement, "", out element))
        {
            Console.WriteLine($"(1)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/foo", out element))
        {
            Console.WriteLine($"(2)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/foo/0", out element))
        {
            Console.WriteLine($"(3)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/", out element))
        {
            Console.WriteLine($"(4)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/a~1b", out element))
        {
            Console.WriteLine($"(5)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/c%d", out element))
        {
            Console.WriteLine($"(6)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/e^f", out element))
        {
            Console.WriteLine($"(7)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/g|h", out element))
        {
            Console.WriteLine($"(8)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/i\\j", out element))
        {
            Console.WriteLine($"(9)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/k\"l", out element))
        {
            Console.WriteLine($"(10)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/ ", out element))
        {
            Console.WriteLine($"(11)\n{JsonSerializer.Serialize(element, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/m~0n", out element))
        {
            Console.WriteLine($"(12)\n{JsonSerializer.Serialize(element, options)}\n");
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

        using JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);

        var options = new JsonSerializerOptions() { WriteIndented = true };

        Console.WriteLine($"{JsonSerializer.Serialize(flattened, options)}\n");

        using JsonDocument unflattened = JsonFlattener.Unflatten(flattened.RootElement);

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

        using JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);
        Console.WriteLine("The flattened document:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(flattened, options)}\n");

        Console.WriteLine("Unflatten integer tokens as array indices if possible:\n");
        using JsonDocument unflattened1 = JsonFlattener.Unflatten(flattened.RootElement,
                                                            IntegerTokenUnflattening.TryIndex);
        Console.WriteLine($"{JsonSerializer.Serialize(unflattened1, options)}\n");

        Console.WriteLine("Always unflatten integer tokens as object names:\n");
        using JsonDocument unflattened2 = JsonFlattener.Unflatten(flattened.RootElement,
                                                            IntegerTokenUnflattening.AssumeName);
        Console.WriteLine($"{JsonSerializer.Serialize(unflattened2, options)}\n");
    }

    static void Main(string[] args)
    {
        JsonPointerExamples.CreateJsonPointer();
        JsonPointerExamples.GetValueExamples();
        JsonPointerExamples.FlattenAndUnflatten();
        JsonPointerExamples.UnflattenAssumingObject();
    }
}
