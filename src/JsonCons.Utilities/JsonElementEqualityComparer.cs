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

    public sealed class JsonElementEqualityComparer : IEqualityComparer<JsonElement>
    {
        /// <summary>Gets a singleton instance of <see cref="JsonElementEqualityComparer"/>. This property is read-only.</summary>
        public static JsonElementEqualityComparer Instance { get; } = new JsonElementEqualityComparer();
    
        private int MaxHashDepth { get; } = 64;
    
        JsonElementEqualityComparer() {}

        /// <summary>
        /// Determines whether the provided <see cref="JsonElement"/> objects are equal.
        /// 
        /// If the two <see cref="JsonElement"/> instances have different data types, they are different.
        /// 
        /// If both <see cref="JsonElement"/> instances are null, true, or false, they are equal.
        /// 
        /// If both are strings, they are compared with the String.Equals method.
        /// 
        /// If both are numbers, and both can be represented by a <see cref="Decimal"/>,
        /// they are compared with the Decimal.Equals method, otherwise they are
        /// compared as doubles.
        /// 
        /// If both are objects, they are compared accoring to the following rules:
        /// 
        /// <ul>
        /// <li>If the two objects have a different number of properties, they are different.</li>
        /// <li>Otherwise, order each object's properties by name and compare sequentially.
        /// The properties are compared first by name with the String.Equals method, then by value with <see cref="JsonElementEqualityComparer"/></li>
        /// <li> A mismatching property means the two <see cref="JsonElement"/> instance are different.</li>
        /// </ul>  
        /// 
        /// If both are arrays, and both have the same length and compare equal element wise with <see cref="JsonElementEqualityComparer"/>,
        /// they are equal, otherwise they are different.
        /// </summary>
        /// <param name="lhs">The first object of type cref="JsonElement"/> to compare.</param>
        /// <param name="rhs">The second object of type cref="JsonElement"/> to compare.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        ///   Unable to compare numbers as either <see cref="Decimal"/> or double (shouldn't happen.)
        /// </exception>
        public bool Equals(JsonElement lhs, JsonElement rhs)
        {
            if (lhs.ValueKind != rhs.ValueKind)
                return false;
    
            switch (lhs.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Undefined:
                    return true;
    
                case JsonValueKind.Number:
                {
                    Decimal dec1;
                    Decimal dec2;
                    double val1;
                    double val2;
                    if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                    {
                        return dec1.Equals(dec2);
                    }
                    else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                    {
                        return val1 == val2;
                    }
                    else
                    {
                        return false;
                    }
                }
    
                case JsonValueKind.String:
                {
                    string str = lhs.GetString() ?? throw new InvalidOperationException("string cannot be null");
                    return str.Equals(rhs.GetString());
                }

                case JsonValueKind.Array:
                    return lhs.EnumerateArray().SequenceEqual(rhs.EnumerateArray(), this);
    
                case JsonValueKind.Object:
                {
                    // OrderBy performs a stable sort (Note that <see cref="JsonElement"/> supports duplicate property names)
                    var baseEnumerator1 = lhs.EnumerateObject();
                    var baseEnumerator2 = rhs.EnumerateObject();
                    if (baseEnumerator1.Count() != baseEnumerator2.Count())
                    {
                        return false;
                    }

                    var enumerator1 = baseEnumerator1.OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
                    var enumerator2 = baseEnumerator2.OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
    
                    bool result1 = enumerator1.MoveNext();
                    bool result2 = enumerator2.MoveNext();
                    while (result1 && result2)
                    {
                        if (enumerator1.Current.Name != enumerator2.Current.Name)
                        {
                            return false;
                        }
                        if (!(Equals(enumerator1.Current.Value,enumerator2.Current.Value)))
                        {
                            return false;
                        }
                        result1 = enumerator1.MoveNext();
                        result2 = enumerator2.MoveNext();
                    }   
    
                    return result1 == false && result2 == false;
                }
    
                default:
                    throw new InvalidOperationException(string.Format("Unknown JsonValueKind {0}", lhs.ValueKind));
            }
        }

        /// <summary>
        /// Returns a hash code for the specified <see cref="JsonElement"/> value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An Int32 value representing the hash code of the value.</returns>
        public int GetHashCode(JsonElement value)
        {
            return ComputeHashCode(value, 0);
        }
    
        int ComputeHashCode(JsonElement element, int depth)
        {
            int hashCode = element.ValueKind.GetHashCode();
    
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Undefined:
                    break;
    
                case JsonValueKind.Number:
                        hashCode += 17*element.GetDouble().GetHashCode();
                        break;
    
                case JsonValueKind.String:
                {
                    string str = element.GetString() ?? throw new InvalidOperationException("string cannot be null");
                    hashCode += 17 * str.GetHashCode();
                    break;
                }

                case JsonValueKind.Array:
                    if (depth < MaxHashDepth)
                        foreach (var item in element.EnumerateArray())
                            hashCode += 17*ComputeHashCode(item, depth+1);
                    break;
    
                 case JsonValueKind.Object:
                     foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                     {
                         hashCode += 17*property.Name.GetHashCode();
                         if (depth < MaxHashDepth)
                             hashCode += 17*ComputeHashCode(property.Value, depth+1);
                     }
                     break;
    
                 default:
                    throw new InvalidOperationException(string.Format("Unknown JsonValueKind {0}", element.ValueKind));
            }
            return hashCode;
        }
    }


} // namespace JsonCons.JsonPath
