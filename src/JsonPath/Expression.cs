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
    static class JsonConstants
    {
        static readonly JsonElement _trueValue;
        static readonly JsonElement _falseValue;
        static readonly JsonElement _nullValue;
        static readonly JsonElement _zeroValue;
        static readonly JsonElement _emptyString;
        static readonly JsonElement _emptyArray;
        static readonly JsonElement _emptyObject;

        static JsonConstants()
        {
            _trueValue = JsonDocument.Parse("true").RootElement;
            _falseValue = JsonDocument.Parse("false").RootElement;
            _nullValue = JsonDocument.Parse("null").RootElement;
            _zeroValue = JsonDocument.Parse("0").RootElement;
            _emptyString = JsonDocument.Parse("\"\"").RootElement;
            _emptyArray = JsonDocument.Parse(@"[]").RootElement;
            _emptyObject = JsonDocument.Parse(@"{}").RootElement;
        }

        internal static JsonElement True {get {return _trueValue;}}
        internal static JsonElement False { get { return _falseValue; } }
        internal static JsonElement Null { get { return _falseValue; } }
        internal static JsonElement Zero { get { return _zeroValue; } }
        internal static JsonElement EmptyString { get { return _emptyString; } }
        internal static JsonElement EmptyArray { get { return _emptyArray; } }
        internal static JsonElement EmptyObject { get { return _emptyObject; } }
    };

    interface IExpression
    {
        JsonElement Evaluate(JsonElement root,
                             PathNode stem, 
                             JsonElement current, 
                             ResultOptions options);
    }

    class Expression : IExpression
    {
        internal static bool IsFalse(JsonElement val)
        {
            //TestContext.WriteLine($"IsFalse {val}");
            var comparer = JsonElementEqualityComparer.Instance;
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

        internal static bool IsTrue(JsonElement val)
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

        public JsonElement Evaluate(JsonElement root,
                                    PathNode stem, 
                                    JsonElement current, 
                                    ResultOptions options)
        {
            //TestContext.WriteLine("Evaluate");

            Stack<JsonElement> stack = new Stack<JsonElement>();

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
                        var values = new List<JsonElement>();
                        var accumulator = new ValueAccumulator(values);
                        token.GetSelector().Select(root, 
                                                   new PathNode("@"), 
                                                   item, 
                                                   accumulator, 
                                                   options);
                        if (values.Count == 1)
                        {
                            stack.Push(values[0]);
                        }
                        else
                        {
                            stack.Push(JsonConstants.Null);
                        }
                        break;
                    }
                }
            }
            return stack.Count == 0 ? JsonConstants.Null : stack.Pop();
        }
    };

} // namespace JsonCons.JsonPathLib

