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

         public int PrecedenceLevel {get;} 

         public bool IsRightAssociative {get;} 
    }


    static class Truthiness 
    {
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

        internal virtual void AddExpression(BaseExpression expressions)
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
        }

        internal override void AddExpression(BaseExpression expr)
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
            value = new ArrayJsonValue(result);
            foreach (var item in current.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Null)
                {
                    IValue j;
                    if (!this.TryApplyExpressions(resources, item.Value, out j))
                    {
                        return false;
                    }
                    if (j.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(j);
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
/*
    class list_projection sealed : BaseProjection
    {
    public:
        list_projection()
            : BaseProjection(11, true)
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (!current.is_array())
            {
                return resources.null_value();
            }

            var result = new List<Ivalue>();
            for (reference item in current.EnumerateArray())
            {
                if (!item.is_null())
                {
                    reference j = this.TryApplyExpressions(item, resources, ec);
                    if (j.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(json_const_pointer_arg, std::addressof(j));
                    }
                }
            }
            return *result;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("list_projection\n");
            for (var expr : this._expressions)
            {
                string sss = expr.ToString(indent+2);
                s.insert(s.end(), sss.begin(), sss.end());
                s.push_back('\n');
            }
            return s;
        }
    };

    class slice_projection sealed : BaseProjection
    {
        slice slice_;
    public:
        slice_projection(const slice& s)
            : BaseProjection(11, true), slice_(s)
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (!current.is_array())
            {
                return resources.null_value();
            }

            var start = slice_.get_start(current.size());
            var end = slice_.get_stop(current.size());
            var step = slice_.step();

            if (step == 0)
            {
                ec = jmespath_errc::step_cannot_be_zero;
                return resources.null_value();
            }

            var result = new List<Ivalue>();
            if (step > 0)
            {
                if (start < 0)
                {
                    start = 0;
                }
                if (end > static_cast<Int32>(current.size()))
                {
                    end = current.size();
                }
                for (Int32 i = start; i < end; i += step)
                {
                    reference j = this.TryApplyExpressions(current.at(static_cast<int>(i)), resources, ec);
                    if (j.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(json_const_pointer_arg, std::addressof(j));
                    }
                }
            }
            else
            {
                if (start >= static_cast<Int32>(current.size()))
                {
                    start = static_cast<Int32>(current.size()) - 1;
                }
                if (end < -1)
                {
                    end = -1;
                }
                for (Int32 i = start; i > end; i += step)
                {
                    reference j = this.TryApplyExpressions(current.at(static_cast<int>(i)), resources, ec);
                    if (j.ValueKind != JsonValueKind.Null)
                    {
                        result.Add(json_const_pointer_arg, std::addressof(j));
                    }
                }
            }

            return *result;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("slice_projection\n");
            for (var expr : this._expressions)
            {
                string sss = expr.ToString(indent+2);
                s.insert(s.end(), sss.begin(), sss.end());
                s.push_back('\n');
            }
            return s;
        }
    };

    class filter_expression sealed : BaseProjection
    {
        std::vector<token> token_list_;
    public:
        filter_expression(std::vector<token>&& token_list)
            : BaseProjection(11, true), token_list_(std::move(token_list))
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (!current.is_array())
            {
                return resources.null_value();
            }
            var result = new List<Ivalue>();

            for (var item in current.EnumerateArray())
            {
                Json j(json_const_pointer_arg, evaluate_tokens(item, token_list_, resources, ec));
                if (is_true(j))
                {
                    reference jj = this.TryApplyExpressions(item, resources, ec);
                    if (!jj.is_null())
                    {
                        result.Add(json_const_pointer_arg, std::addressof(jj));
                    }
                }
            }
            return *result;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("filter_expression\n");
            for (var item : token_list_)
            {
                string sss = item.ToString(indent+2);
                s.insert(s.end(), sss.begin(), sss.end());
                s.push_back('\n');
            }
            return s;
        }
    };

    class flatten_projection sealed : BaseProjection
    {
    public:
        flatten_projection()
            : BaseProjection(11, false)
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (!current.is_array())
            {
                return resources.null_value();
            }

            var result = new List<Ivalue>();
            for (reference current_elem in current.EnumerateArray())
            {
                if (current_elem.is_array())
                {
                    for (reference elem : current_elem.array_range())
                    {
                        if (!elem.is_null())
                        {
                            reference j = this.TryApplyExpressions(elem, resources, ec);
                            if (j.ValueKind != JsonValueKind.Null)
                            {
                                result.Add(json_const_pointer_arg, std::addressof(j));
                            }
                        }
                    }
                }
                else
                {
                    if (!current_elem.is_null())
                    {
                        reference j = this.TryApplyExpressions(current_elem, resources, ec);
                        if (j.ValueKind != JsonValueKind.Null)
                        {
                            result.Add(json_const_pointer_arg, std::addressof(j));
                        }
                    }
                }
            }
            return *result;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("flatten_projection\n");
            for (var expr : this._expressions)
            {
                string sss = expr.ToString(indent+2);
                s.insert(s.end(), sss.begin(), sss.end());
                s.push_back('\n');
            }
            return s;
        }
    };

    class multi_select_list sealed : BaseExpression
    {
        std::vector<std::vector<token>> token_lists_;
    public:
        multi_select_list(std::vector<std::vector<token>>&& token_lists)
            : token_lists_(std::move(token_lists))
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (current.is_null())
            {
                return current;
            }
            var result = new List<Ivalue>();
            result.reserve(token_lists_.size());

            for (var list : token_lists_)
            {
                result.Add(json_const_pointer_arg, evaluate_tokens(current, list, resources, ec));
            }
            return *result;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("multi_select_list\n");
            for (var list : token_lists_)
            {
                for (var item : list)
                {
                    string sss = item.ToString(indent+2);
                    s.insert(s.end(), sss.begin(), sss.end());
                    s.push_back('\n');
                }
                s.append("---\n");
            }
            return s;
        }
    };

    struct key_tokens
    {
        string_type key;
        std::vector<token> tokens;

        key_tokens(string_type&& key, std::vector<token>&& tokens) noexcept
            : key(std::move(key)), tokens(std::move(tokens))
        {
        }
    };

    class multi_select_hash sealed : BaseExpression
    {
    public:
        std::vector<key_tokens> key_toks_;

        multi_select_hash(std::vector<key_tokens>&& key_toks)
            : key_toks_(std::move(key_toks))
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            if (current.is_null())
            {
                return current;
            }
            var resultp = resources.create_json(json_object_arg);
            resultp.reserve(key_toks_.size());
            for (var item : key_toks_)
            {
                resultp.try_emplace(item.key, json_const_pointer_arg, evaluate_tokens(current, item.tokens, resources, ec));
            }

            return *resultp;
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("multi_select_list\n");
            return s;
        }
    };

    class function_expression sealed : BaseExpression
    {
    public:
        std::vector<token> toks_;

        function_expression(std::vector<token>&& toks)
            : toks_(std::move(toks))
        {
        }

        reference Evaluate(reference current, dynamic_resources& resources, std::error_code& ec)
        {
            return *evaluate_tokens(current, toks_, resources, ec);
        }

        string ToString(int indent = 0)
        {
            string s;
            for (int i = 0; i <= indent; ++i)
            {
                s.push_back(' ');
            }
            s.append("function_expression\n");
            for (var tok : toks_)
            {
                for (int i = 0; i <= indent+2; ++i)
                {
                    s.push_back(' ');
                }
                string sss = tok.ToString(indent+2);
                s.insert(s.end(), sss.begin(), sss.end());
                s.push_back('\n');
            }
            return s;
        }
    }

*/
} // namespace JsonCons.JmesPath

