# JsonCons JsonPath Specification

[!include[Grammar](./Grammar.md)]

[!include[Selectors](./Selectors.md)]

## Filter Expressions

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

