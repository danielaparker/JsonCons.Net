```text
bracket-expression = "[" bracketed-element *("," bracketed-element) "]"

bracketed-element = index / slice-expression / single-quoted-string / double-quoted-string
bracketed-element =/ wildcard / filter-expression / jsonpath
```

