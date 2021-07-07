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
    ]
  }
}
            ";

            JsonDocument doc = null;

            try
            {
                doc = JsonDocument.Parse(jsonString);

                Console.WriteLine("The authors of all books in the store");
                IReadOnlyList<JsonElement> values1 = JsonPath.Select(doc.RootElement, "$.store.book[*].author");
                foreach (var value in values1)
                {
                    Console.WriteLine(value);
                }
                Console.WriteLine();
            }
            finally
            {
                if (!Object.ReferenceEquals(null, doc)) 
                    doc.Dispose();
            }

        }

        public static void SelectWithAndWithoutDuplicates()
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
                path = JsonPathExpression.Parse("$.books[3,1,1].title");

                Console.WriteLine("Allow duplicates");
                IReadOnlyList<JsonPathNode> nodes = JsonPath.SelectNodes(doc.RootElement, path);
                foreach (var node in nodes)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Allow duplicates and sort by path");
                IReadOnlyList<JsonPathNode> nodesSort = JsonPath.SelectNodes(doc.RootElement, 
                                                                             path,
                                                                             ResultOptions.Sort);
                foreach (var node in nodesSort)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicates");
                IReadOnlyList<JsonPathNode> nodesNoDups = JsonPath.SelectNodes(doc.RootElement, 
                                                                               path, 
                                                                               ResultOptions.NoDups);
                foreach (var node in nodesNoDups)
                {
                    Console.WriteLine($"{node.Path} => {node.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("Remove duplicates and sort by paths");
                IReadOnlyList<JsonPathNode> nodesNoDupsSort = JsonPath.SelectNodes(doc.RootElement, 
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
                path = JsonPathExpression.Parse("$[*].reviews[?(@.rating == 5)]^^");

                Console.WriteLine("Select grandparent node");
                IReadOnlyList<JsonElement> values = JsonPath.Select(doc.RootElement, path);
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
            SelectWithAndWithoutDuplicates();
            UsingTheParentOperator();
        }
    }
}
