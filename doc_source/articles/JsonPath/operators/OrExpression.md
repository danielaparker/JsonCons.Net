### Or Expression

```
OrExpression = Expression "||" Expression
```
An or expression will evaluate to its left side if its left side
evaluates to not false, it will evaluate to its right side if its
right side evaluates to not false. 

That an expression evaluates to false means that it evaluates to
any of:

- empty array: [],
- empty object: {},
- empty string: "",
- false,
- null,
- zero.

If both the left and right sides evaluate to false, then the expression evaluates to 
its left side. 

