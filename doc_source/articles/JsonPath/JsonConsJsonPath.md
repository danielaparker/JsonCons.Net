# JsonCons JSONPath

The JsonCons.JsonPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of JSONPath. It provides support for querying  
JsonDocument/JsonElement instances with code like this:
```csharp
var selector = JsonSelector.Parse("$.books[?(@.price >= 22 && @.price < 30)]");

IList<JsonElement> elements = selector.Select(doc.RootElement);
```
It targets .Net Standard 2.1.

JSONPath is a loosely standardized syntax for querying JSON. The original JavaScript JSONPath is a creation
of Stefan Goessner and is described [here](https://goessner.net/articles/JsonPath/). Since
the original, there have been many implementations in multiple languages, 
implementations that differ in significant ways. For an exhaustive comparison of differences, 
see Christoph Burgmer's [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/).

The JsonCons implementation attempts to preseve the essential flavor of JSONPath. Where
implementations differ, it generally takes into account the consensus as established in
the [JSONPath comparison](https://cburgmer.github.io/json-path-comparison/).

In addition, the JsonCons incorporates some generalizations and tightening of syntax introduced
in the more innovative and formally specified implementations.

- Unquoted names follow the same rules for the selector and in filter
expressions, and forbid characters such as hyphens that cause difficulties
in expressions.

- Names in the dot notation may be unquoted, single-quoted, or double-quoted.

- Names in the square bracket notation may be single-quoted or double-quoted.

- Like [PaesslerAG/jsonpath/ajson](https://github.com/PaesslerAG/jsonpath), filter expressions 
may omit the parentheses around the expression, as in `$..book[?@.price<10]`. 

- Unions may have separate JSONPath selectors, e.g.

    $..[@.firstName,@.address.city]

- A parent selector `^`, borrowed from [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus), 
provides access to the parent node.

- Options are provided to exclude results corresponding to duplicate paths, and to 
sort results by paths.

The JsonCons implementation is described in an [ABNF grammar](Specification.md) with specification.
It explicitly implements a state machine that corresponds to this grammar. 

[Paths](#S1) 

[Selectors](#S2) 

[Filter expressions](#S3) 

[Duplicates and ordering](#S4) 

<div id="S1"/> 

## Paths

JSONPath uses paths to select values. It is a feature of JSONPath that it selects values in the original JSON document, 
and does not create JSON elements that are not in the original. 

Selector      |       Description
--------------|--------------------------------
`$`                                     |Represents the root JSON value
`@`                                     |Represents the value currently being processed
`.<name>` or `.'<name>'` or `."<name>"` |The '.' character followed by a JSON object member name, unquoted or quoted   
`['<name>']` or `["<name>"]`            |Subscript operator with quoted JSON object member name 
`[<index>]`                             |Index expression used to access elements in a JSON array. A negative index value indicates that indexing is relative to the end of the array.
`*` or ['*']                            |Wildcard. All objects/elements regardless their names.
`[start:stop:step]`                     |Array slice notation, following [Python](https://python-reference.readthedocs.io/en/latest/docs/brackets/slicing.html)
`^`                                     |Parent operator borrowed from [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus)
`..`                                    |Recursive descent
`[,]`                                   |Union operator for alternative object names or array indices or JSONPath expressions 
`?<expr>`                               |Filter by expression

Paths can use the dot-notation or the bracket-notation.

Select the first (indexed 0) book in [Stefan Goessner's store](https://goessner.net/articles/JsonPath/index.html#e3) using the dot notation:

    $.store.book[0]

or

    $.'store'.'book'[0]

or

    $."store"."book"[0]

Unquoted names in the dot notation are restricted to digits 0-9, letters A-Z and a-z, 
the underscore character _, and unicode coded characters that are non-ascii. Note 
that names with hyphens must be quoted.

Select the first (indexed 0) book using the bracket-notation: 

    $['store']['book'][0]

or

    $["store"]["book"][0]

Recursively select all book titles under '$.store':

    $.'store'..'title'

Union of a subset of books identified by index:

    $.store[@.book[0],@.book[1],@.book[3]]

Union of the fourth book and all books with price > 10:

    $.store[@.book[3],@.book[?(@.price > 10)]]

<div id="S2"/> 

[!include[Selectors](./Selectors.md)]

<div id="S3"/> 

## Filter Expressions

[Stefan Goessner's JSONPath](http://goessner.net/articles/JsonPath/) 
does not provide any specification for the allowable filter expressions, 
simply stating that expressions can be anything that the underlying script 
engine can handle. `JsonCons` expressions support the following comparision 
and arithmetic operators. 

[!include[Operands](./Operands.md)]

### Binary operators

Operator| Expression |      Description
--------|--------------------------------
`*`     | expression * expression | Left times right
`/`     | expression / expression | Left divided by right
`%`     | expression % expression | Remainder
`+`     | expression + expression | Left plus right
`-`     | expression - expression | Left minus right
`&&`    | [expression && expression](operators/and-expression.md) | Left is true and right is true
<code>&#124;&#124;</code>| [expression <code>&#124;&#124;</code> expression](operators/or-expression.md) | Left is true or right is true
`==`    |[expression == expression](operators/equality-expression.md)| Left is equal to right 
`!=`    | expression != expression | Left is not equal to right
`<`     | expression < expression | Left is less than right
`<=`    | expression <= expression | Left is less than or equal to right
`>`     | expression > expression | Left is greater than right
`>=`    | expression >= expression | Left is greater than or equal to right
`=~`    | expression `=~` "/" regex "/" [i] | Left matches regular expression, e.g. [?(@.author =~ /Evelyn.*?/)]

The ordering operators `>`, `>=`, `<`, `<=` are only valid if both left and right are numbers,
or if both left and right are strings. Otherwise the item will be excluded from the result set.

### Unary operators

Operator| Expression |      Description
--------|------------|-------------------
`!`     | [!expression](operators/not-expression.md) | Reverses true/false
`-`     | [-expression](operators/unary-minus-expression.md) | Negates right

The unary minus operator is only valid if right is a number.

### Operator precedence

Precedence|Operator|Associativity
----------|--------|-----------
1 |`!` unary `-`    |Right
2 |`=~`             |Left
3 |`*` `/`  `%`     |Left 
4 |`+` `-`          |Left 
5 |`<` `>` `<=` `>=`|Left 
6 |`==` `!=`        |Left 
7 |`&&`             |Left 
8 |<code>&#124;&#124;</code> |Left 

The precedence rules may be overriden with explicit parentheses, e.g. (a || b) && c.

### Functions

Support for function in filter expressions is a JsonCons extension.

Functions can be passed JSONPath paths, single quoted strings, literal JSON values
such as `1.5`, `true`, or `{"foo" : "bar"}`, or expressions such as `@.price*10`. 
Functions can be passed either a path that selects from the root JSON value `$`, 
or a path that selects from the current node `@`.

Function|Description
----------|--------
[abs](functions/abs.md)|Returns the absolute value of a number.
[avg](functions/avg.md)|Returns the average of the items in an array of numbers.
[ceil](functions/ceil.md)|Returns the smallest integer value not less than a given number.
[contains](functions/contains.md)|Returns true if a source array contains a search value, or a source string contains a search string.
[ends_with](functions/ends_with.md)|Returns true if the source string ends with the suffix string, otherwise false.
[floor](functions/floor.md)|Returns the largest integer value not greater than a given number.
[keys](functions/keys.md)|Returns an array of keys in an object.
[length](functions/length.md)|Returns the length of an array, object or string.
[max](functions/max.md)|Returns the highest number found in an array of numbers,or the highest string in an array of strings.
[min](functions/min.md)|Returns the lowest number found in an array of numbers, or the lowest string in an array of strings.
[prod](functions/prod.md)|Returns the product of the items in an array of numbers.
[starts_with](functions/starts_with.md)|Returns true if the source string starts with the prefix string, otherwise false.
[sum](functions/sum.md)|Returns the sum of the items in an array of numbers.
[to_number](functions/to_number.md)|If string, returns the parsed number. If number, returns the passed in value.
[tokenize](functions/tokenize.md)|Returns an array of strings formed by splitting the source string into an array of strings, separated by substrings that match a given regular expression pattern.

<div id="S4"/> 

## Duplicates and ordering

Consider the JSON document 

```json
{
    "books":
    [
        {
            "title" : "A Wild Sheep Chase",
            "author" : "Haruki Murakami"
        },
        {
            "title" : "The Night Watch",
            "author" : "Sergei Lukyanenko"
        },
        {
            "title" : "The Comedians",
            "author" : "Graham Greene"
        },
        {
            "title" : "The Night Watch",
            "author" : "Phillips, David Atlee"
        }
    ]
}
```
with selector
```
$.books[1,1,3].title
```
Note that the second book, _The Night Watch_ by Sergei Lukyanenko, is selected twice.

The majority of JSONPath implementations will produce (with duplicate paths allowed):

Path|Value
-------|------------------
 `$['books'][1]['title']` | "The Night Watch" 
 `$['books'][1]['title']` | "The Night Watch" 
 `$['books'][3]['title']` | "The Night Watch" 

A minority will produce (with duplicate paths excluded):

Path|Value
---------|------------------
`$['books'][1]['title']` | "The Night Watch"
`$['books'][3]['title']` | "The Night Watch"

The `JsonPath.Select` functions default to allowing
duplicates, but have an option for no duplicates.

By default, the ordering of results is unspecified, although the user may
expect array ordering at least to be preserved.  The `JsonPath.Select` functions 
provide an option for sorting results by paths.

