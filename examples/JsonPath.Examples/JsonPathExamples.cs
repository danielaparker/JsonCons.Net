﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPath;

public static class JsonPathExamples
{

    public static void SelectValuesPathsAndNodes()
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
            ""author"" : ""David Atlee Phillips"",
            ""price"" : 260.90
        }
    ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};

        // Selector of titles from union of all books with category 'memoir' 
        // and all books with price > 23
        var selector = JsonSelector.Parse("$.books[?@.category=='memoir',?@.price > 23].title");

        Console.WriteLine("Select values");
        IList<JsonElement> values = selector.Select(doc.RootElement);
        Console.WriteLine(JsonSerializer.Serialize(values, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("Select paths");
        IList<JsonLocation> paths = selector.SelectPaths(doc.RootElement);
        foreach (var path in paths)
        {
            Console.WriteLine(path);
        }
        Console.WriteLine();

        Console.WriteLine("Select nodes");
        IList<PathValuePair> nodes = selector.SelectNodes(doc.RootElement);
        foreach (var node in nodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes");
        IList<PathValuePair> uniqueNodes = selector.SelectNodes(doc.RootElement, 
                                                       new JsonSelectorOptions{NoDuplicates=true});
        foreach (var node in uniqueNodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();
    }

    public static void StoreExample()
    {
        string jsonString = @"
{ ""store"": {
    ""book"": [ 
      { ""category"": ""reference"",
        ""author"": ""Nigel Rees"",
        ""title"": ""Sayings of the Century"",
        ""price"": 8.95
      },
      { ""category"": ""fiction"",
        ""author"": ""Evelyn Waugh"",
        ""title"": ""Sword of Honour"",
        ""price"": 12.99
      },
      { ""category"": ""fiction"",
        ""author"": ""Herman Melville"",
        ""title"": ""Moby Dick"",
        ""isbn"": ""0-553-21311-3"",
        ""price"": 8.99
      },
      { ""category"": ""fiction"",
        ""author"": ""J. R. R. Tolkien"",
        ""title"": ""The Lord of the Rings"",
        ""isbn"": ""0-395-19395-8"",
        ""price"": 22.99
      }
    ],
    ""bicycle"": {
      ""color"": ""red"",
      ""price"": 19.95
    }
  }
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine(@"(1) The authors of all books in the store");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, "$.store.book[*].author");
        Console.WriteLine(JsonSerializer.Serialize(results1, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(2) All authors");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, "$..author");
        Console.WriteLine(JsonSerializer.Serialize(results2, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(3) All things in store - some books and a red bicycle");
        IList<JsonElement> results3 = JsonSelector.Select(doc.RootElement, "$.store.*");
        Console.WriteLine(JsonSerializer.Serialize(results3, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(4) The price of everything in the store.");
        IList<JsonElement> results4 = JsonSelector.Select(doc.RootElement, "$.store..price");
        Console.WriteLine(JsonSerializer.Serialize(results4, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(5) The third book");
        IList<JsonElement> results5 = JsonSelector.Select(doc.RootElement, "$..book[2]");
        Console.WriteLine(JsonSerializer.Serialize(results5, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(6) The last book");
        IList<JsonElement> results6 = JsonSelector.Select(doc.RootElement, "$..book[-1]");
        Console.WriteLine(JsonSerializer.Serialize(results6, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(7) The first two books");
        IList<JsonElement> results7 = JsonSelector.Select(doc.RootElement, "$..book[:2]");
        Console.WriteLine(JsonSerializer.Serialize(results7, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(8) Filter all books with isbn number");
        IList<JsonElement> results8 = JsonSelector.Select(doc.RootElement, "$..book[?(@.isbn)]");
        Console.WriteLine(JsonSerializer.Serialize(results8, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(9) Filter all books with prices between 5 and 10");
        IList<JsonElement> results9 = JsonSelector.Select(doc.RootElement, "$..book[?(@.price >= 5 && @.price < 10)]");
        Console.WriteLine(JsonSerializer.Serialize(results9, serializerOptions));
        Console.WriteLine();

        Console.WriteLine(@"(10) All books with authors that match ""evelyn"" (ignore case)");
        IList<JsonElement> results10 = JsonSelector.Select(doc.RootElement, "$..book[?(@.author =~ /evelyn.*?/i)]");
        Console.WriteLine(JsonSerializer.Serialize(results10, serializerOptions));
        Console.WriteLine();            
    }

    public static void UsingFunctionsInFilters()
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
          ""category"": ""fiction"",
          ""author"": ""J. R. R. Tolkien"",
          ""title"": ""The Lord of the Rings""
        }
    ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine("(1) All books whose author's last name is 'Tolkien'");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, @"$.books[?(tokenize(@.author,'\\s+')[-1] == 'Tolkien')]");
        Console.WriteLine(JsonSerializer.Serialize(results1, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("(2) All titles whose price is greater than the average price");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, @"$.books[?(@.price > avg($.books[*].price))].title");
        Console.WriteLine(JsonSerializer.Serialize(results2, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("(3) All titles whose price is greater than the average price (alternative)");
        IList<JsonElement> results3 = JsonSelector.Select(doc.RootElement, @"$.books[?(@.price > sum($.books[*].price)/length($.books[*].price))].title");
        Console.WriteLine(JsonSerializer.Serialize(results3, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("(4) All books that don't have a price");
        IList<JsonElement> results4 = JsonSelector.Select(doc.RootElement, @"$.books[?(!contains(keys(@),'price'))]");
        Console.WriteLine(JsonSerializer.Serialize(results4, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("(5) All books that have a price that rounds up to 23.6");
        IList<JsonElement> results5 = JsonSelector.Select(doc.RootElement, @"$.books[?(ceil(@.price*10) == 236)]");
        Console.WriteLine(JsonSerializer.Serialize(results5, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("(6) All books that have a price that rounds down to 22.7");
        IList<JsonElement> results6 = JsonSelector.Select(doc.RootElement, @"$.books[?(floor(@.price*10) == 227)]");
        Console.WriteLine(JsonSerializer.Serialize(results6, serializerOptions));
        Console.WriteLine();            
    }

    public static void UnionOfSeparateJsonPathExpressions()
    {
        string jsonString = @"
{
  ""firstName"": ""John"",
  ""lastName"" : ""doe"",
  ""age""      : 26,
  ""address""  : {
    ""streetAddress"": ""naist street"",
    ""city""         : ""Nara"",
    ""postalCode""   : ""630-0192""
  },
  ""phoneNumbers"": [
    {
      ""type""  : ""iPhone"",
      ""number"": ""0123-4567-8888""
    },
    {
      ""type""  : ""home"",
      ""number"": ""0123-4567-8910""
    }
  ]
}    
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine("Union of separate JSONPath expressions");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, @"$..[@.firstName,@.address.city]");
        Console.WriteLine(JsonSerializer.Serialize(results1, serializerOptions));
        Console.WriteLine();
    }

    public static void SelectNodesWithVariousOptions()
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
            ""author"" : ""David Atlee Phillips"",
            ""price"" : 260.90
        }
    ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);
        
        var selector = JsonSelector.Parse("$.books[3,1,1].title");

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};

        Console.WriteLine("Allow duplicate nodes");
        IList<PathValuePair> nodes = selector.SelectNodes(doc.RootElement);
        foreach (var node in nodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();

        Console.WriteLine("Allow duplicate nodes and sort by paths");
        IList<PathValuePair> nodesSort = selector.SelectNodes(doc.RootElement, 
                                                             new JsonSelectorOptions{Sort=true});
        foreach (var node in nodesSort)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes");
        IList<PathValuePair> nodesNoDups = selector.SelectNodes(doc.RootElement, 
                                                           new JsonSelectorOptions{NoDuplicates=true});
        foreach (var node in nodesNoDups)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes and sort by paths");
        IList<PathValuePair> nodesNoDupsSort = selector.SelectNodes(doc.RootElement, 
                                                               new JsonSelectorOptions{NoDuplicates=true, Sort=true});
        foreach (var node in nodesNoDupsSort)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, serializerOptions)}");
        }
        Console.WriteLine();
    }

    public static void UsingTheParentOperator()
    {
        string jsonString = @"
[
    {
      ""title"": ""A Wild Sheep Chase"",
      ""reviews"": [{""rating"": 4, ""reviewer"": ""Nan""}]
    },
    {
      ""title"": ""The Night Watch"",
      ""reviews"": [{""rating"": 5, ""reviewer"": ""Alan""},
                  {""rating"": 3,""reviewer"": ""Anne""}]
    },
    {
      ""title"": ""The Comedians"",
      ""reviews"": [{""rating"": 4, ""reviewer"": ""Lisa""},
                  {""rating"": 5, ""reviewer"": ""Robert""}]
    }
]
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};

        Console.WriteLine("Retrieve selected nodes");
        IList<JsonElement> results = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]");
        Console.WriteLine(JsonSerializer.Serialize(results, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("Retrieve parents of selected nodes");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]^");
        Console.WriteLine(JsonSerializer.Serialize(results1, serializerOptions));
        Console.WriteLine();

        Console.WriteLine("Retrieve grandparents of selected nodes");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]^^");
        Console.WriteLine(JsonSerializer.Serialize(results2, serializerOptions));
        Console.WriteLine();
    }

    public static void ProcessingUnionsSequentiallyAndInParallel()
    {
        string jsonString = @"
[
  {
    ""Title"": ""Title0"",
    ""Author"": ""Author0"",
    ""Category"": ""Fiction""
  },
  {
    ""Title"": ""Title1"",
    ""Author"": ""Author1"",
    ""Category"": ""Poetry""
  },
  {
    ""Title"": ""Title2"",
    ""Author"": ""Author2"",
    ""Category"": ""NonFiction""
  },
  {
    ""Title"": ""Title3"",
    ""Author"": ""Author3"",
    ""Category"": ""Drama""
  },
  {
    ""Title"": ""Title4"",
    ""Author"": ""Author4"",
    ""Category"": ""Drama""
  },
  {
    ""Title"": ""Title5"",
    ""Author"": ""Author5"",
    ""Category"": ""Fiction""
  },
  {
    ""Title"": ""Title6"",
    ""Author"": ""Author6"",
    ""Category"": ""Poetry""
  },
  {
    ""Title"": ""Title7"",
    ""Author"": ""Author7"",
    ""Category"": ""Poetry""
  },
  {
    ""Title"": ""Title8"",
    ""Author"": ""Author8"",
    ""Category"": ""Folktale""
  },
  {
    ""Title"": ""Title9"",
    ""Author"": ""Author9"",
    ""Category"": ""Folktale""
  }
]
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};

        var selector1 = JsonSelector.Parse(@"$[?@.Category=='Fiction' ||
                                                @.Category=='NonFiction' ||                                                 
                                                @.Category=='Drama']
                                           ");
        IList<JsonElement> results1 = selector1.Select(doc.RootElement);

        Console.WriteLine("Results with sequential processing:");
        Console.WriteLine(JsonSerializer.Serialize(results1, serializerOptions));
        Console.WriteLine();

        var selector2 = JsonSelector.Parse(@"$[?@.Category=='Fiction',
                                               ?@.Category=='NonFiction',                                                 
                                               ?@.Category=='Drama'
                                              ]");
        IList<JsonElement> results2 = selector2.Select(doc.RootElement,
            new JsonSelectorOptions{ExecutionMode = PathExecutionMode.Parallel});

        Console.WriteLine("Results with parallel processing:");
        Console.WriteLine(JsonSerializer.Serialize(results2, serializerOptions));
        Console.WriteLine();
    }

    public static void KeyOfInterest()
    {
        string jsonString = @"
{
    ""Data"":[
        {
            ""KeyOfInterest"":true,
            ""AnotherKey"":true
        },
        {
            ""KeyOfInterest"":false,
            ""AnotherKey"":true
        },
        {
            ""KeyOfInterest"":true,
            ""AnotherKey"":true
        }
    ]
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement,
                                                          "$.Data[*].KeyOfInterest");

        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement,
                                                          "$.Data[*]['KeyOfInterest', 'AnotherKey']");

        Console.WriteLine("Key of Interest");
        Console.WriteLine("(1)");
        Console.WriteLine(JsonSerializer.Serialize(results1));
        Console.WriteLine("(2)");
        Console.WriteLine(JsonSerializer.Serialize(results2));
        Console.WriteLine();
    }

    public static void Main(string[] args)
    {
        SelectValuesPathsAndNodes();
        StoreExample();
        UsingFunctionsInFilters();
        UnionOfSeparateJsonPathExpressions();
        SelectNodesWithVariousOptions();
        UsingTheParentOperator();
        ProcessingUnionsSequentiallyAndInParallel();
        KeyOfInterest();
    }
}

