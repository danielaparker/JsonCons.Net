# JsonCons.JsonPath

The JsonCons.JsonPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of JSONPath. It provides support for querying the 
JsonDocument/JsonElement classes. It targets .Net Standard 2.1.

JSONPath is a loosely standardized syntax for querying JSON. The original JavaScript JSONPath is a creation
of Stefan Goessner and is described [here](https://goessner.net/articles/JsonPath/). Since
the original, there have been many implementations in multiple languages, 
implementations that differ in significant ways. For an exhaustive comparison of differences, 
see Christoph Burgmer's [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/).

The JsonCons implementation attempts to preseve the essential flavor of JSONPath. Where
implementations differ, it generally takes into account the consensus as established in
the [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/). It supports
the familar queries against [Stefan Goessner's store](https://goessner.net/articles/JsonPath/index.html#e3):

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

In addition, the JsonCons incorporates some generalizations and tightening of syntax introduced
in the more innovative and rigourously specified implementations.

- Like [dchester/jsonpath](https://github.com/dchester/jsonpath), it requires
that unquoted names be restricted to one or more ASCII letters, digits, or underscores, and
must start with `A-Za-z_`. 

- It allows names in the dot notation to be unquoted, single-quoted, or double-quoted.

- It allows names in the square bracket notation to be single-quoted or double-quoted.

- Like [PaesslerAG/jsonpath/ajson](https://github.com/PaesslerAG/jsonpath), it allows filter expressions 
to omit the enclosing parentheses, as in `$..book[?(@.price<10)]`. 

- It supports unions of separate JSONPath selectors, e.g.

    $..[@.firstName,@.address.city]

- Like [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus), it supports a parent operator `^` 
for providing access to the parent node.

- Options are provided to exclude results corresponding to duplicate paths, and to 
sort results by paths.

The JsonCons implementation is described in an [ABNF grammar](Specification.md) with specification.
It explicitly implements a state machine that corresponds to this grammar. 

Documentation and examples may be found here:

- [JsonCons JSONPath](https://danielaparker.github.io/JsonCons.Net/articles/JsonPath/JsonConsJsonPath.html)

- [Reference](https://danielaparker.github.io/JsonCons.Net/ref/JsonCons.JsonPath.html)

- [Code examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JsonPath.Examples/JsonPathExamples.cs)

## Acknowledgements

- Credit to [Stefan Goessner's JsonPath](https://goessner.net/articles/JsonPath/),
the original JSONPath.

- The specification of JsonCons JsonPath draws heavily on Christoph Burgmer's 
[JSONPath Comparison](https://cburgmer.github.io/json-path-comparison/).
Many of the test cases and some of the examples are borrowed from this resource.

- The specification of JSONPath filter expressions is greatly influenced by
James Saryerwinnie's [JMESPath](https://jmespath.org/specification.html)

