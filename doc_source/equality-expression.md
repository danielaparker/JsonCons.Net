### Equality Expression

```
and-expression = expression "&&" expression
```

If the left side evaluates to true, then the and-expression evaluates
to the right side, otherwise it evaluates to the left side.

A false value is any value on this list:

- empty array: [],
- empty object: {},
- empty string: "",
- false,
- null,
- zero.

A true value is any value that is not false.
