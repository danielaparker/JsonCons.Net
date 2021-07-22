using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.Utilities
{
    /// <summary>
    /// Defines how the unflatten operation handles integer tokens in the JSON Pointer key
    /// </summary>
    public enum IntegerTokenHandling { 
        /// <summary>
        /// The unflatten operation first tries to unflatten into a JSON array
        /// using the integer tokens as sequential indices, and if that fails, unflattens into
        /// a JSON object using the integer tokens as names.
        /// </summary>
        IndexOrName, 
        /// <summary>
        /// The unflatten operation always unflattens into a JSON object
        /// using the integer tokens as names.
        /// </summary>
        NameOnly 
    }

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
            var result = new JsonDocumentBuilder(JsonValueKind.Object);
            string parentKey = "";
            _Flatten(parentKey, value, result);
            return result.ToJsonDocument();
        }

        // unflatten

        static JsonDocumentBuilder SafeUnflatten(JsonDocumentBuilder value)
        {
            if (value.ValueKind != JsonValueKind.Object || value.GetObjectLength() == 0)
            {
                return value;
            }
            bool safe = true;
            int index = 0;
            foreach (var item in value.EnumerateObject())
            {
                int n;
                if (!int.TryParse(item.Key, out n) || index++ != n)
                {
                    safe = false;
                    break;
                }
            }

            if (safe)
            {
                var j = new JsonDocumentBuilder(JsonValueKind.Array);
                foreach (var item in value.EnumerateObject())
                {
                    j.AddArrayItem(item.Value);
                }
                var a = new JsonDocumentBuilder(JsonValueKind.Array);
                foreach (var item in j.EnumerateArray())
                {
                    a.AddArrayItem(SafeUnflatten(item));
                }
                return a;
            }
            else
            {
                var o = new JsonDocumentBuilder(JsonValueKind.Object);
                foreach (var item in value.EnumerateObject())
                {
                    o.TryAddProperty(item.Key, SafeUnflatten (item.Value));
                }
                return o;
            }
        }

        static bool TryUnflattenArray(JsonElement value, out JsonDocumentBuilder result)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Not an object");
            }

            result = new JsonDocumentBuilder(JsonValueKind.Object);

            foreach (var item in value.EnumerateObject())
            {
                JsonDocumentBuilder parent = null;
                JsonDocumentBuilder part = result;
                int parentIndex = 0;
                string parentName = "";

                JsonPointer ptr;
                if (!JsonPointer.TryParse(item.Name, out ptr))
                {
                    throw new InvalidOperationException("Name contains invalid JSON Pointer");
                }
                int index = 0;

                var it = ptr.GetEnumerator();
                bool more = it.MoveNext();
                while (more)
                {
                    string token = it.Current;
                    int n;

                    if (int.TryParse(token, out n) && index++ == n)
                    {
                        if (part.ValueKind != JsonValueKind.Array)
                        {
                            if (parent != null && parent.ValueKind == JsonValueKind.Object)
                            {
                                parent.RemoveProperty(parentName);
                                var val = new JsonDocumentBuilder(JsonValueKind.Array);
                                parent.AddProperty(parentName, val);
                                part = val;
                            }
                            else if (parent != null && parent.ValueKind == JsonValueKind.Array)
                            {
                                var val = new JsonDocumentBuilder(JsonValueKind.Array);
                                parent[parentIndex] = val;
                                part = val;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        parent = part;
                        parentIndex = n;
                        parentName = token;
                        more = it.MoveNext();
                        if (more)
                        {
                            if (n >= part.GetArrayLength())
                            {
                                part.AddArrayItem(new JsonDocumentBuilder(JsonValueKind.Object));
                                part = part[part.GetArrayLength() - 1];
                            }
                            else
                            {
                                part = part[n];
                            }
                        }
                        else
                        {
                            part.AddArrayItem(new JsonDocumentBuilder(item.Value));
                            part = part[part.GetArrayLength() - 1];
                        }
                    }
                    else if (part.ValueKind == JsonValueKind.Object)
                    {
                        more = it.MoveNext();
                        if (more)
                        {
                            JsonDocumentBuilder val;
                            if (part.TryGetProperty(token, out val))
                            {
                                part = val;
                            }
                            else
                            {
                                val = new JsonDocumentBuilder(JsonValueKind.Object);
                                part.AddProperty(token,val);
                                part = val;
                            }
                        }
                        else
                        {
                            JsonDocumentBuilder val;
                            if (part.TryGetProperty(token, out val))
                            {
                                part = val;
                            }
                            else
                            {
                                val = new JsonDocumentBuilder(item.Value);
                                part.AddProperty(token,val);
                                part = val;
                            }
                        }
                    }
                    else 
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static JsonDocumentBuilder UnflattenToObject(JsonElement value, IntegerTokenHandling options = IntegerTokenHandling.IndexOrName)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Not an object");
            }

            var result = new JsonDocumentBuilder(JsonValueKind.Object);

            foreach (var item in value.EnumerateObject())
            {
                JsonDocumentBuilder part = result;
                JsonPointer ptr;
                if (!JsonPointer.TryParse(item.Name, out ptr))
                {
                    throw new InvalidOperationException("Name contains invalid JSON Pointer");
                }
                var it = ptr.GetEnumerator();
                bool more = it.MoveNext();
                while (more)
                {
                    var s = it.Current;
                    more = it.MoveNext();
                    if (more)
                    {
                        JsonDocumentBuilder val;
                        if (part.TryGetProperty(s, out val))
                        {
                            part = val;
                        }
                        else
                        {
                            val = new JsonDocumentBuilder(JsonValueKind.Object);
                            part.AddProperty(s,val);
                            part = val;
                        }
                    }
                    else
                    {
                        JsonDocumentBuilder val;
                        if (part.TryGetProperty(s, out val))
                        {
                            part = val;
                        }
                        else
                        {
                            val = new JsonDocumentBuilder(item.Value);
                            part.AddProperty(s,val);
                            part = val;
                        }
                    }
                }
            }

            return options == IntegerTokenHandling.IndexOrName ? SafeUnflatten (result) : result;
        }

        public static JsonDocument Unflatten(JsonElement value, IntegerTokenHandling options = IntegerTokenHandling.IndexOrName)
        {
            if (options == IntegerTokenHandling.IndexOrName)
            {
                 JsonDocumentBuilder val;
                 if (TryUnflattenArray(value, out val))
                 {
                     return val.ToJsonDocument();
                 }
                 else
                 {
                     return UnflattenToObject(value, options).ToJsonDocument();
                 }
            }
            else
            {
                return UnflattenToObject(value, options).ToJsonDocument();
            }
        }
    }

} // namespace JsonCons.Utilities
