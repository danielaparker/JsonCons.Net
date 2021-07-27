using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
        
namespace JsonCons.JsonPath
{
    /// <summary>
    /// Defines a custom exception object that is thrown when JSONPath parsing fails.
    /// </summary>    

    public class JsonPathParseException : Exception
    {
        /// <summary>
        /// The line in the JSONPath string where a parse error was detected.
        /// </summary>
        public int LineNumber {get;}

        /// <summary>
        /// The column in the JSONPath string where a parse error was detected.
        /// </summary>
        public int ColumnNumber {get;}

        internal JsonPathParseException(string message, int line, int column)
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

    enum JsonPathState
    {
        Start,
        RelativeLocation,
        ExpectedDotOrLeftBracketOrCaret,
        RelativePathOrRecursiveDescent,
        BracketExpressionOrRelativePath,
        RootOrCurrentNode,
        ExpectFunctionExpr,
        RelativePath,
        ParentOperator,
        AncestorDepth,
        FilterExpression,
        ExpressionRhs,
        UnaryOperatorOrPathOrValueOrFunction,
        JsonText,
        JsonTextString,
        JsonStringValue,
        Function,
        FunctionName,
        JsonLiteral,
        AppendDoubleQuote,
        IdentifierOrFunctionExpr,
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
        BracketExpression,
        BracketedWildcard,
        IndexOrSlice,
        Index,
        WildcardOrUnion,
        UnionElement,
        IndexOrSliceOrUnion,
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

