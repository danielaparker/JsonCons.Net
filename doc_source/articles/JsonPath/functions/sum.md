### sum

```
number sum(array[number] value)
```

Returns the sum of the items in an array of numbers. 

Returns 0 if the array is empty.

It is a type error if any item in the array is not a number.

### Examples

```csharp
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPathLib;

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

