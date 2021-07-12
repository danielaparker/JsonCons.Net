using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.JsonPointerLib;
        
namespace JsonCons.JsonPatchLib
{
    public class UnpackedJsonElement
    {
        internal JsonValueKind ValueKind {get;}

        object _item;

        internal UnpackedJsonElement(IList<UnpackedJsonElement> list)
        {
            ValueKind = JsonValueKind.Array;
            _item = list;
        }

        internal UnpackedJsonElement(IDictionary<string,UnpackedJsonElement> dict)
        {
            ValueKind = JsonValueKind.Object;
            _item = dict;
        }

        internal UnpackedJsonElement(JsonElement element)
        {
            ValueKind = element.ValueKind;
            _item = element;
        }

        internal IList<UnpackedJsonElement> GetList()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return (IList<UnpackedJsonElement>)_item;
        }

        internal IDictionary<string, UnpackedJsonElement> GetDictionary()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return (IDictionary<string, UnpackedJsonElement>)_item;
        }

        public UnpackedJsonElement this[int i]
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

        public bool TryGetProperty(string name, out UnpackedJsonElement value)
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

        public static UnpackedJsonElement Unpack(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    var list = new List<UnpackedJsonElement>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(Unpack(item));
                    }
                    return new UnpackedJsonElement(list);
                case JsonValueKind.Object:
                    var dict = new Dictionary<string,UnpackedJsonElement>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict.Add(property.Name, Unpack(property.Value));
                    }
                    return new UnpackedJsonElement(dict);
                default:
                    return new UnpackedJsonElement(element);
            }
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
                    foreach (var item in GetList())
                    {
                        item.ToString(buffer);
                    }
                    buffer.Append("]");
                    break;
                }
                case JsonValueKind.Object:
                {
                    buffer.Append("[");
                    foreach (var property in GetDictionary())
                    {
                        buffer.Append(property.Key);
                        buffer.Append(":");
                        property.Value.ToString(buffer);
                    }
                    buffer.Append("]");
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

    public static class JsonPointerExtensions
    {
        public static bool TryResolve(string token, UnpackedJsonElement current, out UnpackedJsonElement result)
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

        public static bool TryGet(this JsonPointer pointer, UnpackedJsonElement root, out UnpackedJsonElement value)
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
                                  ref UnpackedJsonElement root, 
                                  UnpackedJsonElement value)
        {
            UnpackedJsonElement current = root;
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
                                          ref UnpackedJsonElement root, 
                                          UnpackedJsonElement value)
        {
            UnpackedJsonElement current = root;
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
                                     ref UnpackedJsonElement root)
        {
            UnpackedJsonElement current = root;
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
                                      ref UnpackedJsonElement root, 
                                      UnpackedJsonElement value)
        {
            UnpackedJsonElement current = root;
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
                    current.GetList().Insert(index,value);
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
