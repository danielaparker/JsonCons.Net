using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public readonly struct NameValuePair
    {
        public string Name { get; }
        public IOperand Value { get; }

        public NameValuePair(string name, IOperand value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IJsonArrayEnumerator : IEnumerator<IOperand>, IEnumerable<IOperand>
    {
    }

    public interface IJsonObjectEnumerator : IEnumerator<NameValuePair>, IEnumerable<NameValuePair>
    {
    }

    public interface IOperand 
    {
        JsonValueKind ValueKind {get;}
        IOperand this[int index] {get;}
        int GetArrayLength();
        string GetString();
        bool TryGetDecimal(out decimal value);
        bool TryGetDouble(out double value);
        bool TryGetProperty(string propertyName, out IOperand property);
        IJsonArrayEnumerator EnumerateArray();
        IJsonObjectEnumerator EnumerateObject();

        bool IsJsonElement();
        JsonElement GetJsonElement();
    };

    public readonly struct JsonElementOperand : IOperand
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

            public IOperand Current
            {
                get { return new JsonElementOperand(_enumerator.Current); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IOperand> GetEnumerator()
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
                get { return new NameValuePair(_enumerator.Current.Name, new JsonElementOperand(_enumerator.Current.Value)); }
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

        internal JsonElementOperand(JsonElement element)
        {
            _element = element;
        }

        public JsonValueKind ValueKind {get{return _element.ValueKind;}}

        public IOperand this[int index] {get{return new JsonElementOperand(_element[index]);}}

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

        public bool TryGetProperty(string propertyName, out IOperand property)
        {
            JsonElement prop;
            bool r = _element.TryGetProperty(propertyName, out prop);
            property = new JsonElementOperand(prop);
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

    readonly struct DoubleOperand : IOperand
    {
        private readonly double _value;

        internal DoubleOperand(double value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct DecimalOperand : IOperand
    {
        private readonly Decimal _value;

        internal DecimalOperand(Decimal value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Number;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct StringOperand : IOperand
    {
        private readonly string _value;

        internal StringOperand(string value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.String;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct TrueOperand : IOperand
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.True;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct FalseOperand : IOperand
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.False;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct NullOperand : IOperand
    {
        public JsonValueKind ValueKind {get{return JsonValueKind.Null;}}

        public IOperand this[int index] { get { throw new InvalidOperationException(); } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

    readonly struct ArrayJsonValue : IOperand
    {
        public class ArrayEnumerator : IJsonArrayEnumerator
        {   
            IList<IOperand> _value;
            System.Collections.IEnumerator _enumerator;

            public ArrayEnumerator(IList<IOperand> value)
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

            public IOperand Current
            {
                get { return _enumerator.Current as IOperand; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public IEnumerator<IOperand> GetEnumerator()
            {
                return _value.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
               return (System.Collections.IEnumerator) GetEnumerator();
            }
        }

        private readonly IList<IOperand> _value;

        internal ArrayJsonValue(IList<IOperand> value)
        {
            _value = value;
        }

        public JsonValueKind ValueKind {get{return JsonValueKind.Array;}}

        public IOperand this[int index] { get { return _value[index]; } }

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

        public bool TryGetProperty(string propertyName, out IOperand property)
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

} // namespace JsonCons.JsonPathLib
