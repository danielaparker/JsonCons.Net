using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPath;

public static class JsonPathExamples
{

    public static void SelectValuesPathsAndNodes()
    {
        var doc = JsonDocument.Parse(@"
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
        ");

        var options = new JsonSerializerOptions() {WriteIndented = true};

        // Selector of titles from union of all books with category 'memoir' 
        // and all books with price > 23
        var selector = JsonSelector.Parse("$.books[?@.category=='memoir',?@.price > 23].title");

        Console.WriteLine("Select values");
        IList<JsonElement> values = selector.Select(doc.RootElement);
        foreach (var value in values)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("Select paths");
        IList<NormalizedPath> paths = selector.SelectPaths(doc.RootElement);
        foreach (var path in paths)
        {
            Console.WriteLine(path);
        }
        Console.WriteLine();

        Console.WriteLine("Select nodes");
        IList<JsonPathNode> nodes = selector.SelectNodes(doc.RootElement);
        foreach (var node in nodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes");
        IList<JsonPathNode> uniqueNodes = selector.SelectNodes(doc.RootElement, 
                                                       new JsonSelectorOptions{NoDuplicates=true});
        foreach (var node in uniqueNodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
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

        var options = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine(@"(1) The authors of all books in the store");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, "$.store.book[*].author");
        foreach (var value in results1)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(2) All authors");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, "$..author");
        foreach (var value in results2)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(3) All things in store - some books and a red bicycle");
        IList<JsonElement> results3 = JsonSelector.Select(doc.RootElement, "$.store.*");
        foreach (var value in results3)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(4) The price of everything in the store.");
        IList<JsonElement> results4 = JsonSelector.Select(doc.RootElement, "$.store..price");
        foreach (var value in results4)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(5) The third book");
        IList<JsonElement> results5 = JsonSelector.Select(doc.RootElement, "$..book[2]");
        foreach (var value in results5)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(6) The last book");
        IList<JsonElement> results6 = JsonSelector.Select(doc.RootElement, "$..book[-1]");
        foreach (var value in results6)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(7) The first two books");
        IList<JsonElement> results7 = JsonSelector.Select(doc.RootElement, "$..book[:2]");
        foreach (var value in results7)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(8) Filter all books with isbn number");
        IList<JsonElement> results8 = JsonSelector.Select(doc.RootElement, "$..book[?(@.isbn)]");
        foreach (var value in results8)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(9) Filter all books cheaper than 10");
        IList<JsonElement> results9 = JsonSelector.Select(doc.RootElement, "$..book[?(@.price<10)]");
        foreach (var value in results9)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine(@"(10) All books with authors that match ""evelyn"" (ignore case)");
        IList<JsonElement> results10 = JsonSelector.Select(doc.RootElement, "$..book[?(@.author =~ /evelyn.*?/i)]");
        foreach (var value in results10)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
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

        var options = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine("(1) All books whose author's last name is 'Tolkien'");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, @"$.books[?(tokenize(@.author,'\\s+')[-1] == 'Tolkien')]");
        foreach (var value in results1)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("(2) All titles whose price is greater than the average price");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, @"$.books[?(@.price > avg($.books[*].price))].title");
        foreach (var value in results2)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("(3) All titles whose price is greater than the average price (alternative)");
        IList<JsonElement> results3 = JsonSelector.Select(doc.RootElement, @"$.books[?(@.price > sum($.books[*].price)/length($.books[*].price))].title");
        foreach (var value in results3)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("(4) All books that don't have a price");
        IList<JsonElement> results4 = JsonSelector.Select(doc.RootElement, @"$.books[?(!contains(keys(@),'price'))]");
        foreach (var value in results4)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("(5) All books that have a price that rounds up to 23.6");
        IList<JsonElement> results5 = JsonSelector.Select(doc.RootElement, @"$.books[?(ceil(@.price*10) == 236)]");
        foreach (var value in results5)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("(6) All books that have a price that rounds down to 22.7");
        IList<JsonElement> results6 = JsonSelector.Select(doc.RootElement, @"$.books[?(floor(@.price*10) == 227)]");
        foreach (var value in results6)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
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

        var options = new JsonSerializerOptions() {WriteIndented = true};
        
        Console.WriteLine("Union of separate JSONPath expressions");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, @"$..[@.firstName,@.address.city]");
        foreach (var value in results1)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
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

        var options = new JsonSerializerOptions() {WriteIndented = true};

        Console.WriteLine("Allow duplicate nodes");
        IList<JsonPathNode> nodes = selector.SelectNodes(doc.RootElement);
        foreach (var node in nodes)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
        }
        Console.WriteLine();

        Console.WriteLine("Allow duplicate nodes and sort by paths");
        IList<JsonPathNode> nodesSort = selector.SelectNodes(doc.RootElement, 
                                                             new JsonSelectorOptions{SortByPath=true});
        foreach (var node in nodesSort)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes");
        IList<JsonPathNode> nodesNoDups = selector.SelectNodes(doc.RootElement, 
                                                           new JsonSelectorOptions{NoDuplicates=true});
        foreach (var node in nodesNoDups)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
        }
        Console.WriteLine();

        Console.WriteLine("Remove duplicate nodes and sort by paths");
        IList<JsonPathNode> nodesNoDupsSort = selector.SelectNodes(doc.RootElement, 
                                                               new JsonSelectorOptions{NoDuplicates=true, SortByPath=true});
        foreach (var node in nodesNoDupsSort)
        {
            Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
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

        var options = new JsonSerializerOptions() {WriteIndented = true};

        Console.WriteLine("Retrieve selected nodes");
        IList<JsonElement> results = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]");
        foreach (var value in results)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("Retrieve parents of selected nodes");
        IList<JsonElement> results1 = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]^");
        foreach (var value in results1)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
        Console.WriteLine();

        Console.WriteLine("Retrieve grandparents of selected nodes");
        IList<JsonElement> results2 = JsonSelector.Select(doc.RootElement, "$[*].reviews[?(@.rating == 5)]^^");
        foreach (var value in results2)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, options));
        }
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
    }
}

