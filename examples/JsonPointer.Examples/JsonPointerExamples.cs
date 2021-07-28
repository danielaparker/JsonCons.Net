using System;
using System.Diagnostics;
using System.Text.Json;
using JsonCons.Utilities;

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

        if (JsonPointer.TryGetValue(doc.RootElement, "", out result))
        {
            Console.WriteLine($"(1) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/foo", out result))
        {
            Console.WriteLine($"(2) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/foo/0", out result))
        {
            Console.WriteLine($"(3) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/", out result))
        {
            Console.WriteLine($"(4) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/a~1b", out result))
        {
            Console.WriteLine($"(5) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/c%d", out result))
        {
            Console.WriteLine($"(6) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/e^f", out result))
        {
            Console.WriteLine($"(7) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/g|h", out result))
        {
            Console.WriteLine($"(8) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/i\\j", out result))
        {
            Console.WriteLine($"(9) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/k\"l", out result))
        {
            Console.WriteLine($"(10) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/ ", out result))
        {
            Console.WriteLine($"(11) {JsonSerializer.Serialize(result, options)}\n");
        }
        if (JsonPointer.TryGetValue(doc.RootElement, "/m~0n", out result))
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
        JsonPointerExamples.GetExamples();
        JsonPointerExamples.FlattenAndUnflatten();
        JsonPointerExamples.UnflattenAssumingObject();
    }
}
