// Copyright 2021 Daniel Parker
// Distributed under the Boost license, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)

// See https://github.com/danielaparker/jsoncons for latest version

#ifndef JSONCONS_JSONPATH_JSON_QUERY_HPP
#define JSONCONS_JSONPATH_JSON_QUERY_HPP

#include <string>
#include <vector>
#include <memory>
#include <type_traits> // std::is_const
#include <limits> // std::numeric_limits
#include <utility> // std::move
#include <regex>
#include <jsoncons/json.hpp>
#include <jsoncons_ext/jsonpath/jsonpath_error.hpp>
#include <jsoncons_ext/jsonpath/path_expression.hpp>

namespace jsoncons { namespace jsonpath {

    // token

    struct Slice
    {
        jsoncons::optional<Int64> start_;
        jsoncons::optional<Int64> stop_;
        Int64 step_;

        Slice()
            : start_(), stop_(), step_(1)
        {
        }

        Slice(const jsoncons::optional<Int64>& start, const jsoncons::optional<Int64>& end, Int64 step) 
            : start_(start), stop_(end), step_(step)
        {
        }

        Slice(const Slice& other)
            : start_(other.start_), stop_(other.stop_), step_(other.step_)
        {
        }

        Slice& operator=(const Slice& rhs) 
        {
            if (this != &rhs)
            {
                if (rhs.start_)
                {
                    start_ = rhs.start_;
                }
                else
                {
                    start_.reset();
                }
                if (rhs.stop_)
                {
                    stop_ = rhs.stop_;
                }
                else
                {
                    stop_.reset();
                }
                step_ = rhs.step_;
            }
            return *this;
        }

        Int64 GetStart(std::size_t size) const
        {
            if (start_)
            {
                var len = *start_ >= 0 ? *start_ : (static_cast<Int64>(size) + *start_);
                return len <= static_cast<Int64>(size) ? len : static_cast<Int64>(size);
            }
            else
            {
                if (step_ >= 0)
                {
                    return 0;
                }
                else 
                {
                    return static_cast<Int64>(size);
                }
            }
        }

        Int64 GetStop(std::size_t size) const
        {
            if (stop_)
            {
                var len = *stop_ >= 0 ? *stop_ : (static_cast<Int64>(size) + *stop_);
                return len <= static_cast<Int64>(size) ? len : static_cast<Int64>(size);
            }
            else
            {
                return step_ >= 0 ? static_cast<Int64>(size) : -1;
            }
        }

        Int64 Step const
        {
            return step_; // Allow negative
        }
    };

    namespace detail {
     
    enum class ExprState 
    {
        Start,
        ExpectFunctionExpr,
        PathExpression,
        PathRhs,
        FilterExpression,
        ExpressionRhs,
        RecursiveDescentOrPathExpression,
        PathOrValueOrFunction,
        JsonTextOrFunction,
        JsonTextOrFunctionName,
        JsonTextString,
        JsonValue,
        JsonString,
        IdentifierOrFunctionExpr,
        NameOrLeftBracket,
        UnquotedString,
        Number,
        FunctionExpression,
        Argument,
        ZeroOrOneArguments,
        OneOrMoreArguments,
        Identifier,
        SingleQuotedString,
        DoubleQuotedString,
        BracketedUnquotedNameOrUnion,
        UnionExpression,
        IdentifierOrUnion,
        BracketSpecifierOrUnion,
        BracketedWildcard,
        IndexOrSlice,
        WildcardOrUnion,
        UnionElement,
        IndexOrSliceOrUnion,
        Index,
        Integer,
        Digit,
        SliceExpressionStop,
        SliceExpressionStep,
        CommaOrRightBracket,
        ExpectRightBracket,
        QuotedStringEscapeChar,
        EscapeU1, 
        EscapeU2, 
        EscapeU3, 
        EscapeU4, 
        EscapeExpectSurrogatePair1, 
        EscapeExpectSurrogatePair2, 
        EscapeU5, 
        EscapeU6, 
        EscapeU7, 
        EscapeU8,
        Expression,
        ComparatorExpression,
        EqOrRegex,
        ExpectRegex,
        Regex,
        CmpLtOrLte,
        CmpGtOrGte,
        CmpNe,
        ExpectOr,
        ExpectAnd
    };

    JSONCONS_STRING_LITERAL(length_literal, 'l', 'e', 'n', 'g', 't', 'h')

    template<class Json,
             class JsonReference>
    class jsonpath_evaluator : public ser_context
    {
    public:
        using char_type = typename Json::char_type;
        using string_type = std::basic_string<char_type,std::char_traits<char_type>>;
        using string_view_type = typename Json::string_view_type;
        using path_node_type = path_node<Json,JsonReference>;
        using value_type = Json;
        using JsonElement = JsonReference;
        using pointer = typename path_node_type::pointer;
        using selector_base_type = selector_base<Json,JsonReference>;
        using token_type = token<Json,JsonReference>;
        using pathExpression_type = pathExpression<Json,JsonReference>;
        using expression_type = expression_tree<Json,JsonReference>;
        using path_component_type = path_component<char_type>;

    private:

        // path_selector
        class path_selector : public selector_base_type
        {
            std::unique_ptr<selector_base_type> tail_selector_;
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using selector_base_type::generate_path;

            path_selector()
                : selector_base_type(true, 11), tail_selector_()
            {
            }

            path_selector(bool is_path, std::size_t precedence_level)
                : selector_base_type(is_path, precedence_level), tail_selector_()
            {
            }

            void append_selector(std::unique_ptr<selector_base_type>&& expr) override
            {
                if (!tail_selector_)
                {
                    tail_selector_ = std::move(expr);
                }
                else
                {
                    tail_selector_.append_selector(std::move(expr));
                }
            }

            void EvaluateTail(dynamic_resources<Json,JsonReference>& resources,
                               const std::vector<path_component_type>& path, 
                               JsonElement root,
                               JsonElement val,
                               IList<JsonElement> nodes,
                               node_kind& ndtype,
                               result_options options) const
            {
                if (!tail_selector_)
                {
                    nodes.Add(path, std::addressof(val));
                }
                else
                {
                    tail_selector_.Select(resources, path, root, val, nodes, ndtype, options);
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                if (tail_selector_)
                {
                    s.append(tail_selector_.to_string(level));
                }
                return s;
            }
        };

        class identifier_selector final : public path_selector
        {
            string_type identifier_;
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            identifier_selector(const string_view_type& identifier)
                : path_selector(), identifier_(identifier)
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement val,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::string buf;
                //buf.append("identifier selector: ");
                //unicode_traits::convert(identifier_.data(),identifier_.Count,buf);

                ndtype = node_kind::single;
                if (val.ValueKind == JsonValueKind.Object)
                {
                    var it = val.find(identifier_);
                    if (it != val.object_range().end())
                    {
                        this.EvaluateTail(resources, generate_path(path, identifier_, options), 
                                                root, it.value(), nodes, ndtype, options);
                    }
                }
                else if (val.ValueKind == JsonValueKind.Array)
                {
                    Int64 n{0};
                    var r = jsoncons::detail::to_integer_decimal(identifier_.data(), identifier_.Count, n);
                    if (r)
                    {
                        std::size_t index = (n >= 0) ? static_cast<std::size_t>(n) : static_cast<std::size_t>(static_cast<Int64>(val.Count) + n);
                        if (index < val.Count)
                        {
                            this.EvaluateTail(resources, generate_path(path, index, options), 
                                                root, val[index], nodes, ndtype, options);
                        }
                    }
                    else if (identifier_ == length_literal<char_type>() && val.Count > 0)
                    {
                        pointer ptr = resources.create_json(val.Count);
                        this.EvaluateTail(resources, generate_path(path, identifier_, options), 
                                                root, *ptr, nodes, ndtype, options);
                    }
                }
                else if (val.ValueKind == JsonValueKind.String && identifier_ == length_literal<char_type>())
                {
                    string_view_type sv = val.as_string_view();
                    std::size_t count = unicode_traits::count_codepoints(sv.data(), sv.Count);
                    pointer ptr = resources.create_json(count);
                    this.EvaluateTail(resources, generate_path(path, identifier_, options), 
                                            root, *ptr, nodes, ndtype, options);
                }
                //std::cout << "end identifier_selector\n";
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("identifier selector ");
                unicode_traits::convert(identifier_.data(),identifier_.Count,s);
                s.append(path_selector::to_string(level+1));
                //s.append("\n");

