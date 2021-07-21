using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.Utilities
{
    internal class JsonDocumentBuilder
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
                case JsonValueKind.True:
                    _item = true;
                    break;
                case JsonValueKind.False:
                    _item = false;
                    break;
                case JsonValueKind.Null:
                    _item = null;
                    break;
                case JsonValueKind.String:
                    _item = "";
                    break;
                case JsonValueKind.Number:
                    _item = 0;
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

        internal IEnumerable<JsonDocumentBuilder> EnumerateArray()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return (IList<JsonDocumentBuilder>)_item;
        }

        internal IEnumerable<KeyValuePair<string, JsonDocumentBuilder>> EnumerateObject()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return (IDictionary<string, JsonDocumentBuilder>)_item;
        }

        internal JsonDocumentBuilder this[int i]
        {
            get { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                return ((IList<JsonDocumentBuilder>)_item) [i]; 
            }
            set { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                ((IList<JsonDocumentBuilder>)_item)[i] = value; 
            }
        }

        internal void AddArrayItem(JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            ((IList<JsonDocumentBuilder>)_item).Add(value);
        }

        internal void InsertArrayItem(int index, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            ((IList<JsonDocumentBuilder>)_item).Insert(index, value);
        }

        internal void RemoveArrayItemAt(int index)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            ((IList<JsonDocumentBuilder>)_item).RemoveAt(index);
        }

        internal void AddProperty(string name, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            ((IDictionary<string,JsonDocumentBuilder>)_item).Add(name, value);
        }

        internal bool TryAddProperty(string name, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return ((IDictionary<string,JsonDocumentBuilder>)_item).TryAdd(name, value);
        }

        internal bool ContainsPropertyName(string name)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return ((IDictionary<string,JsonDocumentBuilder>)_item).ContainsKey(name);
        }

        internal void RemoveProperty(string name)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            ((IDictionary<string,JsonDocumentBuilder>)_item).Remove(name);
        }

        internal int GetArrayLength()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return ((IList<JsonDocumentBuilder>)_item).Count();
        }

        internal int GetObjectLength()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return ((IDictionary<string,JsonDocumentBuilder>)_item).Count();
        }

        internal bool TryGetProperty(string name, out JsonDocumentBuilder value)
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
            return ((IDictionary<string,JsonDocumentBuilder>)_item).TryGetValue(name, out value);
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
                    foreach (var item in EnumerateArray())
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
                    foreach (var property in EnumerateObject())
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

        internal JsonDocument ToJsonDocument()
        {
            var json = ToString();
            return JsonDocument.Parse(json);
        }
    }

} // namespace JsonCons.Utilities
