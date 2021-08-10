### Operator Precedence

The table below lists operators in descending order of precedence 
(upper rows bind tighter than lower ones.)

Precedence|Operator|Associativity
----------|--------|-----------
8 |`!` unary `-`    |Right
7 |`=~`             |Left
6 |`*` `/`  `%`     |Left 
5 |`+` `-`          |Left 
4 |`<` `>` `<=` `>=`|Left 
3 |`==` `!=`        |Left 
2 |`&&`             |Left 
1 |<code>&#124;&#124;</code> |Left 

The precedence rules may be overriden with explicit parentheses, e.g. (a || b) && c.

