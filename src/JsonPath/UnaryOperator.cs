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
    interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        IJsonValue Evaluate(IJsonValue elem);
    };

    abstract class UnaryOperator : IUnaryOperator
    {
        internal UnaryOperator(int precedenceLevel,
                                bool isRightAssociative = false)
        {
            PrecedenceLevel = precedenceLevel;
            IsRightAssociative = isRightAssociative;
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract IJsonValue Evaluate(IJsonValue elem);
    };

    class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new NotOperator();

        internal NotOperator()
            : base(1, true)
        {}

        public override IJsonValue Evaluate(IJsonValue val)
        {
            return Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "Not";
        }
    };

    class UnaryMinusOperator : UnaryOperator
    {
        internal static UnaryMinusOperator Instance { get; } = new UnaryMinusOperator();

        internal UnaryMinusOperator()
            : base(1, true)
        {}

        public override IJsonValue Evaluate(IJsonValue val)
        {
            if (!(val.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                return new DecimalJsonValue(-decVal);
            }
            else if (val.TryGetDouble(out dblVal))
            {
                return new DoubleJsonValue(-dblVal);
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "Unary minus";
        }
    };

    class RegexOperator : UnaryOperator
    {
        Regex _regex;

        internal RegexOperator(Regex regex)
            : base(2, true)
        {
            _regex = regex;
        }

        public override IJsonValue Evaluate(IJsonValue val)
        {
            if (!(val.ValueKind == JsonValueKind.String))
            {
                return JsonConstants.Null;
            }
            return _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "Regex";
        }
    };

} // namespace JsonCons.JsonPathLib

