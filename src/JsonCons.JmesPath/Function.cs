using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
        
namespace JsonCons.JmesPath
{
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }
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
            return "to_string";
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            Debug.Assert(args.Count == this.Arity.Value);

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
    };

    sealed class KeysFunction : BaseFunction
    {
        internal KeysFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
    };

    sealed class LengthFunction : BaseFunction
    {
        internal LengthFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, 
                                         out IValue result)
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            Debug.Assert(args.Count == this.Arity.Value);

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

            bool is_number1 = key1.Type == JmesPathType.Number;
            bool is_string1 = key1.Type == JmesPathType.String;
            if (!(is_number1 || is_string1))
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
                bool is_number2 = key2.Type == JmesPathType.Number;
                bool is_string2 = key2.Type == JmesPathType.String;
                if (!(is_number2 == is_number1 && is_string2 == is_string1))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                IValue value;
                if (!greater.TryEvaluate(arg0[i], arg0[index], out value))
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
    }
/*
    sealed class MapFunction : BaseFunction
    {
        internal MapFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(args.Count == this.Arity.Value);

            if (!(args[0].is_expression() && args[1].is_value()))
            {
                result = JsonConstants.Null;
                return false;
            }
            var expr = args[0].GetExpression();

            var arg0 = args[1];
            if (!arg0.is_array())
            {
                result = JsonConstants.Null;
                return false;
            }

            auto result = resources.create_json(json_array_arg);

            for (auto& item : arg0.array_range())
            {
                auto& j = expr.evaluate(item, resources, ec);
                if (ec)
                {
                    result = JsonConstants.Null;
                    return false;
                }
                result->emplace_back(json_const_pointer_arg, std::addressof(j));
            }

            return *result;
            return true;
        }

        std::string to_string(int = 0) const override
        {
            return std::string("map_function\n");
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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

    sealed class MinByFunction : BaseFunction
    {
        internal MinByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(args.Count == this.Arity.Value);

            if (!(args[0].is_value() && args[1].is_expression()))
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (!arg0.is_array())
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() == 0)
            {
                result = JsonConstants.Null;
            }

            var expr = args[1].GetExpression();

            std::error_code ec2;
            IValue key1 = expr.evaluate(arg0[0], resources, ec2); 

            bool is_number1 = key1.Type == JmesPathType.Number;
            bool is_string1 = key1.Type == JmesPathType.String;
            if (!(is_number1 || is_string1))
            {
                result = JsonConstants.Null;
                return false;
            }

            int index = 0;
            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                var key2 = expr.evaluate(arg0[i], resources, ec2); 
                if (!(key2.is_number1() == is_number1 && key2.is_string1() == is_string1))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                if (key2 < key1)
                {
                    key1 = key2;
                    index = i;
                }
            }

            return arg0.at(index);
        }
    }

    sealed class MergeFunction : BaseFunction
    {
        internal MergeFunction()
            : base(jsoncons::optional<int>())
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            if (args.Count() == 0)
            {
                result = JsonConstants.Null;
                return false;
            }

            for (auto& param : args)
            {
                if (!param.is_value())
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }

            var arg0 = args[0];
            if (!arg0.is_object())
            {
                result = JsonConstants.Null;
                return false;
            }
            if (args.Count == 1)
            {
                return arg0;
            }

            auto result = resources.create_json(arg0);
            for (int i = 1; i < args.Count; ++i)
            {
                var argi = args[i];
                if (!argi.is_object())
                {
                    result = JsonConstants.Null;
                    return false;
                }
                for (auto& item : argi.object_range())
                {
                    result->insert_or_assign(item.key(),item);
                }
            }

            return *result;
            return true;
        }
    }

    sealed class NotNullFunction final : BaseFunction
    {
        internal NotNullFunction()
            : base(null)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            for (auto& param : args)
            {
                if (param.is_value() && !param.is_null())
                {
                    return param;
                }
            }
            result = JsonConstants.Null;
        }

        std::string to_string(int = 0) const override
        {
            return std::string("to_string_function\n");
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
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (!arg0.is_array())
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() <= 1)
            {
                return arg0;
            }

            bool is_number1 = arg0[0].Type == JmesPathType.Number;
            bool is_string1 = arg0[0].Type == JmesPathType.String;
            if (!is_number1 && !is_string1)
            {
                result = JsonConstants.Null;
                return false;
            }

            for (int i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (arg0[i].is_number1() != is_number1 || arg0[i].is_string1() != is_string1)
                {
                    result = JsonConstants.Null;
                    return false;
                }
            }

            auto v = resources.create_json(arg0);
            std::stable_sort((v->array_range()).begin(), (v->array_range()).end());
            return *v;
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
            Debug.Assert(args.Count == this.Arity.Value);

            if (!(args[0].is_value() && args[1].is_expression()))
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (!arg0.is_array())
            {
                result = JsonConstants.Null;
                return false;
            }
            if (arg0.GetArrayLength() <= 1)
            {
                return arg0;
            }

            var expr = args[1].GetExpression();

            auto v = resources.create_json(arg0);
            std::stable_sort((v->array_range()).begin(), (v->array_range()).end(),
                [&expr,&resources,&ec](var lhs, var rhs) -> bool
            {
                std::error_code ec2;
                var key1 = expr.evaluate(lhs, resources, ec2);
                bool is_number1 = key1.Type == JmesPathType.Number;
                bool is_string1 = key1.Type == JmesPathType.String;
                if (!(is_number1 || is_string1))
                {
                    result = JsonConstants.Null;
                    return false;
                }

                var key2 = expr.evaluate(rhs, resources, ec2);
                if (!(key2.is_number1() == is_number1 && key2.is_string1() == is_string1))
                {
                    result = JsonConstants.Null;
                    return false;
                }

                return key1 < key2;
            });
            return ec ? resources.null_value() : *v;
        }

        std::string to_string(int = 0) const override
        {
            return std::string("sort_by_function\n");
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
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];

            switch (arg0.type())
            {
                case json_type::int64_value:
                case json_type::uint64_value:
                case json_type::double_value:
                    return resources.number_type_name();
                case json_type::bool_value:
                    return resources.boolean_type_name();
                case json_type::string_value:
                    return resources.string_type_name();
                case json_type::object_value:
                    return resources.object_type_name();
                case json_type::array_value:
                    return resources.array_type_name();
                default:
                    return resources.null_type_name();
                    break;

            }
        }
    }

    sealed class ReverseFunction final : BaseFunction
    {
        internal ReverseFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            switch (arg0.type())
            {
                case json_type::string_value:
                {
                    string_view_type sv = arg0.as_string_view();
                    std::basic_string<char32_t> buf;
                    unicode_traits::convert(sv.data(), sv.size(), buf);
                    std::reverse(buf.begin(), buf.end());
                    string_type s;
                    unicode_traits::convert(buf.data(), buf.size(), s);
                    return *resources.create_json(s);
                }
                case json_type::array_value:
                {
                    auto result = resources.create_json(arg0);
                    std::reverse(result->array_range().begin(),result->array_range().end());
                    return *result;
                    return true;
                }
                default:
                    result = JsonConstants.Null;
                    return false;
            }
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
*/
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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

