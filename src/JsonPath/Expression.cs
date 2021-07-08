using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    static class JsonConstants
    {
        static JsonConstants()
        {
            True = new TrueOperand();
            False = new FalseOperand();
            Null = new NullOperand();
        }

        internal static IOperand True {get;}
        internal static IOperand False {get;}
        internal static IOperand Null {get;}
    }

    interface IExpression
    {
         bool TryEvaluate(DynamicResources resources,
                          IOperand root,
                          IOperand current, 
                          ResultOptions options,
                          out IOperand value);
    }

    sealed class Expression : IExpression
    {
        internal static bool IsFalse(IOperand val)
        {
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

        internal static bool IsTrue(IOperand val)
        {
            return !IsFalse(val);
        }

        IReadOnlyList<Token> _tokens;

        internal Expression(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
        }

        public  bool TryEvaluate(DynamicResources resources,
                                 IOperand root,
                                 IOperand current, 
                                 ResultOptions options,
                                 out IOperand result)
        {
            Stack<IOperand> stack = new Stack<IOperand>();
            IList<IOperand> argStack = new List<IOperand>();

            foreach (var token in _tokens)
            {
                switch (token.TokenKind)
                {
                    case JsonPathTokenKind.Value:
                    {
                        stack.Push(token.GetValue());
                        break;
                    }
                    case JsonPathTokenKind.RootNode:
                    {
                        stack.Push(root);
                        break;
                    }
                    case JsonPathTokenKind.CurrentNode:
                    {
                        stack.Push(current);
                        break;
                    }
                    case JsonPathTokenKind.UnaryOperator:
                    {
                        Debug.Assert(stack.Count >= 1);
                        var item = stack.Pop();
                        IOperand value;
                        if (!token.GetUnaryOperator().TryEvaluate(item, out value))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(value);
                        break;
                    }
                    case JsonPathTokenKind.BinaryOperator:
                    {
                        Debug.Assert(stack.Count >= 2);
                        var rhs = stack.Pop();
                        var lhs = stack.Pop();

                        IOperand value;
                        if (!token.GetBinaryOperator().TryEvaluate(lhs, rhs, out value))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(value);
                        break;
                    }
                    case JsonPathTokenKind.Selector:
                    {
                        Debug.Assert(stack.Count >= 1);
                        IOperand val = stack.Peek();
                        stack.Pop();
                        IOperand value;
                        if (token.GetSelector().TryEvaluate(resources, root, new PathNode("@"), val, options, out value))
                        {
                            stack.Push(value);
                        }
                        else
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
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

                        IOperand value;
                        if (!token.GetFunction().TryEvaluate(argStack, out value))
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
                        IOperand value;
                        if (!token.GetExpression().TryEvaluate(resources, root, current, options, out value))
                        {
                            result = JsonConstants.Null;
                            return false;
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

