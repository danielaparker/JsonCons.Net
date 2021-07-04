// Copyright 2021 Daniel Parker
// Distributed under the Boost license, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)

// See https://github.com/danielaparker/jsoncons for latest version

#ifndef JSONCONS_JSONPATH_EXPRESSION_HPP
#define JSONCONS_JSONPATH_EXPRESSION_HPP

#include <string> // std.basic_string
#include <vector> // std.vector
#include <unordered_map> // std.unordered_map
#include <unordered_set> // std.unordered_set
#include <limits> // std.numeric_limits
#include <set> // std.set
#include <utility> // std.move
#if defined(JSONCONS_HAS_STD_REGEX)
#include <regex>
#endif
#include <jsoncons/json_type.hpp>
#include <jsoncons_ext/jsonpath/normalized_path.hpp>
#include <jsoncons_ext/jsonpath/jsonpath_error.hpp>

namespace jsoncons { 
namespace jsonpath {

    struct reference_arg_t
    {
        explicit reference_arg_t() = default;
    };
   expr reference_arg_t reference_arg{};

    struct_reference_arg_t
    {
        explicit_reference_arg_t() = default;
    };
   expr_reference_arg_t_reference_arg{};

    struct literal_arg_t
    {
        explicit literal_arg_t() = default;
    };
   expr literal_arg_t literal_arg{};

    struct end_of_expression_arg_t
    {
        explicit end_of_expression_arg_t() = default;
    };
   expr end_of_expression_arg_t end_of_expression_arg{};

    struct separator_arg_t
    {
        explicit separator_arg_t() = default;
    };
   expr separator_arg_t separator_arg{};

    struct lparen_arg_t
    {
        explicit lparen_arg_t() = default;
    };
   expr lparen_arg_t lparen_arg{};

    struct rparen_arg_t
    {
        explicit rparen_arg_t() = default;
    };
   expr rparen_arg_t rparen_arg{};

    struct begin_union_arg_t
    {
        explicit begin_union_arg_t() = default;
    };
   expr begin_union_arg_t begin_union_arg{};

    struct end_union_arg_t
    {
        explicit end_union_arg_t() = default;
    };
   expr end_union_arg_t end_union_arg{};

    struct begin_filter_arg_t
    {
        explicit begin_filter_arg_t() = default;
    };
   expr begin_filter_arg_t begin_filter_arg{};

    struct end_filter_arg_t
    {
        explicit end_filter_arg_t() = default;
    };
   expr end_filter_arg_t end_filter_arg{};

    struct begin_expression_arg_t
    {
        explicit begin_expression_arg_t() = default;
    };
   expr begin_expression_arg_t begin_expression_arg{};

    struct end_index_expression_arg_t
    {
        explicit end_index_expression_arg_t() = default;
    };
   expr end_index_expression_arg_t end_index_expression_arg{};

    struct end_argument_expression_arg_t
    {
        explicit end_argument_expression_arg_t() = default;
    };
   expr end_argument_expression_arg_t end_argument_expression_arg{};

    struct current_node_arg_t
    {
        explicit current_node_arg_t() = default;
    };
   expr current_node_arg_t current_node_arg{};

    struct root_node_arg_t
    {
        explicit root_node_arg_t() = default;
    };
   expr root_node_arg_t root_node_arg{};

    struct end_function_arg_t
    {
        explicit end_function_arg_t() = default;
    };
   expr end_function_arg_t end_function_arg{};

    struct argument_arg_t
    {
        explicit argument_arg_t() = default;
    };
   expr argument_arg_t argument_arg{};

    enum class result_options {value=0, nodups=1, sort=2, path=4};

    using result_type = result_options;

    inline result_options operator~(result_options a)
    {
        return static_cast<result_options>(~static_cast<unsigned int>(a));
    }

    inline result_options operator&(result_options a, result_options b)
    {
        return static_cast<result_options>(static_cast<unsigned int>(a) & static_cast<unsigned int>(b));
    }

    inline result_options operator^(result_options a, result_options b)
    {
        return static_cast<result_options>(static_cast<unsigned int>(a) ^ static_cast<unsigned int>(b));
    }

    inline result_options operator|(result_options a, result_options b)
    {
        return static_cast<result_options>(static_cast<unsigned int>(a) | static_cast<unsigned int>(b));
    }

    inline result_options operator&=(result_options& a, result_options b)
    {
        a = a & b;
        return a;
    }

    inline result_options operator^=(result_options& a, result_options b)
    {
        a = a ^ b;
        return a;
    }

    inline result_options operator|=(result_options& a, result_options b)
    {
        a = a | b;
        return a;
    }

    template <class Json>
    class parameter;

    template <class Json,class JsonElement>
    class value_or_pointer
    {
    public:
        friend class parameter<Json>;
        using value_type = Json;
        using reference = JsonElement;
        using pointer = typename std.conditional<std.is_const<typename std.remove_reference<reference>.type>.value,typename Json.const_pointer,typename Json.pointer>.type;
    private:
        bool is_value_;
        union
        {
            value_type val_;
            pointer ptr_;
        };
    public:
        value_or_pointer(value_type&& val)
            : is_value_(true), val_(std.move(val))
        {
        }

        value_or_pointer(pointer ptr)
            : is_value_(false), ptr_(std.move(ptr))
        {
        }

        value_or_pointer(value_or_pointer&& other) noexcept
            : is_value_(other.is_value_)
        {
            if (is_value_)
            {
                new(&val_)value_type(std.move(other.val_));
            }
            else
            {
                ptr_ = other.ptr_;
            }
        }

        ~value_or_pointer() noexcept
        {
            if (is_value_)
            {
                val_.~value_type();
            }
        }

        reference value() 
        {
            return is_value_ ? val_ : *ptr_;
        }

        pointer ptr() 
        {
            return is_value_ ? &val_ : ptr_;
        }
    };

    template <class Json>
    class parameter
    {
        using value_type = Json;
        using reference = Json&;
        using pointer = Json*;
    private:
        value_or_pointer<Json,reference> data_;
    public:
        template <class JsonElement>
        parameter(value_or_pointer<Json,JsonElement>&& data) noexcept
            : data_(nullptr)
        {
            data_.is_value_ = data.is_value_;
            if (data.is_value_)
            {
                data_.val_ = std.move(data.val_);
            }
            else
            {
                data_.ptr_ = data.ptr_;
            }
        }

        parameter(const parameter& other) noexcept = default;

        parameter(parameter&& other) noexcept = default;

        parameter& operator=(const parameter& other) noexcept = default;

        parameter& operator=(parameter&& other) noexcept = default;

        Json& value()
        {
            return data_.is_value_ ? data_.val_ : *data_.ptr_;
        }
    };

    template <class Json>
    class custom_function
    {
    public:
        using value_type = Json;
        using char_type = typename Json.char_type;
        using parameter_type = parameter<Json>;
        using function_type = std.function<value_type(jsoncons.span<const parameter_type>, std.error_code& ec)>;
        using string_type = std.basic_string<char_type>;

        string_type function_name_;
        optional<int> arity_;
        function_type f_;

        custom_function(const string_type& function_name,
                        optional<int>& arity,
                        function_type& f)
            : function_name_(function_name),
              arity_(arity),
              f_(f)
        {
        }

        custom_function(string_type&& function_name,
                        optional<int>&& arity,
                        function_type&& f)
            : function_name_(std.move(function_name)),
              arity_(std.move(arity)),
              f_(std.move(f))
        {
        }

        custom_function(const custom_function&) = default;

        custom_function(custom_function&&) = default;

        string_type& name() 
        {
            return function_name_;
        }

        optional<int> arity() 
        {
            return arity_;
        }

        function_type& function() 
        {
            return f_;
        }
    };

    template <class Json>
    class custom_functions
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using function_type = std.function<value_type(jsoncons.span<const parameter_type>, std.error_code& ec)>;
        using_iterator = typename std.vector<custom_function<Json>>.const_iterator;

