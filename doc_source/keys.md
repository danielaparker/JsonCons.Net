### keys

```
array[string] keys(object value)
```

Returns an array of keys in the object.

It is a type error if the provided argument is not an object.

### Example

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
                IList<JsonElement> results = JsonPath.Select(doc.RootElement, @"$.books[?(tokenize(@.author,'\\s+')[-1] == 'Tolkien')]");
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

