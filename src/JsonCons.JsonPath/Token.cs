using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPath
{
    enum TokenType
    {
        RootNode,
        CurrentNode,
        Expression,
        LeftParen,
        RightParen,
        BeginUnion,
        EndUnion,
        BeginFilter,
        EndFilter,
        BeginArgument,
        Separator,
        Value,
        Selector,
        BeginArguments,
        Function,
        EndArguments,
        Argument,
        EndArgument,
        UnaryOperator,
        BinaryOperator
    };

    readonly struct Token : IEquatable<Token>
    {
        readonly object? _expr;

        internal Token(TokenType type)
        {
            Type = type;
            _expr = null;
        }

        internal Token(ISelector selector)
        {
            Type = TokenType.Selector;
            _expr = selector;
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
            Type = TokenType.Value;
            _expr = expr;
        }

        internal TokenType Type {get;}   

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

        internal bool IsRightAssociative
        {
            get
            {
                switch(Type)
                {
                    case TokenType.Selector:
                        return true;
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
                    case TokenType.Selector:
                        return 11;
                    case TokenType.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case TokenType.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 0;
                }
            }
        }

        internal IValue GetValue()
        {
            Debug.Assert(Type == TokenType.Value);
            return _expr as IValue ?? throw new InvalidOperationException("Value cannot be null");
        }

        internal ISelector GetSelector()
        {
            Debug.Assert(Type == TokenType.Selector);
            return _expr as ISelector ?? throw new InvalidOperationException("Selector cannot be null");;
        }

        internal IFunction GetFunction()
        {
            Debug.Assert(Type == TokenType.Function);
            return _expr as IFunction ?? throw new InvalidOperationException("Function cannot be null");;
        }

        internal IExpression GetExpression()
        {
            Debug.Assert(Type == TokenType.Expression);
            return _expr as IExpression ?? throw new InvalidOperationException("Expression cannot be null");;
        }

        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(Type == TokenType.UnaryOperator);
            return _expr as IUnaryOperator ?? throw new InvalidOperationException("Unary operator cannot be null");;
        }

        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(Type == TokenType.BinaryOperator);
            return _expr as IBinaryOperator ?? throw new InvalidOperationException("Binary operator cannot be null");;
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
                case TokenType.RootNode:
                    return "RootNode";
                case TokenType.CurrentNode:
                    return "CurrentNode";
                case TokenType.BeginFilter:
                    return "BeginFilter";
                case TokenType.EndFilter:
                    return "EndFilter";
                case TokenType.BeginUnion:
                    return "BeginUnion";
                case TokenType.EndUnion:
                    return "EndUnion";
                case TokenType.Value:
                    return $"Value {_expr}";
                case TokenType.Selector:
                    return $"Selector {_expr}";
                case TokenType.UnaryOperator:
                    return $"UnaryOperator {_expr}";
                case TokenType.BinaryOperator:
                    return $"BinaryOperator {_expr}";
                case TokenType.Function:
                    return $"Function {_expr}";
                case TokenType.EndArguments:
                    return "EndArguments";
                case TokenType.Argument:
                    return "Argument";
                case TokenType.EndArgument:
                    return "EndArgument";
                case TokenType.Expression:
                    return "Expression";
                case TokenType.BeginArgument:
                    return "BeginArgument";
                case TokenType.LeftParen:
                    return "LeftParen";
                case TokenType.RightParen:
                    return "RightParen";
                case TokenType.Separator:
                    return "Separator";
                default:
                    return "Other";
            }
        }
    };

} // namespace JsonCons.JsonPath
