using System;
using System.Diagnostics;
using System.Text.Json;
using JsonCons.Utilities;

class JsonMergePatchExamples
{
    // Source: https://datatracker.ietf.org/doc/html/rfc7396
    static void JsonMergePatchExample()
    {
        using var doc = JsonDocument.Parse(@"
{
         ""title"": ""Goodbye!"",
         ""author"" : {
       ""givenName"" : ""John"",
       ""familyName"" : ""Doe""
         },
         ""tags"":[ ""example"", ""sample"" ],
         ""content"": ""This will be unchanged""
}
        ");

        using var patch = JsonDocument.Parse(@"
{
         ""title"": ""Hello!"",
         ""phoneNumber"": ""+01-123-456-7890"",
         ""author"": {
       ""familyName"": null
         },
         ""tags"": [ ""example"" ]
}
            ");

        using JsonDocument result = JsonMergePatch.ApplyMergePatch(doc.RootElement, patch.RootElement);

        var options = new JsonSerializerOptions() { WriteIndented = true };

        Console.WriteLine("The original document:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(doc, options)}\n");
        Console.WriteLine("The patch:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(patch, options)}\n");
        Console.WriteLine("The result:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(result, options)}\n");
    }

    static void FromDiffExample()
    {
        using var sourceDoc = JsonDocument.Parse(@"
{
         ""title"": ""Goodbye!"",
         ""author"" : {
       ""givenName"" : ""John"",
       ""familyName"" : ""Doe""
         },
         ""tags"":[ ""example"", ""sample"" ],
         ""content"": ""This will be unchanged""
}
            ");

        using var targetDoc = JsonDocument.Parse(@"
{
  ""title"": ""Hello!"",
  ""author"": {
    ""givenName"": ""John""
  },
  ""tags"": [
    ""example""
  ],
  ""content"": ""This will be unchanged"",
  ""phoneNumber"": ""\u002B01-123-456-7890""
}
            ");

        using JsonDocument patch = JsonMergePatch.FromDiff(sourceDoc.RootElement, targetDoc.RootElement);

        var options = new JsonSerializerOptions() { WriteIndented = true };

        Console.WriteLine("The source document:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(sourceDoc, options)}\n");
        Console.WriteLine("The target document:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(targetDoc, options)}\n");
        Console.WriteLine("Patch to be applied to source:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(patch, options)}\n");

        using JsonDocument result = JsonMergePatch.ApplyMergePatch(sourceDoc.RootElement, patch.RootElement);
        Debug.Assert(JsonElementEqualityComparer.Instance.Equals(result.RootElement, targetDoc.RootElement));
    }

    static void Main(string[] args)
    {
        JsonMergePatchExamples.JsonMergePatchExample();
        JsonMergePatchExamples.FromDiffExample();
    }
}

