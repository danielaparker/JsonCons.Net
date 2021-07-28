### UnaryMinusExpression

```text
unary-minus-expression = -expression
```

The - (unary minus) operator negates the value of the expression.
It is only valid if the expression evaluates to a number.

### Example

JSON Document: 
```json
[{"key": 0}, {"key": 42}, {"key": -1}, {"key": 41}, {"key": 43}, {"key": 42.0001}, {"key": 41.9999}, {"key": 100}, {"some": "value"}]
```
JSONPath: 
```text
"$[?-@.key > -42]"
```
Result:
```json
[{"key": 0}, {"key": -1}, {"key": 41}, {"key": 41.9999}]
```
