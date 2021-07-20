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
        static void _Flatten(string parentKey,
                            JsonElement parentValue,
                            JsonDocumentBuilder result)
        {
            switch (parentValue.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    if (parentValue.GetArrayLength() == 0)
                    {
                        result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    }
                    else
                    {
                        for (int i = 0; i < parentValue.GetArrayLength(); ++i)
                        {
                            var buffer = new StringBuilder(parentKey);
                            buffer.Append('/');
                            buffer.Append(i.ToString());
                            _Flatten(buffer.ToString(), parentValue[i], result);
                        }
                    }
                    break;
                }

                case JsonValueKind.Object:
                {
                    if (parentValue.EnumerateObject().Count() == 0)
                    {
                        result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    }
                    else
                    {
                        foreach (var item in parentValue.EnumerateObject())
                        {
                            var buffer = new StringBuilder(parentKey);
                            buffer.Append('/');
                            buffer.Append(JsonPointer.Escape(item.Name));
                            _Flatten(buffer.ToString(), item.Value, result);
                        }
                    }
                    break;
                }

                default:
                {
                    result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    break;
                }
            }
        }

        public static JsonDocument Flatten(JsonElement value)
        {
            var result = new JsonDocumentBuilder(new Dictionary<string,JsonDocumentBuilder>());
            string parentKey = "";
            _Flatten(parentKey, value, result);
            return result.ToJsonDocument();
        }

/*
        // unflatten

        enum UnflattenOptions {None, AssumeObject = 1};

        Json SafeUnflatten (JsonDocumentBuilder value)
        {
            if (value.ValueKind != JsonValueKind.Object || value.empty())
            {
                return value;
            }
            bool safe = true;
            int index = 0;
            foreach (var item in value.EnumerateObject())
            {
                int n;
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
                for (auto& item : value.EnumerateObject())
                {
                    j.emplace_back(std::move(item.value()));
                }
                Json a(json_array_arg);
                for (auto& item : j.EnumerateArray())
                {
                    a.emplace_back(SafeUnflatten (item));
                }
                return a;
            }
            else
            {
                Json o(json_object_arg);
                for (auto& item : value.EnumerateObject())
                {
                    o.try_emplace(item.key(), SafeUnflatten (item.value()));
                }
                return o;
            }
        }

        jsoncons::optional<Json> TryUnflattenArray(JsonElement value)
        {
            if (JSONCONS_UNLIKELY(value.ValueKind != JsonValueKind.Object))
            {
                JSONCONS_THROW(jsonpointer_error(jsonpointer_errc::argument_to_unflatten_invalid));
            }
            Json result;

            for (const auto& item: value.EnumerateObject())
            {
                Json* part = &result;
                basic_json_pointer<char_type> ptr(item.key());
                int index = 0;
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

        Json UnflattenToObject(JsonElement value, UnflattenOptions options = UnflattenOptions::None)
        {
            if (JSONCONS_UNLIKELY(value.ValueKind != JsonValueKind.Object))
            {
                JSONCONS_THROW(jsonpointer_error(jsonpointer_errc::argument_to_unflatten_invalid));
            }
            Json result;

            for (const auto& item: value.EnumerateObject())
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

            return options == UnflattenOptions::None ? SafeUnflatten (result) : result;
        }

        Json Unflatten(JsonElement value, UnflattenOptions options = UnflattenOptions::None)
        {
            if (options == UnflattenOptions::None)
            {
                jsoncons::optional<Json> j = TryUnflattenArray(value);
                return j ? *j : UnflattenToObject(value,options);
            }
            else
            {
                return UnflattenToObject(value,options);
            }
        }
*/

    }

} // namespace JsonCons.Utilities
