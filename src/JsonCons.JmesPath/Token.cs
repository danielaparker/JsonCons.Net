using System;

namespace JsonCons.JmesPath
{
    enum JmesPathTokenKind
    {
        CurrentNode,
        LeftParen,
        RightParen,
        BeginMultiSelectHash,
        EndMultiSelectHash,
        BeginMultiSelectList,
        EndMultiSelectList,
        BeginFilter,
        EndFilter,
        Pipe,
        Separator,
        Key,
        Literal,
        Expression,
        BinaryOperator,
        UnaryOperator,
        Function,
        EndFunction,
        Argument,
        BeginExpressionType,
        EndExpressionType,
        EndOfExpression
    }

    readonly struct Token : IEquatable<Token>
    {
        readonly JmesPathTokenKind _tokenKind;
        readonly object _expr;

        internal Token(JmesPathTokenKind type)
        {
            _tokenKind = type;
            _expr = null;
        }

        internal JmesPathTokenKind TokenKind
        {
            get { return _tokenKind; }   
        }

        public bool Equals(Token other)
        {
            if (this._tokenKind == other._tokenKind)
                return true;
            else
                return false;        
        }
    }
}
