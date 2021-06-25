using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    enum TokenKind
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
        EndArgument,
        Separator,
        Value,
        Selector,
        Function,
        EndFunction,
        Argument,
        EndOfExpression,
        UnaryOperator,
        BinaryOperator
    };

    struct Token : IEquatable<Token>
    {
        TokenKind _type;
        object _expr;

        internal Token(TokenKind type)
        {
            _type = type;
            _expr = null;
        }

        internal Token(ISelector selector)
        {
            _type = TokenKind.Selector;
            _expr = selector;
        }

        internal Token(IExpression expr)
        {
            _type = TokenKind.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            _type = TokenKind.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            _type = TokenKind.BinaryOperator;
            _expr = expr;
        }

        internal Token(JsonElement expr)
        {
            _type = TokenKind.Value;
            _expr = expr;
        }

        internal TokenKind Type
        {
            get { return _type; }   
        }

        internal bool IsOperator
        {
            get
            {
                switch(_type)
                {
                    case TokenKind.UnaryOperator:
                        return true;
                    case TokenKind.BinaryOperator:
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
                    case TokenKind.Selector:
                        return true;
                    case TokenKind.UnaryOperator:
                        return GetUnaryOperator().IsRightAssociative;
                    case TokenKind.BinaryOperator:
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
                    case TokenKind.Selector:
                        return 11;
                    case TokenKind.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case TokenKind.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 0;
                }
            }
        }

        internal JsonElement GetValue()
        {
            Debug.Assert(_type == TokenKind.Value);
            return (JsonElement)_expr;
        }

        internal ISelector GetSelector()
        {
            Debug.Assert(_type == TokenKind.Selector);
            return (ISelector)_expr;
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(_type == TokenKind.Expression);
            return (IExpression)_expr;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(_type == TokenKind.UnaryOperator);
            return (IUnaryOperator)_expr;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(_type == TokenKind.BinaryOperator);
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
                case TokenKind.RootNode:
                    return "RootNode";
                case TokenKind.CurrentNode:
                    return "CurrentNode";
                case TokenKind.BeginFilter:
                    return "BeginFilter";
                case TokenKind.EndFilter:
                    return "EndFilter";
                case TokenKind.Value:
                    return "Value";
                case TokenKind.Selector:
                    return $"Selector {_expr}";
                case TokenKind.UnaryOperator:
                    return "UnaryOperator";
                case TokenKind.BinaryOperator:
                    return "BinaryOperator";
                default:
                    return "Other";
            }
        }
    };

} // namespace JsonCons.JsonPathLib
