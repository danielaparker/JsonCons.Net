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

Each selector is respsonsible for performing a select operation
against a single JSON value. The end result is a set of
selected values. Evaluation works as follows:
 
- Provide the value to the selector at the head of the list
- This selector will select zero or more items from the provided value, 
and, for each item, provide the item to its tail.
- This proceeds recursively until the tail is null. The last selector
in the list will add its provided value to the result set. 

Note that only the last selector in the list adds to the result set.

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

The symbol "$" represents the root JSON value, the JSON document to be evaluated.
The root selector selects this value.

<div id="Selector2"/> 

### Current node selector

The symbol "@" represents the "current node". At the start of an expression, 
the current node is the document to be evaluated, and as the expression 
is evaluated, it changes to reflect the node currently being processed.   
The current node selector selects this value.

<div id="Selector3"/> 

### Parent selector 

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

<div id="Selector6"/> 

### Slices

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

### Recursive descent selector (..)

The recursive descent selector performs a select operation
on a provided JSON value as follows:

- If its tail is null, it adds the value to the result list,
and exits. Otherwise, it first provides the value to its tail,
and continues as below.

- If the provided value is a JSON array, it iterates over each 
item in the array, and recursively performs the select
operation on each item.

- If the provided value is a JSON object, it iterates over each 
property in the object, and recursively performs the select
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

![SelectorListWithRecursiveDescent](./images/SelectorListWithRecursiveDescent)

Evaluation proceeds as follows:

![EvaluateSelectorListWithRecursiveDescent](./images/EvaluateSelectorListWithRecursiveDescent.png)

The final result is
```
["baz","qux"]
```

<div id="Selector9"/> 

### Unions

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
- null,
- zero.

It is considered true if it is not false.

