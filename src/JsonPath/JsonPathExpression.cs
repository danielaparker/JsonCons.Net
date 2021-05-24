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

    class Token 
    {
        TokenType _type;

        protected Token(TokenType type)
        {
            _type = type;
        }

        public TokenType Type
        {
          get { return _type; }   
        }
    };

    class RootToken : Token
    {
        public RootToken()
            : base(TokenType.RootNode)
        {
        }
    }

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

    class ExpressionCompiler 
    {
        string _input;
        int _index = 0;
        int _column = 1;
        int _line = 1;
        Stack<ExpressionState> _stateStack = new Stack<ExpressionState>();

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
                                PushToken(new RootToken());
                                _stateStack.Push(ExpressionState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                _stateStack.Push(ExpressionState.PathRhs);
                                _stateStack.Push(ExpressionState.ExpectFunctionExpr);
                                _stateStack.Push(ExpressionState.UnquotedString);
                                break;
                            }
                        }
                        break;
                    }
                    default:
                        ++_index;
                        break;
                    }
            }
            return new JsonPathExpression();
        }

        private void PushToken(Token token)
        {
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
        public static JsonPathExpression Compile(string expr)
        {
            var compiler = new ExpressionCompiler(expr);
            return compiler.Compile();
        }
    }
}