    ref struct JsonPathParser 
    {
        ReadOnlySpan<char> _span;
        int _index;
        int _column;
        int _line;
        Stack<JsonPathState> _stateStack;
        Stack<Token>_outputStack;
        Stack<Token>_operatorStack;

        internal JsonPathParser(string input)
        {
            _span = input.AsSpan();
            _index = 0;
            _column = 1;
            _line = 1;
            _stateStack = new Stack<JsonPathState>();
            _outputStack = new Stack<Token>();
            _operatorStack = new Stack<Token>();
        }

        internal JsonSelector Parse()
        {
            _stateStack = new Stack<JsonPathState>();
            _index = 0;
            _column = 1;

            _stateStack.Push(JsonPathState.Start);

            StringBuilder buffer = new StringBuilder();
            StringBuilder buffer2 = new StringBuilder();

            Int32? sliceStart = null;
            Int32? sliceStop = null;
            Int32 sliceStep = 1;
            UInt32 cp = 0;
            UInt32 cp2 = 0;
            int jsonTextLevel = 0;
            int mark = 0;
            bool pathsRequired = false;
            int ancestorDepth = 0;

            while (_index < _span.Length)
            {
                switch (_stateStack.Peek())
                {
                    case JsonPathState.Start: 
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
                                _stateStack.Pop();
                                _stateStack.Push(JsonPathState.ExpectedDotOrLeftBracketOrCaret);
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                throw new JsonPathParseException("Syntax error", _line, _column);
                            }
                        }
                        break;
                    }
                    case JsonPathState.ExpectedDotOrLeftBracketOrCaret: 
                    {
                        throw new JsonPathParseException("Expected '.' or '[' or '^'", _line, _column);
                    }
                    case JsonPathState.RelativeLocation: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                            {
                                _stateStack.Push(JsonPathState.RelativePathOrRecursiveDescent);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '[':
                                _stateStack.Push(JsonPathState.BracketExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '^':
                                ancestorDepth = 0;
                                _stateStack.Push(JsonPathState.ParentOperator);
                                _stateStack.Push(JsonPathState.AncestorDepth);
                                break;
                            default:
                            {
                                _stateStack.Pop();
                                break;
                            }
                        }
                        break;
                    }
                    case JsonPathState.ParentOperator: 
                    {
                        PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                        pathsRequired = true;
                        ancestorDepth = 0;
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        break;
                    }
                    case JsonPathState.AncestorDepth: 
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
                    case JsonPathState.RelativePathOrRecursiveDescent:
                        switch (_span[_index])
                        {
                            case '.':
                                PushToken(new Token(new RecursiveDescentSelector()));
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JsonPathState.BracketExpressionOrRelativePath);
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JsonPathState.RelativePath);
                                break;
                        }
                        break;
                    case JsonPathState.BracketExpressionOrRelativePath: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[': // [ can follow ..
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.BracketExpression);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.RelativePath);
                                break;
                        }
                        break;
                    case JsonPathState.RelativePath: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\'':
                                // Single quoted string
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Identifier);
                                _stateStack.Push(JsonPathState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                // Double quoted string
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Identifier);
                                _stateStack.Push(JsonPathState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                // Wildcard
                                PushToken(new Token(new WildcardSelector()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                Char ch = _span[_index];
                                if (Char.IsLetterOrDigit(ch) || ch == '_')
                                {
                                    // Unquoted string or function expression
                                    buffer.Clear();
                                    _stateStack.Pop(); 
                                    _stateStack.Push(JsonPathState.IdentifierOrFunctionExpr);
                                    _stateStack.Push(JsonPathState.UnquotedString);
                                    buffer.Append(ch);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JsonPathParseException("Expected unquoted string, or single or double quoted string, or index or '*'", _line, _column);
                                }
                                break;
                        }
                        break;
                    case JsonPathState.RootOrCurrentNode: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                                PushToken(new Token(JsonPathTokenKind.RootNode));
                                PushToken(new Token(new RootSelector(_index)));
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
                                throw new JsonPathParseException("Syntax error", _line, _column);
                        }
                        break;
                    case JsonPathState.UnquotedString: 
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
                    case JsonPathState.IdentifierOrFunctionExpr:
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
                                    throw new JsonPathParseException("Function not found", _line, _column);
                                }
                                buffer.Clear();
                                PushToken(new Token(func));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.FunctionExpression);
                                _stateStack.Push(JsonPathState.ZeroOrOneArguments);
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
                    case JsonPathState.Identifier:
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    case JsonPathState.SingleQuotedString:
                        switch (_span[_index])
                        {
                            case '\'':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(JsonPathState.QuotedStringEscapeChar);
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
                    case JsonPathState.DoubleQuotedString: 
                        switch (_span[_index])
                        {
                            case '\"':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(JsonPathState.QuotedStringEscapeChar);
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
                    case JsonPathState.QuotedStringEscapeChar:
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
                                _stateStack.Push(JsonPathState.EscapeU1);
                                break;
                            default:
                                throw new JsonPathParseException($"Illegal escape character '{_span[_index]}'", _line, _column);
                        }
                        break;
                    case JsonPathState.EscapeU1:
                        cp = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU2);
                        break;
                    case JsonPathState.EscapeU2:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU3);
                        break;
                    case JsonPathState.EscapeU3:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU4);
                        break;
                    case JsonPathState.EscapeU4:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        if (Char.IsHighSurrogate((Char)cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.EscapeExpectSurrogatePair1);
                        }
                        else
                        {
                            buffer.Append(Char.ConvertFromUtf32((int)cp));
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                        }
                        break;
                    case JsonPathState.EscapeExpectSurrogatePair1:
                        switch (_span[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                throw new JsonPathParseException("Invalid codepoint", _line, _column);
                        }
                        break;
                    case JsonPathState.EscapeExpectSurrogatePair2:
                        switch (_span[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.EscapeU5);
                                break;
                            default:
                                throw new JsonPathParseException("Invalid codepoint", _line, _column);
                        }
                        break;
                    case JsonPathState.EscapeU5:
                        cp2 = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU6);
                        break;
                    case JsonPathState.EscapeU6:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU7);
                        break;
                    case JsonPathState.EscapeU7:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeU8);
                        break;
                    case JsonPathState.EscapeU8:
                    {
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        UInt32 codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        buffer.Append(Char.ConvertFromUtf32((int)codepoint));
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }
                    case JsonPathState.ExpectRightBracket:
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
                                throw new JsonPathParseException("Expected ']'", _line, _column);
                        }
                        break;
                    case JsonPathState.ExpectRightParen:
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
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                break;
                            default:
                                throw new JsonPathParseException("Expected ')'", _line, _column);
                        }
                        break;
                    case JsonPathState.BracketExpression:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\'':
                                // Single quoted string
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IdentifierOrUnion);
                                _stateStack.Push(JsonPathState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                // Double quoted string
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IdentifierOrUnion);
                                _stateStack.Push(JsonPathState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                // Index or slice
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IndexOrSliceOrUnion);
                                _stateStack.Push(JsonPathState.Integer);
                                break;
                            case ':': 
                                // Slice expression
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IndexOrSliceOrUnion);
                                break;
                            case '*': 
                                // Wildcard
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.WildcardOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '?': 
                            {
                                // Filter expression
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(JsonPathTokenKind.BeginFilter));
                                _stateStack.Pop(); _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.FilterExpression);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '$': // JsonPath
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(new RootSelector(_index)));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.RelativeLocation);                                
                                ++_index;
                                ++_column;
                                break;
                            case '@': // JsonPath
                                PushToken(new Token(JsonPathTokenKind.BeginUnion));
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonPathParseException("Expected single or double quoted string or index or slice or '*' or '?' or JSONPath", _line, _column);
                        }
                        break;
                    case JsonPathState.WildcardOrUnion:
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
                                _stateStack.Push(JsonPathState.UnionExpression); 
                                _stateStack.Push(JsonPathState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.UnionExpression:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(JsonPathState.RelativePath);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(JsonPathState.BracketExpression);
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                _stateStack.Push(JsonPathState.UnionElement);
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
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.UnionElement:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IndexOrSlice);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.IndexOrSlice);
                                _stateStack.Push(JsonPathState.Integer);
                                break;
                            case '?':
                            {
                                PushToken(new Token(JsonPathTokenKind.BeginFilter));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.FilterExpression);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                PushToken(new Token(new WildcardSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                ++_index;
                                ++_column;
                                break;
                            case '$':
                                PushToken(new Token(new RootSelector(_index)));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(new CurrentNodeSelector()));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Identifier);
                                _stateStack.Push(JsonPathState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Identifier);
                                _stateStack.Push(JsonPathState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonPathParseException("Expected bracket specifier or union", _line, _column);
                        }
                        break;
                    case JsonPathState.FilterExpression:
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
                                throw new JsonPathParseException("Expected comma or right bracket", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.IdentifierOrUnion:
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
                                _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.BracketedWildcard:
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
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.IndexOrSliceOrUnion:
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
                                    throw new JsonPathParseException("Invalid index", _line, _column);
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
                                    throw new JsonPathParseException("Invalid index", _line, _column);
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                PushToken(new Token(JsonPathTokenKind.Separator));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.UnionElement);
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
                                _stateStack.Push(JsonPathState.UnionExpression); // union
                                _stateStack.Push(JsonPathState.SliceExpressionStop);
                                _stateStack.Push(JsonPathState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.SliceExpressionStop:
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
                                _stateStack.Push(JsonPathState.SliceExpressionStep);
                                _stateStack.Push(JsonPathState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.SliceExpressionStep:
                    {
                        if (!(buffer.Length == 0))
                        {
                            Int32 n;
                            if (!Int32.TryParse(buffer.ToString(), out n))
                            {
                                throw new JsonPathParseException("Invalid slice stop", _line, _column);
                            }
                            buffer.Clear();
                            if (n == 0)
                            {
                                throw new JsonPathParseException("Slice step cannot be zero", _line, _column);
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
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.IndexOrSlice:
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
                                    throw new JsonPathParseException("Invalid index", _line, _column);
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                buffer.Clear();
                                _stateStack.Pop(); 
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
                                _stateStack.Push(JsonPathState.SliceExpressionStop);
                                _stateStack.Push(JsonPathState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JsonPathParseException("Expected right bracket", _line, _column);
                        }
                        break;
                    case JsonPathState.Index:
                    {
                        Int32 n;
                        if (!Int32.TryParse(buffer.ToString(), out n))
                        {
                            throw new JsonPathParseException("Invalid index", _line, _column);
                        }
                        PushToken(new Token(new IndexSelector(n)));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    }
                    case JsonPathState.Integer:
                        switch (_span[_index])
                        {
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append (_span[_index]);
                                _stateStack.Pop(); _stateStack.Push(JsonPathState.Digit);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(JsonPathState.Digit);
                                break;
                        }
                        break;
                    case JsonPathState.Digit:
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
                    case JsonPathState.AppendDoubleQuote:
                    {
                        buffer.Append('\"');
                        _stateStack.Pop(); 
                        break;
                    }
                    case JsonPathState.UnaryOperatorOrPathOrValueOrFunction: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                            case '@':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.RelativeLocation);
                                _stateStack.Push(JsonPathState.RootOrCurrentNode);
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(JsonPathTokenKind.LeftParen));
                                _stateStack.Pop();
                                _stateStack.Push(JsonPathState.ExpectRightParen);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                break;
                            }
                            case '\'':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.JsonStringValue);
                                _stateStack.Push(JsonPathState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.JsonStringValue);
                                _stateStack.Push(JsonPathState.DoubleQuotedString);
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
                            case '-':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(UnaryMinusOperator.Instance));
                                break;
                            }
                            case 't':
                            {
                                if (_index+4 <= _span.Length && _span[_index+1] == 'r' && _span[_index+2] == 'u' && _span[_index+3] == 'e')
                                {
                                    PushToken(new Token(JsonConstants.True));
                                    _stateStack.Pop(); 
                                    _index += 4;
                                    _column += 4;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(JsonPathState.Function);
                                    _stateStack.Push(JsonPathState.UnquotedString);
                                }
                                break;
                            }
                            case 'f':
                            {
                                if (_index+5 <= _span.Length && _span[_index+1] == 'a' && _span[_index+2] == 'l' && _span[_index+3] == 's' && _span[_index+4] == 'e')
                                {
                                    PushToken(new Token(JsonConstants.False));
                                    _stateStack.Pop(); 
                                    _index += 5;
                                    _column += 5;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(JsonPathState.Function);
                                    _stateStack.Push(JsonPathState.UnquotedString);
                                }
                                break;
                            }
                            case 'n':
                            {
                                if (_index+4 <= _span.Length && _span[_index+1] == 'u' && _span[_index+2] == 'l' && _span[_index+3] == 'l')
                                {
                                    PushToken(new Token(JsonConstants.Null));
                                    _stateStack.Pop(); 
                                    _index += 4;
                                    _column += 4;
                                }
                                else
                                {
                                    _stateStack.Pop(); 
                                    _stateStack.Push(JsonPathState.Function);
                                    _stateStack.Push(JsonPathState.UnquotedString);
                                }
                                break;
                            }
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.JsonLiteral);
                                _stateStack.Push(JsonPathState.Number);
                                break;
                            }
                            case '{':
                            case '[':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.JsonLiteral);
                                _stateStack.Push(JsonPathState.JsonText);
                                mark = _index;
                                break;
                            default:
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Function);
                                _stateStack.Push(JsonPathState.UnquotedString);
                                break;
                            }
                        }
                        break;
                    }
                    case JsonPathState.Function:
                    {
                        switch (_span[_index])
                        {
                            case '(':
                            {
                                IFunction func; 
                                if (!BuiltInFunctions.Instance.TryGetFunction(buffer.ToString(), out func))
                                {
                                    throw new JsonPathParseException("Function not found", _line, _column);
                                }
                                buffer.Clear();
                                PushToken(new Token(func));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.FunctionExpression);
                                _stateStack.Push(JsonPathState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                throw new JsonPathParseException("Expected function", _line, _column);
                            }
                        }
                        break;
                    }
                    case JsonPathState.FunctionExpression:
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(JsonPathTokenKind.BeginArgument));
                                _stateStack.Push(JsonPathState.Argument);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
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
                                throw new JsonPathParseException("Syntax error", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.ZeroOrOneArguments:
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
                                PushToken(new Token(JsonPathTokenKind.BeginArgument));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.OneOrMoreArguments);
                                _stateStack.Push(JsonPathState.Argument);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                break;
                        }
                        break;
                    }
                    case JsonPathState.OneOrMoreArguments:
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
                                PushToken(new Token(JsonPathTokenKind.BeginArgument));
                                _stateStack.Push(JsonPathState.Argument);
                                _stateStack.Push(JsonPathState.ExpressionRhs);
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case JsonPathState.Argument:
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
                                throw new JsonPathParseException("Expected comma or right parenthesis", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.JsonText:
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
                                _stateStack.Push(JsonPathState.Number);
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Push(JsonPathState.JsonTextString);
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
                                _stateStack.Push(JsonPathState.UnquotedString);
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case JsonPathState.JsonTextString: 
                        switch (_span[_index])
                        {
                            case '\\':
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                if (_index == _span.Length)
                                {
                                    throw new JsonPathParseException("Unexpected end of input", _line, _column);
                                }
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                buffer.Append(_span[_index]);
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case JsonPathState.JsonLiteral:
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(buffer.ToString()))
                            {            
                                PushToken(new Token(new JsonElementValue(doc.RootElement.Clone())));
                                buffer.Clear();
                                _stateStack.Pop(); 
                            }
                        }
                        catch (JsonException)
                        {
                            throw new JsonPathParseException("Invalid JSON literal", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.JsonStringValue:
                    {
                        PushToken(new Token(new StringValue(buffer.ToString())));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    }
                    case JsonPathState.Number: 
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
                    case JsonPathState.ExpressionRhs: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(JsonPathState.RelativePathOrRecursiveDescent);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(JsonPathState.BracketExpression);
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
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                _stateStack.Push(JsonPathState.ExpectOr);
                                break;
                            case '&':
                                ++_index;
                                ++_column;
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                _stateStack.Push(JsonPathState.ExpectAnd);
                                break;
                            case '<':
                            case '>':
                            {
                                _stateStack.Push(JsonPathState.ComparatorExpression);
                                break;
                            }
                            case '=':
                            {
                                _stateStack.Push(JsonPathState.EqOrRegex);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                _stateStack.Push(JsonPathState.CmpNe);
                                break;
                            }
                            case '+':
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                PushToken(new Token(PlusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '-':
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                PushToken(new Token(MinusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                PushToken(new Token(MultOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '/':
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                PushToken(new Token(DivOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case '%':
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                PushToken(new Token(ModulusOperator.Instance));
                                ++_index;
                                ++_column;
                                break;
                            case ']':
                            case ',':
                                _stateStack.Pop();
                                break;
                            default:
                                throw new JsonPathParseException("Syntax error", _line, _column);
                        };
                        break;
                    case JsonPathState.EqOrRegex:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '=':
                            {
                                PushToken(new Token(EqOperator.Instance));
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '~':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JsonPathState.ExpectRegex);
                                break;
                            }
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JsonPathParseException("Syntax error", _line, _column);
                                }
                                break;
                        }
                        break;
                    case JsonPathState.ExpectOr:
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
                                throw new JsonPathParseException("Expected '|'", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.ExpectAnd:
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
                                throw new JsonPathParseException("Expected '&'", _line, _column);
                        }
                        break;
                    }
                    case JsonPathState.ComparatorExpression:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                _stateStack.Push(JsonPathState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                                _stateStack.Push(JsonPathState.CmpGtOrGte);
                                break;
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JsonPathParseException("Syntax error", _line, _column);
                                }
                                break;
                        }
                        break;
                    case JsonPathState.ExpectRegex: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '/':
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Regex);
                                _stateStack.Push(JsonPathState.RegexOptions);
                                _stateStack.Push(JsonPathState.RegexPattern);
                                ++_index;
                                ++_column;
                                break;
                            default: 
                                throw new JsonPathParseException("Expected '/'", _line, _column);
                        };
                        break;
                    case JsonPathState.Regex: 
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
                    case JsonPathState.RegexPattern: 
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
                    case JsonPathState.RegexOptions: 
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
                    case JsonPathState.CmpLtOrLte:
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
                    case JsonPathState.CmpGtOrGte:
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
                    case JsonPathState.CmpNe:
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
                                throw new JsonPathParseException("Expected '='", _line, _column);
                        }
                        break;
                    }
                    default:
                        throw new JsonPathParseException($"Unhandled JSONPath state '{_stateStack.Peek()}'", _line, _column);
                }
            }

            if (_stateStack.Count == 0)
            {
                throw new JsonPathParseException("Syntax error", _line, _column);
            }
            while (_stateStack.Count > 1)
            {
                switch (_stateStack.Peek())
                {
                    case JsonPathState.BracketExpressionOrRelativePath:
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.RelativePath);
                        break;
                    case JsonPathState.RelativePath: 
                        _stateStack.Pop();
                        _stateStack.Push(JsonPathState.IdentifierOrFunctionExpr);
                        _stateStack.Push(JsonPathState.UnquotedString);
                        break;
                    case JsonPathState.IdentifierOrFunctionExpr:
                        if (buffer.Length != 0) // Can't be quoted string
                        {
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        }
                        _stateStack.Pop(); 
                        break;
                    case JsonPathState.UnquotedString: 
                        _stateStack.Pop(); // UnquotedString
                        break;                    
                    case JsonPathState.RelativeLocation: 
                        _stateStack.Pop();
                        break;
                    case JsonPathState.Identifier:
                        if (buffer.Length != 0) // Can't be quoted string
                        {
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        }
                        _stateStack.Pop(); 
                        break;
                    case JsonPathState.Index:
                        Int32 n;
                        if (!Int32.TryParse(buffer.ToString(), out n))
                        {
                            throw new JsonPathParseException("Invalid index", _line, _column);
                        }
                        PushToken(new Token(new IndexSelector(n)));
                        _stateStack.Pop(); 
                        break;
                    case JsonPathState.Digit:
                        _stateStack.Pop(); // digit
                        break;
                    case JsonPathState.ParentOperator: 
                    {
                        PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                        pathsRequired = true;
                        _stateStack.Pop();
                        break;
                    }
                    case JsonPathState.AncestorDepth: 
                        _stateStack.Pop();
                        break;
                    default:
                        throw new JsonPathParseException("Syntax error", _line, _column);
                }
            }

            if (_outputStack.Count < 1)
            {
                throw new JsonPathParseException("Invalid state 1", _line, _column);
            }
            if (_outputStack.Peek().TokenKind != JsonPathTokenKind.Selector)
            {
                throw new JsonPathParseException("Invalid state 2", _line, _column);
            }
            Token token = _outputStack.Pop();

            return new JsonSelector(token.GetSelector(), pathsRequired);
        }

        void UnwindRParen()
        {
            while (_operatorStack.Count > 1 && _operatorStack.Peek().TokenKind != JsonPathTokenKind.LeftParen)
            {
                _outputStack.Push(_operatorStack.Pop());
            }
            if (_operatorStack.Count == 0)
            {
                throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
            }
            _operatorStack.Pop(); // JsonPathTokenKind.LeftParen
        }

        private void PushToken(Token token)
        {
            switch (token.TokenKind)
            {
                case JsonPathTokenKind.BeginFilter:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(JsonPathTokenKind.LeftParen));
                    break;
                case JsonPathTokenKind.EndFilter:
                {
                    UnwindRParen();
                    var tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginFilter)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
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
                case JsonPathTokenKind.BeginArgument:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(JsonPathTokenKind.LeftParen));
                    break;
                case JsonPathTokenKind.Selector:
                    if (!token.GetSelector().IsRoot() && _outputStack.Count != 0 && _outputStack.Peek().TokenKind == JsonPathTokenKind.Selector)
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
                        throw new JsonPathParseException("Syntax error", _line, _column);
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
                    var tokens = new List<Token>();
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
                        throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
                    }
                    tokens.Reverse();

                    if (_outputStack.Peek().GetFunction().Arity.HasValue && _outputStack.Peek().GetFunction().Arity.Value != argCount)
                    {
                        throw new JsonPathParseException("Invalid arity", _line, _column);
                    }
                    tokens.Add(_outputStack.Pop()); // Function

                    _outputStack.Push(new Token(new Expression(tokens)));
                    break;
                }
                case JsonPathTokenKind.EndArgument:
                {
                    UnwindRParen();
                    var tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().TokenKind != JsonPathTokenKind.BeginArgument)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
                    }
                    tokens.Reverse();
                    _outputStack.Pop(); // JsonPathTokenKind.BeginArgument
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
                throw new JsonPathParseException("Invalid codepoint", _line, _column);
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

} // namespace JsonCons.JsonPath
