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

    class Token : IEquatable<Token>
    {
        TokenKind _type;
        object _expr;

        public Token(TokenKind type)
        {
            _type = type;
        }

        public Token(ISelector selector)
        {
            _type = TokenKind.Selector;
            _expr = selector;
        }

        public Token(IExpression expr)
        {
            _type = TokenKind.Expression;
            _expr = expr;
        }

        public TokenKind Type
        {
          get { return _type; }   
        }

        public ISelector GetSelector()
        {
            return _type == TokenKind.Selector ? (ISelector)_expr : null;
        }

        public IExpression GetExpression()
        {
            return _type == TokenKind.Expression ? (IExpression)_expr : null;
        }

        public bool Equals(Token other)
        {
            if (other == null)
                 return false;

            if (this._type == other._type)
                return true;
            else
                return false;        }
    };

} // namespace JsonCons.JsonPathLib
