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
        ExpectRightBracket,
        ExpectRightParen,
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

        internal JsonSearcher Parse()
        {
            _stateStack.Clear();
            _outputStack.Clear();
            _operatorStack.Clear();
            _index = 0;
            _line = 1;
            _column = 1;

            StringBuilder buffer = new StringBuilder();
            Int32? sliceStart = null;
            Int32? sliceStop = null;
            Int32 sliceStep = 1;
            UInt32 cp = 0;
            UInt32 cp2 = 0;

            PushToken(new Token(JmesPathTokenKind.CurrentNode));
            _stateStack.Push(JmesPathState.Start);

            while (_index < _span.Length)
            {
                switch (_stateStack.Peek())
                {
                    default:
                        break;
                    case JmesPathState.Start: 
                    {
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.RhsExpression);
                        _stateStack.Push(JmesPathState.LhsExpression);
                        break;
                    }
                    case JmesPathState.RhsExpression:
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.': 
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.SubExpression);
                                break;
                            case '|':
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectPipeOrOr);
                                break;
                            case '&':
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectAnd);
                                break;
                            case '<':
                            case '>':
                            case '=':
                            {
                                _stateStack.Push(JmesPathState.ComparatorExpression);
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpNe);
                                break;
                            }
                            case ')':
                            {
                                _stateStack.Pop();
                                break;
                            }
                            case '[':
                                _stateStack.Push(JmesPathState.BracketSpecifier);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JmesPathParseException("Syntax error", _line, _column);
                                }
                                break;
                        }
                        break;
                    case JmesPathState.ComparatorExpression:
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpGtOrGte);
                                break;
                            case '=':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpEq);
                                break;
                            }
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JmesPathParseException("Syntax error", _line, _column);
                                }
                                break;
                        }
                        break;
                    case JmesPathState.LhsExpression: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ValExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RawString);
                                ++_index;
                                ++_column;
                                break;
                            case '`':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Literal);
                                ++_index;
                                ++_column;
                                break;
                            case '{':
                                PushToken(new Token(JmesPathTokenKind.BeginMultiSelectHash));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectHash);
                                ++_index;
                                ++_column;
                                break;
                            case '*': // wildcard
                                PushToken(new Token(ObjectProjection.Instance));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(JmesPathTokenKind.LeftParen));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightParen);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(NotOperator.Instance));
                                break;
                            }
                            case '@':
                                ++_index;
                                ++_column;
                                PushToken(new Token(new CurrentNode()));
                                _stateStack.Pop();
                                break;
                            case '[': 
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.BracketSpecifierOrMultiSelectList);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.IdentifierOrFunctionExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JmesPathParseException("Expected identifier", _line, _column);
                                }
                                break;
                        };
                        break;
                    }
