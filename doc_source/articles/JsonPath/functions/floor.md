### floor

```
integer floor(number value)
```

Returns the largest integer value not greater than the given number.

It is a type error if the provided argument is not a number.

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
                IList<JsonElement> results = JsonPath.Select(doc.RootElement, @"$.books[?(floor(@.price*10) == 235)]");
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
    "title" : "A Wild Sheep Chase",
    "author" : "Haruki Murakami",
    "price" : 22.72
}
```

