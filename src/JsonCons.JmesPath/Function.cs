        
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System;

namespace JsonCons.JmesPath
{
    sealed class SortByComparer : IComparer<IValue>, System.Collections.IComparer
    {
        DynamicResources _resources;
        IExpression _expr;

        internal bool IsValid { get; set; } = true;

        internal SortByComparer(DynamicResources resources,
                                IExpression expr)
        {
            _resources = resources;
            _expr = expr;
        }

        public int Compare(IValue lhs, IValue rhs)
        {
            var comparer = ValueComparer.Instance;

            if (!IsValid)
            {
                return 0;
            }
            IValue key1;
            if (!_expr.TryEvaluate(_resources, lhs, out key1))
            {
                IsValid = false;
                return 0;
            }
            bool isNumber1 = key1.Type == JmesPathType.Number;
            bool isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                IsValid = false;
                return 0;
            }
            IValue key2;
            if (!_expr.TryEvaluate(_resources, rhs, out key2))
            {
                IsValid = false;
                return 0;
            }
            bool isNumber2 = key2.Type == JmesPathType.Number;
            bool isString2 = key2.Type == JmesPathType.String;
            if (!(isNumber2 == isNumber1 && isString2 == isString1))
            {
                IsValid = false;
                return 0;
            }
            return comparer.Compare(key1, key2);
        }

        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            return this.Compare((IValue)x, (IValue)y);
        }        
    }

    interface IFunction 
    {
        int? Arity {get;}
        bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
    };

    abstract class BaseFunction : IFunction
    {
        internal BaseFunction(int? argCount)
        {
            Arity = argCount;
        }

        public int? Arity {get;}

        public abstract bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
    };  

    sealed class AbsFunction : BaseFunction
    {
        internal AbsFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result) 
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg = args[0];

            Decimal decVal;
            double dblVal;

            if (arg.TryGetDecimal(out decVal))
            {
                result = new DecimalValue(decVal >= 0 ? decVal : -decVal);
                return true;
            }
            else if (arg.TryGetDouble(out dblVal))
            {
                result = new DecimalValue(dblVal >= 0 ? decVal : new Decimal(-dblVal));
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
            return "abs";
        }
    };

    sealed class AvgFunction : BaseFunction
    {
        internal AvgFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array || arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }

            IValue sum;
            if (!SumFunction.Instance.TryEvaluate(resources, args, out sum))
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (sum.TryGetDecimal(out decVal))
            {
                result = new DecimalValue(decVal/arg0.GetArrayLength());
                return true;
            }
            else if (sum.TryGetDouble(out dblVal))
            {
                result = new DoubleValue(dblVal/arg0.GetArrayLength());
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
            return "avg";
        }
    };

    sealed class CeilFunction : BaseFunction
    {
        internal CeilFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var val = args[0];
            if (val.Type != JmesPathType.Number)
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                result = new DecimalValue(decimal.Ceiling(decVal));
                return true;
            }
            else if (val.TryGetDouble(out dblVal))
            {
                result = new DoubleValue(Math.Ceiling(dblVal));
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
            return "ceil";
        }
    };

    sealed class ContainsFunction : BaseFunction
    {
        internal ContainsFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            var arg1 = args[1];

            var comparer = ValueEqualityComparer.Instance;

            switch (arg0.Type)
            {
                case JmesPathType.Array:
                    foreach (var item in arg0.EnumerateArray())
                    {
                        if (comparer.Equals(item, arg1))
                        {
                            result = JsonConstants.True;
                            return true;
                        }
                    }
                    result = JsonConstants.False;
                    return true;
                case JmesPathType.String:
                {
                    if (arg1.Type != JmesPathType.String)
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    var s0 = arg0.GetString();
                    var s1 = arg1.GetString();
                    if (s0.Contains(s1))
                    {
                        result = JsonConstants.True;
                        return true;
                    }
                    else
                    {
                        result = JsonConstants.False;
                        return true;
                    }
                }
                default:
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }
        }

        public override string ToString()
        {
            return "contains";
        }
    };

    sealed class EndsWithFunction : BaseFunction
    {
        internal EndsWithFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.Type != JmesPathType.String
                || arg1.Type != JmesPathType.String)
            {
                result = JsonConstants.Null;
                return false;
            }

            var s0 = arg0.GetString();
            var s1 = arg1.GetString();

            if (s0.EndsWith(s1))
            {
                result = JsonConstants.True;
            }
            else
            {
                result = JsonConstants.False;
            }
            return true;
        }

        public override string ToString()
        {
            return "ends_with";
        }
    };

    sealed class FloorFunction : BaseFunction
    {
        internal FloorFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var val = args[0];
            if (val.Type != JmesPathType.Number)
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                result = new DecimalValue(decimal.Floor(decVal));
                return true;
            }
            else if (val.TryGetDouble(out dblVal))
            {
                result = new DoubleValue(Math.Floor(dblVal));
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
            return "floor";
        }
    };

    sealed class JoinFunction : BaseFunction
    {
        internal JoinFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            var arg1 = args[1];

            if (!(arg0.Type == JmesPathType.String && args[1].Type == JmesPathType.Array))
            {
                result =  JsonConstants.Null;
                return false;
            }

            string sep = arg0.GetString();
            var buf = new StringBuilder();
            foreach (var j in arg1.EnumerateArray())
            {
                if (j.Type != JmesPathType.String)
                {
                    result =  JsonConstants.Null;
                    return false;
                }
                if (buf.Length != 0)
                {
                    buf.Append(sep);
                }
                var sv = j.GetString();
                buf.Append(sv);
            }
            result = new StringValue(buf.ToString());
            return true;
        }

        public override string ToString()
        {
            return "join";
        }
    }

    sealed class KeysFunction : BaseFunction
    {
        internal KeysFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Object)
            {
                result = JsonConstants.Null;
                return false;
            }

            var values = new List<IValue>();

            foreach (var property in arg0.EnumerateObject())
            {
                values.Add(new StringValue(property.Name));
            }
            result = new ArrayValue(values);
            return true;
        }

        public override string ToString()
        {
            return "keys";
        }
    }

    sealed class LengthFunction : BaseFunction
    {
        internal LengthFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];

            switch (arg0.Type)
            {
                case JmesPathType.Object:
                {
                    int count = 0;
                    foreach (var item in arg0.EnumerateObject())
                    {
                        ++count;
                    }
                    result = new DecimalValue(new Decimal(count));
                    return true;
                }
                case JmesPathType.Array:
                    result = new DecimalValue(new Decimal(arg0.GetArrayLength()));
                    return true;
                case JmesPathType.String:
                {
                    byte[] bytes = Encoding.UTF32.GetBytes(arg0.GetString().ToCharArray());
                    result = new DecimalValue(new Decimal(bytes.Length/4));
                    return true;
                }
                default:
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }
        }

        public override string ToString()
        {
            return "length";
        }
    };

    sealed class MaxFunction : BaseFunction
    {
        internal MaxFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            bool isNumber = arg0[0].Type == JmesPathType.Number;
            bool isString = arg0[0].Type == JmesPathType.String;
            if (!isNumber && !isString)
            {
                result = JsonConstants.Null;
                return false;
            }

            var greater = GtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].Type == JmesPathType.Number) == isNumber) && (arg0[i].Type == JmesPathType.String) == isString))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IValue value;
                if (!greater.TryEvaluate(arg0[i],arg0[index], out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (Expression.IsTrue(value))
                {
                    index = i;
                }
            }

            result = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "max";
        }
    }

    sealed class MaxByFunction : BaseFunction
    {
        internal MaxByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return true;
            }

            var expr = args[1].GetExpression();

            IValue key1;
            if (!expr.TryEvaluate(resources, arg0[0], out key1))
            {
                result = JsonConstants.Null;
                return false;
            }

            bool isNumber1 = key1.Type == JmesPathType.Number;
            bool isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                result = JsonConstants.Null;
                return false;
            }

            var greater = GtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                IValue key2;
                if (!expr.TryEvaluate(resources, arg0[i], out key2))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                bool isNumber2 = key2.Type == JmesPathType.Number;
                bool isString2 = key2.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IValue value;
                if (!greater.TryEvaluate(key2, key1, out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (value.Type == JmesPathType.True )
                {
                    key1 = key2;
                    index = i;
                }
            }

            result = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "max_by";
        }
    }

    sealed class MinFunction : BaseFunction
    {
        internal MinFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            bool isNumber = arg0[0].Type == JmesPathType.Number;
            bool isString = arg0[0].Type == JmesPathType.String;
            if (!isNumber && !isString)
            {
                result = JsonConstants.Null;
                return false;
            }

            var less = LtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].Type == JmesPathType.Number) == isNumber) && (arg0[i].Type == JmesPathType.String) == isString))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IValue value;
                if (!less.TryEvaluate(arg0[i],arg0[index], out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (value.Type == JmesPathType.True )
                {
                    index = i;
                }
            }

            result = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "min";
        }
    }

    sealed class MergeFunction : BaseFunction
    {
        internal MergeFunction()
            : base(null)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            if (args.Count() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Object)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (args.Count == 1)
            {
                result = arg0;
                return true;
            }

            var dict = new Dictionary<string,IValue>();
            for (int i = 0; i < args.Count; ++i)
            {
                var argi = args[i];
                if (argi.Type != JmesPathType.Object)
                {
                    result = JsonConstants.Null;
                    return false;
                }
                foreach (var item in argi.EnumerateObject())
                {
                    if (!dict.TryAdd(item.Name,item.Value))
                    {
                        dict.Remove(item.Name);
                        dict.Add(item.Name,item.Value);
                    }
                }
            }

            result = new ObjectValue(dict);
            return true;
        }

        public override string ToString()
        {
            return "merge";
        }
    }

    sealed class NotNullFunction : BaseFunction
    {
        internal NotNullFunction()
            : base(null)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            foreach (var arg in args)
            {
                if (arg.Type != JmesPathType.Null)
                {
                    result = arg;
                    return true;
                }
            }
            result = JsonConstants.Null;
            return true;
        }

        public override string ToString()
        {
            return "not_null";
        }
    }

    sealed class ReverseFunction : BaseFunction
    {
        internal ReverseFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            switch (arg0.Type)
            {
                case JmesPathType.String:
                {
                    result = new StringValue(string.Join("", GraphemeClusters(arg0.GetString()).Reverse().ToArray()));
                    return true;
                }
                case JmesPathType.Array:
                {
                    var list = new List<IValue>();
                    for (int i = arg0.GetArrayLength()-1; i >= 0; --i)
                    {
                        list.Add(arg0[i]);
                    }
                    result = new ArrayValue(list);
                    return true;
                }
                default:
                    result = JsonConstants.Null;
                    return false;
            }
        }

        private static IEnumerable<string> GraphemeClusters(string s) 
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while(enumerator.MoveNext()) 
            {
                yield return (string)enumerator.Current;
            }
        }

        public override string ToString()
        {
            return "reverse";
        }
    }

    sealed class MapFunction : BaseFunction
    {
        internal MapFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            if (!(args[0].Type == JmesPathType.Expression && args[1].Type == JmesPathType.Array))
            {
                result = JsonConstants.Null;
                return false;
            }
            var expr = args[0].GetExpression();
            var arg0 = args[1];

            var list = new List<IValue>();

            foreach (var item in arg0.EnumerateArray())
            {
                IValue val;
                if (!expr.TryEvaluate(resources, item, out val))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                list.Add(val);
            }

            result = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "map";
        }
    }

    sealed class MinByFunction : BaseFunction
    {
        internal MinByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return true;
            }

            var expr = args[1].GetExpression();

            IValue key1;
            if (!expr.TryEvaluate(resources, arg0[0], out key1))
            {
                result = JsonConstants.Null;
                return false;
            }

            bool isNumber1 = key1.Type == JmesPathType.Number;
            bool isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                result = JsonConstants.Null;
                return false;
            }

            var lessor = LtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                IValue key2;
                if (!expr.TryEvaluate(resources, arg0[i], out key2))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                bool isNumber2 = key2.Type == JmesPathType.Number;
                bool isString2 = key2.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IValue value;
                if (!lessor.TryEvaluate(key2, key1, out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (value.Type == JmesPathType.True )
                {
                    key1 = key2;
                    index = i;
                }
            }

            result = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "min_by";
        }
    }

    sealed class SortFunction : BaseFunction
    {
        internal SortFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() <= 1)
            {
                result = arg0;
                return true;
            }

            bool isNumber1 = arg0[0].Type == JmesPathType.Number;
            bool isString1 = arg0[0].Type == JmesPathType.String;
            if (!isNumber1 && !isString1)
            {
                result = JsonConstants.Null;
                return false;
            }

            var comparer = ValueComparer.Instance;

            var list = new List<IValue>();
            foreach (var item in arg0.EnumerateArray())
            {
                bool isNumber2 = item.Type == JmesPathType.Number;
                bool isString2 = item.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                list.Add(item);
            }

            list.Sort(comparer);
            result = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "sort";
        }
    }

    sealed class SortByFunction : BaseFunction
    {
        internal SortByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() <= 1)
            {
                result = arg0;
                return true;
            }
            var expr = args[1].GetExpression();

            var list = new List<IValue>();
            foreach (var item in arg0.EnumerateArray())
            {
                list.Add(item);
            }
            var comparer = new SortByComparer(resources, expr);
            list.Sort(comparer);
            if (comparer.IsValid)
            {
                result = new ArrayValue(list);
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
            return "sort_by";
        }
    }

    sealed class StartsWithFunction : BaseFunction
    {
        internal StartsWithFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.Type != JmesPathType.String
                || arg1.Type != JmesPathType.String)
            {
                result = JsonConstants.Null;
                return false;
            }

            var s0 = arg0.GetString();
            var s1 = arg1.GetString();
            if (s0.StartsWith(s1))
            {
                result = JsonConstants.True;
            }
            else
            {
                result = JsonConstants.False;
            }
            return true;
        }

        public override string ToString()
        {
            return "starts_with";
        }
    }

    sealed class SumFunction : BaseFunction
    {
        internal static SumFunction Instance { get; } = new SumFunction();

        internal SumFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            foreach (var item in arg0.EnumerateArray())
            {
                if (item.Type != JmesPathType.Number)
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }

            bool success = true;
            decimal decSum = 0;
            foreach (var item in arg0.EnumerateArray())
            {
                decimal dec;
                if (!item.TryGetDecimal(out dec))
                {
                    success = false;
                    break;
                }
                decSum += dec;
            }
            if (success)
            {
                result = new DecimalValue(decSum); 
                return true;
            }
            else
            {
                double dblSum = 0;
                foreach (var item in arg0.EnumerateArray())
                {
                    double dbl;
                    if (!item.TryGetDouble(out dbl))
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    dblSum += dbl;
                }
                result = new DoubleValue(dblSum); 
                return true;
            }
        }

        public override string ToString()
        {
            return "sum";
        }
    }

    sealed class ToArrayFunction : BaseFunction
    {
        internal ToArrayFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type == JmesPathType.Array)
            {
                result = arg0;
                return true;
            }
            else
            {
                var list = new List<IValue>();
                list.Add(arg0);
                result = new ArrayValue(list);
                return true;
            }
        }

        public override string ToString()
        {
            return "to_array";
        }
    }

    sealed class ToNumberFunction : BaseFunction
    {
        internal ToNumberFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            switch (arg0.Type)
            {
                case JmesPathType.Number:
                    result = arg0;
                    return true;
                case JmesPathType.String:
                {
                    var s = arg0.GetString();
                    Decimal dec;
                    double dbl;
                    if (Decimal.TryParse(s, out dec))
                    {
                        result = new DecimalValue(dec);
                        return true;
                    }
                    else if (Double.TryParse(s, out dbl))
                    {
                        result = new DoubleValue(dbl);
                        return true;
                    }
                    else
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                }
                default:
                    result = JsonConstants.Null;
                    return false;
            }
        }
        public override string ToString()
        {
            return "to_number";
        }
    }

    sealed class ToStringFunction : BaseFunction
    {
        internal ToStringFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            if (args[0].Type == JmesPathType.Expression)
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            switch (arg0.Type)
            {
                case JmesPathType.String:
                    result = arg0;
                    return true;
                case JmesPathType.Expression:
                    result = JsonConstants.Null;
                    return false;
                default:
                    result = new StringValue(arg0.ToString());
                    return true;
            }
        }

        public override string ToString()
        {
            return "to_string";
        }
    }

    sealed class ValuesFunction : BaseFunction
    {
        internal ValuesFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Object)
            {
                result = JsonConstants.Null;
                return false;
            }

            var list = new List<IValue>();

            foreach (var item in arg0.EnumerateObject())
            {
                list.Add(item.Value);
            }
            result = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "values";
        }
    }

    sealed class TypeFunction : BaseFunction
    {
        internal TypeFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(this.Arity.HasValue && args.Count == this.Arity!.Value)                   ;

            var arg0 = args[0];

            switch (arg0.Type)
            {
                case JmesPathType.Number:
                    result = new StringValue("number");
                    return true;
                case JmesPathType.True:
                case JmesPathType.False:
                    result = new StringValue("boolean");
                    return true;
                case JmesPathType.String:
                    result = new StringValue("string");
                    return true;
                case JmesPathType.Object:
                    result = new StringValue("object");
                    return true;
                case JmesPathType.Array:
                    result = new StringValue("array");
                    return true;
                case JmesPathType.Null:
                    result = new StringValue("null");
                    return true;
                default:
                    result = JsonConstants.Null;
                    return false;
            }
        }

        public override string ToString()
        {
            return "type";
        }
    }

    sealed class BuiltInFunctions 
    {
        internal static BuiltInFunctions Instance {get;} = new BuiltInFunctions();

        readonly Dictionary<string,IFunction> _functions = new Dictionary<string, IFunction>(); 

        internal BuiltInFunctions()
        {
            _functions.Add("abs", new AbsFunction());
            _functions.Add("avg", new AvgFunction());
            _functions.Add("ceil", new CeilFunction());
            _functions.Add("contains", new ContainsFunction());
            _functions.Add("ends_with", new EndsWithFunction());
            _functions.Add("floor", new FloorFunction());
            _functions.Add("join", new JoinFunction());
            _functions.Add("keys", new KeysFunction());
            _functions.Add("length", new LengthFunction());
            _functions.Add("map", new MapFunction());
            _functions.Add("max", new MaxFunction());
            _functions.Add("max_by", new MaxByFunction());
            _functions.Add("merge", new MergeFunction());
            _functions.Add("min", new MinFunction());
            _functions.Add("min_by", new MinByFunction());
            _functions.Add("not_null", new NotNullFunction());
            _functions.Add("reverse", new ReverseFunction());
            _functions.Add("sort", new SortFunction());
            _functions.Add("sort_by", new SortByFunction());
            _functions.Add("starts_with", new StartsWithFunction());
            _functions.Add("sum", new SumFunction());
            _functions.Add("to_array", new ToArrayFunction());
            _functions.Add("to_number", new ToNumberFunction());
            _functions.Add("to_string", new ToStringFunction());
            _functions.Add("type", new TypeFunction());
            _functions.Add("values", new ValuesFunction());
        }

        internal bool TryGetFunction(string name, out IFunction func)
        {
            return _functions.TryGetValue(name, out func);
        }
    };

} // namespace JsonCons.JmesPath
