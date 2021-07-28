using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.Utilities
{
    /// <summary>
    /// Compares two <see cref="JsonElement"/> instances for equality by using value-based comparison.
    /// </summary>

    public sealed class JsonElementComparer : IComparer<JsonElement>, System.Collections.IComparer
    {
        /// <summary>Gets a singleton instance of JsonElementEqualityComparer. This property is read-only.</summary>
        public static JsonElementComparer Instance { get; } = new JsonElementComparer();

        public JsonElementComparer() {}

        /// <summary>
        /// Determines whether the provided <see cref="JsonElement"/> objects are equal.
        /// </summary>
        /// <param name="lhs">The first object of type cref="JsonElement"/> to compare.</param>
        /// <param name="rhs">The second object of type cref="JsonElement"/> to compare.</param>
        /// <returns></returns>
        public int Compare(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.ValueKind != rhs.ValueKind)
                return (int)lhs.ValueKind - (int)rhs.ValueKind;
    
            switch (lhs.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Undefined:
                    return 0;
    
                case JsonValueKind.Number:
                {
                    Decimal dec1;
                    Decimal dec2;
                    double val1;
                    double val2;
                    if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                    {
                        return dec1.CompareTo(dec2);
                    }
                    else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                    {
                        return val1.CompareTo(val2);
                    }
                    else
                    {
                        throw new JsonException("Unable to compare numbers");
                    }
                }
    
                case JsonValueKind.String:
                    return lhs.GetString().CompareTo(rhs.GetString()); 
    
                case JsonValueKind.Array:
                {
                    var enumerator1 = lhs.EnumerateArray();
                    var enumerator2 = rhs.EnumerateArray();
                    bool result1 = enumerator1.MoveNext();
                    bool result2 = enumerator2.MoveNext();
                    while (result1 && result2)
                    {
                        int diff = this.Compare(enumerator1.Current, enumerator2.Current);
                        if (diff != 0)
                        {
                            return diff;
                        }
                        result1 = enumerator1.MoveNext();
                        result2 = enumerator2.MoveNext();
                    }   
                    return result1 ? 1 : result2 ? -1 : 0;
                }

                case JsonValueKind.Object:
                {
                    // OrderBy performs a stable sort (Note that JsonElement supports duplicate property names)
                    var enumerator1 = lhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
                    var enumerator2 = rhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
    
                    bool result1 = enumerator1.MoveNext();
                    bool result2 = enumerator2.MoveNext();
                    while (result1 && result2)
                    {
                        if (enumerator1.Current.Name != enumerator2.Current.Name)
                        {
                            return enumerator1.Current.Name.CompareTo(enumerator2.Current.Name);
                        }
                        int diff = this.Compare(enumerator1.Current.Value, enumerator2.Current.Value);
                        if (diff != 0)
                        {
                            return diff;
                        }
                        result1 = enumerator1.MoveNext();
                        result2 = enumerator2.MoveNext();
                    }   
    
                    return result1 ? 1 : result2 ? -1 : 0;
                }
    
                default:
                    throw new JsonException(string.Format("Unknown JsonValueKind {0}", lhs.ValueKind));
            }
        }

        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            return this.Compare((JsonElement)x, (JsonElement)y);
        }        
    }


} // namespace JsonCons.JsonPath
