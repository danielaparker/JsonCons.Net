using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.Utilities
{
    public static class JsonFlattener
    {
        void _Flatten(string parentKey,
                      JsonElement parentValue,
                      JsonDocumentBuilder result)
        {
            switch (parentValue.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    if (parentValue.empty())
                    {
                        // Flatten empty array to null
                        //result.try_emplace(parentKey, null_type{});
                        //result[parentKey] = parentValue;
                        result.try_emplace(parentKey, parentValue);
                    }
                    else
                    {
                        for (std::size_t i = 0; i < parentValue.size(); ++i)
                        {
                            string key(parentKey);
                            key.push_back('/');
                            jsoncons::detail::from_integer(i,key);
                            _Flatten(key, parentValue.at(i), result);
                        }
                    }
                    break;
                }

                case JsonValueKind.Object:
                {
                    if (parentValue.empty())
                    {
                        // Flatten empty object to null
                        //result.try_emplace(parentKey, null_type{});
                        //result[parentKey] = parentValue;
                        result.try_emplace(parentKey, parentValue);
                    }
                    else
                    {
                        for (const auto& item : parentValue.object_range())
                        {
                            string key(parentKey);
                            key.push_back('/');
                            escape(jsoncons::basic_string_view<char_type>(item.key().data(),item.key().size()), key);
                            _Flatten(key, item.value(), result);
                        }
                    }
                    break;
                }

                default:
                {
                    // add primitive parentValue with its reference string
                    //result[parentKey] = parentValue;
                    result.try_emplace(parentKey, parentValue);
                    break;
                }
            }
        }

        Json flatten(JsonElement value)
        {
            Json result;
            string parentKey;
            flatten_(parentKey, value, result);
            return result;
        }


        // unflatten

        enum class UnflattenOptions {none,assume_object = 1
        #if !defined(JSONCONS_NO_DEPRECATED)
    ,object = assume_object
    #endif
    };

        Json SafeUnflatten (JsonDocumentBuilder value)
        {
            if (!value.is_object() || value.empty())
            {
                return value;
            }
            bool safe = true;
            std::size_t index = 0;
            for (const auto& item : value.object_range())
            {
                std::size_t n;
                auto r = jsoncons::detail::to_integer_decimal(item.key().data(),item.key().size(), n);
                if (!r || (index++ != n))
                {
                    safe = false;
                    break;
                }
            }

            if (safe)
            {
                Json j(json_array_arg);
                j.reserve(value.size());
                for (auto& item : value.object_range())
                {
                    j.emplace_back(std::move(item.value()));
                }
                Json a(json_array_arg);
                for (auto& item : j.array_range())
                {
                    a.emplace_back(SafeUnflatten (item));
                }
                return a;
            }
            else
            {
                Json o(json_object_arg);
                for (auto& item : value.object_range())
                {
                    o.try_emplace(item.key(), SafeUnflatten (item.value()));
                }
                return o;
            }
        }

        jsoncons::optional<Json> TryUnflattenArray(JsonElement value)
        {
            if (JSONCONS_UNLIKELY(!value.is_object()))
            {
                JSONCONS_THROW(jsonpointer_error(jsonpointer_errc::argument_to_unflatten_invalid));
            }
            Json result;

            for (const auto& item: value.object_range())
            {
                Json* part = &result;
                basic_json_pointer<char_type> ptr(item.key());
                std::size_t index = 0;
                for (auto it = ptr.begin(); it != ptr.end(); )
                {
                    auto s = *it;
                    size_t n{0};
                    auto r = jsoncons::detail::to_integer_decimal(s.data(), s.size(), n);
                    if (r.ec == jsoncons::detail::to_integer_errc() && (index++ == n))
                    {
                        if (!part->is_array())
                        {
                            *part = Json(json_array_arg);
                        }
                        if (++it != ptr.end())
                        {
                            if (n+1 > part->size())
                            {
                                Json& ref = part->emplace_back();
                                part = std::addressof(ref);
                            }
                            else
                            {
                                part = &part->at(n);
                            }
                        }
                        else
                        {
                            Json& ref = part->emplace_back(item.value());
                            part = std::addressof(ref);
                        }
                    }
                    else if (part->is_object())
                    {
                        if (++it != ptr.end())
                        {
                            auto res = part->try_emplace(s,Json());
                            part = &(res.first->value());
                        }
                        else
                        {
                            auto res = part->try_emplace(s, item.value());
                            part = &(res.first->value());
                        }
                    }
                    else 
                    {
                        return jsoncons::optional<Json>();
                    }
                }
            }

            return result;
        }

        Json UnflattenToObject(JsonElement value, UnflattenOptions options = UnflattenOptions::none)
        {
            if (JSONCONS_UNLIKELY(!value.is_object()))
            {
                JSONCONS_THROW(jsonpointer_error(jsonpointer_errc::argument_to_unflatten_invalid));
            }
            Json result;

            for (const auto& item: value.object_range())
            {
                Json* part = &result;
                basic_json_pointer<char_type> ptr(item.key());
                for (auto it = ptr.begin(); it != ptr.end(); )
                {
                    auto s = *it;
                    if (++it != ptr.end())
                    {
                        auto res = part->try_emplace(s,Json());
                        part = &(res.first->value());
                    }
                    else
                    {
                        auto res = part->try_emplace(s, item.value());
                        part = &(res.first->value());
                    }
                }
            }

            return options == UnflattenOptions::none ? SafeUnflatten (result) : result;
        }

        Json Unflatten(JsonElement value, UnflattenOptions options = UnflattenOptions::none)
        {
            if (options == UnflattenOptions::none)
            {
                jsoncons::optional<Json> j = TryUnflattenArray(value);
                return j ? *j : UnflattenToObject(value,options);
            }
            else
            {
                return UnflattenToObject(value,options);
            }
        }

    }

} // namespace JsonCons.Utilities
