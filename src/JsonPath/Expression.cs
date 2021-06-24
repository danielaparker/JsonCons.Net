using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    class Resources 
    {

    };

    static class JsonConstants
    {
        static readonly JsonElement _trueValue;
        static readonly JsonElement _falseValue;
        static readonly JsonElement _nullValue;

        static JsonConstants()
        {
            _trueValue = JsonDocument.Parse("true").RootElement;
            _falseValue = JsonDocument.Parse("false").RootElement;
            _nullValue = JsonDocument.Parse("null").RootElement;
        }


        internal static JsonElement True {get {return _trueValue;}}
        internal static JsonElement False { get { return _falseValue; } }
        internal static JsonElement Null { get { return _falseValue; } }
    };

    interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        JsonElement Evaluate(JsonElement elem);
    };

    interface IBinaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        JsonElement Evaluate(JsonElement lhs, JsonElement rhs);
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

    abstract class BinaryOperator : IBinaryOperator
    {
        internal BinaryOperator(int precedenceLevel,
                                bool isRightAssociative = false)
        {
            PrecedenceLevel = precedenceLevel;
            IsRightAssociative = isRightAssociative;
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract JsonElement Evaluate(JsonElement lhs, JsonElement rhs);
    };

    class EqOperator : BinaryOperator
    {
        internal EqOperator()
            : base(6)
        {
        }

        public override JsonElement Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            var comparer = new JsonElementEqualityComparer();
            return comparer.Equals(lhs, rhs) ? JsonConstants.True : JsonConstants.False;
        }
    };

    interface IExpression
    {
        JsonElement Evaluate(JsonElement root,
                             PathNode stem, 
                             JsonElement current, 
                             ResultOptions options);
    }

    class Expression : IExpression
    {
        IReadOnlyList<Token> _tokens;

        internal Expression(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
        }

        public JsonElement Evaluate(JsonElement root,
                                    PathNode stem, 
                                    JsonElement current, 
                                    ResultOptions options)
        {
            return root;
        }
    };

} // namespace JsonCons.JsonPathLib

