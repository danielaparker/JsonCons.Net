# JsonCons.Net

|JsonCons.Utilities|JsonCons.JsonPath|
|:-:|:-:|
|<a href="https://www.nuget.org/packages/JsonCons.Utilities/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/JsonCons.Utilities.svg?svg=true"></img><br><img alt="NuGet version" src="https://img.shields.io/nuget/dt/JsonCons.Utilities.svg?svg=true"></img></a>|<a href="https://www.nuget.org/packages/JsonCons.JsonPath/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/JsonCons.JsonPath.svg?svg=true"></img><br><img alt="NuGet version" src="https://img.shields.io/nuget/dt/JsonCons.JsonPath.svg?svg=true"></img></a>|

The JsonCons.Net libraries include classes that complement the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1),
offering support for:

- JSON Pointer as defined in [RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901)
- JSON Patch as defined in [RFC 6902](https://datatracker.ietf.org/doc/html/rfc6902)
- JSON Merge Patch as defined in [RFC 7396](https://datatracker.ietf.org/doc/html/rfc7396)
- JSONPath as defined in [JsonCons JsonPath](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/Specification.html)

For example, given a [system.text.json.JsonDocument](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument?view=net-5.0)
obtained as
```
string jsonString = @"
{
    ""books"":
    [
        {
            ""title"" : ""A Wild Sheep Chase"",
            ""author"" : ""Haruki Murakami"",
            ""price"" : 22.72
        },
        {
            ""title"" : ""The Night Watch"",
            ""author"" : ""Sergei Lukyanenko"",
            ""price"" : 23.58
        },
        {
            ""title"" : ""The Comedians"",
            ""author"" : ""Graham Greene"",
            ""price"" : 21.99
        },
        {
            ""title"" : ""The Night Watch"",
            ""author"" : ""David Atlee Phillips"",
            ""price"" : 260.90
        }
    ]
}";

using JsonDocument doc = JsonDocument.Parse(jsonString);
```
the [JsonCons.JsonPath.JsonSelector](https://danielaparker.github.io/JsonCons.Net/ref/JsonCons.JsonPath.JsonSelector.html) 
class provides functionality to select elements in the JSON document with code like this:

```
var selector = JsonSelector.Parse("$.books[?(@.price >= 22 && @.price < 30)]");

IList<JsonElement> elements = selector.Select(doc.RootElement);
```

The JsonCons.Net libraries target .Net Standard 2.1. Reference documentation is available [here](https://danielaparker.github.io/JsonCons.Net/ref/)

Code examples may be found at:

[JSON Pointer examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPointer.Examples/JsonPointerExamples.cs)

[JSON Patch examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPatch.Examples/JsonPatchExamples.cs)

[JSON Merge Patch examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonMergePatch.Examples/JsonMergePatchExamples.cs)

[JSONPath examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPath.Examples/JsonPathExamples.cs)

## Acknowledgements

- Credit to [Stefan Goessner's JsonPath](https://goessner.net/articles/JsonPath/),
the original JSONPath.

- The specification of JsonCons.JsonPath draws heavily on Christoph Burgmer's 
[JSONPath Comparison](https://cburgmer.github.io/json-path-comparison/).
Many of the test cases and some of the examples are borrowed from this resource.

- The specification of JsonCons.JsonPath filter expressions is greatly influenced by
James Saryerwinnie's [JMESPath](https://jmespath.org/specification.html)


