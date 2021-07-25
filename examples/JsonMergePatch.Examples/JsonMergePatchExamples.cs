using System;
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
        Console.WriteLine($"{JsonSerializer.Serialize(doc.RootElement, options)}\n");
        Console.WriteLine("The patch:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(patch.RootElement, options)}\n");
        Console.WriteLine("The result:\n");
        Console.WriteLine($"{JsonSerializer.Serialize(result, options)}\n");
    }

    static void Main(string[] args)
    {
        JsonMergePatchExamples.JsonMergePatchExample();
    }
}

