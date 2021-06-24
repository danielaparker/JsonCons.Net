using System;
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
        LParen,
        RParen,
        BeginUnion,
        EndUnion,
        BeginFilter,
        EndFilter,
        BeginExpression,
        EndExpression,
        EndArgument,
        Separator,
        Literal,
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

        internal TokenKind Type
        {
            get { return _type; }   
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

        internal ISelector GetSelector()
        {
            return _type == TokenKind.Selector ? (ISelector)_expr : null;
        }

        internal IExpression GetExpression()
        {
            return _type == TokenKind.Expression ? (IExpression)_expr : null;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            return _type == TokenKind.UnaryOperator ? (IUnaryOperator)_expr : null;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            return _type == TokenKind.BinaryOperator ? (IBinaryOperator)_expr : null;
        }

        public bool Equals(Token other)
        {
            if (this._type == other._type)
                return true;
            else
                return false;        
        }
    };

} // namespace JsonCons.JsonPathLib
