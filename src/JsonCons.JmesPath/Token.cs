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
        EndFunction,
        Argument,
        BeginExpressionType,
        EndExpressionType,
        EndOfExpression
    }

    readonly struct Token : IEquatable<Token>
    {
        readonly JmesPathTokenKind _type;
        readonly object _expr;

        internal Token(JmesPathTokenKind type)
        {
            _type = type;
            _expr = null;
        }

        internal Token(IExpression expr)
        {
            _type = JmesPathTokenKind.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            _type = JmesPathTokenKind.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            _type = JmesPathTokenKind.BinaryOperator;
            _expr = expr;
        }

        internal Token(IFunction expr)
        {
            _type = JmesPathTokenKind.Function;
            _expr = expr;
        }

        internal Token(IValue expr)
        {
            _type = JmesPathTokenKind.Literal;
            _expr = expr;
        }

        internal JmesPathTokenKind TokenKind
        {
            get { return _type; }   
        }

        internal bool IsOperator
        {
            get
            {
                switch(_type)
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

        internal bool IsRightAssociative
        {
            get
            {
                switch(_type)
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
                switch(_type)
                {
                    case JmesPathTokenKind.Expression:
                        return GetExpression().PrecedenceLevel;
                    case JmesPathTokenKind.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case JmesPathTokenKind.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 0;
                }
            }
        }

        internal IValue GetValue()
        {
            Debug.Assert(_type == JmesPathTokenKind.Literal);
            return (IValue)_expr;
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(_type == JmesPathTokenKind.Function);
            return (IFunction)_expr;
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(_type == JmesPathTokenKind.Expression);
            return (IExpression)_expr;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(_type == JmesPathTokenKind.UnaryOperator);
            return (IUnaryOperator)_expr;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(_type == JmesPathTokenKind.BinaryOperator);
            return (IBinaryOperator)_expr;
        }

        public bool Equals(Token other)
        {
            if (this._type == other._type)
                return true;
            else
                return false;        
        }

        public override string ToString()
        {
            switch(_type)
            {
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
                    return "Key";
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
                case JmesPathTokenKind.EndFunction:
                    return "EndFunction";
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