/*
                    case JmesPathState.subExpression: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ValExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '{':
                                PushToken(new Token(JmesPathTokenKind.BeginMultiSelectHash));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectHash);
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                PushToken(new Token(jsoncons.Make_unique<objectProjection>()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '[': 
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectMultiSelectList);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.IdentifierOrFunctionExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    ec = jmespathErrc.Expected_identifier;
                                    return jmespathExpression();
                                }
                                break;
                        };
                        break;
                    }
                    case JmesPathState.keyExpr:
                        PushToken(new Token(keyArg, buffer));
                        buffer.Clear(); 
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.ValExpr:
                        PushToken(new Token(jsoncons.Make_unique<identifierSelector>(buffer)));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.ExpressionOrExpression_type:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case '&':
                                PushToken(new Token(beginExpression_typeArg));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpressionType);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Argument);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;
                    case JmesPathState.IdentifierOrFunctionExpr:
                        switch(_span[_index])
                        {
                            case '(':
                            {
                                auto f = resources_.getFunction(buffer);
                                buffer.Clear();
                                PushToken(new Token(f));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.FunctionExpression);
                                _stateStack.Push(JmesPathState.ExpressionOrExpressionType);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                PushToken(new Token(jsoncons.Make_unique<identifierSelector>(buffer)));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                break;
                            }
                        }
                        break;

                    case JmesPathState.functionExpression:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ',':
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Push(JmesPathState.ExpressionOrExpressionType);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                PushToken(new Token(endFunctionArg));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                break;
                        }
                        break;

                    case JmesPathState.Argument:
                        PushToken(new Token(JmesPathTokenKind.Argument));
                        _stateStack.Pop();
                        break;

                    case JmesPathState.ExpressionType:
                        PushToken(new Token(JmesPathTokenKind.EndExpressionType));
                        PushToken(new Token(JmesPathTokenKind.Argument));
                        _stateStack.Pop();
                        break;

                    case JmesPathState.QuotedString: 
                        switch (_span[_index])
                        {
                            case '\"':
                                _stateStack.Pop(); // quotedString
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(JmesPathState.QuotedStringEscape_char);
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

                    case JmesPathState.UnquotedString: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                _stateStack.Pop(); // unquotedString
                                advancePastSpace_character();
                                break;
                            default:
                                if ((_span[_index] >= '0' && _span[_index] <= '9') || (_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    _stateStack.Pop(); // unquotedString
                                }
                                break;
                        };
                        break;
                    case JmesPathState.RawStringEscape_char:
                        switch (_span[_index])
                        {
                            case '\'':
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append('\\');
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    case JmesPathState.QuotedStringEscape_char:
                        switch (_span[_index])
                        {
                            case '\"':
                                buffer.Append('\"');
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
                                _stateStack.Push(JmesPathState.EscapeU1);
                                break;
                            default:
                                ec = jmespathErrc.IllegalEscaped_character;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.EscapeU1:
                        cp = appendTo_codepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU2);
                        break;
                    case JmesPathState.EscapeU2:
                        cp = appendTo_codepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU3);
                        break;
                    case JmesPathState.EscapeU3:
                        cp = appendTo_codepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU4);
                        break;
                    case JmesPathState.EscapeU4:
                        cp = appendTo_codepoint(cp, _span[_index]);
                        if (unicodeTraits.IsHighSurrogate(cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                            _stateStack.Push(JmesPathState.EscapeExpectSurrogatePair1);
                        }
                        else
                        {
                            unicodeTraits.Convert(&cp, 1, buffer);
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                        }
                        break;
                    case JmesPathState.EscapeExpectSurrogatePair1:
                        switch (_span[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                ec = jmespathErrc.Invalid_codepoint;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.EscapeExpectSurrogatePair2:
                        switch (_span[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.EscapeU5);
                                break;
                            default:
                                ec = jmespathErrc.Invalid_codepoint;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.EscapeU5:
                        cp2 = appendTo_codepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU6);
                        break;
                    case JmesPathState.EscapeU6:
                        cp2 = appendTo_codepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU7);
                        break;
                    case JmesPathState.EscapeU7:
                        cp2 = appendTo_codepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.EscapeU8);
                        break;
                    case JmesPathState.EscapeU8:
                    {
                        cp2 = appendTo_codepoint(cp2, _span[_index]);
                        UInt32 codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        unicodeTraits.Convert(&codepoint, 1, buffer);
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }
                    case JmesPathState.RawString: 
                        switch (_span[_index])
                        {
                            case '\'':
                            {
                                PushToken(new Token(literalArg, Json(buffer)));
                                buffer.Clear();
                                _stateStack.Pop(); // rawString
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '\\':
                                _stateStack.Push(JmesPathState.RawStringEscape_char);
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
                    case JmesPathState.Literal: 
                        switch (_span[_index])
                        {
                            case '`':
                            {
                                json_decoder<Json> decoder;
                                basic_json_reader<charType,stringSource<charType>> reader(buffer, decoder);
                                std.Error_code parseEc;
                                reader.Read(parseEc);
                                if (parseEc)
                                {
                                    ec = jmespathErrc.InvalidLiteral;
                                    return jmespathExpression();
                                }
                                auto j = decoder.get_result();

                                PushToken(new Token(literalArg, std.Move(j)));
                                buffer.Clear();
                                _stateStack.Pop(); // json_value
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '\\':
                                if (p_+1 < end_input_)
                                {
                                    ++_index;
                                    ++_column;
                                    if (_span[_index] != '`')
                                    {
                                        buffer.Append('\\');
                                    }
                                    buffer.Append(_span[_index]);
                                }
                                else
                                {
                                    ec = jmespathErrc.UnexpectedEndOf_input;
                                    return jmespathExpression();
                                }
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
                    case JmesPathState.number:
                        switch(_span[_index])
                        {
                            case '-':
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.digit);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.digit);
                                break;
                        }
                        break;
                    case JmesPathState.digit:
                        switch(_span[_index])
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // digit
                                break;
                        }
                        break;

                    case JmesPathState.BracketSpecifier:
                        switch(_span[_index])
                        {
                            case '*':
                                PushToken(new Token(jsoncons.MakeUnique<listProjection>()));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Expect_rbracket);
                                ++_index;
                                ++_column;
                                break;
                            case ']': // []
                                PushToken(new Token(jsoncons.MakeUnique<flattenProjection>()));
                                _stateStack.Pop(); // bracketSpecifier
                                ++_index;
                                ++_column;
                                break;
                            case '?':
                                PushToken(new Token(beginFilterArg));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Filter);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            case ':': // sliceExpression
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStop);
                                _stateStack.Push(JmesPathState.number);
                                ++_index;
                                ++_column;
                                break;
                            // number
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.IndexOrSliceExpression);
                                _stateStack.Push(JmesPathState.number);
                                break;
                            default:
                                ec = jmespathErrc.Expected_indexExpression;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.BracketSpecifierOrMultiSelectList:
                        switch(_span[_index])
                        {
                            case '*':
                                if (p_+1 >= end_input_)
                                {
                                    ec = jmespathErrc.UnexpectedEndOf_input;
                                    return jmespathExpression();
                                }
                                if (*(p_+1) == ']')
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.BracketSpecifier);
                                }
                                else
                                {
                                    PushToken(new Token(beginMultiSelectListArg));
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.MultiSelectList);
                                    _stateStack.Push(JmesPathState.LhsExpression);                                
                                }
                                break;
                            case ']': // []
                            case '?':
                            case ':': // sliceExpression
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.BracketSpecifier);
                                break;
                            default:
                                PushToken(new Token(beginMultiSelectListArg));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectList);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;

                    case JmesPathState.ExpectMultiSelectList:
                        switch(_span[_index])
                        {
                            case ']':
                            case '?':
                            case ':':
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                ec = jmespathErrc.ExpectedMultiSelectList;
                                return jmespathExpression();
                            case '*':
                                PushToken(new Token(jsoncons.MakeUnique<listProjection>()));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightBracket);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(beginMultiSelectListArg));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectList);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;

                    case JmesPathState.MultiSelectHash:
                        switch(_span[_index])
                        {
                            case '*':
                            case ']':
                            case '?':
                            case ':':
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.KeyValExpr);
                                break;
                        }
                        break;

                    case JmesPathState.IndexOrSliceExpression:
                        switch(_span[_index])
                        {
                            case ']':
                            {
                                if (buffer.Empty())
                                {
                                    PushToken(new Token(jsoncons.MakeUnique<flattenProjection>()));
                                }
                                else
                                {
                                    int64T val{ 0 };
                                    auto r = jsoncons.detail.to_integer(buffer.data(), buffer.size(), val);
                                    if (!r)
                                    {
                                        ec = jmespathErrc.InvalidNumber;
                                        return jmespathExpression();
                                    }
                                    PushToken(new Token(jsoncons.MakeUnique<indexSelector>(val)));

                                    buffer.Clear();
                                }
                                _stateStack.Pop(); // bracketSpecifier
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ':':
                            {
                                if (!buffer.Empty())
                                {
                                    int64T val;
                                    auto r = jsoncons.detail.to_integer(buffer.data(), buffer.size(), val);
                                    if (!r)
                                    {
                                        ec = jmespathErrc.InvalidNumber;
                                        return jmespathExpression();
                                    }
                                    slic.start_ = val;
                                    buffer.Clear();
                                }
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStop);
                                _stateStack.Push(JmesPathState.number);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.RhsSliceExpressionStop :
                    {
                        if (!buffer.Empty())
                        {
                            int64T val{ 0 };
                            auto r = jsoncons.detail.to_integer(buffer.data(), buffer.size(), val);
                            if (!r)
                            {
                                ec = jmespathErrc.InvalidNumber;
                                return jmespathExpression();
                            }
                            slic.stop_ = jsoncons.optional<int64T>(val);
                            buffer.Clear();
                        }
                        switch(_span[_index])
                        {
                            case ']':
                                PushToken(new Token(jsoncons.MakeUnique<sliceProjection>(slic)));
                                slic = slice{};
                                _stateStack.Pop(); // bracketSpecifier2
                                ++_index;
                                ++_column;
                                break;
                            case ':':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStep);
                                _stateStack.Push(JmesPathState.number);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.RhsSliceExpressionStep:
                    {
                        if (!buffer.Empty())
                        {
                            int64T val{ 0 };
                            auto r = jsoncons.detail.to_integer(buffer.data(), buffer.size(), val);
                            if (!r)
                            {
                                ec = jmespathErrc.InvalidNumber;
                                return jmespathExpression();
                            }
                            if (val == 0)
                            {
                                ec = jmespathErrc.step_cannot_be_zero;
                                return jmespathExpression();
                            }
                            slic.step_ = val;
                            buffer.Clear();
                        }
                        switch(_span[_index])
                        {
                            case ']':
                                PushToken(new Token(jsoncons.MakeUnique<sliceProjection>(slic)));
                                buffer.Clear();
                                slic = slice{};
                                _stateStack.Pop(); // rhsSliceExpressionStep
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.Expect_rbracket:
                    {
                        switch(_span[_index])
                        {
                            case ']':
                                _stateStack.Pop(); // expect_rbracket
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.ExpectRightParen:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ')':
                                ++_index;
                                ++_column;
                                PushToken(new Token(JmesPathTokenKind.Rparen));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsExpression);
                                break;
                            default:
                                ec = jmespathErrc.ExpectedRightParen;
                                return jmespathExpression();
                        }
                        break;
                    case JmesPathState.KeyValExpr: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectColon);
                                _stateStack.Push(JmesPathState.keyExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectColon);
                                _stateStack.Push(JmesPathState.RawString);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.ExpectColon);
                                    _stateStack.Push(JmesPathState.keyExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    ec = jmespathErrc.Expected_key;
                                    return jmespathExpression();
                                }
                                break;
                        };
                        break;
                    }
                    case JmesPathState.CmpLtOrLte:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(resources_.getLteOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(resources_.getLtOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop();
                                break;
                        }
                        break;
                    }
                    case JmesPathState.CmpGtOrGte:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(resources_.getGteOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(resources_.getGtOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case JmesPathState.CmpEq:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(resources_.getEqOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_comparator;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.CmpNe:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(resources_.getNeOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_comparator;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.Expect_dot:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case '.':
                                _stateStack.Pop(); // expect_dot
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_dot;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.ExpectPipeOrOr:
                    {
                        switch(_span[_index])
                        {
                            case '|':
                                PushToken(new Token(resources_.getOrOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(pipeArg));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case JmesPathState.ExpectAnd:
                    {
                        switch(_span[_index])
                        {
                            case '&':
                                PushToken(new Token(resources_.getAndOperator()));
                                PushToken(new Token(currentNodeArg));
                                _stateStack.Pop(); // expectAnd
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.ExpectedAnd;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.ExpectFilter_rbracket:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ']':
                            {
                                _stateStack.Pop();

                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.MultiSelectList:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ',':
                                PushToken(new Token(separatorArg));
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            case '.':
                                _stateStack.Push(JmesPathState.subExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '|':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectPipeOrOr);
                                break;
                            }
                            case ']':
                            {
                                PushToken(new Token(endMultiSelectListArg));
                                _stateStack.Pop();

                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.filter:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ']':
                            {
                                PushToken(new Token(endFilterArg));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jmespathErrc.Expected_rbracket;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.Expect_rbrace:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ',':
                                PushToken(new Token(separatorArg));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.KeyValExpr); 
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                            case '{':
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            case '.':
                                _stateStack.Push(JmesPathState.subExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '}':
                            {
                                _stateStack.Pop();
                                PushToken(new Token(JmesPathTokenKind.EndMultiSelectHash));
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jmespathErrc.Expected_rbrace;
                                return jmespathExpression();
                        }
                        break;
                    }
                    case JmesPathState.ExpectColon:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                advancePastSpace_character();
                                break;
                            case ':':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightBrace);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jmespathErrc.Expected_colon;
                                return jmespathExpression();
                        }
                        break;
                    }
*/                
                }
            }
