using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
   public class JsonElementEqualityComparer : IEqualityComparer<JsonElement>
   {
       public static JsonElementEqualityComparer Instance { get; } = new JsonElementEqualityComparer();

       JsonElementEqualityComparer() {}

       int MaxHashDepth { get; } = -1;

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
                   return lhs.GetDouble() == rhs.GetDouble();

               case JsonValueKind.String:
                   return lhs.GetString() == rhs.GetString(); 

               case JsonValueKind.Array:
                   return lhs.EnumerateArray().SequenceEqual(rhs.EnumerateArray(), this);

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
                   throw new JsonException(string.Format("Unknown JsonValueKind {0}", lhs.ValueKind));
           }
       }

       public int GetHashCode(JsonElement obj)
       {
           return ComputeHashCode(obj, 0);
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
                    hashCode += 17 * element.GetString().GetHashCode();
                   break;

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
                   throw new JsonException(string.Format("Unknown JsonValueKind {0}", element.ValueKind));
           }
           return hashCode;
       }
   }


} // namespace JsonCons.JsonPathLib
