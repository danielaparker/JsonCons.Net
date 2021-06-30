using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;
        
namespace JsonCons.JsonPathLib
{

    interface IExpression
    {
         bool TryEvaluate(IJsonValue root,
                          IJsonValue current, 
                          ResultOptions options,
                          out IJsonValue value);
    }

    class Expression : IExpression
    {
        internal static bool IsFalse(IJsonValue val)
        {
            //TestContext.WriteLine($"IsFalse {val}");
            var comparer = JsonValueEqualityComparer.Instance;
            switch (val.ValueKind)
            {
                case JsonValueKind.False:
                    return true;
                case JsonValueKind.Null:
                    return true;
                case JsonValueKind.Array:
                    return val.GetArrayLength() == 0;
                case JsonValueKind.Object:
                    return val.EnumerateObject().MoveNext() == false;
                case JsonValueKind.String:
                    return val.GetString().Length == 0;
                case JsonValueKind.Number:
                {
                    Decimal dec;
                    if (val.TryGetDecimal(out dec))
                    {
                        return dec == 0;
                    }
                    else
                    {
                        return false;
                    }
                }
                default:
                    return false;
            }
        }

        internal static bool IsTrue(IJsonValue val)
        {
            return !IsFalse(val);
        }

        IReadOnlyList<Token> _tokens;

        internal Expression(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;

            //TestContext.WriteLine("Expression constructor");
            //foreach (var token in _tokens)
            //{
            //    TestContext.WriteLine($"    {token}");
            //}
        }

        public  bool TryEvaluate(IJsonValue root,
                                 IJsonValue current, 
                                 ResultOptions options,
                                 out IJsonValue value)
        {
            //TestContext.WriteLine("Evaluate");

            Stack<IJsonValue> stack = new Stack<IJsonValue>();
            IList<IJsonValue> argStack = new List<IJsonValue>();

            foreach (var token in _tokens)
            {
                switch (token.TokenKind)
                {
                    case JsonPathTokenKind.Value:
                    {
                        stack.Push(token.GetValue());
                        break;
                    }
                    case JsonPathTokenKind.UnaryOperator:
                    {
                        Debug.Assert(stack.Count >= 1);
                        var item = stack.Pop();
                        var val = token.GetUnaryOperator().Evaluate(item);
                        stack.Push(val);
                        break;
                    }
                    case JsonPathTokenKind.BinaryOperator:
                    {
                        Debug.Assert(stack.Count >= 2);
                        var rhs = stack.Pop();
                        var lhs = stack.Pop();

                        var val = token.GetBinaryOperator().Evaluate(lhs, rhs);
                        stack.Push(val);
                        break;
                    }
                    case JsonPathTokenKind.RootNode:
                        stack.Push(root);
                        break;
                    case JsonPathTokenKind.CurrentNode:
                        stack.Push(current);
                        break;
                    case JsonPathTokenKind.Selector:
                    {
                        if (stack.Count == 0)
                        {
                            stack.Push(current);
                        }

                        var item = stack.Pop();
                        var values = new List<IJsonValue>();
                        var result = token.GetSelector().Evaluate(root, new PathNode("@"), item, options);
                        stack.Push(result);
                        break;
                    }
                    case JsonPathTokenKind.Argument:
                        Debug.Assert(stack.Count != 0);
                        argStack.Add(stack.Peek());
                        stack.Pop();
                        break;
                    case JsonPathTokenKind.Function:
                    {
                        if (token.GetFunction().Arity.HasValue && token.GetFunction().Arity.Value != argStack.Count)
                        {
                            value = JsonConstants.Null;
                            return false;
                        }

                        IJsonValue val;
                        if (!token.GetFunction().TryEvaluate(argStack, out val))
                        {
                            value = JsonConstants.Null;
                            return false;
                        }
                        argStack.Clear();
                        stack.Push(val);
                        break;
                    }
                    case JsonPathTokenKind.Expression:
                    {
                        if (stack.Count == 0)
                        {
                            stack.Push(current);
                        }

                        var item = stack.Peek();
                        stack.Pop();
                        IJsonValue val;
                        if (!token.GetExpression().TryEvaluate(root, item, options, out val))
                        {
                            value = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    default:
                        break;
                }
            }

            if (stack.Count == 0)
            {
                value = JsonConstants.Null;
                return false;
            }
            else
            {
                value = stack.Pop();
                return true;
            }
        }
    };

} // namespace JsonCons.JsonPathLib

