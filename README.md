# JsonCons.Net

|JsonCons.Utilities|JsonCons.JsonPath|
|:-:|:-:|:-:|
|<a href="https://www.nuget.org/packages/JsonCons.Utilities/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/JsonCons.Utilities.svg?svg=true"></img><br><img alt="NuGet version" src="https://img.shields.io/nuget/dt/JsonCons.Utilities.svg?svg=true"></img></a>|<a href="https://www.nuget.org/packages/JsonCons.JsonPath/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/JsonCons.JsonPath.svg?svg=true"></img><br><img alt="NuGet version" src="https://img.shields.io/nuget/dt/JsonCons.JsonPath.svg?svg=true"></img></a>|<a href="https://www.nuget.org/packages/JsonCons.JmesPath/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/JsonCons.JmesPath.svg?svg=true"></img><br><img alt="NuGet version" src="https://img.shields.io/nuget/dt/JsonCons.JmesPath.svg?svg=true"></img></a>|

The JsonCons.Net libraries include classes that complement the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1).
The libraries target .Net Standard 2.1. 

The JsonCons.Net libraries offer support for:

- JSON Pointer as defined in [RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901)
- JSON Patch as defined in [RFC 6902](https://datatracker.ietf.org/doc/html/rfc6902)
- JSON Merge Patch as defined in [RFC 7396](https://datatracker.ietf.org/doc/html/rfc7396)
- JSONPath as defined in [JsonCons JsonPath](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/Specification.html)
- JMESPath as defined in [JMESPath Specification](https://jmespath.org/specification.html)

## JSONPath and JMESPath

JSONPath allows you to select from a [JsonDocument](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument?view=net-5.0) 
a list of [JsonElement](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement?view=net-5.0) instances
that belong to it. JMESPath allows you to transform a [JsonDocument](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument?view=net-5.0) 
into another 
[JsonDocument](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument?view=net-5.0).

For example, consider the JSON data
```json
string jsonString = @"
{
    ""Data"":[
        {
            ""KeyOfInterest"":true,
            ""AnotherKey"":true
        },
        {
            ""KeyOfInterest"":false,
            ""AnotherKey"":true
        },
        {
            ""KeyOfInterest"":true,
            ""AnotherKey"":true
        }
    ]
}
        ";

using JsonDocument doc = JsonDocument.Parse(jsonString);
```

JSONPath allows you to select the `KeyOfInterest` values like this:
```csharp
string path = "$.Data[*].KeyOfInterest";
IList<JsonElement> results = JsonSelector.Select(doc.RootElement, path);
```
and the union of `KeyOfInterest` and `AnotherKey` values like this:
```csharp
string path = "$.Data[*]['KeyOfInterest', 'AnotherKey']";
IList<JsonElement> results = JsonSelector.Select(doc.RootElement, path);
```
The first query produces
```json
[true,false,true]
```
and the second
```json
[true,true,false,true,true,true]
```           
Note that each element in the result - `true`, `false`, `true` - corresponds to an element 
at a specific location in the original JSON document. This is a feature of JSONPath.

JMESPath allows you to select the `KeyOfInterest` values like this:
```csharp
string expr = Data[*].KeyOfInterest;
JsonDocument result = JsonTransformer.Transform(doc.RootElement, expr);
```
and a multiselect hash of `KeyOfInterest` and `AnotherKey` values like this:
```csharp
string expr = "Data[*].{\"Key of Interest\" : KeyOfInterest, \"Another Key\": AnotherKey}";
JsonDocument result = JsonTransformer.Transform(doc.RootElement, expr);
```
The first query produces
```json
[true,false,true]
```
and the second
```json
[
  {
    "Key of Interest": true,
    "Another Key": true
  },
  {
    "Key of Interest": false,
    "Another Key": true
  },
  {
    "Key of Interest": true,
    "Another Key": true
  }
]
```

JMESPath, unlike JSONPath, can create new elements that are not in the original document.
JMESPath can transform, while JsonPath can only select.

## Documentation and Examples

Reference documentation is available [here](https://danielaparker.github.io/JsonCons.Net/ref/)

Code examples may be found at:

[JSON Pointer examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPointer.Examples/JsonPointerExamples.cs)

[JSON Patch examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPatch.Examples/JsonPatchExamples.cs)

[JSON Merge Patch examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonMergePatch.Examples/JsonMergePatchExamples.cs)

[JSONPath examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPath.Examples/JsonPathExamples.cs)

[JMESPath examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JmesPath.Examples/JmesPathExamples.cs)


