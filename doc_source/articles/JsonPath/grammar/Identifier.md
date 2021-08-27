```text
identifier = unquoted-string / single-quoted-string / double-quoted-string

unquoted-string   =     %x30-39  /  ; 0-9
                        %x41-5A  /  ; A-Z
                        %x5F     /  ; _
                        %x61-7A  /  ; a-z
                        %x80-10FFFF ; U+0080 ...    

single-quoted-string     = single-quote 
                           1*(unescaped-char / double-quote / 
                              escaped-char / escaped-single-quote) 
                           single-quote

double-quoted-string     = double-quote 
                           1*(unescaped-char / single-quote / 
                              escaped-char / escaped-double-quote) 
                           double-quote

escaped-single-quote      = escape single-quote    ; '    single quote  U+002c
escaped-double-quote      = escape double-quote    ; "    double quote  U+0022

single-quote             = %x2c                    ; Single quote: "'"
double-quote             = %x22                    ; Double quote: '"'

unescaped-char    = %x20-21 / %x23-2b / %x2d-5B / %x5D-10FFFF
escape            = %x5C                ; Back slash: \
escaped-char      = escape (
                        %x5C /          ; \    reverse solidus U+005C
                        %x2F /          ; /    solidus         U+002F
                        %x62 /          ; b    backspace       U+0008
                        %x66 /          ; f    form feed       U+000C
                        %x6E /          ; n    line feed       U+000A
                        %x72 /          ; r    carriage return U+000D
                        %x74 /          ; t    tab             U+0009
                        %x75 4HEXDIG )  ; uXXXX                U+XXXX
```
