### avg

```
number|null avg(array[number] value)
```

Returns the average of the items in an array of numbers, or null if the array is empty

It is a type error if 

- the provided value is not an array

- the array contains items that are not numbers

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
                IList<JsonElement> results = JsonPath.Select(doc.RootElement, @"$.books[?(@.price > avg($.books[*].price))].title");
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
"The Night Watch"
```

