﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JmesPath
{
    enum TokenType
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
        readonly object? _expr;

        internal Token(TokenType type)
        {
            Type = type;
            _expr = null;
        }

        internal Token(TokenType type, string s)
        {
            Type = type;
            _expr = s;
        }

        internal Token(IExpression expr)
        {
            Type = TokenType.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            Type = TokenType.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            Type = TokenType.BinaryOperator;
            _expr = expr;
        }

        internal Token(IFunction expr)
        {
            Type = TokenType.Function;
            _expr = expr;
        }

        internal Token(IValue expr)
        {
            Type = TokenType.Literal;
            _expr = expr;
        }

        internal TokenType Type{get;}

        internal bool IsOperator
        {
            get
            {
                switch(Type)
                {
                    case TokenType.UnaryOperator:
                        return true;
                    case TokenType.BinaryOperator:
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
                switch(Type)
                {
                    case TokenType.Expression:
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
                switch(Type)
                {
                    case TokenType.Expression:
                        return GetExpression().IsRightAssociative;
                    case TokenType.UnaryOperator:
                        return GetUnaryOperator().IsRightAssociative;
                    case TokenType.BinaryOperator:
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
                switch(Type)
                {
                    case TokenType.Expression:
                        return GetExpression().PrecedenceLevel;
                    case TokenType.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case TokenType.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 100;
                }
            }
        }

        internal string GetKey()
        {
            Debug.Assert(Type == TokenType.Key);
            return _expr as string ?? throw new InvalidOperationException("Key cannot be null");
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(Type == TokenType.UnaryOperator);
            return _expr as IUnaryOperator ?? throw new InvalidOperationException("Unary operator cannot be null");
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(Type == TokenType.BinaryOperator);
            return _expr as IBinaryOperator ?? throw new InvalidOperationException("Binary operator cannot be null");
        }

        internal IValue GetValue()
        {
            Debug.Assert(Type == TokenType.Literal);
            return _expr as IValue ?? throw new InvalidOperationException("Value cannot be null");
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(Type == TokenType.Function);
            return _expr as IFunction ?? throw new InvalidOperationException("Function cannot be null");
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(Type == TokenType.Expression);
            return _expr as IExpression ?? throw new InvalidOperationException("Expression cannot be null");
        }
        public bool Equals(Token other)
        {
            if (this.Type == other.Type)
                return true;
            else
                return false;        
        }

        public override string ToString()
        {
            switch(Type)
            {
                case TokenType.BeginArguments:
                    return "BeginArguments";
                case TokenType.CurrentNode:
                    return "CurrentNode";
                case TokenType.LeftParen:
                    return "LeftParen";
                case TokenType.RightParen:
                    return "RightParen";
                case TokenType.BeginMultiSelectHash:
                    return "BeginMultiSelectHash";
                case TokenType.EndMultiSelectHash:
                    return "EndMultiSelectHash";
                case TokenType.BeginMultiSelectList:
                    return "BeginMultiSelectList";
                case TokenType.EndMultiSelectList:
                    return "EndMultiSelectList";
                case TokenType.BeginFilter:
                    return "BeginFilter";
                case TokenType.EndFilter:
                    return "EndFilter";
                case TokenType.Pipe:
                    return $"Pipe";
                case TokenType.Separator:
                    return "Separator";
                case TokenType.Key:
                    return $"Key {_expr}";
                case TokenType.Literal:
                    return $"Literal {_expr}";
                case TokenType.Expression:
                    return "Expression";
                case TokenType.BinaryOperator:
                    return $"BinaryOperator {_expr}";
                case TokenType.UnaryOperator:
                    return $"UnaryOperator {_expr}";
                case TokenType.Function:
                    return $"Function {_expr}";
                case TokenType.EndArguments:
                    return "EndArguments";
                case TokenType.Argument:
                    return "Argument";
                case TokenType.BeginExpressionType:
                    return "BeginExpressionType";
                case TokenType.EndExpressionType:
                    return "EndExpressionType";
                case TokenType.EndOfExpression:
                    return "EndOfExpression";
                default:
                    return "Other";
            }
        }
    }
}
