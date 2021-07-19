﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.JsonPointerLib;
        
namespace JsonCons.JsonPatchLib
{
    class JsonDocumentBuilder
    {
        internal JsonValueKind ValueKind {get;}

        object _item;

        internal JsonDocumentBuilder(JsonValueKind valueKind)
        {
            ValueKind = valueKind;
            switch (valueKind)
            {
                case JsonValueKind.Array:
                    _item = new List<JsonDocumentBuilder>();
                    break;
                case JsonValueKind.Object:
                    _item = new Dictionary<string,JsonDocumentBuilder>();
                    break;
                default:
                    _item = null;
                    break;
            }
        }

        internal JsonDocumentBuilder(IList<JsonDocumentBuilder> list)
        {
            ValueKind = JsonValueKind.Array;
            _item = list;
        }

        internal JsonDocumentBuilder(IDictionary<string,JsonDocumentBuilder> dict)
        {
            ValueKind = JsonValueKind.Object;
            _item = dict;
        }

        internal JsonDocumentBuilder(string str)
        {
            ValueKind = JsonValueKind.String;
            _item = str;
        }

        internal JsonDocumentBuilder(JsonElement element)
        {
            ValueKind = element.ValueKind;
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    var list = new List<JsonDocumentBuilder>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(new JsonDocumentBuilder(item));
                    }
                    _item = list;
                    break;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string,JsonDocumentBuilder>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict.Add(property.Name, new JsonDocumentBuilder(property.Value));
                    }
                    _item = dict;
                    break;
                default:
                    _item = element;
                    break;
            }
        }

        internal IList<JsonDocumentBuilder> GetList()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return (IList<JsonDocumentBuilder>)_item;
        }

        internal IDictionary<string, JsonDocumentBuilder> GetDictionary()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return (IDictionary<string, JsonDocumentBuilder>)_item;
        }

        public JsonDocumentBuilder this[int i]
        {
            get { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                return GetList()[i]; 
            }
            set { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                GetList()[i] = value; 
            }
        }

        public int GetArrayLength()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return GetList().Count;
        }

        public bool TryGetProperty(string name, out JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            if (ValueKind != JsonValueKind.Object)
            {
                value = null;
                return false;
            }
            return GetDictionary().TryGetValue(name, out value);
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            ToString(buffer);
            return buffer.ToString();
        }

        void ToString(StringBuilder buffer)
        {
            switch (ValueKind)
            {
                case JsonValueKind.Array:
                {
                    buffer.Append("[");
                    bool first = true;
                    foreach (var item in GetList())
                    {
                        if (!first)
                        {
                            buffer.Append(",");
                        }
                        else
                        {
                            first = false;
                        }
                        item.ToString(buffer);
                    }
                    buffer.Append("]");
                    break;
                }
                case JsonValueKind.Object:
                {
                    buffer.Append("{");
                    bool first = true;
                    foreach (var property in GetDictionary())
                    {
                        if (!first)
                        {
                            buffer.Append(",");
                        }
                        else
                        {
                            first = false;
                        }
                        buffer.Append(JsonSerializer.Serialize(property.Key));
                        buffer.Append(":");
                        property.Value.ToString(buffer);
                    }
                    buffer.Append("}");
                    break;
                }
                default:
                {
                    buffer.Append(JsonSerializer.Serialize(_item, null));
                    break;
                }
            }
        }

        public JsonDocument ToJsonDocument()
        {
            var json = ToString();
            return JsonDocument.Parse(json);
        }
    }

    static class JsonPointerExtensions
    {
        public static bool TryResolve(string token, JsonDocumentBuilder current, out JsonDocumentBuilder result)
        {
            result = current;

            if (result.ValueKind == JsonValueKind.Array)
            {
                if (token == "-")
                {
                    return false;
                }
                int index = 0;
                if (!int.TryParse(token, out index))
                {
                    return false;
                }
                if (index >= result.GetArrayLength())
                {
                    return false;
                }
                result = result[index];
            }
            else if (result.ValueKind == JsonValueKind.Object)
            {
                if (!result.TryGetProperty(token, out result))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static JsonPointer ToDefinitePath(this JsonPointer pointer, JsonDocumentBuilder value)
        {
            if (value.ValueKind == JsonValueKind.Array && pointer.Tokens.Count > 0 && pointer.Tokens[pointer.Tokens.Count-1] == "-")
            {
                var tokens = new List<string>();
                for (int i = 0; i < pointer.Tokens.Count-1; ++i)
                {
                    tokens.Add(pointer.Tokens[i]);
                }
                tokens.Add(value.GetArrayLength().ToString());
                return new JsonPointer(tokens);
            }
            else
            {
                return pointer;
            }
        }

        public static bool TryGet(this JsonPointer pointer, JsonDocumentBuilder root, out JsonDocumentBuilder value)
        {
            value = root;

            foreach (var token in pointer)
            {
                if (!TryResolve(token,value,out value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryAdd(this JsonPointer location, 
                                  ref JsonDocumentBuilder root, 
                                  JsonDocumentBuilder value)
        {
            JsonDocumentBuilder current = root;
            string token = null;

            var enumerator = location.GetEnumerator();
            bool more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    current.GetList().Add(value);
                    current = current[current.GetArrayLength()-1];
                }
                else
                {
                    int index;
                    if (!int.TryParse(token, out index))
                    {
                        return false;
                    }
                    if (index > current.GetArrayLength())
                    {
                        return false;
                    }
                    if (index == current.GetArrayLength())
                    {
                        current.GetList().Add(value);
                        current = value;
                    }
                    else
                    {
                        current.GetList().Insert(index,value);
                        current = value;
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.GetDictionary().ContainsKey(token))
                {
                    current.GetDictionary().Remove(token);
                }
                current.GetDictionary().Add(token, value);
                current = value;
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryAddIfAbsent(this JsonPointer location, 
                                          ref JsonDocumentBuilder root, 
                                          JsonDocumentBuilder value)
        {
            JsonDocumentBuilder current = root;
            string token = null;

            var enumerator = location.GetEnumerator();
            bool more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    current.GetList().Add(value);
                    current = current[current.GetArrayLength()-1];
                }
                else
                {
                    int index;
                    if (!int.TryParse(token, out index))
                    {
                        return false;
                    }
                    if (index > current.GetArrayLength())
                    {
                        return false;
                    }
                    if (index == current.GetArrayLength())
                    {
                        current.GetList().Add(value);
                        current = value;
                    }
                    else
                    {
                        current.GetList().Insert(index,value);
                        current = value;
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.GetDictionary().ContainsKey(token))
                {
                    return false;
                }
                current.GetDictionary().Add(token, value);
                current = value;
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryRemove(this JsonPointer location, 
                                     ref JsonDocumentBuilder root)
        {
            JsonDocumentBuilder current = root;
            string token = null;

            var enumerator = location.GetEnumerator();
            bool more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    return false;
                }
                else
                {
                    int index;
                    if (!int.TryParse(token, out index))
                    {
                        return false;
                    }
                    if (index >= current.GetArrayLength())
                    {
                        return false;
                    }
                    current.GetList().RemoveAt(index);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.GetDictionary().ContainsKey(token))
                {
                    current.GetDictionary().Remove(token);
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryReplace(this JsonPointer location, 
                                      ref JsonDocumentBuilder root, 
                                      JsonDocumentBuilder value)
        {
            JsonDocumentBuilder current = root;
            string token = null;

            var enumerator = location.GetEnumerator();
            bool more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    return false;
                }
                else
                {
                    int index;
                    if (!int.TryParse(token, out index))
                    {
                        return false;
                    }
                    if (index >= current.GetArrayLength())
                    {
                        return false;
                    }
                    current.GetList()[index] = value;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.GetDictionary().ContainsKey(token))
                {
                    current.GetDictionary().Remove(token);
                }
                else
                {
                    return false;
                }
                current.GetDictionary().Add(token, value);
            }
            else
            {
                return false;
            }
            return true;
        }

    }

} // namespace JsonCons.JsonPointerLib