/*
            if (_stateStack.Empty())
            {
                throw new JmesPathParseException("Syntax error", _line, _column);
            }
            while (_stateStack.Count > 1)
            {
                switch (_stateStack.Peek())
                {
                    case JmesPathState.RhsExpression:
                        if (_stateStack.Count > 1)
                        {
                            _stateStack.Pop();
                        }
                        else
                        {
                            throw new JmesPathParseException("Syntax error", _line, _column);
                        }
                        break;
                    case JmesPathState.ValExpr:
                        PushToken(new Token(jsoncons.MakeUnique<identifierSelector>(buffer)));
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.IdentifierOrFunctionExpr:
                        PushToken(new Token(jsoncons.MakeUnique<identifierSelector>(buffer)));
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.UnquotedString: 
                        _stateStack.Pop(); 
                        break;
                    default:
                        throw new JmesPathParseException("Syntax error", _line, _column);
                }
            }

            if (!(_stateStack.Count == 1 && _stateStack.Peek() == JmesPathState.RhsExpression))
            {
                ec = jmespathErrc.UnexpectedEndOf_input;
                return jmespathExpression();
            }

            _stateStack.Pop();

            PushToken(new Token(JmesPathTokenKind.EndOfExpression));
*/
            return new JsonSearcher();
        }

        private void PushToken(Token token)
        {
            switch (token.TokenKind)
            {
            }
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
    }
}
