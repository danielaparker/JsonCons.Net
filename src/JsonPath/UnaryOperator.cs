using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
        
namespace JsonCons.JsonPathLib
{
    interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IJsonValue elem, out IJsonValue result);
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

        public abstract bool TryEvaluate(IJsonValue elem, out IJsonValue result);
    };

    class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new NotOperator();

        internal NotOperator()
            : base(1, true)
        {}

        public override bool TryEvaluate(IJsonValue val, out IJsonValue result)
        {
            result = Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
            return true;
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

        public override bool TryEvaluate(IJsonValue val, out IJsonValue result)
        {
            if (!(val.ValueKind == JsonValueKind.Number))
            {
                result = JsonConstants.Null;
                return false; // type error
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                result = new DecimalJsonValue(-decVal);
                return true;
            }
            else if (val.TryGetDouble(out dblVal))
            {
                result = new DoubleJsonValue(-dblVal);
                return true;
            }
            else
            {
                result = JsonConstants.Null;
                return false;
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

        public override bool TryEvaluate(IJsonValue val, out IJsonValue result)
        {
            if (!(val.ValueKind == JsonValueKind.String))
            {
                result = JsonConstants.Null;
                return false; // type error
            }
            result = _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Regex";
        }
    };

} // namespace JsonCons.JsonPathLib

