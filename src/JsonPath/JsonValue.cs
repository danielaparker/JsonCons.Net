using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public struct NameValuePair
    {
        public string Name { get; }
        public IJsonValue Value { get; }

        public NameValuePair(string name, IJsonValue value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IJsonArrayEnumerator : IEnumerator<IJsonValue>, IEnumerable<IJsonValue>
    {
    }

    public interface IJsonObjectEnumerator : IEnumerator<NameValuePair>, IEnumerable<NameValuePair>
    {
    }

    public interface IJsonValue 
    {
        JsonValueKind ValueKind {get;}
        IJsonValue this[int index] {get;}
        int GetArrayLength();
        string GetString();
        bool TryGetDecimal(out decimal value);
        bool TryGetDouble(out double value);
        bool TryGetProperty(string propertyName, out IJsonValue property);
        IJsonArrayEnumerator EnumerateArray();
        IJsonObjectEnumerator EnumerateObject();

        bool IsJsonElement();
        JsonElement GetJsonElement();
    };

    public struct JsonElementJsonValue : IJsonValue
    {
        public class ArrayEnumerator : IJsonArrayEnumerator
        {
            JsonElement.ArrayEnumerator _enumerator;

            public ArrayEnumerator(JsonElement.ArrayEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset() { _enumerator.Reset(); }

            void IDisposable.Dispose() { _enumerator.Dispose();}

            public IJsonValue Current
            {
                get { return new JsonElementJsonValue(_enumerator.Current); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IJsonValue> GetEnumerator()
            {
                return new ArrayEnumerator(_enumerator.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        public class ObjectEnumerator : IJsonObjectEnumerator
        {
            JsonElement.ObjectEnumerator _enumerator;

            public ObjectEnumerator(JsonElement.ObjectEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset() { _enumerator.Reset(); }

            void IDisposable.Dispose() { _enumerator.Dispose();}

            public NameValuePair Current
            {
                get { return new NameValuePair(_enumerator.Current.Name, new JsonElementJsonValue(_enumerator.Current.Value)); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<NameValuePair> GetEnumerator()
            {
                return new ObjectEnumerator(_enumerator);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        private JsonElement _element;

        internal JsonElementJsonValue(JsonElement element)
        {
            _element = element;
        }

        public JsonValueKind ValueKind {get{return _element.ValueKind;}}

        public IJsonValue this[int index] {get{return new JsonElementJsonValue(_element[index]);}}

        public int GetArrayLength() {return _element.GetArrayLength();}

        public string GetString()
        {
            return _element.GetString();
        }

        public bool TryGetDecimal(out Decimal value)
        {
            return _element.TryGetDecimal(out value);
        }

        public bool TryGetDouble(out double value)
        {
            return _element.TryGetDouble(out value);
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            JsonElement prop;
            bool r = _element.TryGetProperty(propertyName, out prop);
            property = new JsonElementJsonValue(prop);
            return r;
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            return new ArrayEnumerator(_element.EnumerateArray());
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            return new ObjectEnumerator(_element.EnumerateObject());
        }

        public bool IsJsonElement()
        {
            return true;
        }

        public JsonElement GetJsonElement()
        {
            return _element;
        }      
    };

    struct DoubleJsonValue : IJsonValue
    {
        private double _value;

        internal DoubleJsonValue(double value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString()
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDecimal(out Decimal value)
        {
            if (!(Double.IsNaN(_value) || Double.IsInfinity(_value)) && (_value >= (double)Decimal.MinValue && _value <= (double)Decimal.MaxValue))
            {
                value = Decimal.MinValue;
                return false;
            }
            else
            {
                value = new Decimal(_value);
                return true;
            }
        }

        public bool TryGetDouble(out double value)
        {
            value = _value;
            return true;
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct DecimalJsonValue : IJsonValue
    {
        private Decimal _value;

        internal DecimalJsonValue(Decimal value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString()
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDecimal(out Decimal value)
        {
            value = _value;
            return true;
        }

        public bool TryGetDouble(out double value)
        {
            value = (double)_value;
            return true;
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct StringJsonValue : IJsonValue
    {
        private string _value;

        internal StringJsonValue(string value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.String;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString()
        {
            return _value;
        }

        public bool TryGetDecimal(out Decimal value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct TrueJsonValue : IJsonValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.True;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString() { throw new InvalidOperationException(); }

        public bool TryGetDecimal(out Decimal value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct FalseJsonValue : IJsonValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.False;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString() { throw new InvalidOperationException(); }

        public bool TryGetDecimal(out Decimal value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct NullJsonValue : IJsonValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.Null;}}

        public IJsonValue this[int index] { get { throw new InvalidOperationException(); } }

        public int GetArrayLength() { throw new InvalidOperationException(); }

        public string GetString() { throw new InvalidOperationException(); }

        public bool TryGetDecimal(out Decimal value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    struct ArrayJsonValue : IJsonValue
    {
        public class ArrayEnumerator : IJsonArrayEnumerator
        {   
            IList<IJsonValue> _value;
            System.Collections.IEnumerator _enumerator;

            public ArrayEnumerator(IList<IJsonValue> value)
            {
                _value = value;
                _enumerator = value.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset() { _enumerator.Reset(); }

            void IDisposable.Dispose() {}

            public IJsonValue Current
            {
                get { return _enumerator.Current as IJsonValue; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IJsonValue> GetEnumerator()
            {
                return _value.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        private IList<IJsonValue> _value;

        internal ArrayJsonValue(IList<IJsonValue> value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Array;}}

        public IJsonValue this[int index] { get { return _value[index]; } }

        public int GetArrayLength() { return _value.Count; }

        public string GetString()
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDecimal(out Decimal value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IJsonValue property)
        {
            throw new InvalidOperationException();
        }

        public IJsonArrayEnumerator EnumerateArray()
        {
            return new ArrayEnumerator(_value);
        }

        public IJsonObjectEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public bool IsJsonElement()
        {
            return false;
        }

        public JsonElement GetJsonElement()
        {
            throw new JsonException("Not a JsonElement");
        }      
    };

    static class JsonConstants
    {
        static readonly IJsonValue _trueValue;
        static readonly IJsonValue _falseValue;
        static readonly IJsonValue _nullValue;

        static JsonConstants()
        {
            _trueValue = new TrueJsonValue();
            _falseValue = new FalseJsonValue();
            _nullValue = new NullJsonValue();
        }

        internal static IJsonValue True {get {return _trueValue;}}
        internal static IJsonValue False { get { return _falseValue; } }
        internal static IJsonValue Null { get { return _nullValue; } }
    }

} // namespace JsonCons.JsonPathLib
