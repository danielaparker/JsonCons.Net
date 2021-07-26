### Or Expression

```
or-expression = expression "||" expression

```
If both left and right sides are null, the or-expression evaluates
to null. Otherwise, if the left side evaluates to true,
it will evaluate to its left side, and if
the left side evaluates to false, it will evaluate to its
right side.

A false value is any value on this list:

- empty array: [],
- empty object: {},
- empty string: "",
- false,
- null,
- zero.

A true value is any value that is not false