        std.vector<custom_function<Json>> functions_;
    public:
        void register_function(const string_type& name,
                               jsoncons.optional<int> arity,
                               function_type& f)
        {
            functions_.emplace_back(name, arity, f);
        }

       _iterator begin()
        {
            return functions_.begin();
        }

       _iterator end()
        {
            return functions_.end();
        }
    };

namespace detail {

    enum class node_kind{unknown, single, multi};

    template <class Json,class JsonElement>
    class dynamic_resources;

    template <class Json,class JsonElement>
    struct UnaryOperator
    {
        int _precedenceLevel;
        bool _isRightAssociative;

        UnaryOperator(int precedenceLevel,
                       bool isRightAssociative)
            : _precedenceLevel(precedenceLevel),
              _isRightAssociative(isRightAssociative)
        {
        }

        virtual ~UnaryOperator() = default;

        int PrecedenceLevel() 
        {
            return _precedenceLevel;
        }
        bool IsRightAssociative
        {
            return _isRightAssociative;
        }

        virtual JsonElement Evaluate(JsonElement, 
                              std.error_code&) = 0;
    };

    template <class Json>
    bool IsFalse(const Json& val)
    {
        return ((val.ValueKind == JsonValueKind.Array && val.empty()) ||
                 (val.ValueKind == JsonValueKind.Object && val.empty()) ||
                 (val.ValueKind == JsonValueKind.String && val.as_string_view().empty()) ||
                 (val.is_bool() && !val.as_bool()) ||
                 (val.ValueKind == JsonValueKind.Number && (val == Json(0))) ||
                 val.ValueKind == JsonValueKind.Null);
    }

    template <class Json>
    bool IsTrue(const Json& val)
    {
        return !IsFalse(val);
    }

    template <class Json,class JsonElement>
    class NotOperator : public UnaryOperator<Json,JsonElement>
    {
    public:
        NotOperator()
            : UnaryOperator<Json,JsonElement>(1, true)
        {}

        JsonElement Evaluate(JsonElement val, 
                      std.error_code&) override
        {
            return IsFalse(val) ? JsonConstants::True : JsonConstants::False;
        }
    };

    template <class Json,class JsonElement>
    class UnaryMinusOperator : public UnaryOperator<Json,JsonElement>
    {
    public:
        UnaryMinusOperator()
            : UnaryOperator<Json,JsonElement>(1, true)
        {}

        JsonElement Evaluate(JsonElement val, 
                      std.error_code&) override
        {
            if (val.is_int64())
            {
                return Json(-val.template as<int64_t>());
            }
            else if (val.is_double())
            {
                return Json(-val.as_double());
            }
            else
            {
                return JsonConstants.Null;
            }
        }
    };

    template <class Json,class JsonElement>
    class RegexOperator : public UnaryOperator<Json,JsonElement>
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;
        std.basic_regex<char_type> pattern_;
    public:
        RegexOperator(std.basic_regex<char_type>&& pattern)
            : UnaryOperator<Json,JsonElement>(2, true),
              pattern_(std.move(pattern))
        {
        }

        RegexOperator(RegexOperator&&) = default;
        RegexOperator& operator=(RegexOperator&&) = default;

        JsonElement Evaluate(JsonElement val, 
                             std.error_code&) override
        {
            if (!val.ValueKind == JsonValueKind.String)
            {
                return JsonConstants.Null;
            }
            return std.regex_search(val.as_string(), pattern_) ? JsonConstants::True : JsonConstants::False;
        }
    };

    struct BinaryOperator
    {
        int _precedenceLevel;
        bool _isRightAssociative;

        BinaryOperator(int precedenceLevel,
                        bool isRightAssociative = false)
            : _precedenceLevel(precedenceLevel),
              _isRightAssociative(isRightAssociative)
        {
        }

        int PrecedenceLevel() 
        {
            return _precedenceLevel;
        }
        bool IsRightAssociative
        {
            return _isRightAssociative;
        }

        virtual JsonElement Evaluate(JsonElement, 
                             JsonElement);
    };

    // Implementations

