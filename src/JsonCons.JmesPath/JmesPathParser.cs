using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JmesPath
{
    /// <summary>
    /// Defines a custom exception object that is thrown when JMESPath parsing fails.
    /// </summary>    

    public class JmesPathParseException : Exception
    {
        /// <summary>
        /// The line in the JMESPath string where a parse error was detected.
        /// </summary>
        public int LineNumber {get;}

        /// <summary>
        /// The column in the JMESPath string where a parse error was detected.
        /// </summary>
        public int ColumnNumber {get;}

        internal JmesPathParseException(string message, int line, int column)
            : base(message)
        {
            LineNumber = line;
            ColumnNumber = column;
        }

        /// <summary>
        /// Returns an error message that describes the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString ()
        {
            return $"{base.Message} at line {LineNumber} and column {ColumnNumber}";
        }
    };

    enum JmesPathState
    {
        Start,
        LhsExpression,
        RhsExpression,
        SubExpression,
        ExpressionType,
        ComparatorExpression,
        FunctionExpression,
        Argument,
        ExpressionOrExpressionType,
        QuotedString,
        RawString,
        RawStringEscapeChar,
        QuotedStringEscapeChar,
        EscapeU1, 
        EscapeU2, 
        EscapeU3, 
        EscapeU4, 
        EscapeExpectSurrogatePair1, 
        EscapeExpectSurrogatePair2, 
        EscapeU5, 
        EscapeU6, 
        EscapeU7, 
        EscapeU8, 
        Literal,
        KeyExpr,
        ValExpr,
        IdentifierOrFunctionExpr,
        UnquotedString,
        KeyValExpr,
        Number,
        Digit,
        IndexOrSliceExpression,
        BracketSpecifier,
        BracketSpecifierOrMultiSelectList,
        Filter,
        MultiSelectList,
        MultiSelectHash,
        RhsSliceExpressionStop,
        RhsSliceExpressionStep,
        ExpectRbracket,
        ExpectRparen,
        ExpectDot,
        ExpectFilterRbracket,
        ExpectRbrace,
        ExpectColon,
        ExpectMultiSelectList,
        CmpLtOrLte,
        CmpEq,
        CmpGtOrGte,
        CmpNe,
        ExpectPipeOrOr,
        ExpectAnd
    }

    ref struct JmesPathParser
    {
        ReadOnlySpan<char> _span;
        int _index;
        int _column;
        int _line;
        Stack<JmesPathState> _stateStack;
        Stack<Token>_outputStack;
        Stack<Token>_operatorStack;

        internal JmesPathParser(string input)
        {
            _span = input.AsSpan();
            _index = 0;
            _column = 1;
            _line = 1;
            _stateStack = new Stack<JmesPathState>();
            _outputStack = new Stack<Token>();
            _operatorStack = new Stack<Token>();
        }

        internal JmesPathEvaluator Parse()
        {
            return new JmesPathEvaluator();
        }

        private void PushToken(Token token)
        {
            switch (token.TokenKind)
            {
            }
        }
    }
}
