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
        IJsonValue Evaluate(IJsonValue root,
                            IJsonValue current, 
                            ResultOptions options);
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
                    return comparer.Equals(val,JsonConstants.EmptyObject);
                case JsonValueKind.String:
                    return comparer.Equals(val,JsonConstants.EmptyString);
                case JsonValueKind.Number:
                    return comparer.Equals(val,JsonConstants.Zero);
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

        public IJsonValue Evaluate(IJsonValue root,
                                    IJsonValue current, 
                                    ResultOptions options)
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
                        var result = token.GetSelector().Evaluate(root, item, options);
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
                            return JsonConstants.Null;
                        }

                        IJsonValue val;
                        if (!token.GetFunction().TryEvaluate(argStack, out val))
                        {
                            return JsonConstants.Null;
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
                        IJsonValue val = token.GetExpression().Evaluate(root, item, options);
                        stack.Push(val);
                        break;
                    }
                    default:
                        break;
                }
            }
            return stack.Count == 0 ? JsonConstants.Null : stack.Pop();
        }
    };

} // namespace JsonCons.JsonPathLib