                return s;
            }
        };

        class root_selector final : public path_selector
        {
            std::size_t id_;
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            root_selector(std::size_t id)
                : path_selector(), id_(id)
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                if (resources.is_cached(id_))
                {
                    resources.Retrieve_from_cache(id_, nodes, ndtype);
                }
                else
                {
                    std::vector<path_node_type> v;
                    this.EvaluateTail(resources, path, 
                                        root, root, v, ndtype, options);
                    resources.add_to_cache(id_, v, ndtype);
                    for (var&& item : v)
                    {
                        nodes.Add(std::move(item));
                    }
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("root_selector ");
                s.append(path_selector::to_string(level+1));

                return s;
            }
        };

        class current_node_selector final : public path_selector
        {
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            current_node_selector()
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::cout << "current_node_selector: " << current << "\n";
                ndtype = node_kind::single;
                this.EvaluateTail(resources, path, 
                                    root, current, nodes, ndtype, options);
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("current_node_selector");
                s.append(path_selector::to_string(level+1));

                return s;
            }
        };

        class index_selector final : public path_selector
        {
            Int32 _index;
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            index_selector(Int32 index)
                : path_selector(), _index(index)
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                ndtype = node_kind::single;
                if (current.ValueKind == JsonValueKind.Array)
                {
                    Int32 slen = static_cast<Int32>(current.GetArrayLength());
                    if (_index >= 0 && _index < slen)
                    {
                        std::size_t index = static_cast<std::size_t>(_index);
                        //std::cout << "path: " << path << ", current: " << current << ", index: " << index << "\n";
                        //nodes.Add(generate_path(path, index, options),std::addressof(current.at(index)));
                        //nodes.Add(path, std::addressof(current));
                        this.EvaluateTail(resources, generate_path(path, index, options), 
                                                root, current.at(index), nodes, ndtype, options);
                    }
                    else if ((slen + _index) >= 0 && (slen+_index) < slen)
                    {
                        Int32 index = static_cast<Int32>(slen + _index);
                        //std::cout << "path: " << path << ", current: " << current << ", index: " << index << "\n";
                        //nodes.Add(generate_path(path, index ,options),std::addressof(current.at(index)));
                        this.EvaluateTail(resources, generate_path(path, index, options), 
                                                root, current.at(index), nodes, ndtype, options);
                    }
                }
            }
        };

        class wildcard_selector final : public path_selector
        {
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            wildcard_selector()
                : path_selector()
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::cout << "wildcard_selector: " << current << "\n";
                ndtype = node_kind::multi; // always multi

                node_kind tmptype;
                if (current.ValueKind == JsonValueKind.Array)
                {
                    for (Int32 i = 0; i < current.GetArrayLength(); ++i)
                    {
                        this.EvaluateTail(resources, generate_path(path, i, options), root, current[i], nodes, tmptype, options);
                    }
                }
                else if (current.ValueKind == JsonValueKind.Object)
                {
                    for (var& item : current.object_range())
                    {
                        this.EvaluateTail(resources, generate_path(path, item.key(), options), root, item.value(), nodes, tmptype, options);
                    }
                }
                //std::cout << "end wildcard_selector\n";
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("wildcard selector");
                s.append(path_selector::to_string(level));

                return s;
            }
        };

        class recursive_selector final : public path_selector
        {
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            recursive_selector()
                : path_selector()
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::cout << "wildcard_selector: " << current << "\n";
                if (current.ValueKind == JsonValueKind.Array)
                {
                    this.EvaluateTail(resources, path, root, current, nodes, ndtype, options);
                    for (Int32 i = 0; i < current.GetArrayLength(); ++i)
                    {
                        Select(resources, generate_path(path, i, options), root, current[i], nodes, ndtype, options);
                    }
                }
                else if (current.ValueKind == JsonValueKind.Object)
                {
                    this.EvaluateTail(resources, path, root, current, nodes, ndtype, options);
                    for (var& item : current.object_range())
                    {
                        Select(resources, generate_path(path, item.key(), options), root, item.value(), nodes, ndtype, options);
                    }
                }
                //std::cout << "end wildcard_selector\n";
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("wildcard selector");
                s.append(path_selector::to_string(level));

