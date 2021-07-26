### contains

```
boolean contains(array|string source, any search)
```

If source is an array, returns true if the array contains an item that is equal to 
the search value, false otherwise.

If source is a string, returns true if the string contains a substring that is equal to
the search value, false otherwise.

It is a type error if 

- the provided source is not an array or string, or

- the provided source is a string but the provided search value is not a string.

### Examples


```csharp
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPath;

namespace JsonCons.Examples
{
    public static class JsonPathExamples
    {
        public static void Main(string[] args)
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
                IList<JsonElement> results = JsonPath.Select(doc.RootElement, @"$.books[?(!contains(keys(@),'price'))]");
                foreach (var value in results)
                {
                    Console.WriteLine(value);
                }
            }
        }
    }
}
```
Output:
```
{
    "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings"
}
```

