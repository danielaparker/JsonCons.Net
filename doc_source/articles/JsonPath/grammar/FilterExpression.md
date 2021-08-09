```text
filter-expression = "?" expression

expression = single-quoted-string 
expression =/ json-literal ; any valid JSON value
expression =/ jsonpath 
expression =/ unary-expression / binary-expression / regex-expression / paren-expression 
paren-expression  = "(" expression ")"
unary-expression=unary-operator expression
binary-expression = expression binary-operator expression
regex-expression = expression regex-operator "/" regex "/" [i]
unary-operator = "!" / "-"
binary-operator  = "*" / "/" / "%" / "+" / "-" / "&&" / "||" / <" / "<=" / "==" / ">=" / ">" / "!=" 
regex-operator = "=~"
;
; "regex" represents regular expression characters

function-expression = unquoted-string  (
                        no-args  /
                        one-or-more-args )
no-args             = "(" ")"
one-or-more-args    = "(" ( function-arg *( "," function-arg ) ) ")"
function-arg        = expression
```
