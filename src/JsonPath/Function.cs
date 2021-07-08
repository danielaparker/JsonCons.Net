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
    interface IFunction 
    {
        int? Arity {get;}
        bool TryEvaluate(IList<IOperand> parameters, out IOperand element);
    };

    abstract class BaseFunction : IFunction
    {
        internal BaseFunction(int? argCount)
        {
            Arity = argCount;
        }

        public int? Arity {get;}

        public abstract bool TryEvaluate(IList<IOperand> parameters, out IOperand element);
    };  

    sealed class AbsFunction : BaseFunction
    {
        internal AbsFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, out IOperand result) 
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg = args[0];

            Decimal decVal;
            double dblVal;

            if (arg.TryGetDecimal(out decVal))
            {
                result = new DecimalOperand(decVal >= 0 ? decVal : -decVal);
                return true;
            }
            else if (arg.TryGetDouble(out dblVal))
            {
                result = new DecimalOperand(dblVal >= 0 ? decVal : new Decimal(-dblVal));
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

    sealed class ContainsFunction : BaseFunction
    {
        internal ContainsFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            var arg1 = args[1];

            var comparer = JsonValueEqualityComparer.Instance;

            switch (arg0.ValueKind)
            {
                case JsonValueKind.Array:
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
                case JsonValueKind.String:
                {
                    if (arg1.ValueKind != JsonValueKind.String)
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

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.ValueKind != JsonValueKind.String
                || arg1.ValueKind != JsonValueKind.String)
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

    sealed class StartsWithFunction : BaseFunction
    {
        internal StartsWithFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.ValueKind != JsonValueKind.String
                || arg1.ValueKind != JsonValueKind.String)
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
    };

    sealed class SumFunction : BaseFunction
    {
        internal static SumFunction Instance { get; } = new SumFunction();

        internal SumFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            foreach (var item in arg0.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number)
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
                result = new DecimalOperand(decSum); 
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
                result = new DoubleOperand(dblSum); 
                return true;
            }
        }

        public override string ToString()
        {
            return "sum";
        }
    };

    sealed class ProdFunction : BaseFunction
    {
        internal ProdFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Array || arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            foreach (var item in arg0.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number)
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }

            double prod = 1;
            foreach (var item in arg0.EnumerateArray())
            {
                double dbl;
                if (!item.TryGetDouble(out dbl))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                prod *= dbl;
            }
            result = new DoubleOperand(prod);

            return true;
        }

        public override string ToString()
        {
            return "prod";
        }
    };

    sealed class AvgFunction : BaseFunction
    {
        internal AvgFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }
            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Array || arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }

            IOperand sum;
            if (!SumFunction.Instance.TryEvaluate(args, out sum))
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (sum.TryGetDecimal(out decVal))
            {
                result = new DecimalOperand(decVal/arg0.GetArrayLength());
                return true;
            }
            else if (sum.TryGetDouble(out dblVal))
            {
                result = new DoubleOperand(dblVal/arg0.GetArrayLength());
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
            return "to_string";
        }
    };

    sealed class TokenizeFunction : BaseFunction
    {
        internal TokenizeFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            if (args[0].ValueKind != JsonValueKind.String || args[1].ValueKind != JsonValueKind.String)
            {
                result = JsonConstants.Null;
                return false;
            }
            var sourceStr = args[0].GetString();
            var patternStr = args[1].GetString();

            string[] pieces = Regex.Split(sourceStr, patternStr);

            var values = new List<IOperand>();
            foreach (var s in pieces)
            {
                values.Add(new StringOperand(s));
            }

            result = new ArrayJsonValue(values);
            return true;
        }

        public override string ToString()
        {
            return "tokenize";
        }
    };

    sealed class CeilFunction : BaseFunction
    {
        internal CeilFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var val = args[0];
            if (val.ValueKind != JsonValueKind.Number)
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                result = new DecimalOperand(decimal.Ceiling(decVal));
                return true;
            }
            else if (val.TryGetDouble(out dblVal))
            {
                result = new DoubleOperand(Math.Ceiling(dblVal));
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
    
    sealed class FloorFunction : BaseFunction
    {
        internal FloorFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var val = args[0];
            if (val.ValueKind != JsonValueKind.Number)
            {
                result = JsonConstants.Null;
                return false;
            }

            Decimal decVal;
            double dblVal;

            if (val.TryGetDecimal(out decVal))
            {
                result = new DecimalOperand(decimal.Floor(decVal));
                return true;
            }
            else if (val.TryGetDouble(out dblVal))
            {
                result = new DoubleOperand(Math.Floor(dblVal));
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

    sealed class ToNumberFunction : BaseFunction
    {
        internal ToNumberFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            switch (arg0.ValueKind)
            {
                case JsonValueKind.Number:
                    result = arg0;
                    return true;
                case JsonValueKind.String:
                {
                    var s = arg0.GetString();
                    Decimal dec;
                    double dbl;
                    if (Decimal.TryParse(s, out dec))
                    {
                        result = new DecimalOperand(dec);
                        return true;
                    }
                    else if (Double.TryParse(s, out dbl))
                    {
                        result = new DoubleOperand(dbl);
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
    };

    sealed class MinFunction : BaseFunction
    {
        internal MinFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            bool isNumber = arg0[0].ValueKind == JsonValueKind.Number;
            bool isString = arg0[0].ValueKind == JsonValueKind.String;
            if (!isNumber && !isString)
            {
                result = JsonConstants.Null;
                return false;
            }

            var less = LtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].ValueKind == JsonValueKind.Number) == isNumber) && (arg0[i].ValueKind == JsonValueKind.String) == isString))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IOperand value;
                if (!less.TryEvaluate(arg0[i],arg0[index], out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (value.ValueKind == JsonValueKind.True )
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
    };

    sealed class MaxFunction : BaseFunction
    {
        internal MaxFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Array)
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            bool isNumber = arg0[0].ValueKind == JsonValueKind.Number;
            bool isString = arg0[0].ValueKind == JsonValueKind.String;
            if (!isNumber && !isString)
            {
                result = JsonConstants.Null;
                return false;
            }

            var greater = GtOperator.Instance;
            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].ValueKind == JsonValueKind.Number) == isNumber) && (arg0[i].ValueKind == JsonValueKind.String) == isString))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IOperand value;
                if (!greater.TryEvaluate(arg0[i],arg0[index], out value))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (value.ValueKind == JsonValueKind.True )
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
    };

    sealed class LengthFunction : BaseFunction
    {
        internal LengthFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];

            switch (arg0.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    int count = 0;
                    foreach (var item in arg0.EnumerateObject())
                    {
                        ++count;
                    }
                    result = new DecimalOperand(new Decimal(count));
                    return true;
                }
                case JsonValueKind.Array:
                    result = new DecimalOperand(new Decimal(arg0.GetArrayLength()));
                    return true;
                case JsonValueKind.String:
                {
                    byte[] bytes = Encoding.UTF32.GetBytes(arg0.GetString().ToCharArray());
                    result = new DecimalOperand(new Decimal(bytes.Length/4));
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

    sealed class KeysFunction : BaseFunction
    {
        internal KeysFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IOperand> args, 
                                         out IOperand result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg0 = args[0];
            if (arg0.ValueKind != JsonValueKind.Object)
            {
                result = JsonConstants.Null;
                return false;
            }

            var values = new List<IOperand>();

            foreach (var property in arg0.EnumerateObject())
            {
                values.Add(new StringOperand(property.Name));
            }
            result = new ArrayJsonValue(values);
            return true;
        }

        public override string ToString()
        {
            return "keys";
        }
    };

    sealed class BuiltInFunctions 
    {
        internal static BuiltInFunctions Instance {get;} = new BuiltInFunctions();

        readonly Dictionary<string,IFunction> _functions = new Dictionary<string, IFunction>(); 

        internal BuiltInFunctions()
        {
            _functions.Add("abs", new AbsFunction());
            _functions.Add("contains", new ContainsFunction());
            _functions.Add("ends_with", new EndsWithFunction());
            _functions.Add("starts_with", new StartsWithFunction());
            _functions.Add("sum", new SumFunction());
            _functions.Add("avg", new AvgFunction());
            _functions.Add("prod", new ProdFunction());
            _functions.Add("tokenize", new TokenizeFunction());
            _functions.Add("ceil", new CeilFunction());
            _functions.Add("floor", new FloorFunction());
            _functions.Add("to_number", new ToNumberFunction());
            _functions.Add("min", new MinFunction());
            _functions.Add("max", new MaxFunction());
            _functions.Add("length", new LengthFunction());
            _functions.Add("keys", new KeysFunction());
        }

        internal bool TryGetFunction(string name, out IFunction func)
        {
            return _functions.TryGetValue(name, out func);
        }
    };

} // namespace JsonCons.JsonPathLib
