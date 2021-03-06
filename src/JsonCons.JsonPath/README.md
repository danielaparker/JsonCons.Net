# JsonCons.JsonPath

The JsonCons.JsonPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of JSONPath. It targets .Net Standard 2.1.

The JsonCons.JsonPath library provides support for querying
JsonDocument/JsonElement instances with code like this:
```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonCons.JsonPath;

public static class Example
{

    public static void Main(string[] args)
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
    ],
    ""bicycle"": {
      ""color"": ""red"",
      ""price"": 19.95
    }
  }
}
        ";

        using JsonDocument doc = JsonDocument.Parse(jsonString);

        IList<JsonElement> results = JsonSelector.Select(doc.RootElement, "$..book[?(@.price >= 5 && @.price < 10)]");

        var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};        
        Console.WriteLine(JsonSerializer.Serialize(results, serializerOptions));
    }
}
```
Output:
```json
[
  {
    "category": "reference",
    "author": "Nigel Rees",
    "title": "Sayings of the Century",
    "price": 8.95
  },
  {
    "category": "fiction",
    "author": "Herman Melville",
    "title": "Moby Dick",
    "isbn": "0-553-21311-3",
    "price": 8.99
  }
]
```

JSONPath is a loosely standardized syntax for querying JSON. The original JavaScript JSONPath is a creation
of Stefan Goessner and is described [here](https://goessner.net/articles/JsonPath/). Since
the original, there have been many implementations in multiple languages, 
implementations that differ in significant ways. For an exhaustive comparison of differences, 
see Christoph Burgmer's [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/).

The JsonCons implementation attempts to preseve the essential flavor of JSONPath. Where
implementations differ, it generally takes into account the consensus as established in
the [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/). It supports
the familar queries against the store:

JSONPath	                | Result
---------------------------|----------------------------------------
`$.store.book[*].author`	| The authors of all books in the store
`$..author`	        | All authors
`$.store.*`	        | All things in store, which are some books and a red bicycle.
`$.store..price`	        | The price of everything in the store.
`$..book[2]`	        | The third book
`$..book[-1:]`	        | The last book in order.
`$..book[:2]`	        | The first two books
`$..book[0,1]`             | &nbsp;
`$..book[?(@.isbn)]`	| Filter all books with isbn number
`$..book[?(@.price<10)]`	| Filter all books cheapier than 10
`$..*`	                | All members of JSON structure.

  
In addition, JsonCons.JsonPath incorporates some generalizations and tightening of syntax introduced
in the more innovative and formally specified implementations.

- Unquoted names follow the same rules for the selector and in filter
expressions, and forbid characters such as hyphens that cause difficulties
in expressions.

- Names in the dot notation may be unquoted, single-quoted, or double-quoted.

- Names in the square bracket notation may be single-quoted or double-quoted.

- Like [PaesslerAG/jsonpath/ajson](https://github.com/PaesslerAG/jsonpath), filter expressions 
may omit the parentheses around the expression, as in `$..book[?@.price<10]`. 

- Unions may have separate JSONPath selectors, e.g.
```csharp
    $..[@.firstName,@.address.city]
```
- A parent selector `^`, borrowed from [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus), 
provides access to the parent node.

- Options are provided to exclude results corresponding to duplicate paths, and to 
sort results by paths.

The JsonCons implementation is described in an [ABNF grammar](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/Specification.html) with specification.
It explicitly implements a state machine that corresponds to this grammar. 

Documentation and examples may be found here:

- [JsonCons JSONPath](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/JsonConsJsonPath.html)

- [Reference](https://danielaparker.github.io/JsonCons.Net/ref/JsonCons.JsonPath.html)

- [Code examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPath.Examples/JsonPathExamples.cs)

## Acknowledgements

- Credit to [Stefan Goessner's JsonPath](https://goessner.net/articles/JsonPath/),
the original JSONPath. While not a formal specification, it was enormously
influential.

- The specification of JsonCons.JsonPath draws heavily on Christoph Burgmer's 
[JSONPath Comparison](https://cburgmer.github.io/json-path-comparison/).
Many of the test cases and some of the examples are borrowed from this resource.

- The specification of JsonCons.JsonPath filter expressions is greatly influenced by
James Saryerwinnie's [JMESPath](https://jmespath.org/specification.html),
and largely follows JMSPath with regards to truthiness, comparator syntax and semantics,
and function syntax and semantics. In Stefan Goessner's original article, filter
expressions were left unspecified, and in their original implementations, they were
simply Javascript or PHP. 

