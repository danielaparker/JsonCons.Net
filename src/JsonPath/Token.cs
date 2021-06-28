using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    enum JsonPathTokenKind
    {
        RootNode,
        CurrentNode,
        Expression,
        LeftParen,
        RightParen,
        BeginUnion,
        EndUnion,
        BeginFilter,
        EndFilter,
        BeginExpression,
        EndExpression,
        Separator,
        Value,
        Selector,
        Function,
        EndFunction,
        Argument,
        EndArgument,
        UnaryOperator,
        BinaryOperator
    };

    struct Token : IEquatable<Token>
    {
        JsonPathTokenKind _type;
        object _expr;

        internal Token(JsonPathTokenKind type)
        {
            _type = type;
            _expr = null;
        }

        internal Token(ISelector selector)
        {
            _type = JsonPathTokenKind.Selector;
            _expr = selector;
        }

        internal Token(IExpression expr)
        {
            _type = JsonPathTokenKind.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            _type = JsonPathTokenKind.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            _type = JsonPathTokenKind.BinaryOperator;
            _expr = expr;
        }

        internal Token(IFunction expr)
        {
            _type = JsonPathTokenKind.Function;
            _expr = expr;
        }

        internal Token(JsonElement expr)
        {
            _type = JsonPathTokenKind.Value;
            _expr = expr;
        }

        internal JsonPathTokenKind TokenKind
        {
            get { return _type; }   
        }

        internal bool IsOperator
        {
            get
            {
                switch(_type)
                {
                    case JsonPathTokenKind.UnaryOperator:
                        return true;
                    case JsonPathTokenKind.BinaryOperator:
                        return true;
                    default:
                        return false;
                }
            }
        }

        internal bool IsRightAssociative
        {
            get
            {
                switch(_type)
                {
                    case JsonPathTokenKind.Selector:
                        return true;
                    case JsonPathTokenKind.UnaryOperator:
                        return GetUnaryOperator().IsRightAssociative;
                    case JsonPathTokenKind.BinaryOperator:
                        return GetBinaryOperator().IsRightAssociative;
                    default:
                        return false;
                }
            }
        }

        internal int PrecedenceLevel 
        {
            get
            {
                switch(_type)
                {
                    case JsonPathTokenKind.Selector:
                        return 11;
                    case JsonPathTokenKind.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case JsonPathTokenKind.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 0;
                }
            }
        }

        internal JsonElement GetValue()
        {
            Debug.Assert(_type == JsonPathTokenKind.Value);
            return (JsonElement)_expr;
        }

        internal ISelector GetSelector()
        {
            Debug.Assert(_type == JsonPathTokenKind.Selector);
            return (ISelector)_expr;
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(_type == JsonPathTokenKind.Function);
            return (IFunction)_expr;
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(_type == JsonPathTokenKind.Expression);
            return (IExpression)_expr;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(_type == JsonPathTokenKind.UnaryOperator);
            return (IUnaryOperator)_expr;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(_type == JsonPathTokenKind.BinaryOperator);
            return (IBinaryOperator)_expr;
        }

        public bool Equals(Token other)
        {
            if (this._type == other._type)
                return true;
            else
                return false;        
        }

        public override string ToString()
        {
            switch(_type)
            {
                case JsonPathTokenKind.RootNode:
                    return "RootNode";
                case JsonPathTokenKind.CurrentNode:
                    return "CurrentNode";
                case JsonPathTokenKind.BeginFilter:
                    return "BeginFilter";
                case JsonPathTokenKind.EndFilter:
                    return "EndFilter";
                case JsonPathTokenKind.BeginUnion:
                    return "BeginUnion";
                case JsonPathTokenKind.EndUnion:
                    return "EndUnion";
                case JsonPathTokenKind.Value:
                    return $"Value {_expr}";
                case JsonPathTokenKind.Selector:
                    return $"Selector {_expr}";
                case JsonPathTokenKind.UnaryOperator:
                    return $"UnaryOperator {_expr}";
                case JsonPathTokenKind.BinaryOperator:
                    return $"BinaryOperator {_expr}";
                case JsonPathTokenKind.Function:
                    return $"Function {_expr}";
                case JsonPathTokenKind.EndFunction:
                    return "EndFunction";
                case JsonPathTokenKind.Argument:
                    return "Argument";
                case JsonPathTokenKind.EndArgument:
                    return "EndArgument";
                case JsonPathTokenKind.Expression:
                    return "Expression";
                case JsonPathTokenKind.BeginExpression:
                    return "BeginExpression";
                case JsonPathTokenKind.EndExpression:
                    return "EndExpression";
                case JsonPathTokenKind.LeftParen:
                    return "LeftParen";
                case JsonPathTokenKind.RightParen:
                    return "RightParen";
                case JsonPathTokenKind.Separator:
                    return "Separator";
                default:
                    return "Other";
            }
        }
    };

} // namespace JsonCons.JsonPathLib
