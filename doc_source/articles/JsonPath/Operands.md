## Operands

The operands in an expression may be a jsonpath

```
jsonpath = "$" [relative-location]
jsonpath = "@" [relative-location]
```

or a JSON value
```
json-literal ; Any valid JSON value
```

For example, given a JSON document
```
[[1, 2, 3], [1], [2, 3], 1, 2]
```
the four queries below are all valid

```
$[?@ == 2]          

$[?@ == [1,2,3]]    

$[?@[0:1]==[1]]     

$[?$[2] == [2,3]]   
```
and produce the results
```json
[2]   

[[1,2, 3]]    

[[1, 2, 3],  [1]]  

[[1,2,3],[1],[2,3],1,2]
```

In an expression, a `jsonpath` is not evaluated as a
collection of matching JSON values, but rather, as a single JSON value.
The slice, recursive descent, wildcard, union and filter selectors,
which can evaluate to zero, one, or many items, are wrapped
in a Json array. The others - root, current node, parent node, 
identifier, and index selectors - evaluate to a single value if
matched, otherwise a null value.
  