                return s;
            }
        };

        class union_selector final : public path_selector
        {
            std::vector<pathExpression_type> expressions_;

        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            union_selector(std::vector<pathExpression_type>&& expressions)
                : path_selector(), expressions_(std::move(expressions))
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current, 
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::cout << "union_selector Select current: " << current << "\n";
                ndtype = node_kind::multi;

                var callback = [&](const std::vector<path_component_type>& p, JsonElement v)
                {
                    //std::cout << "union Select callback: node: " << *node.ptr << "\n";
                    this.EvaluateTail(resources, p, root, v, nodes, ndtype, options);
                };
                for (var& expr : expressions_)
                {
                    expr.evaluate(resources, path, root, current, callback, options);
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("union selector ");
                for (var& expr : expressions_)
                {
                    s.append(expr.to_string(level+1));
                    //s.Add('\n');
                }

                return s;
            }
        };

        class FilterSelector final : public path_selector
        {
            expression_type _expr;

        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            FilterSelector(expression_type&& expr)
                : path_selector(), _expr(std::move(expr))
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current, 
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                if (current.ValueKind == JsonValueKind.Array)
                {
                    for (Int32 i = 0; i < current.GetArrayLength(); ++i)
                    {
                        std::error_code ec;
                        value_type r = _expr.evaluate_single(resources, root, current[i], options);
                        bool t = ec ? false : detail::is_true(r);
                        if (t)
                        {
                            this.EvaluateTail(resources, path, root, current[i], nodes, ndtype, options);
                        }
                    }
                }
                else if (current.ValueKind == JsonValueKind.Object)
                {
                    for (var& member : current.object_range())
                    {
                        std::error_code ec;
                        value_type r = _expr.evaluate_single(resources, root, member.value(), options);
                        bool t = ec ? false : detail::is_true(r);
                        if (t)
                        {
                            this.EvaluateTail(resources, path, root, member.value(), nodes, ndtype, options);
                        }
                    }
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("filter selector ");
                s.append(_expr.to_string(level+1));

                return s;
            }
        };

        class indexExpression_selector final : public path_selector
        {
            expression_type _expr;

        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            indexExpression_selector(expression_type&& expr)
                : path_selector(), _expr(std::move(expr))
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current, 
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                //std::cout << "indexExpression_selector current: " << current << "\n";

                std::error_code ec;
                value_type j = _expr.evaluate_single(resources, root, current, options);

                if (!ec)
                {
                    if (j.template is<Int32>() && current.ValueKind == JsonValueKind.Array)
                    {
                        Int32 start = j.template as<Int32>();
                        this.EvaluateTail(resources, path, root, current.at(start), nodes, ndtype, options);
                    }
                    else if (j.ValueKind == JsonValueKind.String && current.ValueKind == JsonValueKind.Object)
                    {
                        this.EvaluateTail(resources, path, root, current.at(j.as_string_view()), nodes, ndtype, options);
                    }
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("bracket expression selector ");
                s.append(_expr.to_string(level+1));
                s.append(path_selector::to_string(level+1));

                return s;
            }
        };

        class argumentExpression final : public expression_base<Json,JsonReference>
        {
            expression_type _expr;

        public:
            using path_component_type = typename selector_base_type::path_component_type;

            argumentExpression(expression_type&& expr)
                : _expr(std::move(expr))
            {
            }

            value_type evaluate_single(dynamic_resources<Json,JsonReference>& resources,
                                       const std::vector<path_component_type>&, 
                                       JsonElement root,
                                       JsonElement current, 
                                       result_options options,
                                       std::error_code& ec)
            {
                value_type ref = _expr.evaluate_single(resources, root, current, options);
                return ec ? Json::null() : ref; 
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("expression selector ");
                s.append(_expr.to_string(level+1));

                return s;
            }
        };

        class SliceSelector final : public path_selector
        {
            Slice slice_;
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            using path_selector::generate_path;

            SliceSelector(const Slice& slic)
                : path_selector(), _slice(slic) 
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current,
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                ndtype = node_kind::multi;

                if (current.ValueKind == JsonValueKind.Array)
                {
                    var start = _slice.GetStart(current.GetArrayLength());
                    var end = _slice.GetStop(current.GetArrayLength());
                    var step = _slice.Step;

                    if (step > 0)
                    {
                        if (start < 0)
                        {
                            start = 0;
                        }
                        if (end > static_cast<Int64>(current.GetArrayLength()))
                        {
                            end = current.GetArrayLength();
                        }
                        for (Int64 i = start; i < end; i += step)
                        {
                            Int32 j = static_cast<Int32>(i);
                            this.EvaluateTail(resources, generate_path(path, j, options), root, current[j], nodes, ndtype, options);
                        }
                    }
                    else if (step < 0)
                    {
                        if (start >= static_cast<Int64>(current.GetArrayLength()))
                        {
                            start = static_cast<Int64>(current.GetArrayLength()) - 1;
                        }
                        if (end < -1)
                        {
                            end = -1;
                        }
                        for (Int64 i = start; i > end; i += step)
                        {
                            Int32 j = static_cast<Int32>(i);
                            if (j < current.GetArrayLength())
                            {
                                this.EvaluateTail(resources, generate_path(path,j,options), root, current[j], nodes, ndtype, options);
                            }
                        }
                    }
                }
            }
        };

        class function_selector final : public path_selector
        {
        public:
            using path_component_type = typename selector_base_type::path_component_type;
            expression_type _expr;

            function_selector(expression_type&& expr)
                : path_selector(), _expr(std::move(expr))
            {
            }

            void Select(dynamic_resources<Json,JsonReference>& resources,
                        const std::vector<path_component_type>& path, 
                        JsonElement root,
                        JsonElement current, 
                        IList<JsonElement> nodes,
                        node_kind& ndtype,
                        result_options options)
            {
                ndtype = node_kind::single;
                std::error_code ec;
                value_type ref = _expr.evaluate_single(resources, root, current, options);
                if (!ec)
                {
                    this.EvaluateTail(resources, path, root, *resources.create_json(std::move(ref)), nodes, ndtype, options);
                }
            }

            std::string to_string(int level = 0)
            {
                std::string s;
                if (level > 0)
                {
                    s.append("\n");
                    s.append(level*2, ' ');
                }
                s.append("function_selector ");
                s.append(_expr.to_string(level+1));

                return s;
            }
        };

        Int32 line_;
        Int32 column_;
        const char_type* begin_input_;
        const char_type* end_input_;
        const char_type* p_;

        using argument_type = std::vector<pointer>;
        std::vector<argument_type> function_stack_;
        std::vector<ExprState> _stateStack;
        std::vector<token_type> _outputStack;
        std::vector<token_type> _operatorStack;

    public:
        jsonpath_evaluator()
            : line_(1), column_(1),
              begin_input_(nullptr), end_input_(nullptr),
              p_(nullptr)
        {
        }

        jsonpath_evaluator(Int32 line, Int32 column)
            : line_(line), column_(column),
              begin_input_(nullptr), end_input_(nullptr),
              p_(nullptr)
        {
        }

        Int32 line() const
        {
            return line_;
        }

        Int32 column() const
        {
            return column_;
        }

        pathExpression_type compile(static_resources<value_type,JsonElement>& resources, const string_view_type& path)
        {
            std::error_code ec;
            var result = compile(resources, path);
            if (ec)
            {
                JSONCONS_THROW(jsonpath_error(ec, line_, column_));
            }
            return result;
        }

        pathExpression_type compile(static_resources<value_type,JsonElement>& resources, 
                                     const string_view_type& path, 
                                     std::error_code& ec)
        {
            Int32 selector_id = 0;

            string_type buffer;
            UInt32 cp = 0;
            UInt32 cp2 = 0;

            begin_input_ = path.data();
            end_input_ = path.data() + path.length();
            p_ = begin_input_;

            Slice slic;

            Stack<Int64> evalStack;
            evalStack.Push(0);

            _stateStack.Push(ExprState.Start);
            while (p_ < end_input_ && !_stateStack.empty())
            {
                switch (_stateStack.Peek())
                {
                    case ExprState.Start: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                            {
                                PushToken(root_node_arg);
                                if (ec) {return pathExpression_type();}
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                _stateStack.Push(ExprState.PathRhs);
                                _stateStack.Push(ExprState.ExpectFunctionExpr);
                                _stateStack.Push(ExprState.UnquotedString);
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.RecursiveDescentOrPathExpression:
                        switch (_input[_index])
                        {
                            case '.':
                                PushToken(new Token(new RecursiveSelector());
                                if (ec) {return pathExpression_type();}
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.NameOrLeftBracket);
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathExpression);
                                break;
                        }
                        break;
                    case ExprState.NameOrLeftBracket: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[': // [ can follow ..
                                _stateStack.Pop(); _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathExpression);
                                break;
                        }
                        break;
                    case ExprState.JsonString:
                    {
                        //std::cout << "literal: " << buffer << "\n";
                        PushToken(new Token(literal_arg, Json(buffer)));
                        if (ec) {return pathExpression_type();}
                        buffer.Clear();
                        _stateStack.Pop(); // JsonValue
                        break;
                    }
                    case ExprState.PathOrValueOrFunction: 
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '$':
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathExpression);
                                break;
                            case '@':
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathExpression);
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                ++evalDepth[evalDepth.Count-1];
                                PushToken(TokenKind.LParen);
                                if (ec) {return pathExpression_type();}
                                break;
                            }
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonString);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonString);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(resources.get_unary_not()));
                                if (ec) {return pathExpression_type();}
                                break;
                            }
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            {
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonValue);
                                _stateStack.Push(ExprState.Number);
                                break;
                            }
                            default:
                            {
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunctionName);
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.JsonTextOrFunction:
                    {
                        switch (_input[_index])
                        {
                            case '(':
                            {
                                evalStack.Push(0);
                                var f = resources.get_function(buffer);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                buffer.Clear();
                                PushToken(current_node_arg);
                                if (ec) {return pathExpression_type();}
                                PushToken(new Token(f));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                json_decoder<Json> decoder;
                                basic_json_parser<char_type> parser;
                                parser.update(buffer.data(),buffer.Length);
                                parser.parse_some(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                parser.finish_parse(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                PushToken(new Token(literal_arg, decoder.get_result()));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.JsonValue:
                    {
                        json_decoder<Json> decoder;
                        basic_json_parser<char_type> parser;
                        parser.update(buffer.data(),buffer.Length);
                        parser.parse_some(decoder);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        parser.finish_parse(decoder);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        PushToken(new Token(literal_arg, decoder.get_result()));
                        if (ec) {return pathExpression_type();}
                        buffer.Clear();
                        _stateStack.Pop();
                        break;
                    }
                    case ExprState.JsonTextOrFunctionName:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '{':
                            case '[':
                            {
                                json_decoder<Json> decoder;
                                basic_json_parser<char_type> parser;
                                parser.update(p_,end_input_ - p_);
                                parser.parse_some(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                parser.finish_parse(decoder);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                PushToken(new Token(literal_arg, decoder.get_result()));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                p_ = parser.current();
                                column_ = column_ + parser.column() - 1;
                                break;
                            }
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.Number);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.JsonTextString);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(ExprState.JsonTextOrFunction);
                                _stateStack.Push(ExprState.UnquotedString);
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.Number: 
                        switch (_input[_index])
                        {
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            case 'e':case 'E':case '.':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // Number
                                break;
                        };
                        break;
                    case ExprState.JsonTextString: 
                        switch (_input[_index])
                        {
                            case '\\':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                if (p_ == end_input_)
                                {
                                    ec = jsonpath_errc::unexpected_eof;
                                    return pathExpression_type();
                                }
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                buffer.Append (_input[_index]);
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.PathExpression: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '*':
                                PushToken(new Token(new WildcardSelector());
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.RootNode));
                                PushToken(new Token(jsoncons::make_unique<root_selector>(selector_id++)));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(current_node_arg)); // ISSUE
                                PushToken(new Token(new CurrentNodeSelector());
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '.':
                                ec = jsonpath_errc::expected_key;
                                return pathExpression_type();
                            default:
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrFunctionExpr);
                                _stateStack.Push(ExprState.UnquotedString);
                                break;
                        }
                        break;
                    case ExprState.IdentifierOrFunctionExpr:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                evalStack.Push(0);
                                var f = resources.get_function(buffer);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                buffer.Clear();
                                PushToken(current_node_arg);
                                PushToken(new Token(f));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); 
                                break;
                            }
                        }
                        break;
                    }
                    case ExprState.ExpectFunctionExpr:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                evalStack.Push(0);
                                var f = resources.get_function(buffer);
                                if (ec)
                                {
                                    return pathExpression_type();
                                }
                                buffer.Clear();
                                PushToken(current_node_arg);
                                PushToken(new Token(f));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FunctionExpression);
                                _stateStack.Push(ExprState.ZeroOrOneArguments);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                ec = jsonpath_errc::expected_root_or_function;
                                return pathExpression_type();
                            }
                        }
                        break;
                    }
                    case ExprState.FunctionExpression:
                    {
                        
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(current_node_arg));
                                if (ec) {return pathExpression_type();}
                                PushToken(new Token(TokenKind.BeginExpression));
                                if (ec) {return pathExpression_type();}
                                if (ec) {return pathExpression_type();}
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                if (evalStack.Count == 0 || (evalDepth[evalDepth.Count-1] != 0))
                                {
                                    ec = jsonpath_errc::unbalanced_parentheses;
                                    return pathExpression_type();
                                }
                                evalStack.Pop();
                                PushToken(new Token(end_function_arg));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jsonpath_errc::syntax_error;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.ZeroOrOneArguments:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                _stateStack.Pop();
                                break;
                            default:
                                PushToken(new Token(TokenKind.BeginExpression));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.OneOrMoreArguments);
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                break;
                        }
                        break;
                    }
                    case ExprState.OneOrMoreArguments:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                _stateStack.Pop();
                                break;
                            case ',':
                                PushToken(new Token(TokenKind.BeginExpression));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Push(ExprState.Argument);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    }
                    case ExprState.Argument:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ')':
                            {
                                PushToken(new Token(end_argumentExpression_arg));
                                PushToken(argument_arg);
                                //PushToken(argument_arg);
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expected_comma_or_right_parenthesis;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.UnquotedString: 
                        switch (_input[_index])
                        {
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':case 'g':case 'h':case 'i':case 'j':case 'k':case 'l':case 'm':case 'n':case 'o':case 'p':case 'q':case 'r':case 's':case 't':case 'u':case 'v':case 'w':case 'x':case 'y':case 'z':
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':case 'G':case 'H':case 'I':case 'J':case 'K':case 'L':case 'M':case 'N':case 'O':case 'P':case 'Q':case 'R':case 'S':case 'T':case 'U':case 'V':case 'W':case 'X':case 'Y':case 'Z':
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            case '_':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if (typename std::make_unsigned<char_type>::type(_input[_index]) > 127)
                                {
                                    buffer.Append (_input[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    _stateStack.Pop(); // UnquotedString
                                }
                                break;
                        };
                        break;                    
                    case ExprState.PathRhs: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(ExprState.RecursiveDescentOrPathExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                if (evalStack.Count == 0)
                                {
                                    ec = jsonpath_errc::unbalanced_parentheses;
                                    return pathExpression_type();
                                }
                                if (evalDepth[evalDepth.Count-1] > 0)
                                {
                                    ++_index;
                                    ++_column;
                                    --evalDepth[evalDepth.Count-1];
                                    PushToken(TokenKind.RParen);
                                    if (ec) {return pathExpression_type();}
                                }
                                else
                                {
                                    _stateStack.Pop();
                                }
                                break;
                            }
                            case ']':
                            case ',':
                                _stateStack.Pop();
                                break;
                            default:
                                ec = jsonpath_errc::expected_separator;
                                return pathExpression_type();
                        };
                        break;
                    case ExprState.ExpressionRhs: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(ExprState.RecursiveDescentOrPathExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                if (evalStack.Count == 0)
                                {
                                    ec = jsonpath_errc::unbalanced_parentheses;
                                    return pathExpression_type();
                                }
                                if (evalDepth[evalDepth.Count-1] > 0)
                                {
                                    ++_index;
                                    ++_column;
                                    --evalDepth[evalDepth.Count-1];
                                    PushToken(TokenKind.RParen);
                                    if (ec) {return pathExpression_type();}
                                }
                                else
                                {
                                    _stateStack.Pop();
                                }
                                break;
                            }
                            case '|':
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.ExpectOr);
                                break;
                            case '&':
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.ExpectAnd);
                                break;
                            case '<':
                            case '>':
                            {
                                _stateStack.Push(ExprState.ComparatorExpression);
                                break;
                            }
                            case '=':
                            {
                                _stateStack.Push(ExprState.EqOrRegex);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpNe);
                                break;
                            }
                            case '+':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_plus_operator()));
                                if (ec) {return pathExpression_type();}
                                ++_index;
                                ++_column;
                                break;
                            case '-':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_minus_operator()));
                                if (ec) {return pathExpression_type();}
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_mult_operator()));
                                if (ec) {return pathExpression_type();}
                                ++_index;
                                ++_column;
                                break;
                            case '/':
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                PushToken(new Token(resources.get_div_operator()));
                                if (ec) {return pathExpression_type();}
                                ++_index;
                                ++_column;
                                break;
                            case ']':
                            case ',':
                                _stateStack.Pop();
                                break;
                            default:
                                ec = jsonpath_errc::expected_separator;
                                return pathExpression_type();
                        };
                        break;
                    case ExprState.ExpectOr:
                    {
                        switch (_input[_index])
                        {
                            case '|':
                                PushToken(new Token(resources.get_or_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_or;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.ExpectAnd:
                    {
                        switch (_input[_index])
                        {
                            case '&':
                                PushToken(new Token(resources.get_and_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); // ExpectAnd
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_and;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.ComparatorExpression:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                _stateStack.Push(ExprState.CmpGtOrGte);
                                break;
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    ec = jsonpath_errc::syntax_error;
                                    return pathExpression_type();
                                }
                                break;
                        }
                        break;
                    case ExprState.EqOrRegex:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '=':
                            {
                                PushToken(new Token(resources.GetEqOperator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '~':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(ExprState.ExpectRegex);
                                break;
                            }
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    ec = jsonpath_errc::syntax_error;
                                    return pathExpression_type();
                                }
                                break;
                        }
                        break;
                    case ExprState.ExpectRegex: 
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '/':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Regex);
                                ++_index;
                                ++_column;
                                break;
                            default: 
                                ec = jsonpath_errc::expected_forward_slash;
                                return pathExpression_type();
                        };
                        break;
                    case ExprState.Regex: 
                    {
                        switch (_input[_index])
                        {                   
                            case '/':
                                {
                                    std::regex::flag_type options = std::regex_constants::ECMAScript; 
                                    if (p_+1  < end_input_ && *(p_+1) == 'i')
                                    {
                                        ++_index;
                                        ++_column;
                                        options |= std::regex_constants::icase;
                                    }
                                    std::basicRegex<char_type> pattern(buffer, options);
                                    PushToken(resources.getRegex_operator(std::move(pattern)));
                                    if (ec) {return pathExpression_type();}
                                    buffer.Clear();
                                }
                                _stateStack.Pop();
                                break;

                            default: 
                                buffer.Append (_input[_index]);
                                break;
                        }
                        ++_index;
                        ++_column;
                        break;
                    }
                    case ExprState.CmpLtOrLte:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(resources.get_lte_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(resources.get_lt_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                break;
                        }
                        break;
                    }
                    case ExprState.CmpGtOrGte:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(resources.get_gte_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                //std::cout << "Parse: gt_operator\n";
                                PushToken(new Token(resources.get_gt_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case ExprState.CmpNe:
                    {
                        switch (_input[_index])
                        {
                            case '=':
                                PushToken(new Token(resources.get_ne_operator()));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_comparator;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.Identifier:
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        if (ec) {return pathExpression_type();}
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    case ExprState.SingleQuotedString:
                        switch (_input[_index])
                        {
                            case '\'':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(ExprState.QuotedStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.DoubleQuotedString: 
                        switch (_input[_index])
                        {
                            case '\"':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(ExprState.QuotedStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        };
                        break;
                    case ExprState.CommaOrRightBracket:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                _stateStack.Pop(); _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ']':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_CommaOrRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.ExpectRightBracket:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.BracketSpecifierOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '(':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.BeginExpression));
                                PushToken(TokenKind.LParen);
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.Expression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++evalDepth[evalDepth.Count-1];
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '?':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(TokenKind.BeginFilter));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                _stateStack.Pop(); _stateStack.Push(ExprState.WildcardOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IdentifierOrUnion);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSliceOrUnion);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(root_node_arg);
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);                                
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(current_node_arg)); // ISSUE
                                PushToken(new Token(new CurrentNodeSelector());
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_BracketSpecifierOrUnion;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.UnionElement:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ':': // SliceExpression
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSlice);
                                break;
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop(); _stateStack.Push(ExprState.IndexOrSlice);
                                _stateStack.Push(ExprState.Integer);
                                break;
                            case '(':
                            {
                                PushToken(new Token(TokenKind.BeginExpression));
                                PushToken(TokenKind.LParen);
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.Expression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++evalDepth[evalDepth.Count-1];
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '?':
                            {
                                PushToken(new Token(TokenKind.BeginFilter));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.FilterExpression);
                                _stateStack.Push(ExprState.ExpressionRhs);
                                _stateStack.Push(ExprState.PathOrValueOrFunction);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '*':
                                PushToken(new Token(new WildcardSelector());
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '$':
                                PushToken(new Token(TokenKind.RootNode));
                                PushToken(new Token(jsoncons::make_unique<root_selector>(selector_id++)));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '@':
                                PushToken(new Token(current_node_arg)); // ISSUE
                                PushToken(new Token(new CurrentNodeSelector());
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.PathRhs);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.SingleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\"':
                                _stateStack.Pop(); _stateStack.Push(ExprState.Identifier);
                                _stateStack.Push(ExprState.DoubleQuotedString);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expected_BracketSpecifierOrUnion;
                                return pathExpression_type();
                        }
                        break;

                    case ExprState.Integer:
                        switch (_input[_index])
                        {
                            case '-':
                                buffer.Append (_input[_index]);
                                _stateStack.Pop(); _stateStack.Push(ExprState.Digit);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); _stateStack.Push(ExprState.Digit);
                                break;
                        }
                        break;
                    case ExprState.Digit:
                        switch (_input[_index])
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // digit
                                break;
                        }
                        break;
                    case ExprState.IndexOrSliceOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            {
                                if (buffer.Length == 0)
                                {
                                    ec = jsonpath_errc::invalid_number;
                                    return pathExpression_type();
                                }
                                Int64 n{0};
                                var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                if (!r)
                                {
                                    ec = jsonpath_errc::invalid_number;
                                    return pathExpression_type();
                                }
                                PushToken(new Token(new IndexSelector(n)));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); // IndexOrSliceOrUnion
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ',':
                            {
                                PushToken(new Token(TokenKind.BeginUnion));
                                if (ec) {return pathExpression_type();}
                                if (buffer.Length == 0)
                                {
                                    ec = jsonpath_errc::invalid_number;
                                    return pathExpression_type();
                                }
                                else
                                {
                                    Int64 n{0};
                                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                    if (!r)
                                    {
                                        ec = jsonpath_errc::invalid_number;
                                        return pathExpression_type();
                                    }
                                    PushToken(new Token(new IndexSelector(n)));
                                    if (ec) {return pathExpression_type();}

                                    buffer.Clear();
                                }
                                PushToken(new Token(TokenKind.Separator));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ':':
                            {
                                if (!(buffer.Length == 0))
                                {
                                    Int64 n{0};
                                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                    if (!r)
                                    {
                                        ec = jsonpath_errc::invalid_number;
                                        return pathExpression_type();
                                    }
                                    slic.start_ = n;
                                    buffer.Clear();
                                }
                                PushToken(new Token(TokenKind.BeginUnion));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.SliceExpressionStop);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.Index:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case '.':
                            case ',':
                            {
                                if (buffer.Length == 0)
                                {
                                    ec = jsonpath_errc::invalid_number;
                                    return pathExpression_type();
                                }
                                else
                                {
                                    Int64 n{0};
                                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                    if (!r)
                                    {
                                        ec = jsonpath_errc::invalid_number;
                                        return pathExpression_type();
                                    }
                                    PushToken(new Token(new IndexSelector(n)));
                                    if (ec) {return pathExpression_type();}

                                    buffer.Clear();
                                }
                                _stateStack.Pop(); // index
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.SliceExpressionStop:
                    {
                        if (!(buffer.Length == 0))
                        {
                            Int64 n{0};
                            var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                            if (!r)
                            {
                                ec = jsonpath_errc::invalid_number;
                                return pathExpression_type();
                            }
                            slic.stop_ = jsoncons::optional<Int64>(n);
                            buffer.Clear();
                        }
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case ',':
                                PushToken(new Token(jsoncons::make_unique<SliceSelector>(slic)));
                                if (ec) {return pathExpression_type();}
                                slic = Slice{};
                                _stateStack.Pop(); // BracketSpecifier2
                                break;
                            case ':':
                                _stateStack.Pop(); _stateStack.Push(ExprState.SliceExpressionStep);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.SliceExpressionStep:
                    {
                        if (!(buffer.Length == 0))
                        {
                            Int64 n{0};
                            var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                            if (!r)
                            {
                                ec = jsonpath_errc::invalid_number;
                                return pathExpression_type();
                            }
                            if (n == 0)
                            {
                                ec = jsonpath_errc::step_cannot_be_zero;
                                return pathExpression_type();
                            }
                            slic.step_ = n;
                            buffer.Clear();
                        }
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            case ',':
                                PushToken(new Token(jsoncons::make_unique<SliceSelector>(slic)));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                slic = Slice{};
                                _stateStack.Pop(); // SliceExpressionStep
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    }

                    case ExprState.BracketedUnquotedNameOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']': 
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '.':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathExpression);                                
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathExpression);                                
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                PushToken(new Token(TokenKind.Separator));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.PathExpression);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append (_input[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;
                    case ExprState.UnionExpression:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Push(ExprState.PathExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(ExprState.BracketSpecifierOrUnion);
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.Separator));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Push(ExprState.UnionElement);
                                ++_index;
                                ++_column;
                                break;
                            case ']': 
                                PushToken(new Token(TokenKind.EndUnion));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.IdentifierOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']': 
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                PushToken(new Token(TokenKind.Separator));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.BracketedWildcard:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '[':
                            case ']':
                            case ',':
                            case '.':
                                PushToken(new Token(new WildcardSelector());
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.IndexOrSlice:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                if (buffer.Length == 0)
                                {
                                    ec = jsonpath_errc::invalid_number;
                                    return pathExpression_type();
                                }
                                else
                                {
                                    Int64 n{0};
                                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                    if (!r)
                                    {
                                        ec = jsonpath_errc::invalid_number;
                                        return pathExpression_type();
                                    }
                                    PushToken(new Token(new IndexSelector(n)));
                                    if (ec) {return pathExpression_type();}

                                    buffer.Clear();
                                }
                                _stateStack.Pop(); // BracketSpecifier
                                break;
                            }
                            case ':':
                            {
                                if (!(buffer.Length == 0))
                                {
                                    Int64 n{0};
                                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                                    if (!r)
                                    {
                                        ec = jsonpath_errc::invalid_number;
                                        return pathExpression_type();
                                    }
                                    slic.start_ = n;
                                    buffer.Clear();
                                }
                                _stateStack.Pop(); _stateStack.Push(ExprState.SliceExpressionStop);
                                _stateStack.Push(ExprState.Integer);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.WildcardOrUnion:
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']': 
                                PushToken(new Token(new WildcardSelector());
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case ',': 
                                PushToken(new Token(TokenKind.BeginUnion));
                                PushToken(new Token(new WildcardSelector());
                                PushToken(new Token(TokenKind.Separator));
                                if (ec) {return pathExpression_type();}
                                buffer.Clear();
                                _stateStack.Pop(); _stateStack.Push(ExprState.UnionExpression); // union
                                _stateStack.Push(ExprState.UnionElement);                                
                                ++_index;
                                ++_column;
                                break;
                            default:
                                ec = jsonpath_errc::expectedRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.QuotedStringEscapeChar:
                        switch (_input[_index])
                        {
                            case '\"':
                                buffer.Append('\"');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '\'':
                                buffer.Append('\'');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '\\': 
                                buffer.Append('\\');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '/':
                                buffer.Append('/');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'b':
                                buffer.Append('\b');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'f':
                                buffer.Append('\f');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'n':
                                buffer.Append('\n');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'r':
                                buffer.Append('\r');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 't':
                                buffer.Append('\t');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'u':
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU1);
                                break;
                            default:
                                ec = jsonpath_errc::illegal_escaped_character;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.EscapeU1:
                        cp = AppendToCodepoint(0, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU2);
                        break;
                    case ExprState.EscapeU2:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU3);
                        break;
                    case ExprState.EscapeU3:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU4);
                        break;
                    case ExprState.EscapeU4:
                        cp = AppendToCodepoint(cp, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        if (unicode_traits::is_high_surrogate(cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop(); _stateStack.Push(ExprState.EscapeExpectSurrogatePair1);
                        }
                        else
                        {
                            unicode_traits::convert(&cp, 1, buffer);
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                        }
                        break;
                    case ExprState.EscapeExpectSurrogatePair1:
                        switch (_input[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                ec = jsonpath_errc::invalid_codepoint;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.EscapeExpectSurrogatePair2:
                        switch (_input[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU5);
                                break;
                            default:
                                ec = jsonpath_errc::invalid_codepoint;
                                return pathExpression_type();
                        }
                        break;
                    case ExprState.EscapeU5:
                        cp2 = AppendToCodepoint(0, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU6);
                        break;
                    case ExprState.EscapeU6:
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU7);
                        break;
                    case ExprState.EscapeU7:
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); _stateStack.Push(ExprState.EscapeU8);
                        break;
                    case ExprState.EscapeU8:
                    {
                        cp2 = AppendToCodepoint(cp2, _input[_index]);
                        if (ec)
                        {
                            return pathExpression_type();
                        }
                        UInt32 codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        unicode_traits::convert(&codepoint, 1, buffer);
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }
                    case ExprState.FilterExpression:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                PushToken(new Token(TokenKind.EndFilter));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expected_CommaOrRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    }
                    case ExprState.Expression:
                    {
                        switch (_input[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                            case ']':
                            {
                                PushToken(new Token(end_indexExpression_arg));
                                if (ec) {return pathExpression_type();}
                                _stateStack.Pop();
                                break;
                            }
                            default:
                                ec = jsonpath_errc::expected_CommaOrRightBracket;
                                return pathExpression_type();
                        }
                        break;
                    }
                    default:
                        ++_index;
                        ++_column;
                        break;
                }
            }

            if (_stateStack.empty())
            {
                ec = jsonpath_errc::syntax_error;
                return pathExpression_type();
            }
            if (_stateStack.Peek() == ExprState.Start)
            {
                ec = jsonpath_errc::unexpected_eof;
                return pathExpression_type();
            }

            if (_stateStack.Count >= 3)
            {
                if (_stateStack.Peek() == ExprState.UnquotedString || _stateStack.Peek() == ExprState.Identifier)
                {
                    PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                    if (ec) {return pathExpression_type();}
                    _stateStack.Pop(); // UnquotedString
                    buffer.Clear();
                    if (_stateStack.Peek() == ExprState.IdentifierOrFunctionExpr)
                    {
                        _stateStack.Pop(); // identifier
                    }
                }
                else if (_stateStack.Peek() == ExprState.Digit)
                {
                    if (buffer.Length == 0)
                    {
                        ec = jsonpath_errc::invalid_number;
                        return pathExpression_type();
                    }
                    Int64 n{0};
                    var r = jsoncons::detail::to_integer(buffer.data(), buffer.Length, n);
                    if (!r)
                    {
                        ec = jsonpath_errc::invalid_number;
                        return pathExpression_type();
                    }
                    PushToken(new Token(new IndexSelector(n)));
                    if (ec) {return pathExpression_type();}
                    buffer.Clear();
                    _stateStack.Pop(); // IndexOrSliceOrUnion
                    if (_stateStack.Peek() == ExprState.Index)
                    {
                        _stateStack.Pop(); // index
                    }
                }
            }

            if (_stateStack.Count > 2)
            {
                ec = jsonpath_errc::unexpected_eof;
                return pathExpression_type();
            }
            if (evalStack.Count != 1 || evalDepth[evalDepth.Count-1] != 0)
            {
                ec = jsonpath_errc::unbalanced_parentheses;
                return pathExpression_type();
            }

            //std::cout << "\nTokens\n\n";
            //for (const var& tok : _outputStack)
            //{
            //    std::cout << tok.to_string() << "\n";
            //}
            //std::cout << "\n";

            if (_outputStack.Count == 0 || !_operatorStack.Count == 0())
            {
                ec = jsonpath_errc::unexpected_eof;
                return pathExpression_type();
            }

            return pathExpression_type(std::move(_outputStack.Peek().selector_));
        }

        void SkipWhiteSpace()
        {
            switch (_input[_index])
            {
                case ' ':case '\t':
                    ++_index;
                    ++_column;
                    break;
                case '\r':
                    if (p_+1 < end_input_ && *(p_+1) == '\n')
                        ++_index;
                    ++line_;
                    column_ = 1;
                    ++_index;
                    break;
                case '\n':
                    ++line_;
                    column_ = 1;
                    ++_index;
                    break;
                default:
                    break;
            }
        }

        void unwind_rparen(std::error_code& ec)
        {
            var it = _operatorStack.Rbegin();
            while (it != _operatorStack.Rend() && !it.Type == TokenKind.LeftParen)
            {
                _outputStack.Push(std::move(*it));
                ++it;
            }
            if (it == _operatorStack.Rend())
            {
                ec = jsonpath_errc::unbalanced_parentheses;
                return;
            }
            ++it;
            _operatorStack.erase(it.base(),_operatorStack.end());
        }

        void PushToken(token_type&& tok, std::error_code& ec)
        {
            //std::cout << tok.to_string() << "\n";
            switch (tok.type())
            {
                case TokenKind.BeginFilter:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenKind.LParen));
                    break;
                case TokenKind.EndFilter:
                {
                    UnwindRParen();
                    if (ec)
                    {
                        return;
                    }
                    std::vector<token_type> toks;
                    var it = _outputStack.Rbegin();
                    while (it != _outputStack.Rend() && it.type() != TokenKind.BeginFilter)
                    {
                        toks.insert(toks.begin(), std::move(*it));
                        ++it;
                    }
                    if (it == _outputStack.Rend())
                    {
                        ec = jsonpath_errc::unbalanced_parentheses;
                        return;
                    }
                    ++it;
                    _outputStack.erase(it.base(),_outputStack.end());

                    if (!_outputStack.Count == 0 && _outputStack.Peek().is_path())
                    {
                        _outputStack.Peek().selector_.AppendSelector(jsoncons::make_unique<FilterSelector>(expression_type(std::move(toks))));
                    }
                    else
                    {
                        _outputStack.Push(new Token(jsoncons::make_unique<FilterSelector>(expression_type(std::move(toks)))));
                    }
                    break;
                }
                case TokenKind.beginExpression:
                    //std::cout << "beginExpression\n";
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenKind.LParen));
                    break;
                case TokenKind.end_indexExpression:
                {
                    //std::cout << "TokenKind.end_indexExpression\n";
                    //for (const var& t : _outputStack)
                    //{
                    //    std::cout << t.to_string() << "\n";
                    //}
                    //std::cout << "/TokenKind.end_indexExpression\n";
                    UnwindRParen();
                    if (ec)
                    {
                        return;
                    }
                    std::vector<token_type> toks;
                    var it = _outputStack.Rbegin();
                    while (it != _outputStack.Rend() && it.type() != TokenKind.beginExpression)
                    {
                        toks.insert(toks.begin(), std::move(*it));
                        ++it;
                    }
                    if (it == _outputStack.Rend())
                    {
                        ec = jsonpath_errc::unbalanced_parentheses;
                        return;
                    }
                    ++it;
                    _outputStack.erase(it.base(),_outputStack.end());

                    if (!_outputStack.Count == 0 && _outputStack.Peek().is_path())
                    {
                        _outputStack.Peek().selector_.AppendSelector(jsoncons::make_unique<indexExpression_selector>(expression_type(std::move(toks))));
                    }
                    else
                    {
                        _outputStack.Push(new Token(jsoncons::make_unique<indexExpression_selector>(expression_type(std::move(toks)))));
                    }
                    break;
                }
                case TokenKind.end_argumentExpression:
                {
                    //std::cout << "TokenKind.end_indexExpression\n";
                    //for (const var& t : _outputStack)
                    //{
                    //    std::cout << t.to_string() << "\n";
                    //}
                    //std::cout << "/TokenKind.end_indexExpression\n";
                    UnwindRParen();
                    if (ec)
                    {
                        return;
                    }
                    std::vector<token_type> toks;
                    var it = _outputStack.Rbegin();
                    while (it != _outputStack.Rend() && it.type() != TokenKind.beginExpression)
                    {
                        toks.insert(toks.begin(), std::move(*it));
                        ++it;
                    }
                    if (it == _outputStack.Rend())
                    {
                        ec = jsonpath_errc::unbalanced_parentheses;
                        return;
                    }
                    ++it;
                    _outputStack.erase(it.base(),_outputStack.end());
                    _outputStack.Push(new Token(jsoncons::make_unique<argumentExpression>(expression_type(std::move(toks)))));
                    break;
                }
                case TokenKind.selector:
                {
                    if (!_outputStack.Count == 0 && _outputStack.Peek().is_path())
                    {
                        _outputStack.Peek().selector_.AppendSelector(std::move(tok.selector_));
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                }
                case TokenKind.Separator:
                    _outputStack.Push(token);
                    break;
                case TokenKind.BeginUnion:
                    _outputStack.Push(token);
                    break;
                case TokenKind.EndUnion:
                {
                    std::vector<pathExpression_type> expressions;
                    var it = _outputStack.Rbegin();
                    while (it != _outputStack.Rend() && it.type() != TokenKind.BeginUnion)
                    {
                        if (it.type() == TokenKind.selector)
                        {
                            expressions.emplace(expressions.begin(), pathExpression_type(std::move(it.selector_)));
                        }
                        do
                        {
                            ++it;
                        } 
                        while (it != _outputStack.Rend() && it.type() != TokenKind.BeginUnion && it.type() != TokenKind.Separator);
                        if (it.type() == TokenKind.Separator)
                        {
                            ++it;
                        }
                    }
                    if (it == _outputStack.Rend())
                    {
                        ec = jsonpath_errc::unbalanced_parentheses;
                        return;
                    }
                    ++it;
                    _outputStack.erase(it.base(),_outputStack.end());

                    if (!_outputStack.Count == 0 && _outputStack.Peek().is_path())
                    {
                        _outputStack.Peek().selector_.AppendSelector(jsoncons::make_unique<union_selector>(std::move(expressions)));
                    }
                    else
                    {
                        _outputStack.Push(new Token(jsoncons::make_unique<union_selector>(std::move(expressions))));
                    }
                    break;
                }
                case TokenKind.LParen:
                    _operatorStack.Push(token);
                    break;
                case TokenKind.RParen:
                {
                    UnwindRParen();
                    break;
                }
                case TokenKind.end_function:
                {
                    //std::cout << "TokenKind.end_function\n";
                    UnwindRParen();
                    if (ec)
                    {
                        return;
                    }
                    std::vector<token_type> toks;
                    var it = _outputStack.Rbegin();
                    Int32 arg_count = 0;
                    while (it != _outputStack.Rend() && it.type() != TokenKind.function)
                    {
                        if (it.type() == TokenKind.Argument)
                        {
                            ++arg_count;
                        }
                        toks.insert(toks.begin(), std::move(*it));
                        ++it;
                    }
                    if (it == _outputStack.Rend())
                    {
                        ec = jsonpath_errc::unbalanced_parentheses;
                        return;
                    }
                    if (it.arity() && arg_count != *(it.arity()))
                    {
                        ec = jsonpath_errc::invalid_arity;
                        return;
                    }
                    toks.Add(std::move(*it));
                    ++it;
                    _outputStack.erase(it.base(),_outputStack.end());

                    if (!_outputStack.Count == 0 && _outputStack.Peek().is_path())
                    {
                        _outputStack.Peek().selector_.AppendSelector(jsoncons::make_unique<function_selector>(expression_type(std::move(toks))));
                    }
                    else
                    {
                        _outputStack.Push(new Token(jsoncons::make_unique<function_selector>(std::move(toks))));
                    }
                    break;
                }
                case TokenKind.literal:
                    if (!_outputStack.Count == 0 && (_outputStack.Peek().Type == TokenKind.CurrentNode || _outputStack.Peek().Type == TokenKind.RootNode))
                    {
                        _outputStack.Peek() = std::move(tok);
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case TokenKind.function:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenKind.LParen));
                    break;
                case TokenKind.Argument:
                    _outputStack.Push(token);
                    break;
                case TokenKind.RootNode:
                case TokenKind.CurrentNode:
                    _outputStack.Push(token);
                    break;
                case TokenKind.UnaryOperator:
                case TokenKind.BinaryOperator:
                {
                    if (_operatorStack.Count == 0() || _operatorStack.Peek().Type == TokenKind.LeftParen)
                    {
                        _operatorStack.Push(token);
                    }
                    else if (tok.PrecedenceLevel < _operatorStack.Peek().PrecedenceLevel
                             || (tok.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && tok.is_right_associative()))
                    {
                        _operatorStack.Push(token);
                    }
                    else
                    {
                        var it = _operatorStack.Rbegin();
                        while (it != _operatorStack.Rend() && it.IsOperator
                               && (tok.PrecedenceLevel > it.PrecedenceLevel
                             || (tok.PrecedenceLevel == it.PrecedenceLevel && tok.is_right_associative())))
                        {
                            _outputStack.Push(std::move(*it));
                            ++it;
                        }

                        _operatorStack.erase(it.base(),_operatorStack.end());
                        _operatorStack.Push(token);
                    }
                    break;
                }
                default:
                    break;
            }
            //std::cout << "  " << "Output Stack\n";
            //for (var&& t : _outputStack)
            //{
            //    std::cout << t.to_string(2) << "\n";
            //}
            //if (!_operatorStack.Count == 0())
            //{
            //    std::cout << "  " << "Operator Stack\n";
            //    for (var&& t : _operatorStack)
            //    {
            //        std::cout << t.to_string(2) << "\n";
            //    }
            //}
        }

        UInt32 AppendToCodepoint(UInt32 cp, int c, std::error_code& ec)
        {
            cp *= 16;
            if (c >= '0'  &&  c <= '9')
            {
                cp += c - '0';
            }
            else if (c >= 'a'  &&  c <= 'f')
            {
                cp += c - 'a' + 10;
            }
            else if (c >= 'A'  &&  c <= 'F')
            {
                cp += c - 'A' + 10;
            }
            else
            {
                ec = jsonpath_errc::invalid_codepoint;
            }
            return cp;
        }
    };

    } // namespace detail

    template <class Json,class JsonReference = const Json&>
    class jsonpathExpression
    {
    public:
        using evaluator_t = typename jsoncons::jsonpath::detail::jsonpath_evaluator<Json, JsonReference>;
        using char_type = typename evaluator_t::char_type;
        using string_type = typename evaluator_t::string_type;
        using string_view_type = typename evaluator_t::string_view_type;
        using value_type = typename evaluator_t::value_type;
        using JsonElement = typename evaluator_t::JsonElement;
        using parameter_type = parameter<Json>;
        using json_selector_t = typename evaluator_t::pathExpression_type;
        using path_node_type = typename evaluator_t::path_node_type;
        using path_component_type = typename evaluator_t::path_component_type;
        using function_type = std::function<value_type(jsoncons::span<const parameter_type>, std::error_code& ec)>;
    private:
        jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> static_resources_;
        json_selector_t _expr;
    public:
        jsonpathExpression(jsoncons::jsonpath::detail::static_resources<value_type,JsonElement>&& resources,
                            json_selector_t&& expr)
            : static_resources_(std::move(resources)), 
              _expr(std::move(expr))
        {
        }

        jsonpathExpression(jsoncons::jsonpath::detail::static_resources<value_type,JsonElement>&& resources,
                            json_selector_t&& expr, std::vector<function_type>&& custom_functions)
            : static_resources_(std::move(resources)), 
              _expr(std::move(expr), std::move(custom_functions))
        {
        }

        template <class BinaryCallback>
        typename std::enable_if<type_traits::is_binary_function_object<BinaryCallback,const string_type&,JsonElement>::value,void>::type
        evaluate(JsonElement instance, BinaryCallback callback, result_options options = result_options())
        {
            std::vector<path_component_type> path = { path_component_type(root_node_arg) };

            jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;
            var f = [&callback](const std::vector<path_component_type>& path, JsonElement val)
            {
                callback(to_string(path), val);
            };
            _expr.evaluate(resources, path, instance, instance, f, options);
        }

        Json evaluate(JsonElement instance, result_options options = result_options())
        {
            std::vector<path_component_type> path = {path_component_type(root_node_arg)};

            if ((options & result_options::path) == result_options::path)
            {
                jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;

                Json result(json_array_arg);
                var callback = [&result](const std::vector<path_component_type>& p, JsonElement)
                {
                    result.Add(to_string(p));
                };
                _expr.evaluate(resources, path, instance, instance, callback, options);
                return result;
            }
            else
            {
                jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;
                return _expr.evaluate(resources, path, instance, instance, options);
            }
        }

        static jsonpathExpression compile(const string_view_type& path)
        {
            jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> resources;

            evaluator_t e;
            json_selector_t expr = e.compile(resources, path);
            return jsonpathExpression(std::move(resources), std::move(expr));
        }

        static jsonpathExpression compile(const string_view_type& path, std::error_code& ec)
        {
            jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> resources;
            evaluator_t e;
            json_selector_t expr = e.compile(resources, path);
            return jsonpathExpression(std::move(resources), std::move(expr));
        }

        static jsonpathExpression compile(const string_view_type& path, 
                                           const custom_functions<Json>& functions)
        {
            jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> resources(functions);

            evaluator_t e;
            json_selector_t expr = e.compile(resources, path);
            return jsonpathExpression(std::move(resources), std::move(expr));
        }

        static jsonpathExpression compile(const string_view_type& path, 
                                           const custom_functions<Json>& functions, 
                                           std::error_code& ec)
        {
            jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> resources(functions);
            evaluator_t e;
            json_selector_t expr = e.compile(resources, path);
            return jsonpathExpression(std::move(resources), std::move(expr));
        }
    };

    template <class Json>
    jsonpathExpression<Json> makeExpression(const typename Json::string_view_type& expr, 
                                              const custom_functions<Json>& functions = custom_functions<Json>())
    {
        return jsonpathExpression<Json>::compile(expr, functions);
    }

    template <class Json>
    jsonpathExpression<Json> makeExpression(const typename Json::string_view_type& expr, std::error_code& ec)
    {
        return jsonpathExpression<Json>::compile(expr);
    }

    template <class Json>
    jsonpathExpression<Json> makeExpression(const typename Json::string_view_type& expr, 
                                              const custom_functions<Json>& functions, 
                                              std::error_code& ec)
    {
        return jsonpathExpression<Json>::compile(expr, functions);
    }

    template<class Json>
    Json json_query(const Json& instance,
                    const typename Json::string_view_type& path, 
                    result_options options = result_options(),
                    const custom_functions<Json>& functions = custom_functions<Json>())
    {
        var expr = makeExpression<Json>(path, functions);
        return expr.evaluate(instance, options);
    }

    template<class Json,class Callback>
    typename std::enable_if<type_traits::is_binary_function_object<Callback,const std::basic_string<typename Json::char_type>&,const Json&>::value,void>::type
    json_query(const Json& instance, 
               const typename Json::string_view_type& path, 
               Callback callback,
               result_options options = result_options(),
               const custom_functions<Json>& functions = custom_functions<Json>())
    {
        var expr = makeExpression<Json>(path, functions);
        expr.evaluate(instance, callback, options);
    }

    template<class Json, class T>
    typename std::enable_if<is_json_type_traits_specialized<Json,T>::value,void>::type
        json_replace(Json& instance, const typename Json::string_view_type& path, T&& new_value,
                     result_options options = result_options::nodups,
                     const custom_functions<Json>& funcs = custom_functions<Json>())
    {
        using evaluator_t = typename jsoncons::jsonpath::detail::jsonpath_evaluator<Json, Json&>;
        //using string_type = typename evaluator_t::string_type;
        using value_type = typename evaluator_t::value_type;
        using JsonElement = typename evaluator_t::JsonElement;
        using json_selector_t = typename evaluator_t::pathExpression_type;
        using path_component_type = typename evaluator_t::path_component_type;

        std::vector<path_component_type> output_path = { path_component_type(root_node_arg ) };

        jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> static_resources(funcs);
        evaluator_t e;
        json_selector_t expr = e.compile(static_resources, path);

        jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;
        var callback = [&new_value](const std::vector<path_component_type>&, JsonElement v)
        {
            v = std::forward<T>(new_value);
        };
        expr.evaluate(resources, output_path, instance, instance, callback, options);
    }

    template<class Json, class UnaryCallback>
    typename std::enable_if<type_traits::is_unary_function_object<UnaryCallback,Json>::value,void>::type
    json_replace(Json& instance, const typename Json::string_view_type& path , UnaryCallback callback)
    {
        using evaluator_t = typename jsoncons::jsonpath::detail::jsonpath_evaluator<Json, Json&>;
        //using string_type = typename evaluator_t::string_type;
        using value_type = typename evaluator_t::value_type;
        using JsonElement = typename evaluator_t::JsonElement;
        using json_selector_t = typename evaluator_t::pathExpression_type;
        using path_component_type = typename evaluator_t::path_component_type;

        std::vector<path_component_type> output_path = { path_component_type(root_node_arg) };

        jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> static_resources;
        evaluator_t e;
        json_selector_t expr = e.compile(static_resources, path);

        jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;
        var f = [callback](const std::vector<path_component_type>&, JsonElement v)
        {
            v = callback(v);
        };
        expr.evaluate(resources, output_path, instance, instance, f, result_options::nodups);
    }

    template<class Json, class BinaryCallback>
    typename std::enable_if<type_traits::is_binary_function_object<BinaryCallback,const std::basic_string<typename Json::char_type>&,Json&>::value,void>::type
    json_replace(Json& instance, const typename Json::string_view_type& path , BinaryCallback callback, 
                 result_options options = result_options::nodups,
                 const custom_functions<Json>& funcs = custom_functions<Json>())
    {
        using evaluator_t = typename jsoncons::jsonpath::detail::jsonpath_evaluator<Json, Json&>;
        //using string_type = typename evaluator_t::string_type;
        using value_type = typename evaluator_t::value_type;
        using JsonElement = typename evaluator_t::JsonElement;
        using json_selector_t = typename evaluator_t::pathExpression_type;
        using path_component_type = typename evaluator_t::path_component_type;

        std::vector<path_component_type> output_path = { path_component_type(root_node_arg) };

        jsoncons::jsonpath::detail::static_resources<value_type,JsonElement> static_resources(funcs);
        evaluator_t e;
        json_selector_t expr = e.compile(static_resources, path);

        jsoncons::jsonpath::detail::dynamic_resources<Json,JsonElement> resources;

        var f = [&callback](const std::vector<path_component_type>& path, JsonElement val)
        {
            callback(to_string(path), val);
        };
        expr.evaluate(resources, output_path, instance, instance, f, options);
    }

} // namespace jsonpath
} // namespace jsoncons

#endif
