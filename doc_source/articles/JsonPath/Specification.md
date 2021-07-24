# JsonCons JsonPath Specification

## Grammar

```
JsonPath = "$" [RelativeLocation]
JsonPath = "@" [RelativeLocation]

RelativeLocation = "." RelativePath
RelativeLocation =/ ".." RelativePath
RelativeLocation =/ ".." BracketExpression [RelativeLocation]
RelativeLocation =/ "^" [RelativeLocation]

RelativePath = Identifier [RelativeLocation]
RelativePath =/ Index [RelativeLocation]
RelativePath =/ Wildcard [RelativeLocation]

BracketExpression = "[" BracketedElement *("," BracketedElement) "]"

BracketedElement = Index / SliceExpression / SingleQuotedString / DoubleQuotedString
BracketedElement =/ Wildcard / FilterExpression / JsonPath

FilterExpression = "?" Expression

SliceExpression  = [Integer] ":" [Integer] [ ":" [Integer] ]

Integer            = ["-"]1*Digit

Identifier = UnquotedString / SingleQuotedString / DoubleQuotedString

UnquotedString   = (%x41-5A / %x61-7A / %x5F) *(  ; A-Za-z_
                        %x30-39  /  ; 0-9
                        %x41-5A /  ; A-Z
                        %x5F    /  ; _
                        %x61-7A)   ; a-z

DoubleQuotedString     = DoubleQuote 1*(UnescapedChar / SingleQuote / EscapedChar / EscapedDoubleQuote) DoubleQuote
UnescapedChar    = %x20-21 / %x23-2b / %x2d-5B / %x5D-10FFFF
Escape            = %x5C   ; Back slash: \
EscapedChar      = Escape (
                        %x5C /          ; \    reverse solidus U+005C
                        %x2F /          ; /    solidus         U+002F
                        %x62 /          ; b    backspace       U+0008
                        %x66 /          ; f    form feed       U+000C
                        %x6E /          ; n    line feed       U+000A
                        %x72 /          ; r    carriage return U+000D
                        %x74 /          ; t    tab             U+0009
                        %x75 4HEXDIG )  ; uXXXX                U+XXXX

SingleQuote             = %x2c   ; Single quote: "'"
DoubleQuote             = %x22   ; Double quote: '"'
EscapedDoubleQuote      = Escape %x22          ; "    double quote  U+0022
EscapedSingleQuote      = Escape %x2c          ; '    single quote  U+002c

SingleQuotedString     = SingleQuote 1*(UnescapedChar / DoubleQuote / EscapedChar / EscapedSingleQuote) SingleQuote



Expression = SingleQuotedString 
Expression =/ JsonLiteral
Expression =/ JsonPath 
Expression =/ UnaryExpression / BinaryExpression / RegexBinaryExpression / ParenExpression 
ParenExpression  = "(" Expression ")"
UnaryExpression=UnaryOperator Expression
BinaryExpression = Expression BinaryOperator Expression
RegexBinaryExpression = Expression RegexOperator RegexExpression
RegexExpression = "/" RegexCharacters "/" [i]   
UnaryOperator = "!" / "-"
BinaryOperator  = "*" / "/" / "%" / "+" / "-" / "&&" / "||" / <" / "<=" / "==" / ">=" / ">" / "!=" 
RegexOperator = "=~"

; The "JsonLiteral" is any valid JSON value.  
;
; The "RegexCharacters" represents regular Expression characters

FunctionExpression = UnquotedString  (
                        NoArgs  /
                        OneOrMoreArgs )
NoArgs             = "(" ")"
OneOrMoreArgs    = "(" ( FunctionArg *( "," FunctionArg ) ) ")"
FunctionArg        = Expression

```


