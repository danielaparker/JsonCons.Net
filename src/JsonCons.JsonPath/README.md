# JsonCons.JsonPath

The JsonCons.JsonPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of [Stefan Goessner's JsonPath](https://goessner.net/articles/JsonPath/). 
This implementation has the following features:

- It is based on a hand-constructed finite state machine that follows a formal
grammar described in an [ABNF grammar](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/Specification.html)
with specification.

- The basic features are largely compatible with the loose consensus seen in Christoph Burgmer's 
[comparison of 41 JSONPath implementations](https://cburgmer.github.io/json-path-comparison/).

- Names in the dot notation may be unquoted (no spaces), single-quoted, or double-quoted.

- Names in the square bracket notation may be single-quoted or double-quoted.

- Unions of separate JSONPath expressions are allowed, e.g.

    $..[@.firstName,@.address.city]

- Fiter expressions, e.g. `$..book[?(@.price<10)]`, may omit the enclosing parentheses, like so `$..book[?@.price<10]`. 

- It supports a parent operator `^` for providing access to the parent node, borrowed from [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus).

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
class provides functionality to retrieve elements in the JSON document selected according to some criteria,

```
var selector = JsonSelector.Parse("$.books[?(@.price >= 22 && @.price < 30)]");

IList<JsonElement> elements = selector.Select(doc.RootElement);
```

The JsonCons.JsonPath library targets .Net Standard 2.1. Reference documentation is available [here](https://danielaparker.github.io/JsonCons.Net/ref/JsonCons.JsonPath.html)

Code examples may be found at:

[JSONPath examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPath.Examples/JsonPathExamples.cs)

## Acknowledgements

- Credit to [Stefan Goessner's JsonPath](https://goessner.net/articles/JsonPath/),
the original JSONPath.

- The specification of JsonCons JsonPath draws heavily on Christoph Burgmer's 
[JSONPath Comparison](https://cburgmer.github.io/json-path-comparison/).
Many of the test cases and some of the examples are borrowed from this resource.

- The specification of JSONPath filter expressions is greatly influenced by
James Saryerwinnie's [JMESPath](https://jmespath.org/specification.html)


