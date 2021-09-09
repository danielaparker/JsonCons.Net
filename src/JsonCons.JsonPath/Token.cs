using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPath
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
        BeginArgument,
        Separator,
        Value,
        Selector,
        BeginArguments,
        Function,
        EndArguments,
        Argument,
        EndArgument,
        UnaryOperator,
        BinaryOperator
    };

    readonly struct Token : IEquatable<Token>
    {
        readonly JsonPathTokenKind _type;
        readonly object? _expr;

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

        internal Token(IValue expr)
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

        internal IValue GetValue()
        {
            Debug.Assert(_type == JsonPathTokenKind.Value);
            return _expr as IValue ?? throw new InvalidOperationException("Expression is null");
        }

        internal ISelector GetSelector()
        {
            Debug.Assert(_type == JsonPathTokenKind.Selector);
            return _expr as ISelector ?? throw new InvalidOperationException("Expression is null");;
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(_type == JsonPathTokenKind.Function);
            return _expr as IFunction ?? throw new InvalidOperationException("Expression is null");;
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(_type == JsonPathTokenKind.Expression);
            return _expr as IExpression ?? throw new InvalidOperationException("Expression is null");;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(_type == JsonPathTokenKind.UnaryOperator);
            return _expr as IUnaryOperator ?? throw new InvalidOperationException("Expression is null");;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(_type == JsonPathTokenKind.BinaryOperator);
            return _expr as IBinaryOperator ?? throw new InvalidOperationException("Expression is null");;
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
                case JsonPathTokenKind.BeginArguments:
                    return "BeginArguments";
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
                case JsonPathTokenKind.EndArguments:
                    return "EndArguments";
                case JsonPathTokenKind.Argument:
                    return "Argument";
                case JsonPathTokenKind.EndArgument:
                    return "EndArgument";
                case JsonPathTokenKind.Expression:
                    return "Expression";
                case JsonPathTokenKind.BeginArgument:
                    return "BeginArgument";
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

} // namespace JsonCons.JsonPath
