using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

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
        BeginArguments,
        EndArguments,
        Argument,
        BeginExpressionType,
        EndExpressionType,
        EndOfExpression
    }

    readonly struct Token : IEquatable<Token>
    {
        readonly JmesPathTokenKind _tokenKind;
        readonly object? _expr;

        internal Token(JmesPathTokenKind tokenKind)
        {
            _tokenKind = tokenKind;
            _expr = null;
        }

        internal Token(JmesPathTokenKind tokenKind, string s)
        {
            _tokenKind = tokenKind;
            _expr = s;
        }

        internal Token(IExpression expr)
        {
            _tokenKind = JmesPathTokenKind.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            _tokenKind = JmesPathTokenKind.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            _tokenKind = JmesPathTokenKind.BinaryOperator;
            _expr = expr;
        }

        internal Token(IFunction expr)
        {
            _tokenKind = JmesPathTokenKind.Function;
            _expr = expr;
        }

        internal Token(IValue expr)
        {
            _tokenKind = JmesPathTokenKind.Literal;
            _expr = expr;
        }

        internal JmesPathTokenKind TokenKind
        {
            get { return _tokenKind; }   
        }

        internal bool IsOperator
        {
            get
            {
                switch(_tokenKind)
                {
                    case JmesPathTokenKind.UnaryOperator:
                        return true;
                    case JmesPathTokenKind.BinaryOperator:
                        return true;
                    default:
                        return false;
                }
            }
        }

        internal bool IsProjection
        {
            get
            {
                switch(_tokenKind)
                {
                    case JmesPathTokenKind.Expression:
                        return GetExpression().IsProjection;
                    default:
                        return false;
                }
            }
        }

        internal bool IsRightAssociative
        {
            get
            {
                switch(_tokenKind)
                {
                    case JmesPathTokenKind.Expression:
                        return GetExpression().IsRightAssociative;
                    case JmesPathTokenKind.UnaryOperator:
                        return GetUnaryOperator().IsRightAssociative;
                    case JmesPathTokenKind.BinaryOperator:
                        return GetBinaryOperator().IsRightAssociative;
                    default:
                        return false;
                }
            }
        }

        internal int PrecedenceLevel 
        {
            get
            {
                switch(_tokenKind)
                {
                    case JmesPathTokenKind.Expression:
                        return GetExpression().PrecedenceLevel;
                    case JmesPathTokenKind.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case JmesPathTokenKind.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 100;
                }
            }
        }

        internal string GetKey()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.Key);
            return _expr as string ?? throw new InvalidOperationException("Key cannot be null");
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.UnaryOperator);
            return _expr as IUnaryOperator ?? throw new InvalidOperationException("Unary operator cannot be null");
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.BinaryOperator);
            return _expr as IBinaryOperator ?? throw new InvalidOperationException("Binary operator cannot be null");
        }

        internal IValue GetValue()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.Literal);
            return _expr as IValue ?? throw new InvalidOperationException("Value cannot be null");
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.Function);
            return _expr as IFunction ?? throw new InvalidOperationException("Function cannot be null");
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(_tokenKind == JmesPathTokenKind.Expression);
            return _expr as IExpression ?? throw new InvalidOperationException("Expression cannot be null");
        }
        public bool Equals(Token other)
        {
            if (this._tokenKind == other._tokenKind)
                return true;
            else
                return false;        
        }

        public override string ToString()
        {
            switch(_tokenKind)
            {
                case JmesPathTokenKind.BeginArguments:
                    return "BeginArguments";
                case JmesPathTokenKind.CurrentNode:
                    return "CurrentNode";
                case JmesPathTokenKind.LeftParen:
                    return "LeftParen";
                case JmesPathTokenKind.RightParen:
                    return "RightParen";
                case JmesPathTokenKind.BeginMultiSelectHash:
                    return "BeginMultiSelectHash";
                case JmesPathTokenKind.EndMultiSelectHash:
                    return "EndMultiSelectHash";
                case JmesPathTokenKind.BeginMultiSelectList:
                    return "BeginMultiSelectList";
                case JmesPathTokenKind.EndMultiSelectList:
                    return "EndMultiSelectList";
                case JmesPathTokenKind.BeginFilter:
                    return "BeginFilter";
                case JmesPathTokenKind.EndFilter:
                    return "EndFilter";
                case JmesPathTokenKind.Pipe:
                    return $"Pipe";
                case JmesPathTokenKind.Separator:
                    return "Separator";
                case JmesPathTokenKind.Key:
                    return $"Key {_expr}";
                case JmesPathTokenKind.Literal:
                    return $"Literal {_expr}";
                case JmesPathTokenKind.Expression:
                    return "Expression";
                case JmesPathTokenKind.BinaryOperator:
                    return $"BinaryOperator {_expr}";
                case JmesPathTokenKind.UnaryOperator:
                    return $"UnaryOperator {_expr}";
                case JmesPathTokenKind.Function:
                    return $"Function {_expr}";
                case JmesPathTokenKind.EndArguments:
                    return "EndArguments";
                case JmesPathTokenKind.Argument:
                    return "Argument";
                case JmesPathTokenKind.BeginExpressionType:
                    return "BeginExpressionType";
                case JmesPathTokenKind.EndExpressionType:
                    return "EndExpressionType";
                case JmesPathTokenKind.EndOfExpression:
                    return "EndOfExpression";
                default:
                    return "Other";
            }
        }
    }
}
