### length

```
integer|null length(array|object|string value)
```

Returns the length of an array, object or string.

If array, returns the number of items in the array
If object, returns the number of key-value pairs in the object
If string, returns the number of codepoints in the string
Otherwise, returns null.

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
                IList<JsonElement> results3 = JsonPath.Select(doc.RootElement, @"$.books[?(@.price > sum($.books[*].price)/length($.books[*].price))].title");
                foreach (var value in results3)
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
"The Night Watch"
```

