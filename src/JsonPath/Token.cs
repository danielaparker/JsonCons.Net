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

    public interface ISelector 
    {
        void Select(JsonElement root,
                    JsonElement current, 
                    IList<JsonElement> nodes);

        void AppendSelector(ISelector tail);
    };

    class Token : IEquatable<Token>
    {
        TokenKind _type;
        object _expression;

        public Token(TokenKind type)
        {
            _type = type;
        }

        public Token(ISelector selector)
        {
            _type = TokenKind.Selector;
            _expression = selector;
        }

        public TokenKind Type
        {
          get { return _type; }   
        }

        public ISelector GetSelector()
        {
            return _type == TokenKind.Selector ? (ISelector)_expression : null;
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