    template <class Json,class JsonElement>
    class OrOperator : BinaryOperator
    {
    public:
        OrOperator()
            : BinaryOperator(9)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.ValueKind == JsonValueKind.Null && rhs.ValueKind == JsonValueKind.Null)
            {
                return JsonConstants.Null;
            }
            if (!IsFalse(lhs))
            {
                return lhs;
            }
            else
            {
                return rhs;
            }
        }
        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                //s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("or operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class AndOperator : BinaryOperator
    {
    public:
        AndOperator()
            : base(8)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (IsTrue(lhs))
            {
                return rhs;
            }
            else
            {
                return lhs;
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("and operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class EqOperator : BinaryOperator
    {
    public:
        EqOperator()
            : base(6)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            return lhs == rhs ? JsonConstants::True : JsonConstants::False;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("equal operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class NeOperator : BinaryOperator
    {
    public:
        NeOperator()
            : base(6)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            return lhs != rhs ? JsonConstants::True : JsonConstants::False;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("not equal operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class LtOperator : BinaryOperator
    {
    public:
        LtOperator()
            : base(5)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
            {
                return lhs < rhs ? JsonConstants::True : JsonConstants::False;
            }
            else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
            {
                return lhs < rhs ? JsonConstants::True : JsonConstants::False;
            }
            return JsonConstants.Null;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("less than operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class LteOperator : BinaryOperator
    {
    public:
        LteOperator()
            : base(5)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
            {
                return lhs <= rhs ? JsonConstants::True : JsonConstants::False;
            }
            else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
            {
                return lhs <= rhs ? JsonConstants::True : JsonConstants::False;
            }
            return JsonConstants.Null;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("less than or equal operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class GtOperator : BinaryOperator
    {
    public:
        GtOperator()
            : base(5)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            //std.cout << "operator> lhs: " << lhs << ", rhs: " << rhs << "\n";

            if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
            {
                return lhs > rhs ? JsonConstants::True : JsonConstants::False;
            }
            else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
            {
                return lhs > rhs ? JsonConstants::True : JsonConstants::False;
            }
            return JsonConstants.Null;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("greater than operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class GteOperator : BinaryOperator
    {
    public:
        GteOperator()
            : base(5)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
            {
                return lhs >= rhs ? JsonConstants::True : JsonConstants::False;
            }
            else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
            {
                return lhs >= rhs ? JsonConstants::True : JsonConstants::False;
            }
            return JsonConstants.Null;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("greater than or equal operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class PlusOperator : BinaryOperator
    {
    public:
        PlusOperator()
            : base(4)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }
            else if (lhs.is_int64() && rhs.is_int64())
            {
                return Json(((lhs.template as<int64_t>() + rhs.template as<int64_t>())));
            }
            else if (lhs.is_uint64() && rhs.is_uint64())
            {
                return Json((lhs.template as<uint64_t>() + rhs.template as<uint64_t>()));
            }
            else
            {
                return Json((lhs.as_double() + rhs.as_double()));
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("plus operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class MinusOperator : BinaryOperator
    {
    public:
        MinusOperator()
            : base(4)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }
            else if (lhs.is_int64() && rhs.is_int64())
            {
                return Json(((lhs.template as<int64_t>() - rhs.template as<int64_t>())));
            }
            else if (lhs.is_uint64() && rhs.is_uint64())
            {
                return Json((lhs.template as<uint64_t>() - rhs.template as<uint64_t>()));
            }
            else
            {
                return Json((lhs.as_double() - rhs.as_double()));
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("minus operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class MultOperator : BinaryOperator
    {
    public:
        MultOperator()
            : base(3)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }
            else if (lhs.is_int64() && rhs.is_int64())
            {
                return Json(((lhs.template as<int64_t>() * rhs.template as<int64_t>())));
            }
            else if (lhs.is_uint64() && rhs.is_uint64())
            {
                return Json((lhs.template as<uint64_t>() * rhs.template as<uint64_t>()));
            }
            else
            {
                return Json((lhs.as_double() * rhs.as_double()));
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("multiply operator");
            return s;
        }
    };

    template <class Json,class JsonElement>
    class DivOperator : BinaryOperator
    {
    public:
        DivOperator()
            : base(3)
        {
        }

        JsonElement Evaluate(JsonElement lhs, JsonElement rhs)
        {
            //std.cout << "operator/ lhs: " << lhs << ", rhs: " << rhs << "\n";

            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }
            else if (lhs.is_int64() && rhs.is_int64())
            {
                return Json(((lhs.template as<int64_t>() / rhs.template as<int64_t>())));
            }
            else if (lhs.is_uint64() && rhs.is_uint64())
            {
                return Json((lhs.template as<uint64_t>() / rhs.template as<uint64_t>()));
            }
            else
            {
                return Json((lhs.as_double() / rhs.as_double()));
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("divide operator");
            return s;
        }
    };

    // function_base
    template <class Json>
    class BaseFunction
    {
        jsoncons.optional<int> arg_count_;
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        BaseFunction(jsoncons.optional<int> arg_count)
            : arg_count_(arg_count)
        {
        }

        virtual ~BaseFunction() noexcept = default;

        jsoncons.optional<int> arity()
        {
            return arg_count_;
        }

        virtual value_type Evaluate(const std.vector<parameter_type>& args, 
                                    std.error_code& ec) = 0;

        virtual string to_string(int level = 0)
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("function");
            return s;
        }
    };  

    template <class Json>
    class decorator_function : public BaseFunction<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;
        using function_type = std.function<value_type(jsoncons.span<const parameter_type>, std.error_code& ec)>;
    private:
        function_type f_;
    public:
        decorator_function(jsoncons.optional<int> arity,
            function_type& f)
            : BaseFunction<Json>(arity), f_(f)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args,
            std.error_code& ec) override
        {
            return f_(args, ec);
        }
    };

    template <class Json>
    class ContainsFunction : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        ContainsFunction()
            : BaseFunction(2)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            var arg1= args[1].value();

            switch (arg0.type())
            {
                case json_type.array_value:
                    for (var& j : arg0.array_range())
                    {
                        if (j == arg1)
                        {
                            return value_type(true);
                        }
                    }
                    return value_type(false);
                case json_type.string_value:
                {
                    if (!arg1.ValueKind == JsonValueKind.String)
                    {
                        ec = jsonpath_errc.invalid_type;
                        return value_type.null();
                    }
                    var sv0 = arg0.template as<string_view_type>();
                    var sv1 = arg1.template as<string_view_type>();
                    return sv0.find(sv1) != string_view_type.npos ? value_type(true) : value_type(false);
                }
                default:
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("contains function");
            return s;
        }
    };

    template <class Json>
    class ends_with_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        ends_with_function()
            : BaseFunction(2)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.String)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            var arg1= args[1].value();
            if (!arg1.ValueKind == JsonValueKind.String)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            var sv0 = arg0.template as<string_view_type>();
            var sv1 = arg1.template as<string_view_type>();

            if (sv1.length() <= sv0.length() && sv1 == sv0.substr(sv0.length() - sv1.length()))
            {
                return value_type(true);
            }
            else
            {
                return value_type(false);
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("ends_with function");
            return s;
        }
    };

    template <class Json>
    class starts_with_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        starts_with_function()
            : BaseFunction(2)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.String)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            var arg1= args[1].value();
            if (!arg1.ValueKind == JsonValueKind.String)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            var sv0 = arg0.template as<string_view_type>();
            var sv1 = arg1.template as<string_view_type>();

            if (sv1.length() <= sv0.length() && sv1 == sv0.substr(0, sv1.length()))
            {
                return value_type(true);
            }
            else
            {
                return value_type(false);
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("starts_with function");
            return s;
        }
    };

    template <class Json>
    class sum_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        sum_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Array)
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            //std.cout << "sum function arg: " << arg0 << "\n";

            double sum = 0;
            for (var& j : arg0.array_range())
            {
                if (!j.ValueKind == JsonValueKind.Number)
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                sum += j.template as<double>();
            }

            return value_type(sum);
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("sum function");
            return s;
        }
    };

#if defined(JSONCONS_HAS_STD_REGEX)

    template <class Json>
    class tokenize_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;

        tokenize_function()
            : BaseFunction(2)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            if (!args[0].value().ValueKind == JsonValueKind.String || !args[1].value().ValueKind == JsonValueKind.String)
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            var arg0 = args[0].value().template as<string_type>();
            var arg1 = args[1].value().template as<string_type>();

            std.regex.flag_type options = std.regex_constants.ECMAScript; 
            std.basic_regex<char_type> pieces_regex(arg1, options);

            std.regex_token_iterator<typename string_type.const_iterator> rit ( arg0.begin(), arg0.end(), pieces_regex, -1);
            std.regex_token_iterator<typename string_type.const_iterator> rend;

            value_type j(json_array_arg);
            while (rit != rend) 
            {
                j.emplace_back(rit.str());
                ++rit;
            }
            return j;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("tokenize function");
            return s;
        }
    };

#endif // defined(JSONCONS_HAS_STD_REGEX)

    template <class Json>
    class ceil_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        ceil_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            switch (arg0.type())
            {
                case json_type.uint64_value:
                case json_type.int64_value:
                {
                    return value_type(arg0.template as<double>());
                }
                case json_type.double_value:
                {
                    return value_type(std.ceil(arg0.template as<double>()));
                }
                default:
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("ceil function");
            return s;
        }
    };

    template <class Json>
    class floor_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        floor_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            switch (arg0.type())
            {
                case json_type.uint64_value:
                case json_type.int64_value:
                {
                    return value_type(arg0.template as<double>());
                }
                case json_type.double_value:
                {
                    return value_type(std.floor(arg0.template as<double>()));
                }
                default:
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("floor function");
            return s;
        }
    };

    template <class Json>
    class to_number_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        to_number_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            switch (arg0.type())
            {
                case json_type.int64_value:
                case json_type.uint64_value:
                case json_type.double_value:
                    return arg0;
                case json_type.string_value:
                {
                    var sv = arg0.as_string_view();
                    uint64_t un{0};
                    var result1 = jsoncons.detail.to_integer(sv.data(), sv.length(), un);
                    if (result1)
                    {
                        return value_type(un);
                    }
                    int64_t sn{0};
                    var result2 = jsoncons.detail.to_integer(sv.data(), sv.length(), sn);
                    if (result2)
                    {
                        return value_type(sn);
                    }
                    jsoncons.detail.to_double_t to_double;
                    try
                    {
                        var s = arg0.as_string();
                        double d = to_double(s.c_str(), s.length());
                        return value_type(d);
                    }
                    catch (const std.exception&)
                    {
                        return value_type.null();
                    }
                }
                default:
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("to_number function");
            return s;
        }
    };

    template <class Json>
    class prod_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        prod_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Array || arg0.empty())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            double prod = 1;
            for (var& j : arg0.array_range())
            {
                if (!j.ValueKind == JsonValueKind.Number)
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                prod *= j.template as<double>();
            }

            return value_type(prod);
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("prod function");
            return s;
        }
    };

    template <class Json>
    class avg_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        avg_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Array)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }
            double sum = 0;
            for (var& j : arg0.array_range())
            {
                if (!j.ValueKind == JsonValueKind.Number)
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                sum += j.template as<double>();
            }

            return value_type(sum / static_cast<double>(arg0.size()));
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("to_string function");
            return s;
        }
    };

    template <class Json>
    class min_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        min_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Array)
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }
            bool is_number = arg0.at(0).ValueKind == JsonValueKind.Number;
            bool is_string = arg0.at(0).ValueKind == JsonValueKind.String;
            if (!is_number && !is_string)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            int index = 0;
            for (int i = 1; i < arg0.size(); ++i)
            {
                if (!(arg0.at(i).ValueKind == JsonValueKind.Number == is_number && arg0.at(i).ValueKind == JsonValueKind.String == is_string))
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                if (arg0.at(i) < arg0.at(index))
                {
                    index = i;
                }
            }

            return arg0.at(index);
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("min function");
            return s;
        }
    };

    template <class Json>
    class max_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        max_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Array)
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }

            bool is_number = arg0.at(0).ValueKind == JsonValueKind.Number;
            bool is_string = arg0.at(0).ValueKind == JsonValueKind.String;
            if (!is_number && !is_string)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            int index = 0;
            for (int i = 1; i < arg0.size(); ++i)
            {
                if (!(arg0.at(i).ValueKind == JsonValueKind.Number == is_number && arg0.at(i).ValueKind == JsonValueKind.String == is_string))
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                if (arg0.at(i) > arg0.at(index))
                {
                    index = i;
                }
            }

            return arg0.at(index);
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("max function");
            return s;
        }
    };

    template <class Json>
    class abs_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        abs_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            switch (arg0.type())
            {
                case json_type.uint64_value:
                    return arg0;
                case json_type.int64_value:
                {
                    return arg0.template as<int64_t>() >= 0 ? arg0 : value_type(std.abs(arg0.template as<int64_t>()));
                }
                case json_type.double_value:
                {
                    return arg0.template as<double>() >= 0 ? arg0 : value_type(std.abs(arg0.template as<double>()));
                }
                default:
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("abs function");
            return s;
        }
    };

    template <class Json>
    class length_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using string_view_type = typename Json.string_view_type;
        using parameter_type = parameter<Json>;

        length_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            //std.cout << "length function arg: " << arg0 << "\n";

            switch (arg0.type())
            {
                case json_type.object_value:
                case json_type.array_value:
                    return value_type(arg0.size());
                case json_type.string_value:
                {
                    var sv0 = arg0.template as<string_view_type>();
                    var length = unicode_traits.count_codepoints(sv0.data(), sv0.size());
                    return value_type(length);
                }
                default:
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
            }
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("length function");
            return s;
        }
    };

    template <class Json>
    class keys_function : public BaseFunction
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        keys_function()
            : BaseFunction(1)
        {
        }

        public override bool Evaluate(IList<IJsonValue> args, 
                            out IJsonValue result) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            var arg0= args[0].value();
            if (!arg0.ValueKind == JsonValueKind.Object)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            value_type result(json_array_arg);
            result.reserve(args.size());

            for (var& item : arg0.object_range())
            {
                result.emplace_back(item.key());
            }
            return result;
        }

        public override string ToString()
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("keys function");
            return s;
        }
    };

    enum class JsonPathTokenKind
    {
        root_node,
        current_node,
        expression,
        lparen,
        rparen,
        begin_union,
        end_union,
        begin_filter,
        end_filter,
        begin_expression,
        end_index_expression,
        end_argument_expression,
        separator,
        literal,
        selector,
        function,
        end_function,
        argument,
        end_of_expression,
        UnaryOperator,
        BinaryOperator
    };

    inline
    string to_string(JsonPathTokenKind kind)
    {
        switch (kind)
        {
            case JsonPathTokenKind.RootNode:
                return "root_node";
            case JsonPathTokenKind.CurrentNode:
                return "current_node";
            case JsonPathTokenKind.lparen:
                return "lparen";
            case JsonPathTokenKind.rparen:
                return "rparen";
            case JsonPathTokenKind.begin_union:
                return "begin_union";
            case JsonPathTokenKind.end_union:
                return "end_union";
            case JsonPathTokenKind.begin_filter:
                return "begin_filter";
            case JsonPathTokenKind.end_filter:
                return "end_filter";
            case JsonPathTokenKind.begin_expression:
                return "begin_expression";
            case JsonPathTokenKind.end_index_expression:
                return "end_index_expression";
            case JsonPathTokenKind.end_argument_expression:
                return "end_argument_expression";
            case JsonPathTokenKind.Separator:
                return "separator";
            case JsonPathTokenKind.literal:
                return "literal";
            case JsonPathTokenKind.Selector:
                return "selector";
            case JsonPathTokenKind.Function:
                return "function";
            case JsonPathTokenKind.EndFunction:
                return "end_function";
            case JsonPathTokenKind.argument:
                return "argument";
            case JsonPathTokenKind.end_of_expression:
                return "end_of_expression";
            case JsonPathTokenKind.UnaryOperator:
                return "UnaryOperator";
            case JsonPathTokenKind.BinaryOperator:
                return "BinaryOperator";
            default:
                return "";
        }
    }

    template <class Json,class JsonElement>
    struct path_value_pair
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using value_type = Json;
        using reference = JsonElement;
        using value_pointer = typename std.conditional<std.is_const<typename std.remove_reference<JsonElement>.type>.value,typename Json.const_pointer,typename Json.pointer>.type;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;
        using path_pointer = path_node_type*;

        normalized_path_type path_;
        value_pointer value_ptr_;

        path_value_pair(const normalized_path_type& path, reference value) noexcept
            : path_(path), value_ptr_(std.addressof(value))
        {
        }

        path_value_pair(normalized_path_type&& path, value_pointer valp) noexcept
            : path_(std.move(path)), value_ptr_(valp)
        {
        }

        path_value_pair(const path_value_pair&) = default;
        path_value_pair(path_value_pair&& other) = default;
        path_value_pair& operator=(const path_value_pair&) = default;
        path_value_pair& operator=(path_value_pair&& other) = default;

        normalized_path_type path()
        {
            return path_;
        }

        reference value() 
        {
            return *value_ptr_;
        }
    };
 
    template <class Json,class JsonElement>
    struct path_value_pair_less
    {
        bool operator()(const path_value_pair<Json,JsonElement>& lhs,
                        path_value_pair<Json,JsonElement>& rhs) noexcept
        {
            return lhs.path() < rhs.path();
        }
    };

    template <class Json,class JsonElement>
    struct path_value_pair_equal
    {
        bool operator()(const path_value_pair<Json,JsonElement>& lhs,
                        path_value_pair<Json,JsonElement>& rhs) noexcept
        {
            return lhs.path() == rhs.path();
        }
    };

    template <class Json,class JsonElement>
    struct path_stem_value_pair
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using value_type = Json;
        using reference = JsonElement;
        using value_pointer = typename std.conditional<std.is_const<typename std.remove_reference<JsonElement>.type>.value,typename Json.const_pointer,typename Json.pointer>.type;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;
        using path_pointer = path_node_type*;
    private:
        path_node_type* stem_ptr_;
        value_pointer value_ptr_;
    public:
        path_stem_value_pair(const path_node_type& stem, reference value) noexcept
            : stem_ptr_(std.addressof(stem)), value_ptr_(std.addressof(value))
        {
        }

        path_node_type& stem()
        {
            return *stem_ptr_;
        }

        reference value()
        {
            return *value_ptr_;
        }
    };

    template <class Json,class JsonElement>
    class node_accumulator
    {
    public:
        using char_type = typename Json.char_type;
        using reference = JsonElement;
        using path_node_type = path_node<char_type>;

        virtual ~node_accumulator() noexcept = default;

        virtual void accumulate(const path_node_type& path_tail, 
                                reference value) = 0;
    };

    template <class Json,class JsonElement>
    class path_value_accumulator : public node_accumulator<Json,JsonElement>
    {
    public:
        using reference = JsonElement;
        using char_type = typename Json.char_type;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;
        using path_value_pair_type = path_value_pair<Json,JsonElement>;

        std.vector<path_value_pair_type> nodes;

        void accumulate(const path_node_type& path_tail, 
                        reference value) override
        {
            nodes.emplace_back(normalized_path_type(path_tail), std.addressof(value));
        }
    };

    template <class Json,class JsonElement>
    class path_stem_value_accumulator : public node_accumulator<Json,JsonElement>
    {
    public:
        using reference = JsonElement;
        using char_type = typename Json.char_type;
        using path_node_type = path_node<char_type>;
        using path_stem_value_pair_type = path_stem_value_pair<Json,JsonElement>;

        std.vector<path_stem_value_pair_type> nodes;

        void accumulate(const path_node_type& path_tail, 
                        reference value) override
        {
            nodes.emplace_back(path_tail, value);
        }
    };

    template <class Json, class JsonElement>
    class dynamic_resources
    {
        using path_node_type = path_node<typename Json.char_type>;
        using path_stem_value_pair_type = path_stem_value_pair<Json,JsonElement>;
        std.vector<std.unique_ptr<Json>> temp_json_values_;
        std.vector<std.unique_ptr<path_node_type>> temp_path_node_values_;
        std.unordered_map<int,std.pair<std.vector<path_stem_value_pair_type>,node_kind>> cache_;
        using node_accumulator_type = node_accumulator<Json,JsonElement>;
    public:

        bool is_cached(int id)
        {
            return cache_.find(id) != cache_.end();
        }

        void add_to_cache(int id, std.vector<path_stem_value_pair_type>& val, node_kind ndtype) 
        {
            cache_.emplace(id,std.make_pair(val,ndtype));
        }

        void add_to_cache(int id, std.vector<path_stem_value_pair_type>&& val, node_kind ndtype) 
        {
            cache_.emplace(id,std.make_pair(std.forward<std.vector<path_stem_value_pair_type>>(val),ndtype));
        }

        void retrieve_from_cache(int id, node_accumulator_type& accumulator, node_kind& ndtype) 
        {
            var it = cache_.find(id);
            if (it != cache_.end())
            {
                for (var& item : it.second.first)
                {
                    accumulator.accumulate(item.stem(), item.value());
                }
                ndtype = it.second.second;
            }
        }

        template <typename... Args>
        Json* new_json(Args&& ... args)
        {
            var temp = jsoncons.make_unique<Json>(std.forward<Args>(args)...);
            Json* ptr = temp.get();
            temp_json_values_.emplace_back(std.move(temp));
            return ptr;
        }

        path_node_type& root_path_node()
        {
            static path_node_type root('$');
            return root;
        }

        path_node_type& current_path_node()
        {
            static path_node_type root('@');
            return root;
        }

        template <typename... Args>
        path_node_type* new_path_node(Args&& ... args)
        {
            var temp = jsoncons.make_unique<path_node_type>(std.forward<Args>(args)...);
            path_node_type* ptr = temp.get();
            temp_path_node_values_.emplace_back(std.move(temp));
            return ptr;
        }
    };

    template <class Json,class JsonElement>
    struct node_less
    {
        bool operator()(const path_value_pair<Json,JsonElement>& a, path_value_pair<Json,JsonElement>& b)
        {
            return *(a.ptr) < *(b.ptr);
        }
    };

    template <class Json,class JsonElement>
    class jsonpath_selector
    {
        bool is_path_;
        int _precedenceLevel;

    public:
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using string_view_type = jsoncons.basic_string_view<char_type, std.char_traits<char_type>>;
        using value_type = Json;
        using reference = JsonElement;
        using pointer = typename std.conditional<std.is_const<typename std.remove_reference<JsonElement>.type>.value,typename Json.const_pointer,typename Json.pointer>.type;
        using path_value_pair_type = path_value_pair<Json,JsonElement>;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;
        using node_accumulator_type = node_accumulator<Json,JsonElement>;
        using selector_type = jsonpath_selector<Json,JsonElement>;

        jsonpath_selector(bool is_path,
                          int precedenceLevel = 0)
            : is_path_(is_path), 
              _precedenceLevel(precedenceLevel)
        {
        }

        virtual ~jsonpath_selector() noexcept = default;

        bool is_path() 
        {
            return is_path_;
        }

        int PrecedenceLevel()
        {
            return _precedenceLevel;
        }

        bool IsRightAssociative
        {
            return true;
        }

        virtual void select(dynamic_resources<Json,JsonElement>& resources,
                            reference root,
                            path_node_type& pathTail, 
                            reference val, 
                            node_accumulator_type& accumulator,
                            node_kind& ndtype,
                            result_options options) = 0;

        virtual void append_selector(jsonpath_selector*) 
        {
        }

        virtual string to_string(int = 0)
        {
            return string();
        }
    };

    template <class Json, class JsonElement>
    struct Resources
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;
        using value_type = Json;
        using reference = JsonElement;
        using function_base_type = BaseFunction;
        using selector_type = jsonpath_selector<Json,JsonElement>;

        std.vector<std.unique_ptr<selector_type>> selectors_;
        std.vector<std.unique_ptr<Json>> temp_json_values_;
        std.vector<std.unique_ptr<UnaryOperator<Json,JsonElement>>> UnaryOperators_;
        std.unordered_map<string_type,std.unique_ptr<function_base_type>> custom_functions_;

        Resources()
        {
        }

        Resources(const custom_functions<Json>& functions)
        {
            for (const var& item : functions)
            {
                custom_functions_.emplace(item.name(),
                                          jsoncons.make_unique<decorator_function<Json>>(item.arity(),item.function()));
            }
        }

        Resources(const Resources&) = default;

        Resources(Resources&& other) noexcept 
            : selectors_(std.move(other.selectors_)),
              temp_json_values_(std.move(other.temp_json_values_)),
              UnaryOperators_(std.move(other.UnaryOperators_)),
              custom_functions_(std.move(other.custom_functions_))
        {
        }

        function_base_type* get_function(const string_type& name, std.error_code& ec)
        {
            static abs_function<Json> abs_func;
            static ContainsFunction<Json> contains_func;
            static starts_with_function<Json> starts_with_func;
            static ends_with_function<Json> ends_with_func;
            static ceil_function<Json> ceil_func;
            static floor_function<Json> floor_func;
            static to_number_function<Json> to_number_func;
            static sum_function<Json> sum_func;
            static prod_function<Json> prod_func;
            static avg_function<Json> avg_func;
            static min_function<Json> min_func;
            static max_function<Json> max_func;
            static length_function<Json> length_func;
            static keys_function<Json> keys_func;
#if defined(JSONCONS_HAS_STD_REGEX)
            static tokenize_function<Json> tokenize_func;
#endif

            static std.unordered_map<string_type,const function_base_type*> functions =
            {
                {string_type{'a','b','s'}, &abs_func},
                {string_type{'c','o','n','t','a','i','n','s'}, &contains_func},
                {string_type{'s','t','a','r','t','s','_','w','i','t','h'}, &starts_with_func},
                {string_type{'e','n','d','s','_','w','i','t','h'}, &ends_with_func},
                {string_type{'c','e','i','l'}, &ceil_func},
                {string_type{'f','l','o','o','r'}, &floor_func},
                {string_type{'t','o','_','n','u','m','b','e','r'}, &to_number_func},
                {string_type{'s','u','m'}, &sum_func},
                {string_type{'p','r','o', 'd'}, &prod_func},
                {string_type{'a','v','g'}, &avg_func},
                {string_type{'m','i','n'}, &min_func},
                {string_type{'m','a','x'}, &max_func},
                {string_type{'l','e','n','g','t','h'}, &length_func},
                {string_type{'k','e','y','s'}, &keys_func},
#if defined(JSONCONS_HAS_STD_REGEX)
                {string_type{'t','o','k','e','n','i','z','e'}, &tokenize_func},
#endif
                {string_type{'c','o','u','n','t'}, &length_func}
            };

            var it = functions.find(name);
            if (it == functions.end())
            {
                var it2 = custom_functions_.find(name);
                if (it2 == custom_functions_.end())
                {
                    ec = jsonpath_errc.unknown_function;
                    return nullptr;
                }
                else
                {
                    return it2.second.get();
                }
            }
            else
            {
                return it.second;
            }
        }

        UnaryOperator get_unary_not()
        {
            static NotOperator<Json,JsonElement> oper;
            return &oper;
        }

        UnaryOperator get_unary_minus()
        {
            static UnaryMinusOperator<Json,JsonElement> oper;
            return &oper;
        }

        UnaryOperator GetRegexOperator(std.basic_regex<char_type>&& pattern) 
        {
            UnaryOperators_.push_back(jsoncons.make_unique<RegexOperator<Json,JsonElement>>(std.move(pattern)));
            return UnaryOperators_.back().get();
        }

        BinaryOperator GetOrOperator()
        {
            static OrOperator<Json,JsonElement> oper;

            return &oper;
        }

        BinaryOperator GetAndOperator()
        {
            static AndOperator<Json,JsonElement> oper;

            return &oper;
        }

        BinaryOperator GetEqOperator()
        {
            static EqOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetNeOperator()
        {
            static NeOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetLtOperator()
        {
            static LtOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetLeOperator()
        {
            static LteOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetGtOperator()
        {
            static GtOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetGteOperator()
        {
            static GteOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetPlusOperator()
        {
            static PlusOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetMinusOperator()
        {
            static MinusOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetMuLtOperator()
        {
            static MultOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetDivOperator()
        {
            static DivOperator<Json,JsonElement> oper;
            return &oper;
        }

        template <typename T>
        selector_type* new_selector(T&& val)
        {
            selectors_.emplace_back(jsoncons.make_unique<T>(std.forward<T>(val)));
            return selectors_.back().get();
        }

        template <typename... Args>
        Json* new_json(Args&& ... args)
        {
            var temp = jsoncons.make_unique<Json>(std.forward<Args>(args)...);
            Json* ptr = temp.get();
            temp_json_values_.emplace_back(std.move(temp));
            return ptr;
        }
    };

    template <class Json, class JsonElement>
    class expression_base
    {
    public:
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using string_view_type = jsoncons.basic_string_view<char_type, std.char_traits<char_type>>;
        using value_type = Json;
        using reference = JsonElement;
        using pointer = typename std.conditional<std.is_const<typename std.remove_reference<JsonElement>.type>.value,typename Json.const_pointer,typename Json.pointer>.type;
        using path_value_pair_type = path_value_pair<Json,JsonElement>;
        using path_node_type = path_node<char_type>;

        virtual ~expression_base() noexcept = default;

        virtual value_type evaluate_single(dynamic_resources<Json,JsonElement>& resources,
                                           reference root,
                                           path_node_type& path, 
                                           reference val, 
                                           result_options options,
                                           std.error_code& ec) = 0;

        virtual string to_string(int level = 0) = 0;
    };

    template <class Json,class JsonElement>
    class Token
    {
    public:
        using selector_type = jsonpath_selector<Json,JsonElement>;
        using expression_base_type = expression_base<Json,JsonElement>;

        JsonPathTokenKind _type;

        union
        {
            selector_type* _selector;
            std.unique_ptr<expression_base_type> expression_;
            UnaryOperator _unaryOperator;;
            BinaryOperator _binaryOperator;
            BaseFunction* function_;
            Json value_;
        };
    public:

        Token(UnaryOperator expr) noexcept
            : _type(JsonPathTokenKind.UnaryOperator),
              _unaryOperator;(expr)
        {
        }

        Token(BinaryOperator expr) noexcept
            : _type(JsonPathTokenKind.BinaryOperator),
              _binaryOperator(expr)
        {
        }

        Token(current_node_arg_t) noexcept
            : _type(JsonPathTokenKind.CurrentNode)
        {
        }

        Token(root_node_arg_t) noexcept
            : _type(JsonPathTokenKind.RootNode)
        {
        }

        Token(end_function_arg_t) noexcept
            : _type(JsonPathTokenKind.EndFunction)
        {
        }

        Token(separator_arg_t) noexcept
            : _type(JsonPathTokenKind.Separator)
        {
        }

        Token(lparen_arg_t) noexcept
            : _type(JsonPathTokenKind.lparen)
        {
        }

        Token(rparen_arg_t) noexcept
            : _type(JsonPathTokenKind.rparen)
        {
        }

        Token(end_of_expression_arg_t) noexcept
            : _type(JsonPathTokenKind.end_of_expression)
        {
        }

        Token(begin_union_arg_t) noexcept
            : _type(JsonPathTokenKind.begin_union)
        {
        }

        Token(end_union_arg_t) noexcept
            : _type(JsonPathTokenKind.end_union)
        {
        }

        Token(begin_filter_arg_t) noexcept
            : _type(JsonPathTokenKind.begin_filter)
        {
        }

        Token(end_filter_arg_t) noexcept
            : _type(JsonPathTokenKind.end_filter)
        {
        }

        Token(begin_expression_arg_t) noexcept
            : _type(JsonPathTokenKind.begin_expression)
        {
        }

        Token(end_index_expression_arg_t) noexcept
            : _type(JsonPathTokenKind.end_index_expression)
        {
        }

        Token(end_argument_expression_arg_t) noexcept
            : _type(JsonPathTokenKind.end_argument_expression)
        {
        }

        Token(selector_type* selector)
            : _type(JsonPathTokenKind.Selector), _selector(selector)
        {
        }

        Token(std.unique_ptr<expression_base_type>&& expr)
            : _type(JsonPathTokenKind.expression)
        {
            new (&expression_) std.unique_ptr<expression_base_type>(std.move(expr));
        }

        Token(const BaseFunction* function) noexcept
            : _type(JsonPathTokenKind.Function),
              function_(function)
        {
        }

        Token(argument_arg_t) noexcept
            : _type(JsonPathTokenKind.argument)
        {
        }

        Token(literal_arg_t, Json&& value) noexcept
            : _type(JsonPathTokenKind.literal), value_(std.move(value))
        {
        }

        Token(Token&& other) noexcept
        {
           ruct(std.forward<Token>(other));
        }

        Json& get_value(const_reference_arg_t, dynamic_resources<Json,JsonElement>&)
        {
            return value_;
        }

        Json& get_value(reference_arg_t, dynamic_resources<Json,JsonElement>& resources)
        {
            return *resources.new_json(value_);
        }

        Token& operator=(Token&& other)
        {
            if (&other != this)
            {
                if (_type == other._type)
                {
                    switch (_type)
                    {
                        case JsonPathTokenKind.Selector:
                            _selector = other._selector;
                            break;
                        case JsonPathTokenKind.expression:
                            expression_ = std.move(other.expression_);
                            break;
                        case JsonPathTokenKind.UnaryOperator:
                            _unaryOperator; = other._unaryOperator;;
                            break;
                        case JsonPathTokenKind.BinaryOperator:
                            _binaryOperator = other._binaryOperator;
                            break;
                        case JsonPathTokenKind.Function:
                            function_ = other.function_;
                            break;
                        case JsonPathTokenKind.literal:
                            value_ = std.move(other.value_);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    destroy();
                   ruct(std.forward<Token>(other));
                }
            }
            return *this;
        }

        ~Token() noexcept
        {
            destroy();
        }

        JsonPathTokenKind type()
        {
            return _type;
        }

        bool IsLeftParen
        {
            return _type == JsonPathTokenKind.lparen; 
        }

        bool IsRightParen
        {
            return _type == JsonPathTokenKind.rparen; 
        }

        bool IsCurrentNode
        {
            return _type == JsonPathTokenKind.CurrentNode; 
        }

        bool is_path()
        {
            return _type == JsonPathTokenKind.Selector && _selector.is_path(); 
        }

        bool IsOperator
        {
            return _type == JsonPathTokenKind.UnaryOperator || 
                   _type == JsonPathTokenKind.BinaryOperator; 
        }

        int PrecedenceLevel
        {
            switch(_type)
            {
                case JsonPathTokenKind.Selector:
                    return _selector.PrecedenceLevel;
                case JsonPathTokenKind.UnaryOperator:
                    return _unaryOperator;.PrecedenceLevel;
                case JsonPathTokenKind.BinaryOperator:
                    return _binaryOperator.PrecedenceLevel;
                default:
                    return 0;
            }
        }

        jsoncons.optional<int> arity()
        {
            return _type == JsonPathTokenKind.Function ? function_.arity() : jsoncons.optional<int>();
        }

        bool IsRightAssociative
        {
            switch(_type)
            {
                case JsonPathTokenKind.Selector:
                    return _selector.IsRightAssociative;
                case JsonPathTokenKind.UnaryOperator:
                    return _unaryOperator;.IsRightAssociative;
                case JsonPathTokenKind.BinaryOperator:
                    return _binaryOperator.IsRightAssociative;
                default:
                    return false;
            }
        }

        voidruct(Token&& other)
        {
            _type = other._type;
            switch (_type)
            {
                case JsonPathTokenKind.Selector:
                    _selector = other._selector;
                    break;
                case JsonPathTokenKind.expression:
                    new (&expression_) std.unique_ptr<expression_base_type>(std.move(other.expression_));
                    break;
                case JsonPathTokenKind.UnaryOperator:
                    _unaryOperator; = other._unaryOperator;;
                    break;
                case JsonPathTokenKind.BinaryOperator:
                    _binaryOperator = other._binaryOperator;
                    break;
                case JsonPathTokenKind.Function:
                    function_ = other.function_;
                    break;
                case JsonPathTokenKind.literal:
                    new (&value_) Json(std.move(other.value_));
                    break;
                default:
                    break;
            }
        }

        void destroy() noexcept 
        {
            switch(_type)
            {
                case JsonPathTokenKind.expression:
                    expression_.~unique_ptr();
                    break;
                case JsonPathTokenKind.literal:
                    value_.~Json();
                    break;
                default:
                    break;
            }
        }

        string to_string(int level = 0)
        {
            string s;
            switch (_type)
            {
                case JsonPathTokenKind.RootNode:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("root node");
                    break;
                case JsonPathTokenKind.CurrentNode:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("current node");
                    break;
                case JsonPathTokenKind.argument:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("argument");
                    break;
                case JsonPathTokenKind.Selector:
                    s.append(_selector.to_string(level));
                    break;
                case JsonPathTokenKind.expression:
                    s.append(expression_.to_string(level));
                    break;
                case JsonPathTokenKind.literal:
                {
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    var sbuf = value_.to_string();
                    unicode_traits.convert(sbuf.data(), sbuf.size(), s);
                    break;
                }
                case JsonPathTokenKind.BinaryOperator:
                    s.append(_binaryOperator.to_string(level));
                    break;
                case JsonPathTokenKind.Function:
                    s.append(function_.to_string(level));
                    break;
                default:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("Token kind: ");
                    s.append(jsoncons.jsonpath.detail.to_string(_type));
                    break;
            }
            //s.append("\n");
            return s;
        }
    };

    template <class Callback, class Json,class JsonElement>
    class callback_accumulator : public node_accumulator<Json,JsonElement>
    {
        Callback& callback_;
    public:
        using reference = JsonElement;
        using char_type = typename Json.char_type;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;

        callback_accumulator(Callback& callback)
            : callback_(callback)
        {
        }

        void accumulate(const path_node_type& path_tail, 
                        reference value) override
        {
            callback_(normalized_path_type(path_tail), value);
        }
    };

    template <class Json,class JsonElement>
    class path_expression
    {
    public:
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using string_view_type = typename Json.string_view_type;
        using path_value_pair_type = path_value_pair<Json,JsonElement>;
        using path_value_pair_less_type = path_value_pair_less<Json,JsonElement>;
        using path_value_pair_equal_type = path_value_pair_equal<Json,JsonElement>;
        using value_type = Json;
        using reference = typename path_value_pair_type.reference;
        using pointer = typename path_value_pair_type.value_pointer;
        using token_type = Token<Json,JsonElement>;
        using reference_arg_type = typename std.conditional<std.is_const<typename std.remove_reference<JsonElement>.type>.value,
           _reference_arg_t,reference_arg_t>.type;
        using path_node_type = path_node<char_type>;
        using normalized_path_type = normalized_path<char_type>;
        using selector_type = jsonpath_selector<Json,JsonElement>;
    private:
        selector_type* _selector;
    public:

        path_expression()
        {
        }

        path_expression(path_expression&& expr)
            : _selector(expr._selector)
        {
        }

        path_expression(selector_type* selector)
            : _selector(selector)
        {
        }

        path_expression& operator=(path_expression&& expr) = default;

        JsonElement Evaluate(dynamic_resources<Json,JsonElement>& resources, 
                      reference root,
                      path_node_type& path, 
                      reference instance,
                      result_options options)
        {
            Json result(json_array_arg);

            if ((options & result_options.path) == result_options.path)
            {
                var callback = [&result](const normalized_path_type& path, reference)
                {
                    result.emplace_back(path.to_string());
                };
                Evaluate(resources, root, path, instance, callback, options);
            }
            else
            {
                var callback = [&result](const normalized_path_type&, reference val)
                {
                    result.push_back(val);
                };
                Evaluate(resources, root, path, instance, callback, options);
            }

            return result;
        }

        template <class Callback>
        typename std.enable_if<type_traits.is_binary_function_object<Callback,const normalized_path_type&,reference>.value,void>.type
        Evaluate(dynamic_resources<Json,JsonElement>& resources, 
                 reference root,
                 path_node_type& path, 
                 reference current, 
                 Callback callback,
                 result_options options)
        {
            std.error_code ec;

            node_kind ndtype = node_kind();

            result_options require_more = result_options.nodups | result_options.sort;

            if ((options & require_more) != result_options())
            {
                path_value_accumulator<Json,JsonElement> accumulator;
                _selector.select(resources, root, path, current, accumulator, ndtype, options);

                if (accumulator.nodes.size() > 1 && (options & result_options.sort) == result_options.sort)
                {
                    std.sort(accumulator.nodes.begin(), accumulator.nodes.end(), path_value_pair_less_type());
                }

                if (accumulator.nodes.size() > 1 && (options & result_options.nodups) == result_options.nodups)
                {
                    if ((options & result_options.sort) == result_options.sort)
                    {
                        var last = std.unique(accumulator.nodes.begin(),accumulator.nodes.end(),path_value_pair_equal_type());
                        accumulator.nodes.erase(last,accumulator.nodes.end());
                        for (var& node : accumulator.nodes)
                        {
                            callback(node.path(), node.value());
                        }
                    }
                    else
                    {
                        std.vector<path_value_pair_type> index(accumulator.nodes);
                        std.sort(index.begin(), index.end(), path_value_pair_less_type());
                        var last = std.unique(index.begin(),index.end(),path_value_pair_equal_type());
                        index.erase(last,index.end());

                        std.vector<path_value_pair_type> temp2;
                        temp2.reserve(index.size());
                        for (var&& node : accumulator.nodes)
                        {
                            var it = std.lower_bound(index.begin(),index.end(),node, path_value_pair_less_type());
                            if (it != index.end() && it.path() == node.path()) 
                            {
                                temp2.emplace_back(std.move(node));
                                index.erase(it);
                            }
                        }
                        for (var& node : temp2)
                        {
                            callback(node.path(), node.value());
                        }
                    }
                }
                else
                {
                    for (var& node : accumulator.nodes)
                    {
                        callback(node.path(), node.value());
                    }
                }
            }
            else
            {
                callback_accumulator<Callback,Json,JsonElement> accumulator(callback);
                _selector.select(resources, root, path, current, accumulator, ndtype, options);
            }
        }

        string to_string(int level)
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("expression ");
            s.append(_selector.to_string(level+1));

            return s;

        }
    };

    template <class Json,class JsonElement>
    class expression
    {
    public:
        using path_value_pair_type = path_value_pair<Json,JsonElement>;
        using value_type = Json;
        using reference = typename path_value_pair_type.reference;
        using pointer = typename path_value_pair_type.value_pointer;
        using_pointer = value_type*;
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type,std.char_traits<char_type>>;
        using string_view_type = typename Json.string_view_type;
        using path_value_pair_less_type = path_value_pair_less<Json,reference>;
        using path_value_pair_equal_type = path_value_pair_equal<Json,reference>;
        using parameter_type = parameter<Json>;
        using token_type = Token<Json,reference>;
        using reference_arg_type = typename std.conditional<std.is_const<typename std.remove_reference<reference>.type>.value,
           _reference_arg_t,reference_arg_t>.type;
        using path_node_type = path_node<char_type>;
        using stack_item_type = value_or_pointer<Json,JsonElement>;
    private:
        std.vector<token_type> _tokens;
    public:

        expression()
        {
        }

        expression(expression&& expr)
            : _tokens(std.move(expr._tokens))
        {
        }

        expression(std.vector<token_type>&& token_stack)
            : _tokens(std.move(token_stack))
        {
        }

        expression& operator=(expression&& expr) = default;

        value_type evaluate_single(dynamic_resources<Json,reference>& resources, 
                                   reference root,
                                   reference current,
                                   result_options options,
                                   std.error_code& ec)
        {
            std.vector<stack_item_type> stack;
            std.vector<parameter_type> arg_stack;

            //std.cout << "EVALUATE TOKENS\n";
            //for (var& tok : _tokens)
            //{
            //    std.cout << tok.to_string() << "\n";
            //}
            //std.cout << "\n";

            if (!_tokens.empty())
            {
                for (var& token : _tokens)
                {
                    //std.cout << "Token: " << token.to_string() << "\n";
                    switch (token.type())
                    { 
                        case JsonPathTokenKind.Value:
                        {
                            stack.emplace_back(std.addressof(token.get_value(reference_arg_type(), resources)));
                            break;
                        }
                        case JsonPathTokenKind.UnaryOperator:
                        {
                            Debug.Assert(stack.Count >= 1);
                            var item = stack.Peek();
                            stack.Pop();

                            var val = token._unaryOperator.Evaluate(item.GetValue(), ec);
                            stack.Push(val);
                            break;
                        }
                        case JsonPathTokenKind.BinaryOperator:
                        {
                            //std.cout << "binary operator: " << stack.Count << "\n";
                            Debug.Assert(stack.Count >= 2);
                            var rhs = stack.Peek();
                            //std.cout << "rhs: " << *rhs << "\n";
                            stack.Pop();
                            var lhs = stack.Peek();
                            //std.cout << "lhs: " << *lhs << "\n";
                            stack.Pop();

                            var val = token._binaryOperator.Evaluate(lhs.GetValue(), rhs.GetValue(), ec);
                            //std.cout << "Evaluate binary expression: " << r << "\n";
                            stack.Push(val);
                            break;
                        }
                        case JsonPathTokenKind.RootNode:
                            //std.cout << "root: " << root << "\n";
                            stack.Push(root);
                            break;
                        case JsonPathTokenKind.CurrentNode:
                            //std.cout << "current: " << current << "\n";
                            stack.Push(current);
                            break;
                        case JsonPathTokenKind.argument:
                            Debug.Assert(!stack.Count == 0);
                            //std.cout << "argument stack items " << stack.Count << "\n";
                            //for (var& item : stack)
                            //{
                            //    std.cout << *item.to_pointer(resources) << "\n";
                            //}
                            //std.cout << "\n";
                            arg_stack.emplace_back(stack.Peek());
                            //for (var& item : arg_stack)
                            //{
                            //    std.cout << *item << "\n";
                            //}
                            //std.cout << "\n";
                            stack.Pop();
                            break;
                        case JsonPathTokenKind.Function:
                        {
                            if (token.function_.arity() && *(token.function_.arity()) != arg_stack.size())
                            {
                                ec = jsonpath_errc.invalid_arity;
                                return JsonConstants.Null;
                            }
                            //std.cout << "function arg stack:\n";
                            //for (var& item : arg_stack)
                            //{
                            //    std.cout << *item << "\n";
                            //}
                            //std.cout << "\n";

                            value_type val = token.function_.Evaluate(arg_stack, ec);
                            if (ec)
                            {
                                return JsonConstants.Null;
                            }
                            //std.cout << "function result: " << val << "\n";
                            arg_stack.clear();
                            stack.Push(val);
                            break;
                        }
                        case JsonPathTokenKind.expression:
                        {
                            if (stack.Count == 0)
                            {
                                stack.Push(current);
                            }

                            var item = stack.Peek();
                            stack.Pop();
                            value_type val = token.expression_.evaluate_single(resources, root, resources.current_path_node(), item.GetValue(), options, ec);
                            //std.cout << "ref2: " << ref << "\n";
                            stack.Push(val);
                            break;
                        }
                        case JsonPathTokenKind.Selector:
                        {
                            if (stack.Count == 0)
                            {
                                stack.Push(current);
                            }

                            var item = stack.Peek();
                            //for (var& item : stack)
                            //{
                                //std.cout << "selector stack input:\n";
                                //switch (item.tag)
                                //{
                                //    case node_set_tag.single:
                                //        std.cout << "single: " << *(item.node.ptr) << "\n";
                                //        break;
                                //    case node_set_tag.multi:
                                //        for (var& node : stack.back().ptr().nodes)
                                //        {
                                //            std.cout << "multi: " << *node.ptr << "\n";
                                //        }
                                //        break;
                                //    default:
                                //        break;
                            //}
                            //std.cout << "\n";
                            //}
                            //std.cout << "selector item: " << *ptr << "\n";
                            stack.Pop();
                            node_kind ndtype = node_kind();
                            path_value_accumulator<Json,JsonElement> accumulator;
                            token.GetSelector().Select(resources, root, resources.current_path_node(), item.GetValue(), accumulator, ndtype, options);
                            
                            if ((options & result_options.sort) == result_options.sort)
                            {
                                std.sort(accumulator.nodes.begin(), accumulator.nodes.end(), path_value_pair_less_type());
                            }

                            if ((options & result_options.nodups) == result_options.nodups)
                            {
                                if ((options & result_options.sort) == result_options.sort)
                                {
                                    var last = std.unique(accumulator.nodes.begin(),accumulator.nodes.end(),path_value_pair_equal_type());
                                    accumulator.nodes.erase(last,accumulator.nodes.end());
                                    stack.Push(nodes_to_stack_item(accumulator.nodes, ndtype));
                                }
                                else
                                {
                                    std.vector<path_value_pair_type> index(accumulator.nodes);
                                    std.sort(index.begin(), index.end(), path_value_pair_less_type());
                                    var last = std.unique(index.begin(),index.end(),path_value_pair_equal_type());
                                    index.erase(last,index.end());

                                    std.vector<path_value_pair_type> temp2;
                                    temp2.reserve(index.size());
                                    for (var&& node : accumulator.nodes)
                                    {
                                        //std.cout << "node: " << node.path << ", " << *node.ptr << "\n";
                                        var it = std.lower_bound(index.begin(),index.end(),node, path_value_pair_less_type());

                                        if (it != index.end() && it.path() == node.path()) 
                                        {
                                            temp2.emplace_back(std.move(node));
                                            index.erase(it);
                                        }
                                    }
                                    stack.Push(nodes_to_stack_item(temp2, ndtype));
                                }
                            }
                            else
                            {
                                //std.cout << "selector output " << accumulator.nodes.size() << "\n";
                                //for (var& item : accumulator.nodes)
                                //{
                                //    std.cout << *item.ptr << "\n";
                                //}
                                //std.cout << "\n";
                                stack.Push(nodes_to_stack_item(accumulator.nodes, ndtype));
                            }

                            
                            break;
                        }
                        default:
                            break;
                    }
                }
            }

            //if (stack.Count != 1)
            //{
            //    std.cout << "Stack size: " << stack.Count << "\n";
            //}
            return stack.Count == 0 ? JsonConstants.Null : stack.back().GetValue();
        }
 
        string to_string(int level)
        {
            string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("expression ");
            for (const var& item : _tokens)
            {
                s.append(item.to_string(level+1));
            }

            return s;

        }
    private:
        static stack_item_type nodes_to_stack_item(std.vector<path_value_pair_type>& nodes, node_kind tag)
        {
            if (nodes.empty())
            {
                return stack_item_type(Json(null_type()));
            }
            else if (nodes.size() == 1 && (tag == node_kind.single || tag == node_kind()))
            {
                return stack_item_type(nodes.back().value_ptr_);
            }
            else
            {
                Json j(json_array_arg);
                j.reserve(nodes.size());
                for (var& item : nodes)
                {
                    j.emplace_back(item.GetValue());
                }
                return stack_item_type(std.move(j));
            }
        }
    };

} // namespace detail
} // namespace jsonpath
} // namespace jsoncons

#endif // JSONCONS_JSONPATH_JSONPATH_EXPRESSION_HPP
