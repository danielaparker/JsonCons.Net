using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonCons.JsonPathLib
{
    enum TokenType
    {
        RootNode,
        CurrentNode,
        Expression,
        Lparen,
        Rparen,
        BeginUnion,
        EndUnion,
        BeginFilter,
        EndFilter,
        BeginExpression,
        End_indexExpression,
        EndArgumentExpression,
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
        TokenType _type;

        public Token(TokenType type)
        {
            _type = type;
        }

        public TokenType Type
        {
          get { return _type; }   
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
