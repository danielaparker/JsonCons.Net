using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

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

    public interface ISelector 
    {
        void Select(JsonElement root,
                    JsonElement current, 
                    IList<JsonElement> nodes);

        void AppendSelector(ISelector tail);
    };

    class Token : IEquatable<Token>
    {
        TokenType _type;
        object _expression;

        public Token(TokenType type)
        {
            _type = type;
        }

        public Token(ISelector selector)
        {
            _type = TokenType.Selector;
            _expression = selector;
        }

        public TokenType Type
        {
          get { return _type; }   
        }

        public ISelector GetSelector()
        {
            return _type == TokenType.Selector ? (ISelector)_expression : null;
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
