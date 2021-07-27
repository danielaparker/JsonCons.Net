# JsonCons JsonPath Specification

## Grammar

```
jsonpath = "$" [relative-location]
jsonpath = "@" [relative-location]

relative-location = "." relative-path
relative-location =/ ".." relative-path
relative-location =/ ".." bracket-expression [relative-location]
relative-location =/ "^" [relative-location]

relative-path = identifier [relative-location]
relative-path =/ wildcard [relative-location]

bracket-expression = "[" bracketed-element *("," bracketed-element) "]"

bracketed-element = index / slice-expression / single-quoted-string / double-quoted-string
bracketed-element =/ wildcard / filter-expression / jsonpath

filter-expression = "?" expression

slice-expression  = [integer] ":" [integer] [ ":" [integer] ]

integer            = ["-"]1*digit

identifier = unquoted-string / single-quoted-string / double-quoted-string

unquoted-string   =     %x30-39  /  ; 0-9
                        %x41-5A  /  ; A-Z
                        %x5F     /  ; _
                        %x61-7A  /  ; a-z
                        %x80-10FFFF ;    

double-quoted-string     = double-quote 1*(unescaped-char / single-quote / escaped-char / escaped-double-quote) double-quote
unescaped-char    = %x20-21 / %x23-2b / %x2d-5B / %x5D-10FFFF
Escape            = %x5C   ; Back slash: \
escaped-char      = Escape (
                        %x5C /          ; \    reverse solidus U+005C
                        %x2F /          ; /    solidus         U+002F
                        %x62 /          ; b    backspace       U+0008
                        %x66 /          ; f    form feed       U+000C
                        %x6E /          ; n    line feed       U+000A
                        %x72 /          ; r    carriage return U+000D
                        %x74 /          ; t    tab             U+0009
                        %x75 4HEXDIG )  ; uXXXX                U+XXXX

single-quote             = %x2c   ; Single quote: "'"
double-quote             = %x22   ; Double quote: '"'
escaped-double-quote      = Escape %x22          ; "    double quote  U+0022
escaped-single-quote      = Escape %x2c          ; '    single quote  U+002c

single-quoted-string     = single-quote 1*(unescaped-char / double-quote / escaped-char / escaped-single-quote) single-quote

expression = single-quoted-string 
expression =/ json-literal
expression =/ jsonpath 
expression =/ unary-expression / binary-expression / regex-binary-expression / paren-expression 
paren-expression  = "(" expression ")"
unary-expression=unary-operator expression
binary-expression = expression binary-operator expression
regex-binary-expression = expression regex-operator "/" regex-expression "/" [i]
unary-operator = "!" / "-"
binary-operator  = "*" / "/" / "%" / "+" / "-" / "&&" / "||" / <" / "<=" / "==" / ">=" / ">" / "!=" 
regex-operator = "=~"

; The "json-literal" is any valid JSON value.  
;
; The "regex-characters" represents regular expression characters

function-expression = unquoted-string  (
                        no-args  /
                        one-or-more-args )
no-args             = "(" ")"
one-or-more-args    = "(" ( function-arg *( "," function-arg ) ) ")"
function-arg        = expression

```

[!include(operators/and-expression.md)]

