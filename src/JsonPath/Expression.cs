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
         bool Evaluate(DynamicResources resources,
                          IJsonValue root,
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

        public  bool Evaluate(DynamicResources resources,
                                 IJsonValue root,
                                 IJsonValue current, 
                                 ResultOptions options,
                                 out IJsonValue result)
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
                        var value = token.GetUnaryOperator().Evaluate(item);
                        if (value == JsonConstants.Null)
                        {
                            stack.Push(JsonConstants.Null);
                        }
                        else
                        {
                            stack.Push(value);
                        }
                        break;
                    }
                    case JsonPathTokenKind.BinaryOperator:
                    {
                        Debug.Assert(stack.Count >= 2);
                        var rhs = stack.Pop();
                        var lhs = stack.Pop();

                        var value = token.GetBinaryOperator().Evaluate(lhs, rhs);
                        if (value == JsonConstants.Null)
                        {
                            stack.Push(JsonConstants.Null);
                        }
                        else
                        {
                            stack.Push(value);
                        }
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
                        stack.Push(token.GetSelector().Evaluate(resources, root, new PathNode("@"), item, options));
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
                            result = JsonConstants.Null;
                            return false;
                        }

                        IJsonValue value;
                        if (!token.GetFunction().Evaluate(argStack, out value))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        argStack.Clear();
                        stack.Push(value);
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
                        IJsonValue value;
                        if (!token.GetExpression().Evaluate(resources, root, item, options, out value))
                        {
                            stack.Push(JsonConstants.Null);
                            //result = JsonConstants.Null;
                            //return false;
                        }
                        else
                        {
                            stack.Push(value);
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            if (stack.Count == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            else
            {
                result = stack.Pop();
                return true;
            }
        }
    };

} // namespace JsonCons.JsonPathLib

