using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;

namespace JsonCons.Utilities
{
    public static class JsonMergePatch
    {
        /* define ApplyMergePatch(Target, Patch):
     if Patch is an Object:
       if Target is not an Object:
         Target = {} # Ignore the contents and set it to an empty Object
       for each Name/Value pair in Patch:
         if Value is null:
           if Name exists in Target:
             remove the Name/Value pair from Target
         else:
           Target[Name] = ApplyMergePatch(Target[Name], Value)
       return Target
     else:
       return Patch*/

        public static JsonDocument ApplyMergePatch(JsonElement source, JsonElement patch)
        {
            var documentBuilder = new JsonDocumentBuilder(source);
            var builder = ApplyMergePatch(ref documentBuilder, patch);
            return builder.ToJsonDocument();
        }

        static JsonDocumentBuilder ApplyMergePatch(ref JsonDocumentBuilder target, JsonElement patch)
        {
            if (patch.ValueKind == JsonValueKind.Object)
            {
                if (target.ValueKind != JsonValueKind.Object)
                {
                    target = new JsonDocumentBuilder(JsonValueKind.Object);
                }
                foreach (var property in patch.EnumerateObject())
                {
                    JsonDocumentBuilder item;
                    if (target.TryGetProperty(property.Name, out item))
                    {
                        target.RemoveProperty(property.Name);
                        if (property.Value.ValueKind != JsonValueKind.Null)
                        {
                            target.AddProperty(property.Name, ApplyMergePatch(ref item, property.Value));
                        }
                    }
                    else
                    {
                        target.AddProperty(property.Name, ApplyMergePatch(ref item, property.Value));
                    }
                }
                return target;
            }
            else
            {
                return new JsonDocumentBuilder(patch);
            }
        }
    }


} // namespace JsonCons.Utilities