/*
    sealed class ToArrayFunction : BaseFunction
    {
        internal ToArrayFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.is_array())
            {
                return arg0;
            }
            else
            {
                auto result = resources.create_json(json_array_arg);
                result->push_back(arg0);
                return *result;
                return true;
            }
        }

        std::string to_string(int = 0) const override
        {
            return std::string("to_array_function\n");
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
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

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
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            return *resources.create_json(arg0.template as<string_type>());
        }

        std::string to_string(int = 0) const override
        {
            return std::string("to_string_function\n");
        }
    }

    sealed class ValuesFunction final : BaseFunction
    {
        internal ValuesFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue result)
        {
            Debug.Assert(args.Count == this.Arity.Value);

            if (!args[0].is_value())
            {
                result = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (!arg0.is_object())
            {
                result = JsonConstants.Null;
                return false;
            }

            auto result = resources.create_json(json_array_arg);
            result->reserve(args.Count);

            for (auto& item : arg0.object_range())
            {
                result->emplace_back(item);
            }
            return *result;
            return true;
        }
    }
*/
    sealed class BuiltInFunctions 
    {
        internal static BuiltInFunctions Instance {get;} = new BuiltInFunctions();

        readonly Dictionary<string,IFunction> _functions = new Dictionary<string, IFunction>(); 

        internal BuiltInFunctions()
        {
/*            _functions.Add("abs", new AbsFunction());
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
*/
        }

        internal bool TryGetFunction(string name, out IFunction func)
        {
            return _functions.TryGetValue(name, out func);
        }
    };

} // namespace JsonCons.JmesPath
