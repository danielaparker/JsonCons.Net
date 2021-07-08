using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPathLib;

namespace JsonCons.Examples
{
    public static class JsonPathExamples
    {
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

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                Console.WriteLine(@"(1) The authors of all books in the store");
                IList<JsonElement> values1 = JsonPath.Select(doc.RootElement, "$.store.book[*].author");
                foreach (var value in values1)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(2) All authors");
                IList<JsonElement> values2 = JsonPath.Select(doc.RootElement, "$..author");
                foreach (var value in values2)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(3) All things in store, which are some books and a red bicycle");
                IList<JsonElement> values3 = JsonPath.Select(doc.RootElement, "$.store.*");
                foreach (var value in values3)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(4) The price of everything in the store.");
                IList<JsonElement> values4 = JsonPath.Select(doc.RootElement, "$.store..price");
                foreach (var value in values4)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(5) The third book");
                IList<JsonElement> values5 = JsonPath.Select(doc.RootElement, "$..book[2]");
                foreach (var value in values5)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(6) The last book");
                IList<JsonElement> values6 = JsonPath.Select(doc.RootElement, "$..book[-1]");
                foreach (var value in values6)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(7) The first two books");
                IList<JsonElement> values7 = JsonPath.Select(doc.RootElement, "$..book[:2]");
                foreach (var value in values7)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(8) Filter all books with isbn number");
                IList<JsonElement> values8 = JsonPath.Select(doc.RootElement, "$..book[?(@.isbn)]");
                foreach (var value in values8)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(9) Filter all books cheaper than 10");
                IList<JsonElement> values9 = JsonPath.Select(doc.RootElement, "$..book[?(@.price<10)]");
                foreach (var value in values9)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine(@"(10) All books with authors that match ""evelyn"" (ignore case)");
                IList<JsonElement> values10 = JsonPath.Select(doc.RootElement, "$..book[?(@.author =~ /evelyn.*?/i)]");
                foreach (var value in values10)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();
            }
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

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                Console.WriteLine("(1) All books whose author's last name is 'Tolkien'");
                IList<JsonElement> values1 = JsonPath.Select(doc.RootElement, @"$.books[?(tokenize(@.author,'\\s+')[-1] == 'Tolkien')]");
                foreach (var value in values1)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine("(2) All titles whose price is greater than the average price");
                IList<JsonElement> values2 = JsonPath.Select(doc.RootElement, @"$.books[?(@.price > avg($.books[*].price))].title");
                foreach (var value in values2)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();

                Console.WriteLine("(3) All books that don't have a price");
                IList<JsonElement> values3 = JsonPath.Select(doc.RootElement, @"$.books[?(!contains(keys(@),'price'))]");
                foreach (var value in values3)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();
            }
        }

        public static void SelectWithAndWithoutDuplicateNodes()
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
            ""author"" : ""David Atlee Phillips""
        }
    ]
}
            ";

            JsonDocument doc = null;
            JsonPathExpression path = null;

            try
            {
                doc = JsonDocument.Parse(jsonString);
                path = JsonPathExpression.Compile("$.books[3,1,1].title");

                Console.WriteLine("Allow duplicate nodes");
                IList<JsonPathNode> nodes = JsonPath.SelectNodes(doc.RootElement, path);
                foreach (var node in nodes)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Allow duplicate nodes and sort by path");
                IList<JsonPathNode> nodesSort = JsonPath.SelectNodes(doc.RootElement, 
                                                                             path,
                                                                             ResultOptions.Sort);
                foreach (var node in nodesSort)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicate nodes");
                IList<JsonPathNode> nodesNoDups = JsonPath.SelectNodes(doc.RootElement, 
                                                                               path, 
                                                                               ResultOptions.NoDups);
                foreach (var node in nodesNoDups)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicate nodes and sort by paths");
                IList<JsonPathNode> nodesNoDupsSort = JsonPath.SelectNodes(doc.RootElement, 
                                                                                   path, 
                                                                                   ResultOptions.NoDups | ResultOptions.Sort);
                foreach (var node in nodesNoDupsSort)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();
            }
            finally
            {
                if (!Object.ReferenceEquals(null, doc)) 
                    doc.Dispose();
                if (!Object.ReferenceEquals(null, path)) 
                    path.Dispose();
            }

        }

        public static void UsingTheParentOperator()
        {
            string jsonString = @"
[
    {
      ""author"" : ""Haruki Murakami"",
      ""title"": ""A Wild Sheep Chase"",
      ""reviews"": [{""rating"": 4, ""reviewer"": ""Nan""}]
    },
    {
      ""author"" : ""Sergei Lukyanenko"",
      ""title"": ""The Night Watch"",
      ""reviews"": [{""rating"": 5, ""reviewer"": ""Alan""},
                  {""rating"": 3,""reviewer"": ""Anne""}]
    },
    {
      ""author"" : ""Graham Greene"",
      ""title"": ""The Comedians"",
      ""reviews"": [{""rating"": 4, ""reviewer"": ""Lisa""},
                  {""rating"": 5, ""reviewer"": ""Robert""}]
    }
]
            ";

            JsonDocument doc = null;
            JsonPathExpression path = null;

            try
            {
                doc = JsonDocument.Parse(jsonString);
                path = JsonPathExpression.Compile("$[*].reviews[?(@.rating == 5)]^^");

                Console.WriteLine("Select grandparent nodes");
                IList<JsonElement> values = JsonPath.Select(doc.RootElement, path);
                foreach (var value in values)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();
            }
            finally
            {
                if (!Object.ReferenceEquals(null, doc)) 
                    doc.Dispose();
                if (!Object.ReferenceEquals(null, path)) 
                    path.Dispose();
            }

        }

        public static void Main(string[] args)
        {
            StoreExample();
            UsingFunctionsInFilters();
            SelectWithAndWithoutDuplicateNodes();
            UsingTheParentOperator();
        }
    }
}
