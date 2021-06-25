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
    interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        JsonElement Evaluate(JsonElement elem);
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

        public abstract JsonElement Evaluate(JsonElement elem);
    };

    class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new NotOperator();

        internal NotOperator()
            : base(1, true)
        {}

        public override JsonElement Evaluate(JsonElement val)
        {
            return Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
        }
    };

    class UnaryMinusOperator : UnaryOperator
    {
        internal static UnaryMinusOperator Instance { get; } = new UnaryMinusOperator();

        internal UnaryMinusOperator()
            : base(1, true)
        {}

        public override JsonElement Evaluate(JsonElement val)
        {
            if (!(val.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }
            var s = val.ToString();
            if (s.StartsWith("-"))
            {
                return JsonDocument.Parse(s.Substring(1)).RootElement;
            }
            else
            {
                StringBuilder builder = new StringBuilder("-", 50);
                builder.Append(s);
                return JsonDocument.Parse(builder.ToString()).RootElement;
            }
        }
    };

} // namespace JsonCons.JsonPathLib

