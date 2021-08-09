## Grammar

```text
jsonpath = root [relative-location]
jsonpath = current-node [relative-location]

relative-location = "." relative-path
relative-location =/ recursive-descent relative-path
relative-location =/ recursive-descent bracket-expression [relative-location]
relative-location =/ parent [relative-location]

relative-path = identifier [relative-location]
relative-path =/ wildcard [relative-location]

union = "[" union-element *("," union-element) "]"
union-element = index / slice / single-quoted-string / double-quoted-string
union-element =/ wildcard / filter-expression / jsonpath

```text
root              = "$"
current-node      = "@"
parent            = "^"
```

```
[!include[](./Identifier.md)]

```text
index             = integer 
slice             = [integer] ":" [integer] [ ":" [integer] ]
recursive-descent = ".."
wildcard          = "*"

integer = ["-"]1*digit
```

[!include[](./FilterExpression.md)]

