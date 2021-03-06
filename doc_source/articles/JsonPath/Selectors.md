## Selectors

After tokenization, a JSONPath string is transformed into a null terminated 
linked list of selectors. There are ten different kinds of selectors:

[Root selector ($)](#Selector1)  
[Current node selector (@)](#Selector2)  
[Parent node selector (^)](#Selector3)  
[Identifier selector](#Selector4)  
[Index selector](#Selector5)  
[Slice selector](#Selector6)  
[Recursive descent selector (..)](#Selector7)  
[Wildcard selector (*)](#Selector8)  
[Union selector](#Selector9)  
[Filter selector](#Selector10)  

The selectors arranged in a linked list take a JSON value as
input and produce a list of JSON values as output. Evaluation
proceeds as follows:

* The selector at the head of the list will select zero, one or
many items from its provided value, and, for each item,
evaluate the tail of the list (recursively.) For example,
given
<br/><br/><pre><code>[{"a":"bar"},{"b":"baz"},{"b":"qux"}]</code></pre>
and a JSONPath
<br/><br/><pre><code>$.*.b</code></pre>
the root selector will select the root and evaluate `*.b(root)`,
the wildcard selector will select the elements in the root and
evaluate `b({"a":"bar"})`, `b({"b":"baz"})`, and `b({"b":"qux"})`.
   
* When the tail is null, evaluation stops. The last selector
in the list will add its provided value to the output list. 

Note that only the last selector adds to the output list.

Consider the JSON document
```
{"foo":[
  {"a":"bar"},
  {"b":"baz"},
  {"b":"qux"}
]}
```
and JSONPath
```
$.foo[*].b 
```

After tokenization, the JSONPath becomes

![SelectorList](./images/SelectorList.png)

Evaluation proceeds as follows:

![ExecuteSelectorList](./images/ExecuteSelectorList.png)

The final result is
```
["baz","qux"]
```

<div id="Selector1"/> 

### Root selector

```text
root = "$"
```

The symbol "$" represents the root JSON value, the JSON document to be evaluated.
The root selector selects this value.

<div id="Selector2"/> 

### Current node selector

```text
current-node = "@"
```

The symbol "@" represents the "current node". At the start of an expression, 
the current node is the document to be evaluated, and as the expression 
is evaluated, it changes to reflect the node currently being processed.   
The current node selector selects this value.

<div id="Selector3"/> 

### Parent selector 

```text
parent = "^"
```

The symbol "^" represents the parent of the current node.

Consider the JSON document 

```
[
    {
      "author" : "Haruki Murakami",
      "title": "A Wild Sheep Chase",
      "reviews": [{"rating": 4, "reviewer": "Nan"}]
    },
    {
      "author" : "Sergei Lukyanenko",
      "title": "The Night Watch",
      "reviews": [{"rating": 5, "reviewer": "Alan"},
                  {"rating": 3,"reviewer": "Anne"}]
    },
    {
      "author" : "Graham Greene",
      "title": "The Comedians",
      "reviews": [{"rating": 4, "reviewer": "Lisa"},
                  {"rating": 5, "reviewer": "Robert"}]
    }
]
```

JsonCons supports the parent selector, `^`, borrowed from [JSONPath Plus](https://www.npmjs.com/package/jsonpath-plus),
that allows you to select book objects based on criteria applied to descendent values.

Query                               | Output paths
------------------------------------|------
`$[*]reviews[?(@.rating == 5)]`     | "$[1]['reviews'][0]"
&nbsp;                              | "$[2]['reviews'][1]"
`$[*]reviews[?(@.rating == 5)]^`    | "$[1]['reviews']"
&nbsp;                              | "$[2]['reviews']"
`$[*]reviews[?(@.rating == 5)]^^`   | "$[1]"
&nbsp;                              | "$[2]"

The JSONPath expression
```
$[*].reviews[?(@.rating == 5)]^^
```

selects all the book objects that have ratings of 5:
```
[
    {
        "author": "Sergei Lukyanenko",
        "reviews": [
            {
                "rating": 5,
                "reviewer": "Alan"
            },
            {
                "rating": 3,
                "reviewer": "Anne"
            }
        ],
        "title": "The Night Watch"
    },
    {
        "author": "Graham Greene",
        "reviews": [
            {
                "rating": 4,
                "reviewer": "Lisa"
            },
            {
                "rating": 5,
                "reviewer": "Robert"
            }
        ],
        "title": "The Comedians"
    }
]
```

<div id="Selector4"/> 

### Identifier selector

[!include[](./grammar/Identifier.md)]

An identifier selector selects zero or one values from a JSON value,
depending on whether it is an object that has a member with a
corresponding name.

<div id="Selector5"/> 

### Index selector 

```text
index   = integer
```

An index selector selects zero or one values from a JSON value,
depending on whether it is an array with an element at a
corresponding index. Indexing is zero-based. A negative index
indicates that indexing is relative to the end of the array.

<div id="Selector6"/> 

### Slice selector

```text
slice   = [integer] ":" [integer] [ ":" [integer] ]
```

JsonCons jsonpath slices have the same semantics as Python slices

The syntax for a slice is
```
[start:stop:step]
```
Each component is optional.

- If `start` is omitted, it defaults to `0` if `step` is positive,
or the end of the array if `step` is negative.

- If `stop` is omitted, it defaults to the length of the array if `step` 
is positive, or the beginning of the array if `step` is negative.

- If `step` is omitted, it defaults to `1`.

Slice expression|       Description
--------|--------------------------------
`[start:stop]`  | Items `start` through `stop-1`
`[start:]`      | Items `start` to the end of the array
`[:stop]`       | Items from the beginning of the array through `stop-1`
`[:]`           | All items
`[start:stop:step]`|Items `start` up to but not including `stop`, by `step` 

A component `start`, `stop`, or `step` may be a negative number.

Example | Description
--------|------------
$[-1]    | Last item 
$[-2:]   | Last two items
$[:-2]   | All items except the last two
$[::-1]    | All items, reversed
$[1::-1]   | First two items, reversed
$[:-3:-1]  | Last two items, reversed
$[-3::-1]  | All items except the last two, reversed

<div id="Selector7"/> 

### Recursive descent selector 

```text
recursive-descent = ".."
```

The recursive descent selector performs a select operation
on a provided JSON value as follows:

- If its tail is null, it adds the value to the result list,
and exits. Otherwise, it continues as below.

- If the provided value is a JSON array, it first provides 
the value to its tail, and then iterates over each 
item in the array, recursively performing the select
operation on each item.

- If the provided value is a JSON object, it first provides 
the value to its tail, and then iterates over each 
property in the object, recursively performing the select
operation on each property's value.

Consider the JSON document
```
{"foo":[
  {"a":"bar"},
  {"b":"baz"},
  {"b":"qux"}
]}
```
and JSONPath
```
$..b 
```

After tokenization, the JSONPath becomes

![SelectorListWithRecursiveDescent](./images/SelectorListWithRecursiveDescent.png)

Evaluation proceeds as follows:

![EvaluateSelectorListWithRecursiveDescent](./images/EvaluateSelectorListWithRecursiveDescent.png)

The final result is
```
["baz","qux"]
```

<div id="Selector8"/> 

### Wildcard selector 

```text
wildcard = "*"
```

The wildcard selector can select multiple items. If provided with an array,
it will select all the array's elements, and if provided with an object,
it will select the value part of all the object's name-value pairs.

<div id="Selector9"/> 

### Unions

[!include[](./grammar/BracketExpression.md)]

In JsonCons, a JSONPath union element can be

- an index or slice expression
- a single quoted name
- a double quoted name
- a filter
- a wildcard, i.e. `*`
- a path relative to the root of the JSON document (begins with `$`)
- a path relative to the current value being processed (begins with `@`)

To illustrate, the path expression below selects the first and second titles, 
the last, and the third from [Stefan Goessner's store](https://goessner.net/articles/JsonPath/index.html#e3):

```
"$.store.book[0:2,-1,?(@.author=='Herman Melville')].title"
```

<div id="Selector10"/> 

### Filter selector

[!include[](./grammar/FilterExpression.md)]

JSONPath uses filter expressions `[?<expr>]` to restrict the set of nodes
returned by a path, e.g. `$..book[?(@.price<10)]` returns the books with 
prices less than 10. Filter expressions are applied to each element in a 
JSON array or each member in a JSON object. The symbol `@` represents the 
value currently being processed. An expression evaluates to true or false,
if true, the array element, or value part of an object member, is selected.

An expression is considered false if it evaluates to any of the following values:

- empty array: [],
- empty object: {},
- empty string: "",
- false,
- null.

It is considered true if it is not false.

