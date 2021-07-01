using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        ParentOperator,
        AncestorDepth,
        FilterExpression,
        ExpressionRhs,
        PathStepOrRecursiveDescent,
        PathOrValueOrFunction,
        JsonText,
        Function,
        FunctionName,
        JsonValue,
        JsonValue2,
        AppendDoubleQuote,
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
        RegexOptions,
        RegexPattern,
        CmpLtOrLte,
        CmpGtOrGte,
        CmpNe,
        ExpectOr,
        ExpectAnd
    };

    ref struct JsonPathCompiler 
    {
        ReadOnlySpan<char> _span;
        int _index;
        int _column;
        int _line;
        Stack<ExprState> _stateStack;
        Stack<Token>_outputStack;
        Stack<Token>_operatorStack;

        internal JsonPathCompiler(string input)
        {
            _span = input.AsSpan();
            _index = 0;
            _column = 1;
            _line = 1;
            _stateStack = new Stack<ExprState>();
            _outputStack = new Stack<Token>();
            _operatorStack = new Stack<Token>();
        }

        internal JsonPathExpression Compile()
        {
            StaticResources resources = null;
            try
            {
                resources = new StaticResources();
                var expr = DoCompile(resources);
                return expr;
            }
            catch (Exception ex)
            {
                if (resources != null)
                {
                    resources.Dispose();
                }
                throw ex;
            }
        }

        private JsonPathExpression DoCompile(StaticResources resources)
        {
            _stateStack = new Stack<ExprState>();
            _index = 0;
            _column = 1;

            _stateStack.Push(ExprState.Start);

            StringBuilder buffer = new StringBuilder();
            StringBuilder buffer2 = new StringBuilder();

            Int32? sliceStart = null;
            Int32? sliceStop = null;
            Int32 sliceStep = 1;
            Int32 selector_id = 0;
            UInt32 cp = 0;
            UInt32 cp2 = 0;
            var trueSpan = "true".AsSpan();
            var falseSpan = "false".AsSpan();
            var nullSpan = "null".AsSpan();
            int jsonTextLevel = 0;
            int mark = 0;
            bool pathsRequired = false;
            int ancestorDepth = 0;

            while (_index < _span.Length)
            {
                switch (_stateStack.Peek())
                {
                    case ExprState.Start: 
                    {
                        switch (_span[_index])
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
                                throw new JsonException($"Invalid state {_span[_index]}");
                            }
                        }
                        break;
                    }
                    case ExprState.PathRhs: 
                    {
                        switch (_span[_index])
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
                            case '^':
                                ancestorDepth = 0;
                                _stateStack.Push(ExprState.ParentOperator);
                                _stateStack.Push(ExprState.AncestorDepth);
                                break;
                            default:
                            {
                                //TestContext.WriteLine("PathRhs DEFAULT");
                                _stateStack.Pop();
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.ParentOperator: 
                    {
                        PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                        pathsRequired = true;
                        ancestorDepth = 0;
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        break;
                    }
                    case ExprState.AncestorDepth: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '^':
                            {
                                ++ancestorDepth;
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                _stateStack.Pop();
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.PathStepOrRecursiveDescent:
                        switch (_span[_index])
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[': // [ can follow ..
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathExpression);
                                break;
                        }
                        break;
                    case ExprState.PathExpression: 
                        switch (_span[_index])
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
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Identifier);
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
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IdentifierOrFunctionExpr);
                                _stateStack.Push(ExprState.UnquotedString);
                                break;
                        }
                        break;
                    case ExprState.RootOrCurrentNode: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                                PushToken(new Token(JsonPathTokenKind.RootNode));
                                PushToken(new Token(new RootSelector(selector_id++)));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(JsonPathTokenKind.CurrentNode));
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
                        Char ch = _span[_index];
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                IFunction func; 
                                if (!BuiltInFunctions.Instance.TryGetFunction(buffer.ToString(), out func))
                                {
                                    throw new JsonException("Function not found");
                                }
                                buffer.Clear();
                                PushToken(new Token(JsonPathTokenKind.CurrentNode));
                                PushToken(new Token(func));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
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
                        switch (_span[_index])
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
                                buffer.Append (_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.DoubleQuotedString: 
                        switch (_span[_index])
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
                                buffer.Append (_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.QuotedStringEscapeChar:
                        switch (_span[_index])
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
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.EscapeU1);
                                break;
                            default:
                                throw new JsonException("Illegal escape character");
                        }
                        break;
                    case ExprState.EscapeU1:
                        cp = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU2);
                        break;
                    case ExprState.EscapeU2:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU3);
                        break;
                    case ExprState.EscapeU3:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU4);
                        break;
                    case ExprState.EscapeU4:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        if (Char.IsHighSurrogate((Char)cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(ExprState.EscapeExpectSurrogatePair1);
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
                        switch (_span[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                throw new JsonException("Invalid codepoint");
                        }
                        break;
                    case ExprState.EscapeExpectSurrogatePair2:
                        switch (_span[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.EscapeU5);
                                break;
                            default:
                                throw new JsonException("Invalid codepoint");
                        }
                        break;
                    case ExprState.EscapeU5:
                        cp2 = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU6);
                        break;
                    case ExprState.EscapeU6:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU7);
                        break;
                    case ExprState.EscapeU7:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.EscapeU8);
                        break;
                    case ExprState.EscapeU8:
                    {
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        UInt32 codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        buffer.Append(Char.ConvertFromUtf32((int)codepoint));
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }
                    case ExprState.ExpectRightBracket:
                        switch (_span[_index])
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                ++_index;
                                ++_column;
                                PushToken(new Token(JsonPathTokenKind.RightParen));
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.ExpressionRhs);
                                break;
                            default:
                                throw new JsonException("Expected )");
                        }
                        break;
                    case ExprState.BracketSpecifierOrUnion:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(JsonPathTokenKind.BeginExpression));
                                PushToken(new Token(JsonPathTokenKind.LeftParen));
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
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(JsonPathTokenKind.BeginFilter));
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.WildcardOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '$':
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(JsonPathTokenKind.RootNode));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);                                
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(JsonPathTokenKind.CurrentNode));
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected bracket specifier or union");
                        }
                        break;
                    case ExprState.WildcardOrUnion:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']': 
                                PushToken(new Token(new WildcardSelector()));
                                buffer.Clear();
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(new WildcardSelector()));
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); 
                                _stateStack.Push(ExprState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.UnionExpression:
                        switch (_span[_index])
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
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                _stateStack.Push(ExprState.UnionElement);
                                ++_index;
                                ++_column;
                                break;
                            case ']': 
                                PushToken(new Token(JsonPathTokenKind.EndUnion));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.UnionElement:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IndexOrSlice);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.IndexOrSlice);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '(':
                            {
                                PushToken(new Token(JsonPathTokenKind.BeginExpression));
                                PushToken(new Token(JsonPathTokenKind.LeftParen));
                                _stateStack.Pop(); 
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
                                PushToken(new Token(JsonPathTokenKind.BeginFilter));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                PushToken(new Token(new WildcardSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '$':
                                PushToken(new Token(JsonPathTokenKind.RootNode));
                                PushToken(new Token(new RootSelector(selector_id++)));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(JsonPathTokenKind.CurrentNode)); // ISSUE
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Identifier);
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                PushToken(new Token(JsonPathTokenKind.EndFilter));
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                throw new JsonException("Expected comma or right bracket");
                        }
                        break;
                    }
                    case ExprState.IdentifierOrUnion:
                        switch (_span[_index])
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
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected right bracket");
                        }
                        break;
                    case ExprState.BracketedWildcard:
                        switch (_span[_index])
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
                        switch (_span[_index])
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
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                Int32 n;
                                if (!Int32.TryParse(buffer.ToString(), out n))
                                {
                                    throw new JsonException("Invalid index");
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); // union
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
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.UnionExpression); // union
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
                        switch (_span[_index])
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
                        switch (_span[_index])
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
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.SliceExpressionStep);
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
                        switch (_span[_index])
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
                        switch (_span[_index])
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
                                _stateStack.Pop(); 
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
                    case ExprState.Integer:
                        switch (_span[_index])
                        {
                            case '-':
                                buffer.Append (_span[_index]);
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
                        switch (_span[_index])
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append (_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // digit
                                break;
                        }
                        break;
                    case ExprState.AppendDoubleQuote:
                    {
                        buffer.Append('\"');
                        _stateStack.Pop(); 
                        break;
                    }
                    case ExprState.PathOrValueOrFunction: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                            case '@':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathRhs);
                                _stateStack.Push(ExprState.RootOrCurrentNode);
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(JsonPathTokenKind.LeftParen));
                                _stateStack.Pop();
                                _stateStack.Push(ExprState.ExpectRightParen);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                break;
                            }
                            case '\'':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.JsonValue);
                                _stateStack.Push(ExprState.AppendDoubleQuote);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                buffer.Append('\"');
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.JsonValue);
                                _stateStack.Push(ExprState.AppendDoubleQuote);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                buffer.Append('\"');
                                ++_index;
                                ++_column;
                                break;
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(NotOperator.Instance));
                                break;
                            }
                            case 't':
                            {
                                if (_index+4 <= _span.Length && _span.Slice(_index,4) == trueSpan)
                                {
                                    PushToken(new Token(JsonConstants.True));
                                    _stateStack.Pop(); 
                                    _index+= trueSpan.Length;
                                    _column += trueSpan.Length;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(ExprState.Function);
                                    _stateStack.Push(ExprState.UnquotedString);
                                }
                                break;
                            }
                            case 'f':
                            {
                                if (_index+falseSpan.Length <= _span.Length && _span.Slice(_index,falseSpan.Length) == falseSpan)
                                {
                                    PushToken(new Token(JsonConstants.False));
                                    _stateStack.Pop(); 
                                    _index+= falseSpan.Length;
                                    _column += falseSpan.Length;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(ExprState.Function);
                                    _stateStack.Push(ExprState.UnquotedString);
                                }
                                break;
                            }
                            case 'n':
                            {
                                if (_index+nullSpan.Length <= _span.Length && _span.Slice(_index,nullSpan.Length) == nullSpan)
                                {
                                    PushToken(new Token(JsonConstants.Null));
                                    _stateStack.Pop(); 
                                    _index+= nullSpan.Length;
                                    _column += nullSpan.Length;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(ExprState.Function);
                                    _stateStack.Push(ExprState.UnquotedString);
                                }
                                break;
                            }
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.JsonValue);
                                _stateStack.Push(ExprState.Number);
                                break;
                            }
                            case '{':
                            case '[':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.JsonValue2);
                                _stateStack.Push(ExprState.JsonText);
                                mark = _index;
                                break;
                            default:
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Function);
                                _stateStack.Push(ExprState.UnquotedString);
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.Function:
                    {
                        switch (_span[_index])
                        {
                            case '(':
                            {
                                IFunction func; 
                                if (!BuiltInFunctions.Instance.TryGetFunction(buffer.ToString(), out func))
                                {
                                    throw new JsonException("Function not found");
                                }
                                buffer.Clear();
                                PushToken(new Token(JsonPathTokenKind.CurrentNode));
                                PushToken(new Token(func));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                throw new JsonException("Expected function");
                            }
                        }
                        break;
                    }
                    case ExprState.FunctionExpression:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(JsonPathTokenKind.CurrentNode));
                                PushToken(new Token(JsonPathTokenKind.BeginExpression));
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                PushToken(new Token(JsonPathTokenKind.EndFunction));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JsonException("Syntax error");
                        }
                        break;
                    }
                    case ExprState.ZeroOrOneArguments:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                _stateStack.Pop();
                                break;
                            default:
                                PushToken(new Token(JsonPathTokenKind.BeginExpression));
                                _stateStack.Pop(); _stateStack.Push(ExprState.OneOrMoreArguments);
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                break;
                        }
                        break;
                    }
                    case ExprState.OneOrMoreArguments:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                _stateStack.Pop();
                                break;
                            case ',':
                                PushToken(new Token(JsonPathTokenKind.BeginExpression));
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case ExprState.Argument:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ')':
                            {
                                PushToken(new Token(JsonPathTokenKind.EndArgument));
                                PushToken(new Token(JsonPathTokenKind.Argument));
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                throw new JsonException("Expected comma or right parenthesis");
                        }
                        break;
                    }
                    case ExprState.JsonText:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '{':
                            case '[':
                                ++jsonTextLevel;
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '}':
                            case ']':
                                --jsonTextLevel;
                                if (jsonTextLevel == 0)
                                {
                                    _stateStack.Pop(); 
                                }
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Push(ExprState.Number);
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Push(ExprState.AppendDoubleQuote);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case ':':
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Push(ExprState.UnquotedString);
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case ExprState.JsonValue:
                    {
                        PushToken(new Token(new JsonElementJsonValue(resources.CreateJsonElement(buffer.ToString()))));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    }
                    case ExprState.JsonValue2:
                    {
                        PushToken(new Token(new JsonElementJsonValue(resources.CreateJsonElement(buffer.ToString()))));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    }
                    case ExprState.Number: 
                        switch (_span[_index])
                        {
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            case 'e':case 'E':case '.':
                                buffer.Append (_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // Number
                                break;
                        };
                        break;
                    case ExprState.ExpressionRhs: 
                        switch (_span[_index])
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
                            case '+':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(PlusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '-':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(MinusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(MultOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '/':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(DivOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '%':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(ModulusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case ']':
                            case ',':
                                _stateStack.Pop();
                                break;
                            default:
                                throw new JsonException("Syntax error");
                        };
                        break;
                    case ExprState.EqOrRegex:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '=':
                            {
                                PushToken(new Token(EqOperator.Instance));
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
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
                    case ExprState.ExpectOr:
                    {
                        switch (_span[_index])
                        {
                            case '|':
                                PushToken(new Token(OrOperator.Instance));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonException("Expected '|'");
                        }
                        break;
                    }
                    case ExprState.ExpectAnd:
                    {
                        switch (_span[_index])
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
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
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '/':
                                _stateStack.Pop(); 
                                _stateStack.Push(ExprState.Regex);
                                _stateStack.Push(ExprState.RegexOptions);
                                _stateStack.Push(ExprState.RegexPattern);
                                ++_index;
                                ++_column;
                                break;
                            default: 
                                throw new JsonException("Expected '/'");
                        };
                        break;
                    case ExprState.Regex: 
                    {
                        RegexOptions options = 0;
                        if (buffer2.Length > 0)
                        {
                            var str = buffer2.ToString();
                            if (str.Contains('i'))
                            {
                                options |= RegexOptions.IgnoreCase;
                            }
                        }
                        Regex regex = new Regex(buffer.ToString(), options);
                        PushToken(new Token(new RegexOperator(regex)));
                        buffer.Clear();
                        buffer2.Clear();
                        _stateStack.Pop();
                        break;
                    }
                    case ExprState.RegexPattern: 
                    {
                        switch (_span[_index])
                        {                   
                            case '/':
                            {
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            }
                            default: 
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case ExprState.RegexOptions: 
                    {
                        char c = _span[_index];
                        if (c == 'i') // ignore case
                        {
                            buffer2.Append(c);
                            ++_index;
                            ++_column;
                        }
                        else
                        {
                            _stateStack.Pop();
                        }
                        break;
                    }
                    case ExprState.CmpLtOrLte:
                    {
                        switch (_span[_index])
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
                        switch (_span[_index])
                        {
                            case '=':
                                PushToken(new Token(GteOperator.Instance));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                //std.cout << "Parse: gt_operator\n";
                                PushToken(new Token(GtOperator.Instance));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case ExprState.CmpNe:
                    {
                        switch (_span[_index])
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
                        throw new JsonException($"Unhandled JSONPath state {_stateStack.Peek()}");
                }
            }

            if (_stateStack.Count == 0)
            {
                throw new JsonException("Syntax error");
            }
            while (_stateStack.Count > 1)
            {
                switch (_stateStack.Peek())
                {
                    case ExprState.NameOrLeftBracket:
                        _stateStack.Pop(); 
                        _stateStack.Push(ExprState.PathExpression);
                        break;
                    case ExprState.PathExpression: 
                        _stateStack.Pop();
                        _stateStack.Push(ExprState.IdentifierOrFunctionExpr);
                        _stateStack.Push(ExprState.UnquotedString);
                        break;
                    case ExprState.IdentifierOrFunctionExpr:
                        if (buffer.Length != 0) // Can't be quoted string
                        {
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        }
                        _stateStack.Pop(); 
                        break;
                    case ExprState.UnquotedString: 
                        _stateStack.Pop(); // UnquotedString
                        break;                    
                    case ExprState.PathRhs: 
                        _stateStack.Pop();
                        break;
                    case ExprState.Identifier:
                        if (buffer.Length != 0) // Can't be quoted string
                        {
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        }
                        _stateStack.Pop(); 
                        break;
                    case ExprState.ParentOperator: 
                    {
                        PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                        pathsRequired = true;
                        _stateStack.Pop();
                        break;
                    }
                    case ExprState.AncestorDepth: 
                        _stateStack.Pop();
                        break;
                    default:
                        throw new JsonException("Syntax error");
                }
            }

            if (_outputStack.Count < 1)
            {
                throw new JsonException("Invalid state 1");
            }
            if (_outputStack.Peek().TokenKind != JsonPathTokenKind.Selector)
            {
                throw new JsonException("Invalid state 2");
            }
            Token token = _outputStack.Pop();

            //TestContext.WriteLine($"Main token: {token}");

            return new JsonPathExpression(resources, token.GetSelector(), pathsRequired);
        }

        void UnwindRParen()
        {
            while (_operatorStack.Count > 1 && _operatorStack.Peek().TokenKind != JsonPathTokenKind.LeftParen)
            {
                _outputStack.Push(_operatorStack.Pop());
            }
            if (_operatorStack.Count == 0)
            {
                throw new JsonException("Unbalanced parentheses");
            }
            _operatorStack.Pop(); // JsonPathTokenKind.LeftParen
        }

        private void PushToken(Token token)
        {
            //TestContext.WriteLine($"Token {token}");
            //TestContext.WriteLine("OutputStack:");
            //foreach (var item in _outputStack)
            //{
            //    TestContext.WriteLine($"    {item}");
            //}
            //TestContext.WriteLine("OperatorStack:");
            //foreach (var item in _operatorStack)
            //{
            //   TestContext.WriteLine($"    {item}");
            //}
            //TestContext.WriteLine("---");

            switch (token.TokenKind)
            {
                case JsonPathTokenKind.BeginFilter:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(JsonPathTokenKind.LeftParen));
                    break;
                case JsonPathTokenKind.EndFilter:
                {
                    UnwindRParen();
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginFilter)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    _outputStack.Pop(); // JsonPathTokenKind.LeftParen
                    tokens.Reverse();
                    if (_outputStack.Count > 1 && _outputStack.Peek().TokenKind == JsonPathTokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(new FilterSelector(new Expression(tokens)));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new FilterSelector(new Expression(tokens))));
                    }
                    break;
                }
                case JsonPathTokenKind.BeginExpression:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(JsonPathTokenKind.LeftParen));
                    break;
                case JsonPathTokenKind.EndExpression:
                {
                    UnwindRParen();
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginExpression)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    _outputStack.Pop(); // JsonPathTokenKind.LeftParen
                    tokens.Reverse();
                    _outputStack.Push(new Token(new Expression(tokens)));
                    break;
                }
                case JsonPathTokenKind.Selector:
                    if (_outputStack.Count != 0 && _outputStack.Peek().TokenKind == JsonPathTokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(token.GetSelector());
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case JsonPathTokenKind.Separator:
                    _outputStack.Push(token);
                    break;
                case JsonPathTokenKind.BeginUnion:
                    _outputStack.Push(token);
                    break;
                case JsonPathTokenKind.EndUnion:
                {
                    List<ISelector> selectors = new List<ISelector>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginUnion)
                    {
                        switch (_outputStack.Peek().TokenKind)
                        {
                            case JsonPathTokenKind.Selector:
                                selectors.Add(_outputStack.Pop().GetSelector());
                                break;
                            case JsonPathTokenKind.Separator:
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
                    _outputStack.Pop(); // JsonPathTokenKind.BeginUnion

                    if (_outputStack.Count != 0 && _outputStack.Peek().TokenKind == JsonPathTokenKind.Selector)
                    {
                        _outputStack.Peek().GetSelector().AppendSelector(new UnionSelector(selectors));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new UnionSelector(selectors)));
                    }
                    break;
                }
                case JsonPathTokenKind.LeftParen:
                    _operatorStack.Push(token);
                    break;
                case JsonPathTokenKind.RightParen:
                {
                    UnwindRParen();
                    break;
                }
                case JsonPathTokenKind.UnaryOperator:
                case JsonPathTokenKind.BinaryOperator:
                {
                    if (_operatorStack.Count == 0 || _operatorStack.Peek().TokenKind == JsonPathTokenKind.LeftParen)
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
                case JsonPathTokenKind.Value:
                    if (_outputStack.Count > 0 && (_outputStack.Peek().TokenKind == JsonPathTokenKind.CurrentNode || _outputStack.Peek().TokenKind == JsonPathTokenKind.RootNode))
                    {
                        _outputStack.Pop();
                        _outputStack.Push(token);
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case JsonPathTokenKind.RootNode:
                case JsonPathTokenKind.CurrentNode:
                    _outputStack.Push(token);
                    break;
                case JsonPathTokenKind.Function:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(JsonPathTokenKind.LeftParen));
                    break;
                case JsonPathTokenKind.Argument:
                    _outputStack.Push(token);
                    break;
                case JsonPathTokenKind.EndFunction:
                {
                    UnwindRParen();

                    Int32 argCount = 0;
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.Function)
                    {
                        if (_outputStack.Peek().TokenKind == JsonPathTokenKind.Argument)
                        {
                            ++argCount;
                        }
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    tokens.Reverse();

                    //TestContext.WriteLine($"ARITY: {_outputStack.Peek().GetFunction().Arity}, ARG COUNT: {argCount}");
                    if (_outputStack.Peek().GetFunction().Arity.HasValue && _outputStack.Peek().GetFunction().Arity.Value != argCount)
                    {
                        throw new JsonException("Invalid arity");
                    }
                    tokens.Add(_outputStack.Pop()); // Function

                    _outputStack.Push(new Token(new Expression(tokens)));
                    break;
                }
                case JsonPathTokenKind.EndArgument:
                {
                    UnwindRParen();
                    List<Token> tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginExpression)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonException("Unbalanced parentheses");
                    }
                    tokens.Reverse();
                    _outputStack.Pop(); // JsonPathTokenKind.BeginExpression
                    _outputStack.Push(new Token(new Expression(tokens)));
                    break;
                }
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
            switch (_span[_index])
            {
                case ' ':case '\t':
                    ++_index;
                    ++_column;
                    break;
                case '\r':
                    if (_index+1 < _span.Length && _span[_index+1] == '\n')
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
