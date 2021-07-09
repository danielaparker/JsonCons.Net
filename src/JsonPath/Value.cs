using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    readonly struct NameValuePair
    {
        public string Name { get; }
        public IValue Value { get; }

        public NameValuePair(string name, IValue value)
        {
            Name = name;
            Value = value;
        }
    }

    interface IArrayValueEnumerator : IEnumerator<IValue>, IEnumerable<IValue>
    {
    }

    interface IObjectValueEnumerator : IEnumerator<NameValuePair>, IEnumerable<NameValuePair>
    {
    }

    interface IValue 
    {
        JsonValueKind ValueKind {get;}
        IValue this[int index] {get;}
        int GetArrayLength();
        string GetString();
        bool TryGetDecimal(out decimal value);
        bool TryGetDouble(out double value);
        bool TryGetProperty(string propertyName, out IValue property);
        IArrayValueEnumerator EnumerateArray();
        IObjectValueEnumerator EnumerateObject();

        bool IsJsonElement();
        JsonElement GetJsonElement();
    };

    readonly struct JsonElementValue : IValue
    {
        internal class ArrayEnumerator : IArrayValueEnumerator
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

            public IValue Current
            {
                get { return new JsonElementValue(_enumerator.Current); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IValue> GetEnumerator()
            {
                return new ArrayEnumerator(_enumerator.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        internal class ObjectEnumerator : IObjectValueEnumerator
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
                get { return new NameValuePair(_enumerator.Current.Name, new JsonElementValue(_enumerator.Current.Value)); }
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

        private readonly JsonElement _element;

        internal JsonElementValue(JsonElement element)
        {
            _element = element;
        }

        public JsonValueKind ValueKind {get{return _element.ValueKind;}}

        public IValue this[int index] {get{return new JsonElementValue(_element[index]);}}

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            JsonElement prop;
            bool r = _element.TryGetProperty(propertyName, out prop);
            property = new JsonElementValue(prop);
            return r;
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            return new ArrayEnumerator(_element.EnumerateArray());
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct DoubleValue : IValue
    {
        private readonly double _value;

        internal DoubleValue(double value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct DecimalValue : IValue
    {
        private readonly Decimal _value;

        internal DecimalValue(Decimal value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct StringValue : IValue
    {
        private readonly string _value;

        internal StringValue(string value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.String;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct TrueValue : IValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.True;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct FalseValue : IValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.False;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct NullValue : IValue
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.Null;}}

        public IValue this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
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

    readonly struct ArrayJsonValue : IValue
    {
        internal class ArrayEnumerator : IArrayValueEnumerator
        {   
            IList<IValue> _value;
            System.Collections.IEnumerator _enumerator;

            public ArrayEnumerator(IList<IValue> value)
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

            public IValue Current
            {
                get { return _enumerator.Current as IValue; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IValue> GetEnumerator()
            {
                return _value.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        private readonly IList<IValue> _value;

        internal ArrayJsonValue(IList<IValue> value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Array;}}

        public IValue this[int index] { get { return _value[index]; } }

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

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            throw new InvalidOperationException();
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            return new ArrayEnumerator(_value);
        }

        public IObjectValueEnumerator EnumerateObject()
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

} // namespace JsonCons.JsonPathLib
