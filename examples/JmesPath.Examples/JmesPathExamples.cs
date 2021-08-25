using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JmesPath;

public static class JmesPathExamples
{
    // Source: https://jmespath.org/examples.html#filters-and-multiselect-lists
    public static void FiltersAndMultiselectLists()
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
        Console.WriteLine("Filters and Multiselect Lists");
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
        Console.WriteLine();
    }

    // Source: https://jmespath.org/examples.html#filters-and-multiselect-hashes
    public static void FiltersAndMultiselectHashes()
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

        var transformer = JsonTransformer.Parse("people[?age > `20`].{name: name, age: age}");

        using JsonDocument result = transformer.Transform(doc.RootElement);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        Console.WriteLine("Filters and Multiselect Hashes");
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
        Console.WriteLine();
    }

    // Source: https://jmespath.org/examples.html#working-with-nested-data
    public static void WorkingWithNestedData()
    {
        string jsonString = @"
{
  ""reservations"": [
    {
      ""instances"": [
        {""type"": ""small"",
         ""state"": {""name"": ""running""},
         ""tags"": [{""Key"": ""Name"",
                   ""Values"": [""Web""]},
                  {""Key"": ""version"",
                   ""Values"": [""1""]}]},
        {""type"": ""large"",
         ""state"": {""name"": ""stopped""},
         ""tags"": [{""Key"": ""Name"",
                   ""Values"": [""Web""]},
                  {""Key"": ""version"",
                   ""Values"": [""1""]}]}
      ]
    }, {
      ""instances"": [
        {""type"": ""medium"",
         ""state"": {""name"": ""terminated""},
         ""tags"": [{""Key"": ""Name"",
                   ""Values"": [""Web""]},
                  {""Key"": ""version"",
                   ""Values"": [""1""]}]},
        {""type"": ""xlarge"",
         ""state"": {""name"": ""running""},
         ""tags"": [{""Key"": ""Name"",
                   ""Values"": [""DB""]},
                  {""Key"": ""version"",
                   ""Values"": [""1""]}]}
      ]
    }
  ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("reservations[].instances[].[tags[?Key=='Name'].Values[] | [0], type, state.name]");

        using JsonDocument result = transformer.Transform(doc.RootElement);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        Console.WriteLine("Working with Nested Data");
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
        Console.WriteLine();
    }

    // Source: https://jmespath.org/examples.html#filtering-and-selecting-nested-data
    public static void FilteringAndSelectingNestedData()
    {
        string jsonString = @"
{
  ""people"": [
    {
      ""general"": {
        ""id"": 100,
        ""age"": 20,
        ""other"": ""foo"",
        ""name"": ""Bob""
      },
      ""history"": {
        ""first_login"": ""2014-01-01"",
        ""last_login"": ""2014-01-02""
      }
    },
    {
      ""general"": {
        ""id"": 101,
        ""age"": 30,
        ""other"": ""bar"",
        ""name"": ""Bill""
      },
      ""history"": {
        ""first_login"": ""2014-05-01"",
        ""last_login"": ""2014-05-02""
      }
    }
  ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("people[?general.id==`100`].general | [0]");

        using JsonDocument result = transformer.Transform(doc.RootElement);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        Console.WriteLine("Filtering and Selecting Nested Data");
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
        Console.WriteLine();
    }

    // Source: https://jmespath.org/examples.html#using-functions
    public static void UsingFunctions()
    {
        string jsonString = @"
{
  ""Contents"": [
    {
      ""Date"": ""2014-12-21T05:18:08.000Z"",
      ""Key"": ""logs/bb"",
      ""Size"": 303
    },
    {
      ""Date"": ""2014-12-20T05:19:10.000Z"",
      ""Key"": ""logs/aa"",
      ""Size"": 308
    },
    {
      ""Date"": ""2014-12-20T05:19:12.000Z"",
      ""Key"": ""logs/qux"",
      ""Size"": 297
    },
    {
      ""Date"": ""2014-11-20T05:22:23.000Z"",
      ""Key"": ""logs/baz"",
      ""Size"": 329
    },
    {
      ""Date"": ""2014-12-20T05:25:24.000Z"",
      ""Key"": ""logs/bar"",
      ""Size"": 604
    },
    {
      ""Date"": ""2014-12-20T05:27:12.000Z"",
      ""Key"": ""logs/foo"",
      ""Size"": 647
    }
  ]
}        
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var transformer = JsonTransformer.Parse("sort_by(Contents, &Date)[*].{Key: Key, Size: Size}");

        using JsonDocument result = transformer.Transform(doc.RootElement);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        Console.WriteLine("Using Functions");
        Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
        Console.WriteLine();
    }

    static void Main(string[] args)
    {
        FiltersAndMultiselectLists();
        FiltersAndMultiselectHashes();
        WorkingWithNestedData();
        FilteringAndSelectingNestedData();
        UsingFunctions();
    }
}

