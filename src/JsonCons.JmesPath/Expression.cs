using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JmesPath
{
    static class JsonConstants
    {
        static JsonConstants()
        {
            True = new TrueValue();
            False = new FalseValue();
            Null = new NullValue();
        }

        internal static IValue True {get;}
        internal static IValue False {get;}
        internal static IValue Null {get;}
    }

    interface IExpression
    {
         bool TryEvaluate(DynamicResources resources,
                          IValue current, 
                          out IValue value);

         int PrecedenceLevel {get;} 

         bool IsProjection {get;} 

         bool IsRightAssociative {get;}

        void AddExpression(BaseExpression expr);
    }

    class Expression
    {
        IReadOnlyCollection<Token> _tokens;

        internal Expression(IReadOnlyCollection<Token> tokens)
        {
            _tokens = tokens;
        }

        public  bool TryEvaluate(DynamicResources resources,
                                 IValue current, 
                                 out IValue result)
        {
            result = JsonConstants.Null;
            return true;
        }

        internal static bool IsFalse(IValue val)
        {
            var comparer = ValueEqualityComparer.Instance;
            switch (val.ValueKind)
            {
                case JsonValueKind.False:
                    return true;
                case JsonValueKind.Null:
                    return true;
                case JsonValueKind.Array:
                    return val.GetArrayLength() == 0;
                case JsonValueKind.Object:
                    return val.EnumerateObject().MoveNext() == false;
                case JsonValueKind.String:
                    return val.GetString().Length == 0;
                case JsonValueKind.Number:
                    return false;
                default:
                    return false;
            }
        }

        internal static bool IsTrue(IValue val)
        {
            return !IsFalse(val);
        }
    }

    // BaseExpression
    abstract class BaseExpression : IExpression
    {
        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public bool IsProjection {get;} 

        internal BaseExpression(int precedenceLevel, 
                                bool isRightAssociative, 
                                bool isProjection)
        {
            PrecedenceLevel = precedenceLevel;
            IsRightAssociative = isRightAssociative;
            IsProjection = isProjection;
        }

        public abstract bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value);

        public virtual void AddExpression(BaseExpression expressions)
        {
        }

        public override string ToString()
        {
            return "ToString not implemented";
        }
    }  

    sealed class IdentifierSelector : BaseExpression
    {
        string _identifier;
    
        internal IdentifierSelector(string name)
            : base(1, false, false)
        {
            _identifier = name;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(_identifier, out value))
            {
                return true;
            }
            else 
            {
                value = JsonConstants.Null;
                return true;
            }
        }

        public override string ToString()
        {
            return $"IdentifierSelector {_identifier}";
        }
    };

    sealed class CurrentNode : BaseExpression
    {
        internal CurrentNode()
            : base(1, false, false)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            value = current;
            return true;
        }

        public override string ToString()
        {
            return "CurrentNode";
        }
    };

    sealed class IndexSelector : BaseExpression
    {
        Int32 _index;
        internal IndexSelector(Int32 index)
            : base(1, false, false)
        {
            _index = index;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            Int32 slen = current.GetArrayLength();
            if (_index >= 0 && _index < slen)
            {
                value = current[_index];
            }
            else if ((slen + _index) >= 0 && (slen+_index) < slen)
            {
                Int32 index = slen + _index;
                value = current[index];
            }
            else
            {
                value = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return $"Index Selector {_index}";
        }
    };

    // BaseProjection
    abstract class BaseProjection : BaseExpression
    {
        List<BaseExpression> _expressions;

        internal BaseProjection(int precedence_level, bool isRightAssociative = true)
            : base(precedence_level, isRightAssociative, true)
        {
            _expressions = new List<BaseExpression>();
        }

        public override void AddExpression(BaseExpression expr)
        {
            if (_expressions.Count != 0 && _expressions[_expressions.Count-1].IsProjection && 
                (expr.PrecedenceLevel < _expressions[_expressions.Count-1].PrecedenceLevel ||
                 (expr.PrecedenceLevel == _expressions[_expressions.Count-1].PrecedenceLevel && expr.IsRightAssociative)))
            {
                _expressions[_expressions.Count-1].AddExpression(expr);
            }
            else
            {
                _expressions.Add(expr);
            }
        }
        internal bool TryApplyExpressions(DynamicResources resources, IValue current, out IValue value)
        {
            value = current;
            foreach (var expression in _expressions)
            {
                if (!expression.TryEvaluate(resources, value, out value))
                {
                    return false;
                }
            }
            return true;
        }
    };

    sealed class ObjectProjection : BaseProjection
    {
        internal static ObjectProjection Instance {get;} = new ObjectProjection();

        internal ObjectProjection()
            : base(11, true)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            value = new ArrayValue(result);
            foreach (var item in current.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Null)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item.Value, out val))
                    {
                        return false;
                    }
                    if (val.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            return true;
        }

        public override string ToString()
        {
            return "ObjectProjection";
        }
    };
    sealed class ListProjection : BaseProjection
    {    
        internal ListProjection()
            : base(11, true)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Null)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item, out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "ListProjection";
        }
    };

    sealed class FlattenProjection : BaseProjection
    {
        internal FlattenProjection()
            : base(11, false)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                value = JsonConstants.Null;
                return false;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in item.EnumerateArray())
                    {
                        if (elem.ValueKind != JsonValueKind.Null)
                        {
                            IValue val;
                            if (!this.TryApplyExpressions(resources, elem, out val))
                            {
                                value = JsonConstants.Null;
                                return false;
                            }
                            if (val.ValueKind != JsonValueKind.Null)
                            {
                                result.Add(val);
                            }
                        }
                    }
                }
                else
                {
                    if (item.ValueKind != JsonValueKind.Null)
                    {
                        IValue val;
                        if (!this.TryApplyExpressions(resources, item, out val))
                        {
                            value = JsonConstants.Null;
                            return false;
                        }
                        if (val.ValueKind != JsonValueKind.Null)
                        {
                            result.Add(val);
                        }
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FlattenProjection";
        }
    };

    sealed class SliceProjection : BaseProjection
    {
        Slice _slice;
    
        internal SliceProjection(Slice s)
            : base(11, true)
        {
            _slice = s;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var start = _slice.GetStart(current.GetArrayLength());
            var end = _slice.GetStop(current.GetArrayLength());
            var step = _slice.Step;

            if (step == 0)
            {
                value = JsonConstants.Null;
                return false;;
            }

            var result = new List<IValue>();
            if (step > 0)
            {
                if (start < 0)
                {
                    start = 0;
                }
                if (end > current.GetArrayLength())
                {
                    end = current.GetArrayLength();
                }
                for (Int32 i = start; i < end; i += step)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, current[i], out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            else
            {
                if (start >= current.GetArrayLength())
                {
                    start = current.GetArrayLength() - 1;
                }
                if (end < -1)
                {
                    end = -1;
                }
                for (Int32 i = start; i > end; i += step)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, current[i], out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(val);
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "SliceProjection";
        }
    };

    sealed class FilterExpression : BaseProjection
    {
        readonly Expression _expr;
    
        internal FilterExpression(Expression expr)
            : base(11, true)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind != JsonValueKind.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var item in current.EnumerateArray())
            {
                IValue test;
                if (!_expr.TryEvaluate(resources, item, out test))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                if (Expression.IsTrue(test))
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item, out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FilterExpression";
        }
    };

    sealed class MultiSelectList : BaseExpression
    {
        IList<IExpression> _expressions;
    
        internal MultiSelectList(IList<IExpression> expressions)
            : base(1, false, false)
        {
            _expressions = expressions;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var expr in _expressions)
            {
                IValue val;
                if (!expr.TryEvaluate(resources, current, out val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(val);
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectList";
        }
    };

    struct KeyExpressionPair
    {
        internal string Key {get;}
        internal IExpression Expression {get;}

        internal KeyExpressionPair(string key, IExpression expression) 
        {
            Key = key;
            Expression = expression;
        }
    };

    sealed class MultiSelectHash : BaseExpression
    {
    
        IList<KeyExpressionPair> _keyExprPairs;

        internal MultiSelectHash(IList<KeyExpressionPair> keyExprPairs)
            : base(1, false, false)
        {
            _keyExprPairs = keyExprPairs;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new Dictionary<string,IValue>();
            foreach (var item in _keyExprPairs)
            {
                IValue val;
                if (!item.Expression.TryEvaluate(resources, current, out val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(item.Key, val);
            }

            value = new ObjectValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectHash";
        }
    }

    sealed class FunctionExpression : BaseExpression
    {
        IExpression _expr;

        internal FunctionExpression(IExpression expr)
            : base(1, false, false)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            IValue val;
            if (!_expr.TryEvaluate(resources, current, out val))
            {
                value = JsonConstants.Null;
                return true;
            }
            value = val;
            return true;
        }

        public override string ToString()
        {
            return "FunctionExpression";
        }
    }

} // namespace JsonCons.JmesPath

