# JsonCons.JmesPath

The JsonCons.JmesPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of JMESPath. It targets .Net Standard 2.1. 

The libray supports applying a JMESPath expression to transform a JsonDocument/JsonElement instance
into another JsonDocument instance with code like this:
```csharp
using System;
using System.Text.Json;
using JsonCons.JmesPath;

public static class Example
{
    // Source: https://jmespath.org/examples.html#filters-and-multiselect-lists
    public static void Main(string[] args)
    {
        string jsonString = @"
{
  ""people"": [
    {
      ""age"": 20,
      ""other"": ""foo"",
      ""name"": ""Bob""
    },
    {
      ""age"": 25,
      ""other"": ""bar"",
      ""name"": ""Fred""
    },
    {
      ""age"": 30,
      ""other"": ""baz"",
      ""name"": ""George""
    }
  ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("people[?age > `20`].[name, age]");

        using JsonDocument result = transformer.Transform(doc.RootElement);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
    }
}
```
Output:
```json
[
  [
    "Fred",
    25
  ],
  [
    "George",
    30
  ]
]
```

Documentation and examples may be found here:

- [JmesPath Tutorial](https://jmespath.org/tutorial.html)

- [JmesPath Examples](https://jmespath.org/examples.html)

- [JmesPath Reference](https://jmespath.org/specification.html)

- [Code examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JmesPath.Examples/JmesPathExamples.cs)


