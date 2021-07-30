# JsonCons JsonPath Specification

[!include[Grammar](./Grammar.md)]

## Selectors

After tokenization, a JSONPath string is transformed into a null terminated 
linked list of selectors. There are ten different kinds of selectors:

- Root selector ("$")
- Current node selector ("@")
- Parent node selector ("^")
- Identifier selector
- Index selector
- Slice selector
- Recursive descent selector ("..")
- Wildcard selector ("*")
- Union selector
- Filter selector

Each selector supports a select operation for selecting zero or more values 
from a provided JSON value, and for adding selected values to a result list.

Executing a list of selectors against a JSON value means executing 
the select operation on the head of the list with that JSON value 
as an argument. This operation selects zero or more items from the provided value, 
and executes the tail of the list for each item with the item as an argument.
This proceeds recursively until the tail is null, when the provided JSON
value is added to the result list. Note that only the last selector in the list
adds to the result list.

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

Execution proceeds as follows:

![ExecuteSelectorList](./images/ExecuteSelectorList.png)

The final result is
```
["bar","qux"]
```

## Filter Expressions

[!include[True and False Values](./TrueAndFalseValues.md)]

[!include[Operator Precedence](./operators/OperatorPrecedence.md)]

[!include[Or Expression](./operators/or-expression.md)]

[!include[And Expression](./operators/and-expression.md)]

[!include[Not Expression](./operators/not-expression.md)]

[!include[Unary Minus Expression](./operators/unary-minus-expression.md)]

### Functions

[!include[abs](./functions/abs.md)]

[!include[avg](./functions/avg.md)]

[!include[ceil](./functions/ceil.md)]

[!include[contains](./functions/contains.md)]

[!include[ends_with](./functions/ends_with.md)]

[!include[floor](./functions/floor.md)]

[!include[keys](./functions/keys.md)]

[!include[length](./functions/length.md)]

[!include[max](./functions/max.md)]

[!include[min](./functions/min.md)]

[!include[prod](./functions/prod.md)]

[!include[starts_with](./functions/starts_with.md)]

[!include[sum](./functions/sum.md)]

[!include[to_number](./functions/to_number.md)]

[!include[tokenize](./functions/tokenize.md)]

