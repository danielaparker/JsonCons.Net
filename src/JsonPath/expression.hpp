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
        bool IsRightAssociative()
        {
            return _isRightAssociative;
        }

        virtual Json Evaluate(JsonElement, 
                              std.error_code&) = 0;
    };

    template <class Json>
    bool is_false(const Json& val)
    {
        return ((val.is_array() && val.empty()) ||
                 (val.is_object() && val.empty()) ||
                 (val.is_string() && val.as_string_view().empty()) ||
                 (val.is_bool() && !val.as_bool()) ||
                 (val.is_number() && (val == Json(0))) ||
                 val.ValueKind == JsonValueKind.Null);
    }

    template <class Json>
    bool is_true(const Json& val)
    {
        return !is_false(val);
    }

    template <class Json,class JsonElement>
    class unary_notOperator final : public UnaryOperator<Json,JsonElement>
    {
    public:
        unary_notOperator()
            : UnaryOperator<Json,JsonElement>(1, true)
        {}

        Json Evaluate(JsonElement val, 
                      std.error_code&) override
        {
            return is_false(val) ? Json(true) : Json(false);
        }
    };

    template <class Json,class JsonElement>
    class unary_minusOperator final : public UnaryOperator<Json,JsonElement>
    {
    public:
        unary_minusOperator()
            : UnaryOperator<Json,JsonElement>(1, true)
        {}

        Json Evaluate(JsonElement val, 
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
                return Json.null();
            }
        }
    };

    template <class Json,class JsonElement>
    class regexOperator final : public UnaryOperator<Json,JsonElement>
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;
        std.basic_regex<char_type> pattern_;
    public:
        regexOperator(std.basic_regex<char_type>&& pattern)
            : UnaryOperator<Json,JsonElement>(2, true),
              pattern_(std.move(pattern))
        {
        }

        regexOperator(regexOperator&&) = default;
        regexOperator& operator=(regexOperator&&) = default;

        Json Evaluate(JsonElement val, 
                             std.error_code&) override
        {
            if (!val.is_string())
            {
                return Json.null();
            }
            return std.regex_search(val.as_string(), pattern_) ? Json(true) : Json(false);
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
        bool IsRightAssociative()
        {
            return _isRightAssociative;
        }

        virtual Json Evaluate(JsonElement, 
                             JsonElement);
    };

    // Implementations

    template <class Json,class JsonElement>
    class OrOperator final : BinaryOperator
    {
    public:
        OrOperator()
            : BinaryOperator(9)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.ValueKind == JsonValueKind.Null && rhs.ValueKind == JsonValueKind.Null)
            {
                return Json.null();
            }
            if (!is_false(lhs))
            {
                return lhs;
            }
            else
            {
                return rhs;
            }
        }
        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class andOperator final : BinaryOperator
    {
    public:
        andOperator()
            : BinaryOperator(8)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (is_true(lhs))
            {
                return rhs;
            }
            else
            {
                return lhs;
            }
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class EqOperator final : BinaryOperator
    {
    public:
        EqOperator()
            : BinaryOperator(6)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            return lhs == rhs ? Json(true) : Json(false);
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class neOperator final : BinaryOperator
    {
    public:
        neOperator()
            : BinaryOperator(6)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            return lhs != rhs ? Json(true) : Json(false);
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class ltOperator final : BinaryOperator
    {
    public:
        ltOperator()
            : BinaryOperator(5)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            if (lhs.is_number() && rhs.is_number())
            {
                return lhs < rhs ? Json(true) : Json(false);
            }
            else if (lhs.is_string() && rhs.is_string())
            {
                return lhs < rhs ? Json(true) : Json(false);
            }
            return Json.null();
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class lteOperator final : BinaryOperator
    {
    public:
        lteOperator()
            : BinaryOperator(5)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs) 
        {
            if (lhs.is_number() && rhs.is_number())
            {
                return lhs <= rhs ? Json(true) : Json(false);
            }
            else if (lhs.is_string() && rhs.is_string())
            {
                return lhs <= rhs ? Json(true) : Json(false);
            }
            return Json.null();
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class gtOperator final : BinaryOperator
    {
    public:
        gtOperator()
            : BinaryOperator(5)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            //std.cout << "operator> lhs: " << lhs << ", rhs: " << rhs << "\n";

            if (lhs.is_number() && rhs.is_number())
            {
                return lhs > rhs ? Json(true) : Json(false);
            }
            else if (lhs.is_string() && rhs.is_string())
            {
                return lhs > rhs ? Json(true) : Json(false);
            }
            return Json.null();
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class gteOperator final : BinaryOperator
    {
    public:
        gteOperator()
            : BinaryOperator(5)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.is_number() && rhs.is_number())
            {
                return lhs >= rhs ? Json(true) : Json(false);
            }
            else if (lhs.is_string() && rhs.is_string())
            {
                return lhs >= rhs ? Json(true) : Json(false);
            }
            return Json.null();
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class plusOperator final : BinaryOperator
    {
    public:
        plusOperator()
            : BinaryOperator(4)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.is_number() && rhs.is_number()))
            {
                return Json.null();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class minusOperator final : BinaryOperator
    {
    public:
        minusOperator()
            : BinaryOperator(4)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.is_number() && rhs.is_number()))
            {
                return Json.null();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class multOperator final : BinaryOperator
    {
    public:
        multOperator()
            : BinaryOperator(3)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            if (!(lhs.is_number() && rhs.is_number()))
            {
                return Json.null();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class divOperator final : BinaryOperator
    {
    public:
        divOperator()
            : BinaryOperator(3)
        {
        }

        Json Evaluate(JsonElement lhs, JsonElement rhs)
        {
            //std.cout << "operator/ lhs: " << lhs << ", rhs: " << rhs << "\n";

            if (!(lhs.is_number() && rhs.is_number()))
            {
                return Json.null();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class function_base
    {
        jsoncons.optional<int> arg_count_;
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        function_base(jsoncons.optional<int> arg_count)
            : arg_count_(arg_count)
        {
        }

        virtual ~function_base() noexcept = default;

        jsoncons.optional<int> arity()
        {
            return arg_count_;
        }

        virtual value_type Evaluate(const std.vector<parameter_type>& args, 
                                    std.error_code& ec) = 0;

        virtual std.string to_string(int level = 0)
        {
            std.string s;
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
    class decorator_function : public function_base<Json>
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
            : function_base<Json>(arity), f_(f)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args,
            std.error_code& ec) override
        {
            return f_(args, ec);
        }
    };

    template <class Json>
    class contains_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        contains_function()
            : function_base<Json>(2)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            auto arg1= args[1].value();

            switch (arg0.type())
            {
                case json_type.array_value:
                    for (auto& j : arg0.array_range())
                    {
                        if (j == arg1)
                        {
                            return value_type(true);
                        }
                    }
                    return value_type(false);
                case json_type.string_value:
                {
                    if (!arg1.is_string())
                    {
                        ec = jsonpath_errc.invalid_type;
                        return value_type.null();
                    }
                    auto sv0 = arg0.template as<string_view_type>();
                    auto sv1 = arg1.template as<string_view_type>();
                    return sv0.find(sv1) != string_view_type.npos ? value_type(true) : value_type(false);
                }
                default:
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
            }
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class ends_with_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        ends_with_function()
            : function_base<Json>(2)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_string())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            auto arg1= args[1].value();
            if (!arg1.is_string())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            auto sv0 = arg0.template as<string_view_type>();
            auto sv1 = arg1.template as<string_view_type>();

            if (sv1.length() <= sv0.length() && sv1 == sv0.substr(sv0.length() - sv1.length()))
            {
                return value_type(true);
            }
            else
            {
                return value_type(false);
            }
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class starts_with_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        starts_with_function()
            : function_base<Json>(2)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_string())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            auto arg1= args[1].value();
            if (!arg1.is_string())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            auto sv0 = arg0.template as<string_view_type>();
            auto sv1 = arg1.template as<string_view_type>();

            if (sv1.length() <= sv0.length() && sv1 == sv0.substr(0, sv1.length()))
            {
                return value_type(true);
            }
            else
            {
                return value_type(false);
            }
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class sum_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        sum_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_array())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            //std.cout << "sum function arg: " << arg0 << "\n";

            double sum = 0;
            for (auto& j : arg0.array_range())
            {
                if (!j.is_number())
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                sum += j.template as<double>();
            }

            return value_type(sum);
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class tokenize_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;

        tokenize_function()
            : function_base<Json>(2)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            if (!args[0].value().is_string() || !args[1].value().is_string())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            auto arg0 = args[0].value().template as<string_type>();
            auto arg1 = args[1].value().template as<string_type>();

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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class ceil_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        ceil_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class floor_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        floor_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class to_number_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        to_number_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            switch (arg0.type())
            {
                case json_type.int64_value:
                case json_type.uint64_value:
                case json_type.double_value:
                    return arg0;
                case json_type.string_value:
                {
                    auto sv = arg0.as_string_view();
                    uint64_t un{0};
                    auto result1 = jsoncons.detail.to_integer(sv.data(), sv.length(), un);
                    if (result1)
                    {
                        return value_type(un);
                    }
                    int64_t sn{0};
                    auto result2 = jsoncons.detail.to_integer(sv.data(), sv.length(), sn);
                    if (result2)
                    {
                        return value_type(sn);
                    }
                    jsoncons.detail.to_double_t to_double;
                    try
                    {
                        auto s = arg0.as_string();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class prod_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        prod_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_array() || arg0.empty())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            double prod = 1;
            for (auto& j : arg0.array_range())
            {
                if (!j.is_number())
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                prod *= j.template as<double>();
            }

            return value_type(prod);
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class avg_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        avg_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_array())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }
            double sum = 0;
            for (auto& j : arg0.array_range())
            {
                if (!j.is_number())
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
                sum += j.template as<double>();
            }

            return value_type(sum / static_cast<double>(arg0.size()));
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class min_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        min_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_array())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }
            bool is_number = arg0.at(0).is_number();
            bool is_string = arg0.at(0).is_string();
            if (!is_number && !is_string)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            int index = 0;
            for (int i = 1; i < arg0.size(); ++i)
            {
                if (!(arg0.at(i).is_number() == is_number && arg0.at(i).is_string() == is_string))
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class max_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        max_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_array())
            {
                //std.cout << "arg: " << arg0 << "\n";
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }
            if (arg0.empty())
            {
                return value_type.null();
            }

            bool is_number = arg0.at(0).is_number();
            bool is_string = arg0.at(0).is_string();
            if (!is_number && !is_string)
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            int index = 0;
            for (int i = 1; i < arg0.size(); ++i)
            {
                if (!(arg0.at(i).is_number() == is_number && arg0.at(i).is_string() == is_string))
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class abs_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;

        abs_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
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

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class length_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using string_view_type = typename Json.string_view_type;
        using parameter_type = parameter<Json>;

        length_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            //std.cout << "length function arg: " << arg0 << "\n";

            switch (arg0.type())
            {
                case json_type.object_value:
                case json_type.array_value:
                    return value_type(arg0.size());
                case json_type.string_value:
                {
                    auto sv0 = arg0.template as<string_view_type>();
                    auto length = unicode_traits.count_codepoints(sv0.data(), sv0.size());
                    return value_type(length);
                }
                default:
                {
                    ec = jsonpath_errc.invalid_type;
                    return value_type.null();
                }
            }
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
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
    class keys_function : public function_base<Json>
    {
    public:
        using value_type = Json;
        using parameter_type = parameter<Json>;
        using string_view_type = typename Json.string_view_type;

        keys_function()
            : function_base<Json>(1)
        {
        }

        value_type Evaluate(const std.vector<parameter_type>& args, 
                            std.error_code& ec) override
        {
            if (args.size() != *this.arity())
            {
                ec = jsonpath_errc.invalid_arity;
                return value_type.null();
            }

            auto arg0= args[0].value();
            if (!arg0.is_object())
            {
                ec = jsonpath_errc.invalid_type;
                return value_type.null();
            }

            value_type result(json_array_arg);
            result.reserve(args.size());

            for (auto& item : arg0.object_range())
            {
                result.emplace_back(item.key());
            }
            return result;
        }

        std.string to_string(int level = 0) override
        {
            std.string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("keys function");
            return s;
        }
    };

    enum class TokenKind
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
    std.string to_string(TokenKind kind)
    {
        switch (kind)
        {
            case TokenKind.root_node:
                return "root_node";
            case TokenKind.current_node:
                return "current_node";
            case TokenKind.lparen:
                return "lparen";
            case TokenKind.rparen:
                return "rparen";
            case TokenKind.begin_union:
                return "begin_union";
            case TokenKind.end_union:
                return "end_union";
            case TokenKind.begin_filter:
                return "begin_filter";
            case TokenKind.end_filter:
                return "end_filter";
            case TokenKind.begin_expression:
                return "begin_expression";
            case TokenKind.end_index_expression:
                return "end_index_expression";
            case TokenKind.end_argument_expression:
                return "end_argument_expression";
            case TokenKind.Separator:
                return "separator";
            case TokenKind.literal:
                return "literal";
            case TokenKind.Selector:
                return "selector";
            case TokenKind.function:
                return "function";
            case TokenKind.end_function:
                return "end_function";
            case TokenKind.argument:
                return "argument";
            case TokenKind.end_of_expression:
                return "end_of_expression";
            case TokenKind.UnaryOperator:
                return "UnaryOperator";
            case TokenKind.BinaryOperator:
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
            auto it = cache_.find(id);
            if (it != cache_.end())
            {
                for (auto& item : it.second.first)
                {
                    accumulator.accumulate(item.stem(), item.value());
                }
                ndtype = it.second.second;
            }
        }

        template <typename... Args>
        Json* new_json(Args&& ... args)
        {
            auto temp = jsoncons.make_unique<Json>(std.forward<Args>(args)...);
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
            auto temp = jsoncons.make_unique<path_node_type>(std.forward<Args>(args)...);
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

        bool IsRightAssociative()
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

        virtual std.string to_string(int = 0)
        {
            return std.string();
        }
    };

    template <class Json, class JsonElement>
    struct Resources
    {
        using char_type = typename Json.char_type;
        using string_type = std.basic_string<char_type>;
        using value_type = Json;
        using reference = JsonElement;
        using function_base_type = function_base<Json>;
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
            for (const auto& item : functions)
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
            static contains_function<Json> contains_func;
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

            auto it = functions.find(name);
            if (it == functions.end())
            {
                auto it2 = custom_functions_.find(name);
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
            static unary_notOperator<Json,JsonElement> oper;
            return &oper;
        }

        UnaryOperator get_unary_minus()
        {
            static unary_minusOperator<Json,JsonElement> oper;
            return &oper;
        }

        UnaryOperator GetRegexOperator(std.basic_regex<char_type>&& pattern) 
        {
            UnaryOperators_.push_back(jsoncons.make_unique<regexOperator<Json,JsonElement>>(std.move(pattern)));
            return UnaryOperators_.back().get();
        }

        BinaryOperator GetOrOperator()
        {
            static OrOperator<Json,JsonElement> oper;

            return &oper;
        }

        BinaryOperator GetAndOperator()
        {
            static andOperator<Json,JsonElement> oper;

            return &oper;
        }

        BinaryOperator GetEqOperator()
        {
            static EqOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetNeOperator()
        {
            static neOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetLtOperator()
        {
            static ltOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetLeOperator()
        {
            static lteOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetGtOperator()
        {
            static gtOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetGteOperator()
        {
            static gteOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetPlusOperator()
        {
            static plusOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetMinusOperator()
        {
            static minusOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetMultOperator()
        {
            static multOperator<Json,JsonElement> oper;
            return &oper;
        }

        BinaryOperator GetDivOperator()
        {
            static divOperator<Json,JsonElement> oper;
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
            auto temp = jsoncons.make_unique<Json>(std.forward<Args>(args)...);
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

        virtual std.string to_string(int level = 0) = 0;
    };

    template <class Json,class JsonElement>
    class Token
    {
    public:
        using selector_type = jsonpath_selector<Json,JsonElement>;
        using expression_base_type = expression_base<Json,JsonElement>;

        TokenKind _type;

        union
        {
            selector_type* _selector;
            std.unique_ptr<expression_base_type> expression_;
            UnaryOperator _unaryOperator;;
            BinaryOperator _binaryOperator;
            function_base<Json>* function_;
            Json value_;
        };
    public:

        Token(UnaryOperator expr) noexcept
            : _type(TokenKind.UnaryOperator),
              _unaryOperator;(expr)
        {
        }

        Token(BinaryOperator expr) noexcept
            : _type(TokenKind.BinaryOperator),
              _binaryOperator(expr)
        {
        }

        Token(current_node_arg_t) noexcept
            : _type(TokenKind.current_node)
        {
        }

        Token(root_node_arg_t) noexcept
            : _type(TokenKind.root_node)
        {
        }

        Token(end_function_arg_t) noexcept
            : _type(TokenKind.end_function)
        {
        }

        Token(separator_arg_t) noexcept
            : _type(TokenKind.Separator)
        {
        }

        Token(lparen_arg_t) noexcept
            : _type(TokenKind.lparen)
        {
        }

        Token(rparen_arg_t) noexcept
            : _type(TokenKind.rparen)
        {
        }

        Token(end_of_expression_arg_t) noexcept
            : _type(TokenKind.end_of_expression)
        {
        }

        Token(begin_union_arg_t) noexcept
            : _type(TokenKind.begin_union)
        {
        }

        Token(end_union_arg_t) noexcept
            : _type(TokenKind.end_union)
        {
        }

        Token(begin_filter_arg_t) noexcept
            : _type(TokenKind.begin_filter)
        {
        }

        Token(end_filter_arg_t) noexcept
            : _type(TokenKind.end_filter)
        {
        }

        Token(begin_expression_arg_t) noexcept
            : _type(TokenKind.begin_expression)
        {
        }

        Token(end_index_expression_arg_t) noexcept
            : _type(TokenKind.end_index_expression)
        {
        }

        Token(end_argument_expression_arg_t) noexcept
            : _type(TokenKind.end_argument_expression)
        {
        }

        Token(selector_type* selector)
            : _type(TokenKind.Selector), _selector(selector)
        {
        }

        Token(std.unique_ptr<expression_base_type>&& expr)
            : _type(TokenKind.expression)
        {
            new (&expression_) std.unique_ptr<expression_base_type>(std.move(expr));
        }

        Token(const function_base<Json>* function) noexcept
            : _type(TokenKind.function),
              function_(function)
        {
        }

        Token(argument_arg_t) noexcept
            : _type(TokenKind.argument)
        {
        }

        Token(literal_arg_t, Json&& value) noexcept
            : _type(TokenKind.literal), value_(std.move(value))
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
                        case TokenKind.Selector:
                            _selector = other._selector;
                            break;
                        case TokenKind.expression:
                            expression_ = std.move(other.expression_);
                            break;
                        case TokenKind.UnaryOperator:
                            _unaryOperator; = other._unaryOperator;;
                            break;
                        case TokenKind.BinaryOperator:
                            _binaryOperator = other._binaryOperator;
                            break;
                        case TokenKind.function:
                            function_ = other.function_;
                            break;
                        case TokenKind.literal:
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

        TokenKind type()
        {
            return _type;
        }

        bool is_lparen()
        {
            return _type == TokenKind.lparen; 
        }

        bool is_rparen()
        {
            return _type == TokenKind.rparen; 
        }

        bool is_current_node()
        {
            return _type == TokenKind.current_node; 
        }

        bool is_path()
        {
            return _type == TokenKind.Selector && _selector.is_path(); 
        }

        bool isOperator()
        {
            return _type == TokenKind.UnaryOperator || 
                   _type == TokenKind.BinaryOperator; 
        }

        int PrecedenceLevel()
        {
            switch(_type)
            {
                case TokenKind.Selector:
                    return _selector.PrecedenceLevel();
                case TokenKind.UnaryOperator:
                    return _unaryOperator;.PrecedenceLevel();
                case TokenKind.BinaryOperator:
                    return _binaryOperator.PrecedenceLevel();
                default:
                    return 0;
            }
        }

        jsoncons.optional<int> arity()
        {
            return _type == TokenKind.function ? function_.arity() : jsoncons.optional<int>();
        }

        bool IsRightAssociative()
        {
            switch(_type)
            {
                case TokenKind.Selector:
                    return _selector.IsRightAssociative();
                case TokenKind.UnaryOperator:
                    return _unaryOperator;.IsRightAssociative();
                case TokenKind.BinaryOperator:
                    return _binaryOperator.IsRightAssociative();
                default:
                    return false;
            }
        }

        voidruct(Token&& other)
        {
            _type = other._type;
            switch (_type)
            {
                case TokenKind.Selector:
                    _selector = other._selector;
                    break;
                case TokenKind.expression:
                    new (&expression_) std.unique_ptr<expression_base_type>(std.move(other.expression_));
                    break;
                case TokenKind.UnaryOperator:
                    _unaryOperator; = other._unaryOperator;;
                    break;
                case TokenKind.BinaryOperator:
                    _binaryOperator = other._binaryOperator;
                    break;
                case TokenKind.function:
                    function_ = other.function_;
                    break;
                case TokenKind.literal:
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
                case TokenKind.expression:
                    expression_.~unique_ptr();
                    break;
                case TokenKind.literal:
                    value_.~Json();
                    break;
                default:
                    break;
            }
        }

        std.string to_string(int level = 0)
        {
            std.string s;
            switch (_type)
            {
                case TokenKind.root_node:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("root node");
                    break;
                case TokenKind.current_node:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("current node");
                    break;
                case TokenKind.argument:
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    s.append("argument");
                    break;
                case TokenKind.Selector:
                    s.append(_selector.to_string(level));
                    break;
                case TokenKind.expression:
                    s.append(expression_.to_string(level));
                    break;
                case TokenKind.literal:
                {
                    if (level > 0)
                    {
                        s.append("\n");
                        s.append(level*2, ' ');
                    }
                    auto sbuf = value_.to_string();
                    unicode_traits.convert(sbuf.data(), sbuf.size(), s);
                    break;
                }
                case TokenKind.BinaryOperator:
                    s.append(_binaryOperator.to_string(level));
                    break;
                case TokenKind.function:
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

        Json Evaluate(dynamic_resources<Json,JsonElement>& resources, 
                      reference root,
                      path_node_type& path, 
                      reference instance,
                      result_options options)
        {
            Json result(json_array_arg);

            if ((options & result_options.path) == result_options.path)
            {
                auto callback = [&result](const normalized_path_type& path, reference)
                {
                    result.emplace_back(path.to_string());
                };
                Evaluate(resources, root, path, instance, callback, options);
            }
            else
            {
                auto callback = [&result](const normalized_path_type&, reference val)
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
                        auto last = std.unique(accumulator.nodes.begin(),accumulator.nodes.end(),path_value_pair_equal_type());
                        accumulator.nodes.erase(last,accumulator.nodes.end());
                        for (auto& node : accumulator.nodes)
                        {
                            callback(node.path(), node.value());
                        }
                    }
                    else
                    {
                        std.vector<path_value_pair_type> index(accumulator.nodes);
                        std.sort(index.begin(), index.end(), path_value_pair_less_type());
                        auto last = std.unique(index.begin(),index.end(),path_value_pair_equal_type());
                        index.erase(last,index.end());

                        std.vector<path_value_pair_type> temp2;
                        temp2.reserve(index.size());
                        for (auto&& node : accumulator.nodes)
                        {
                            auto it = std.lower_bound(index.begin(),index.end(),node, path_value_pair_less_type());
                            if (it != index.end() && it.path() == node.path()) 
                            {
                                temp2.emplace_back(std.move(node));
                                index.erase(it);
                            }
                        }
                        for (auto& node : temp2)
                        {
                            callback(node.path(), node.value());
                        }
                    }
                }
                else
                {
                    for (auto& node : accumulator.nodes)
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

        std.string to_string(int level)
        {
            std.string s;
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
        std.vector<token_type> token_list_;
    public:

        expression()
        {
        }

        expression(expression&& expr)
            : token_list_(std.move(expr.token_list_))
        {
        }

        expression(std.vector<token_type>&& token_stack)
            : token_list_(std.move(token_stack))
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
            //for (auto& tok : token_list_)
            //{
            //    std.cout << tok.to_string() << "\n";
            //}
            //std.cout << "\n";

            if (!token_list_.empty())
            {
                for (auto& tok : token_list_)
                {
                    //std.cout << "Token: " << tok.to_string() << "\n";
                    switch (tok.type())
                    { 
                        case TokenKind.literal:
                        {
                            stack.emplace_back(std.addressof(tok.get_value(reference_arg_type(), resources)));
                            break;
                        }
                        case TokenKind.UnaryOperator:
                        {
                            JSONCONS_ASSERT(stack.size() >= 1);
                            auto item = std.move(stack.back());
                            stack.pop_back();

                            auto val = tok._unaryOperator;.Evaluate(item.value(), ec);
                            stack.emplace_back(std.move(val));
                            break;
                        }
                        case TokenKind.BinaryOperator:
                        {
                            //std.cout << "binary operator: " << stack.size() << "\n";
                            JSONCONS_ASSERT(stack.size() >= 2);
                            auto rhs = std.move(stack.back());
                            //std.cout << "rhs: " << *rhs << "\n";
                            stack.pop_back();
                            auto lhs = std.move(stack.back());
                            //std.cout << "lhs: " << *lhs << "\n";
                            stack.pop_back();

                            auto val = tok._binaryOperator.Evaluate(lhs.value(), rhs.value(), ec);
                            //std.cout << "Evaluate binary expression: " << r << "\n";
                            stack.emplace_back(std.move(val));
                            break;
                        }
                        case TokenKind.root_node:
                            //std.cout << "root: " << root << "\n";
                            stack.emplace_back(std.addressof(root));
                            break;
                        case TokenKind.current_node:
                            //std.cout << "current: " << current << "\n";
                            stack.emplace_back(std.addressof(current));
                            break;
                        case TokenKind.argument:
                            JSONCONS_ASSERT(!stack.empty());
                            //std.cout << "argument stack items " << stack.size() << "\n";
                            //for (auto& item : stack)
                            //{
                            //    std.cout << *item.to_pointer(resources) << "\n";
                            //}
                            //std.cout << "\n";
                            arg_stack.emplace_back(std.move(stack.back()));
                            //for (auto& item : arg_stack)
                            //{
                            //    std.cout << *item << "\n";
                            //}
                            //std.cout << "\n";
                            stack.pop_back();
                            break;
                        case TokenKind.function:
                        {
                            if (tok.function_.arity() && *(tok.function_.arity()) != arg_stack.size())
                            {
                                ec = jsonpath_errc.invalid_arity;
                                return Json.null();
                            }
                            //std.cout << "function arg stack:\n";
                            //for (auto& item : arg_stack)
                            //{
                            //    std.cout << *item << "\n";
                            //}
                            //std.cout << "\n";

                            value_type val = tok.function_.Evaluate(arg_stack, ec);
                            if (ec)
                            {
                                return Json.null();
                            }
                            //std.cout << "function result: " << val << "\n";
                            arg_stack.clear();
                            stack.emplace_back(std.move(val));
                            break;
                        }
                        case TokenKind.expression:
                        {
                            if (stack.empty())
                            {
                                stack.emplace_back(std.addressof(current));
                            }

                            auto item = std.move(stack.back());
                            stack.pop_back();
                            value_type val = tok.expression_.evaluate_single(resources, root, resources.current_path_node(), item.value(), options, ec);
                            //std.cout << "ref2: " << ref << "\n";
                            stack.emplace_back(std.move(val));
                            break;
                        }
                        case TokenKind.Selector:
                        {
                            if (stack.empty())
                            {
                                stack.emplace_back(std.addressof(current));
                            }

                            auto item = std.move(stack.back());
                            //for (auto& item : stack)
                            //{
                                //std.cout << "selector stack input:\n";
                                //switch (item.tag)
                                //{
                                //    case node_set_tag.single:
                                //        std.cout << "single: " << *(item.node.ptr) << "\n";
                                //        break;
                                //    case node_set_tag.multi:
                                //        for (auto& node : stack.back().ptr().nodes)
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
                            stack.pop_back();
                            node_kind ndtype = node_kind();
                            path_value_accumulator<Json,JsonElement> accumulator;
                            tok._selector.select(resources, root, resources.current_path_node(), item.value(), accumulator, ndtype, options);
                            
                            if ((options & result_options.sort) == result_options.sort)
                            {
                                std.sort(accumulator.nodes.begin(), accumulator.nodes.end(), path_value_pair_less_type());
                            }

                            if ((options & result_options.nodups) == result_options.nodups)
                            {
                                if ((options & result_options.sort) == result_options.sort)
                                {
                                    auto last = std.unique(accumulator.nodes.begin(),accumulator.nodes.end(),path_value_pair_equal_type());
                                    accumulator.nodes.erase(last,accumulator.nodes.end());
                                    stack.emplace_back(nodes_to_stack_item(accumulator.nodes, ndtype));
                                }
                                else
                                {
                                    std.vector<path_value_pair_type> index(accumulator.nodes);
                                    std.sort(index.begin(), index.end(), path_value_pair_less_type());
                                    auto last = std.unique(index.begin(),index.end(),path_value_pair_equal_type());
                                    index.erase(last,index.end());

                                    std.vector<path_value_pair_type> temp2;
                                    temp2.reserve(index.size());
                                    for (auto&& node : accumulator.nodes)
                                    {
                                        //std.cout << "node: " << node.path << ", " << *node.ptr << "\n";
                                        auto it = std.lower_bound(index.begin(),index.end(),node, path_value_pair_less_type());

                                        if (it != index.end() && it.path() == node.path()) 
                                        {
                                            temp2.emplace_back(std.move(node));
                                            index.erase(it);
                                        }
                                    }
                                    stack.emplace_back(nodes_to_stack_item(temp2, ndtype));
                                }
                            }
                            else
                            {
                                //std.cout << "selector output " << accumulator.nodes.size() << "\n";
                                //for (auto& item : accumulator.nodes)
                                //{
                                //    std.cout << *item.ptr << "\n";
                                //}
                                //std.cout << "\n";
                                stack.emplace_back(nodes_to_stack_item(accumulator.nodes, ndtype));
                            }

                            
                            break;
                        }
                        default:
                            break;
                    }
                }
            }

            //if (stack.size() != 1)
            //{
            //    std.cout << "Stack size: " << stack.size() << "\n";
            //}
            return stack.empty() ? Json.null() : stack.back().value();
        }
 
        std.string to_string(int level)
        {
            std.string s;
            if (level > 0)
            {
                s.append("\n");
                s.append(level*2, ' ');
            }
            s.append("expression ");
            for (const auto& item : token_list_)
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
                for (auto& item : nodes)
                {
                    j.emplace_back(item.value());
                }
                return stack_item_type(std.move(j));
            }
        }
    };

} // namespace detail
} // namespace jsonpath
} // namespace jsoncons

#endif // JSONCONS_JSONPATH_JSONPATH_EXPRESSION_HPP
