using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;
        
namespace JsonCons.JsonPathLib
{
    enum ExprState
    {
        Start,
        RootOrCurrentNode,
        ExpectFunctionExpr,
        PathExpression,
        PathRhs,
        FilterExpression,
        ExpressionRhs,
        PathStepOrRecursiveDescent,
        PathOrValueOrFunction,
        JsonTextOrFunction,
        JsonTextOrFunctionName,
        JsonTextString,
        JsonValue,
        JsonString,
        IdentifierOrFunctionExpr,
        NameOrLeftBracket,
        UnquotedString,
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
        BracketedWildcard,
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
        ExpectRightParen,
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

    class JsonPathCompiler 
    {
        string _input;
        int _index = 0;
        int _column = 1;
        int _line = 1;
        Stack<ExprState> _stateStack = new Stack<ExprState>();
        Stack<Token>_outputStack = new Stack<Token>();
        Stack<Token>_operatorStack = new Stack<Token>();

        internal JsonPathCompiler(string input)
        {
            _input = input;
        }

        internal JsonPath Compile()
        {
            _stateStack = new Stack<ExprState>();
            _index = 0;
            _column = 1;

            _stateStack.Push(ExprState.Start);

            StringBuilder buffer = new StringBuilder();

            Int32? sliceStart = null;
            Int32? sliceStop = null;
            Int32 sliceStep = 1;
            Int32 selector_id = 0;
            UInt32 cp = 0;
            UInt32 cp2 = 0;

            var trueSpan = "true".AsSpan();
            var falseSpan = "false".AsSpan();
            var nullSpan = "null".AsSpan();

            while (_index < _input.Length)
            {
                switch (_stateStack.Peek())
                {
                    case ExprState.Start: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                            case '@':
                            {
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                throw new JsonException("Invalid state");
                            }
                        }
                        break;
                    }
                    case ExprState.PathRhs: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                            {
                                _stateStack.Push(ExprState.PathStepOrRecursiveDescent);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            default:
                            {
                                _stateStack.Pop();
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.PathStepOrRecursiveDescent:
                        switch (_input[_index])
                        {
                            case '.':
                                PushToken(new Token(new RecursiveDescentSelector()));
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.NameOrLeftBracket);
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.PathExpression);
                                break;
                        }
                        break;
                    case ExprState.NameOrLeftBracket: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[': // [ can follow ..
                                _stateStack.Pop(); _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathExpression);
                                break;
                        }
                        break;
                    case ExprState.PathExpression: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '*':
                                PushToken(new Token(new WildcardSelector()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '.':
                                throw new JsonException("Expected identifier");
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrFunctionExpr);
                                _stateStack.Push(ExprState.UnquotedString);
                                break;
                        }
                        break;
                    case ExprState.RootOrCurrentNode: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.RootNode));
                                PushToken(new Token(new RootSelector(selector_id++)));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(TokenKind.CurrentNode));
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Syntax error");
                        }
                        break;
                    case ExprState.UnquotedString: 
                    {
                        Char ch = _input[_index];
                        if (Char.IsLetterOrDigit(ch) || ch == '_')
                        {
                            buffer.Append (ch);
                            ++_index;
                            ++_column;
                        }
                        else
                        {
                            _stateStack.Pop(); // UnquotedString
                        }
                        break;                    
                    }
                    case ExprState.IdentifierOrFunctionExpr:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                /*
                                var f = resources.get_function(buffer);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                buffer.Clear();
                                PushToken(current_node_arg);
                                PushToken(new Token(f));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                */
                                break;
                            }
                            default:
                            {
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.Identifier:
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    case ExprState.SingleQuotedString:
                        switch (_input[_index])
                        {
                            case '\'':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(ExprState.QuotedStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.DoubleQuotedString: 
                        switch (_input[_index])
                        {
                            case '\"':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(ExprState.QuotedStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.QuotedStringEscapeChar:
                        switch (_input[_index])
                        {
                            case '\"':
                                buffer.Append('\"');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '\'':
                                buffer.Append('\'');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '\\': 
                                buffer.Append('\\');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '/':
                                buffer.Append('/');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'b':
                                buffer.Append('\b');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'f':
                                buffer.Append('\f');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'n':
                                buffer.Append('\n');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'r':
                                buffer.Append('\r');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 't':
                                buffer.Append('\t');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'u':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU1);
                                break;
                            default:
                                throw new JsonException("Illegal escape character");
                        }
                        break;
                    case ExprState.EscapeU1:
                        cp = AppendToCodepoint(0, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU2);
                        break;
                    case ExprState.EscapeU2:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU3);
                        break;
                    case ExprState.EscapeU3:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU4);
                        break;
                    case ExprState.EscapeU4:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        if (Char.IsHighSurrogate((Char)cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop(); _stateStack.Push(ExprState.EscapeExpectSurrogatePair1);
                        }
                        else
                        {
                            buffer.Append(Char.ConvertFromUtf32((int)cp));
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                        }
                        break;
                    case ExprState.EscapeExpectSurrogatePair1:
                        switch (_input[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                throw new JsonException("Invalid codepoint");
                        }
                        break;
                    case ExprState.EscapeExpectSurrogatePair2:
                        switch (_input[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU5);
                                break;
                            default:
                                throw new JsonException("Invalid codepoint");
                        }
                        break;
                    case ExprState.EscapeU5:
                        cp2 = AppendToCodepoint(0, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU6);
                        break;
                    case ExprState.EscapeU6:
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU7);
                        break;
                    case ExprState.EscapeU7:
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU8);
                        break;
                    case ExprState.EscapeU8:
                    {
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        UInt32 codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        buffer.Append(Char.ConvertFromUtf32((int)codepoint));
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }
                    case ExprState.ExpectRightBracket:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected ]");
                        }
                        break;
                    case ExprState.ExpectRightParen:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                ++_index;
                                ++_column;
                                PushToken(new Token(TokenKind.RightParen));
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.ExpressionRhs);
                                break;
                            default:
                                throw new JsonException("Expected )");
                        }
                        break;
                    case ExprState.BracketSpecifierOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.BeginExpression));
                                PushToken(new Token(TokenKind.LeftParen));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.Expression);
                                _stateStack.Push(ExprState.ExpectRightParen);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '?':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.BeginFilter));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                _stateStack.Pop(); _stateStack.Push(ExprState.WildcardOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.RootNode));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);                                
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.CurrentNode));
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected bracket specifier or union");
                        }
                        break;
                    case ExprState.UnionExpression:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(ExprState.PathExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.Separator));
                                _stateStack.Push(ExprState.UnionElement);
                                ++_index;
                                ++_column;
                                break;
                            case ']': 
                                PushToken(new Token(TokenKind.EndUnion));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.UnionElement:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSlice);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSlice);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '(':
                            {
                                PushToken(new Token(TokenKind.BeginExpression));
                                PushToken(new Token(TokenKind.LeftParen));
                                _stateStack.Pop(); _stateStack.Push(ExprState.Expression);
                                _stateStack.Push(ExprState.ExpectRightParen);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '?':
                            {
                                PushToken(new Token(TokenKind.BeginFilter));
                                _stateStack.Pop(); _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                PushToken(new Token(new WildcardSelector()));
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.RootNode));
                                PushToken(new Token(new RootSelector(selector_id++)));
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(TokenKind.CurrentNode)); // ISSUE
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected bracket specifier or union");
                        }
                        break;
                    case ExprState.FilterExpression:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                PushToken(new Token(TokenKind.EndFilter));
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                throw new JsonException("Expected comma or right bracket");
                        }
                        break;
                    }
                    case ExprState.IdentifierOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']': 
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                buffer.Clear();
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                PushToken(new Token(TokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.BracketedWildcard:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[':
                            case ']':
                            case ',':
                            case '.':
                                PushToken(new Token(new WildcardSelector()));
                                buffer.Clear();
                                _stateStack.Pop();
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.IndexOrSliceOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            {
                                Int32 n;
                                if (!Int32.TryParse(buffer.ToString(),out n))
                                {
                                    throw new JsonException("Invalid index");
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                _stateStack.Pop(); // IndexOrSliceOrUnion
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ',':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                Int32 n;
                                if (!Int32.TryParse(buffer.ToString(), out n))
                                {
                                    throw new JsonException("Invalid index");
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                PushToken(new Token(TokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ':':
                            {
                                if (!(buffer.Length == 0))
                                {
                                    Int32 n;
                                    string s = buffer.ToString();
                                    if (!Int32.TryParse(s, out n))
                                    {
                                        n = s.StartsWith("-") ? Int32.MinValue : Int32.MaxValue;
                                    }
                                    sliceStart = n;
                                    buffer.Clear();
                                }
                                PushToken(new Token(TokenKind.BeginUnion));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.SliceExpressionStop);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.Index:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case '.':
                            case ',':
                            {
                                Int32 n;
                                if (!Int32.TryParse(buffer.ToString(), out n))
                                {
                                    throw new JsonException("Invalid index");
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                _stateStack.Pop(); // index
                                break;
                            }
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.SliceExpressionStop:
                    {
                        if (!(buffer.Length == 0))
                        {
                            Int32 n;
                            string s = buffer.ToString();
                            if (!Int32.TryParse(s, out n))
                            {
                                n = s.StartsWith("-") ? Int32.MinValue : Int32.MaxValue;
                            }
                            sliceStop = n;
                            buffer.Clear();
                        }
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case ',':
                                PushToken(new Token(new SliceSelector(new Slice(sliceStart,sliceStop,sliceStep))));
                                sliceStart = null;
                                sliceStop = null;
                                sliceStep = 1;
                                _stateStack.Pop(); // BracketSpecifier2
                                break;
                            case ':':
                                _stateStack.Pop(); _stateStack.Push(ExprState.SliceExpressionStep);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    }
                    case ExprState.SliceExpressionStep:
                    {
                        if (!(buffer.Length == 0))
                        {
                            Int32 n;
                            if (!Int32.TryParse(buffer.ToString(), out n))
                            {
                                throw new JsonException("Invalid slice stop");
                            }
                            buffer.Clear();
                            if (n == 0)
                            {
                                throw new JsonException("Slice step cannot be zero");
                            }
                            sliceStep = n;
                            buffer.Clear();
                        }
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case ',':
                                PushToken(new Token(new SliceSelector(new Slice(sliceStart,sliceStop,sliceStep))));
                                sliceStart = null;
                                sliceStop = null;
                                sliceStep = 1;
                                buffer.Clear();
                                _stateStack.Pop(); // SliceExpressionStep
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    }
                    case ExprState.IndexOrSlice:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                Int32 n;
                                if (!Int32.TryParse(buffer.ToString(), out n))
                                {
                                    throw new JsonException("Invalid index");
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                _stateStack.Pop(); // BracketSpecifier
                                break;
                            }
                            case ':':
                            {
                                if (!(buffer.Length == 0))
                                {
                                    Int32 n;
                                    string s = buffer.ToString();
                                    if (!Int32.TryParse(s, out n))
                                    {
                                        n = s.StartsWith("-") ? Int32.MinValue : Int32.MaxValue;
                                    }
                                    sliceStart = n;
                                    buffer.Clear();
                                }
                                _stateStack.Pop(); _stateStack.Push(ExprState.SliceExpressionStop);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.Integer:
                        switch (_input[_index])
                        {
                            case '-':
                                buffer.Append (_input[_index]);
                                _stateStack.Pop(); _stateStack.Push(ExprState.Digit);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(ExprState.Digit);
                                break;
                        }
                        break;
                    case ExprState.Digit:
                        switch (_input[_index])
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // digit
                                break;
                        }
                        break;
                    case ExprState.JsonString:
                    {
                        buffer.Insert(0,'\"');
                        buffer.Append('\"');
                        PushToken(new Token(JsonDocument.Parse(buffer.ToString()).RootElement));
                        buffer.Clear();
                        _stateStack.Pop(); // JsonValue
                        break;
                    }
                    case ExprState.PathOrValueOrFunction: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                            case '@':
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                _stateStack.Push(ExprState.RootOrCurrentNode);
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(TokenKind.LeftParen));
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.ExpectRightParen);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                break;
                            }
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonString);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonString);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                //PushToken(new Token(resources.get_unary_not()));
                                break;
                            }
                            case 't':
                            {
                                if (_index+4 <= _input.Length && _input.AsSpan().Slice(_index,4) == trueSpan)
                                {
                                    PushToken(new Token(JsonConstants.True));
                                    _stateStack.Pop(); 
                                    _index+= trueSpan.Length;
                                    _column += trueSpan.Length;
                                }
                                else
                                {
                                }
                                break;
                            }
                            case 'f':
                            {
                                if (_index+falseSpan.Length <= _input.Length && _input.AsSpan().Slice(_index,falseSpan.Length) == falseSpan)
                                {
                                    PushToken(new Token(JsonConstants.False));
                                    _stateStack.Pop(); 
                                    _index+= falseSpan.Length;
                                    _column += falseSpan.Length;
                                }
                                else
                                {
                                }
                                break;
                            }
                            case 'n':
                            {
                                if (_index+nullSpan.Length <= _input.Length && _input.AsSpan().Slice(_index,nullSpan.Length) == nullSpan)
                                {
                                    PushToken(new Token(JsonConstants.Null));
                                    _stateStack.Pop(); 
                                    _index+= nullSpan.Length;
                                    _column += nullSpan.Length;
                                }
                                else
                                {
                                }
                                break;
                            }
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            {
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonValue);
                                _stateStack.Push(ExprState.Number);
                                break;
                            }
                            case '{':
                            case '[':
                                break;
                            default:
                            {
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunctionName);
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.JsonTextOrFunction:
                    {
                        switch (_input[_index])
                        {
                            case '(':
                            {
                                /*var f = resources.get_function(buffer);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                buffer.Clear();
                                PushToken(current_node_arg);
                                if (ec) {return pathExpression_type();}
                                PushToken(new Token(f));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;*/
                                break;
                            }
                            default:
                            {
                                /*json_decoder<Json> decoder;
                                basic_json_parser<char_type> parser;
                                parser.update(buffer.data(),buffer.Length);
                                parser.parse_some(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                parser.finish_parse(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                PushToken(new Token(literal_arg, decoder.get_result()));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();*/
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.JsonValue:
                    {
                        PushToken(new Token(JsonDocument.Parse(buffer.ToString()).RootElement));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    }
                    case ExprState.JsonTextOrFunctionName:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '{':
                            case '[':
                            {
                                /*json_decoder<Json> decoder;
                                basic_json_parser<char_type> parser;
                                parser.update(_index,end_input_ - _index);
                                parser.parse_some(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                parser.finish_parse(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                PushToken(new Token(literal_arg, decoder.get_result()));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                _index = parser.current();
                                _column = _column + parser.column() - 1;*/
                                break;
                            }
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.Number);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.JsonTextString);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.UnquotedString);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.Number: 
                        switch (_input[_index])
                        {
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            case 'e':case 'E':case '.':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // Number
                                break;
                        };
                        break;
                    case ExprState.JsonTextString: 
                        switch (_input[_index])
                        {
                            case '\\':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                if (_index >= _input.Length)
                                {
                                    throw new JsonException("Syntax error");
                                }
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                buffer.Append (_input[_index]);
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.ExpressionRhs: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(ExprState.PathStepOrRecursiveDescent);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                _stateStack.Pop();
                                break;
                            }
                            case '|':
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.ExpectOr);
                                break;
                            case '&':
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.ExpectAnd);
                                break;
                            case '<':
                            case '>':
                            {
                                _stateStack.Push(ExprState.ComparatorExpression);
                                break;
                            }
                            case '=':
                            {
                                _stateStack.Push(ExprState.EqOrRegex);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpNe);
                                break;
                            }
                            /*case '+':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_plus_operator()));
                                ++_index;
                                ++_column;
                                break;
                            case '-':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_minus_operator()));
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_mult_operator()));
                                ++_index;
                                ++_column;
                                break;
                            case '/':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_div_operator()));
                                ++_index;
                                ++_column;
                                break;*/
                            case ']':
                            case ',':
                                _stateStack.Pop();
                                break;
                            default:
                                throw new JsonException("Syntax error");
                        };
                        break;
                    case ExprState.EqOrRegex:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '=':
                            {
                                PushToken(new Token(EqOperator.Instance));
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '~':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.ExpectRegex);
                                break;
                            }
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JsonException("Syntax error");
                                }
                                break;
                        }
                        break;
                    case ExprState.ExpectAnd:
                    {
                        switch (_input[_index])
                        {
                            case '&':
                                PushToken(new Token(AndOperator.Instance));
                                _stateStack.Pop(); // ExpectAnd
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected '&'");
                        }
                        break;
                    }
                    case ExprState.ComparatorExpression:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpGtOrGte);
                                break;
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JsonException("Syntax error");
                                }
                                break;
                        }
                        break;
                    case ExprState.ExpectRegex: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '/':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Regex);
                                ++_index;
                                ++_column;
                                break;
                            default: 
                                throw new JsonException("Expected '/'");
                        };
                        break;
                    case ExprState.Regex: 
                    {
                        switch (_input[_index])
                        {                   
                            case '/':
                                /*{
                                    std::regex::flag_type options = std::regex_constants::ECMAScript; 
                                    if (_index+1  < end_input_ && *(_index+1) == 'i')
                                    {
                                        ++_index;
                                        ++_column;
                                        options |= std::regex_constants::icase;
                                    }
                                    std::basicRegex<char_type> pattern(buffer, options);
                                    PushToken(resources.getRegex_operator(std::move(pattern)));
                                    buffer.Clear();
                                }*/
                                _stateStack.Pop();
                                break;

                            default: 
                                buffer.Append (_input[_index]);
                                break;
                        }
                        ++_index;
                        ++_column;
                        break;
                    }
                    case ExprState.CmpLtOrLte:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(LteOperator.Instance));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(LtOperator.Instance));
                                _stateStack.Pop();
                                break;
                        }
                        break;
                    }
                    case ExprState.CmpGtOrGte:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(GteOperator.Instance));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                //std::cout << "Parse: gt_operator\n";
                                PushToken(new Token(GtOperator.Instance));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case ExprState.CmpNe:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(NeOperator.Instance));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected '='");
                        }
                        break;
                    }
                    default:
                        ++_index;
                        break;
                    }
            }

            if (_stateStack.Count == 0)
            {
                throw new JsonException("Syntax error");
            }
            if (_stateStack.Peek() == ExprState.Start)
            {
                throw new JsonException("Unexpected end of input");
            }
            switch (_stateStack.Peek())
            {
                case ExprState.UnquotedString:
                case ExprState.Identifier:
                {
                    PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                    _stateStack.Pop(); // UnquotedString
                    Debug.Assert(buffer.Length != 0);
                    if (_stateStack.Peek() == ExprState.IdentifierOrFunctionExpr)
                    {
                        _stateStack.Pop(); // identifier
                    }
                    break;
                }
                case ExprState.Digit:
                {
                    if (buffer.Length == 0)
                    {
                        throw new JsonException("Invalid number");
                    }
                    Int64 n;
                    if (!Int64.TryParse(buffer.ToString(), out n))
                    {
                        throw new JsonException("Invalid number");
                    }
                    _stateStack.Pop(); // IndexOrSliceOrUnion
                    Debug.Assert(buffer.Length != 0);
                    if (_stateStack.Peek() == ExprState.Index)
                    {
                        _stateStack.Pop(); // index
                    }
                    break;
                }
                default:
                    break;
            }

            if (_outputStack.Count != 1 && _outputStack.Peek().Type != TokenKind.Selector)
            {
                throw new JsonException("Invalid state");
            }
            Token token = _outputStack.Pop();

            TestContext.WriteLine($"Main token: {token}");

            return new JsonPath(token.GetSelector());
        }

        void UnwindRParen()
        {
            while (_operatorStack.Count > 1 && _operatorStack.Peek().Type != TokenKind.LeftParen)
            {
                _outputStack.Push(_operatorStack.Pop());
            }
            if (_operatorStack.Count == 0)
            {
                throw new JsonException("Unbalanced parentheses");
            }
            _operatorStack.Pop(); // TokenKind.LeftParen
        }

        private void PushToken(Token token)
        {
            switch (token.Type)
            {
                case TokenKind.BeginFilter:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenKind.LeftParen));
                    break;
                case TokenKind.EndFilter:
                {
                    UnwindRParen();
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenKind.BeginFilter)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    _outputStack.Pop(); // TokenKind.LeftParen
                    tokens.Reverse();
                    if (_outputStack.Count > 1 && _outputStack.Peek().Type == TokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(new FilterSelector(new Expression(tokens)));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new FilterSelector(new Expression(tokens))));
                    }
                    break;
                }
                case TokenKind.BeginExpression:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenKind.LeftParen));
                    break;
                case TokenKind.EndExpression:
                {
                    UnwindRParen();
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenKind.BeginExpression)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    _outputStack.Pop(); // TokenKind.LeftParen
                    tokens.Reverse();
                    _outputStack.Push(new Token(new Expression(tokens)));
                    break;
                }
                case TokenKind.Selector:
                    if (_outputStack.Count != 0 && _outputStack.Peek().Type == TokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(token.GetSelector());
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case TokenKind.Separator:
                    _outputStack.Push(token);
                    break;
                case TokenKind.BeginUnion:
                    _outputStack.Push(token);
                    break;
                case TokenKind.EndUnion:
                {
                    List<ISelector> selectors = new List<ISelector>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenKind.BeginUnion)
                    {
                        switch (_outputStack.Peek().Type)
                        {
                            case TokenKind.Selector:
                                selectors.Add(_outputStack.Pop().GetSelector());
                                break;
                            case TokenKind.Separator:
                                _outputStack.Pop(); // Ignore separator
                                break;
                            default:
                                _outputStack.Pop(); // Probably error
                                break;
                        }
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Syntax error");
                    }
                    selectors.Reverse();
                    _outputStack.Pop(); // TokenKind.BeginUnion

                    if (_outputStack.Count != 0 && _outputStack.Peek().Type == TokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(new UnionSelector(selectors));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new UnionSelector(selectors)));
                    }
                    break;
                }
                case TokenKind.LeftParen:
                    _operatorStack.Push(token);
                    break;
                case TokenKind.RightParen:
                {
                    UnwindRParen();
                    break;
                }
                case TokenKind.UnaryOperator:
                case TokenKind.BinaryOperator:
                {
                    if (_operatorStack.Count == 0 || _operatorStack.Peek().Type == TokenKind.LeftParen)
                    {
                        _operatorStack.Push(token);
                    }
                    else if (token.PrecedenceLevel < _operatorStack.Peek().PrecedenceLevel
                             || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative))
                    {
                        _operatorStack.Push(token);
                    }
                    else
                    {
                        while (_operatorStack.Count > 0 && _operatorStack.Peek().IsOperator
                               && (token.PrecedenceLevel > _operatorStack.Peek().PrecedenceLevel
                             || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                        {
                            _outputStack.Push(_operatorStack.Pop());
                        }

                        _operatorStack.Push(token);
                    }
                    break;
                }
                case TokenKind.Value:
                    if (_outputStack.Count > 0 && (_outputStack.Peek().Type == TokenKind.CurrentNode || _outputStack.Peek().Type == TokenKind.RootNode))
                    {
                        _outputStack.Pop();
                        _outputStack.Push(token);
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case TokenKind.RootNode:
                case TokenKind.CurrentNode:
                    _outputStack.Push(token);
                    break;
            }
        }

        private UInt32 AppendToCodepoint(UInt32 cp, uint c)
        {
            cp *= 16;
            if (c >= '0'  &&  c <= '9')
            {
                cp += c - '0';
            }
            else if (c >= 'a'  &&  c <= 'f')
            {
                cp += c - 'a' + 10;
            }
            else if (c >= 'A'  &&  c <= 'F')
            {
                cp += c - 'A' + 10;
            }
            else
            {
                throw new JsonException("Invalid codepoint");
            }
            return cp;
        }

        private void SkipWhiteSpace()
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

} // namespace JsonCons.JsonPathLib
