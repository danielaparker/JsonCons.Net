using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    enum ExpressionState
    {
        Start,
        ExpectFunctionExpr,
        PathLhs,
        PathRhs,
        FilterExpression,
        ExpressionRhs,
        RecursiveDescentOrExpressionLhs,
        PathOrLiteralOrFunction,
        JsonTextOrFunction,
        JsonTextOrFunctionName,
        JsonTextString,
        JsonValue,
        JsonString,
        IdentifierOrFunctionExpr,
        NameOrLeftBracket,
        UnquotedString,
        Anything,
        Number,
        FunctionExpression,
        Argument,
        ZeroOrOneArguments,
        OneOrMoreArguments,
        Identifier,
        SingleQuotedString,
        DoubleQuotedString,
        BracketedUnquotedNameOrUnion,
        UnionExpression,
        IdentifierOrUnion,
        BracketSpecifierOrUnion,
        Bracketed_wildcard,
        IndexOrSlice,
        WildcardOrUnion,
        UnionElement,
        IndexOrSliceOrUnion,
        Index,
        Integer,
        Digit,
        SliceExpressionStop,
        SliceExpressionStep,
        CommaOrRightBracket,
        ExpectRightBracket,
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
        Expression,
        ComparatorExpression,
        EqOrRegex,
        ExpectRegex,
        Regex,
        CmpLtOrLte,
        CmpGtOrGte,
        CmpNe,
        ExpectOr,
        ExpectAnd
    };

    public class JsonPathException : Exception
    {
        public JsonPathException(string message)
            : base(message)
        {
        }
    }

    class ExpressionCompiler 
    {
        string _input;
        int _index = 0;
        int _column = 1;
        int _line = 1;
        Stack<ExpressionState> _stateStack = new Stack<ExpressionState>();
        List<Token> _tokens = new List<Token>();

        public ExpressionCompiler(string input)
        {
            _input = input;
        }

        public JsonPathExpression Compile()
        {
            _stateStack = new Stack<ExpressionState>();
            _index = 0;
            _column = 1;

            _stateStack.Push(ExpressionState.Start);

            while (_index < _input.Length)
            {
                switch (_stateStack.Peek())
                {
                    case ExpressionState.Start: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                AdvancePastSpaceCharacter();
                                break;
                            case '$':
                            {
                                PushToken(new Token(TokenType.RootNode));
                                _stateStack.Push(ExpressionState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                throw new JsonPathException("Invalid state");
                            }
                        }
                        break;
                    }
                    default:
                        ++_index;
                        break;
                    }
            }
            return new JsonPathExpression(_tokens);
        }

        private void PushToken(Token token)
        {
            switch (token.Type)
            {
            case TokenType.RootNode:
                break;
            }
        }

        private void AdvancePastSpaceCharacter()
        {
            switch (_input[_index])
            {
                case ' ':case '\t':
                    ++_index;
                    ++_column;
                    break;
                case '\r':
                    if (_index+1 < _input.Length && _input[_index+1] == '\n')
                        ++_index;
                    ++_line;
                    _column = 1;
                    ++_index;
                    break;
                case '\n':
                    ++_line;
                    _column = 1;
                    ++_index;
                    break;
                default:
                    break;
            }
        }
    };

    public class JsonPathExpression
    {
        List<Token> _tokens;

        internal JsonPathExpression(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public void Evaluate(JsonElement value)
        {
        }

        public static JsonPathExpression Compile(string expr)
        {

            var compiler = new ExpressionCompiler(expr);
            return compiler.Compile();
        }
    }

} // namespace JsonCons.JsonPathLib
