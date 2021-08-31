using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

#nullable enable        

namespace JsonCons.JsonSchema
{
    static class KeywordValidatorBuilder 
    {
        static IList<SchemaLocation> UpdateUris(JsonElement schema,
                                                IList<SchemaLocation> uris,
                                                IList<string> keys)
        {
            // Exclude uri's that are not plain name identifiers
            var newUris = new List<SchemaLocation>();
            foreach (var uri in uris)
            {
                if (!uri.HasIdentifier)
                    newUris.Add(uri);
            }

            // Append the keys for this sub-schema to the uri's
            foreach (var key in keys)
            {
                for (int i = 0; i < newUris.Count; ++i)
                {
                    var new_u = SchemaLocation.Append(newUris[i], key);
                    newUris[i] = new_u;
                }
            }
            if (schema.ValueKind == JsonValueKind.Object)
            {
                JsonElement element;
                // If $id is found, this schema can be referenced by the id
                if (schema.TryGetProperty("$id", out element)) 
                {
                    var relative = new SchemaLocation(element.GetString()); 

                    if (newUris.Find(x => x.Equals(relative)) == null)
                    {
                        SchemaLocation newUri = SchemaLocation.Resolve(relative, newUris[newUris.Count-1]);
                        newUris.Add(newUri); 
                    }
                }
            }

            return newUris;
        }

        static internal KeywordValidator Build(JsonElement schema,
                                               IList<SchemaLocation> uris,
                                               IList<string> keys) 
        {
            IList<SchemaLocation> newUris = UpdateUris(schema, uris, keys);

            KeywordValidator? sch = null;
/*
            switch (schema.ValueKind)
            {
                case JsonValueKind.True:
                    sch = make_true_keyword(newUris);
                    break;
                case JsonValueKind.False:
                    if (schema.template as<bool>())
                    {
                        sch = make_true_keyword(newUris);
                    }
                    else
                    {
                        sch = make_false_keyword(newUris);
                    }
                    break;
                case JsonValueKind.Object:
                {
                    var it = schema.find("definitions");
                    if (it != schema.object_range().end()) 
                    {
                        for (const var& def : it->value().object_range())
                            Build(def.value(), newUris, {"definitions", def.key()});
                    }

                    it = schema.find("$ref");
                    if (it != schema.object_range().end()) // this schema is a reference
                    { 
                        SchemaLocation relative(it->value().template as<string>()); 
                        SchemaLocation id = relative.resolve(newUris.back());
                        sch = get_or_create_reference(id);
                    } 
                    else 
                    {
                        sch = make_type_keyword(schema, newUris);
                    }
                    break;
                }
                default:
                    JSONCONS_THROW(schema_error("invalid JSON-type for a schema for " + newUris[0].string() + ", expected: boolean or object"));
                    break;
            }

            for (const var& uri : newUris) 
            { 
                insert(uri, sch);

                if (schema.ValueKind == JsonValueKind.Object)
                {
                    for (const var& item : schema.object_range())
                        insert_unknown_keyword(uri, item.key(), item.value()); // save unknown keywords for later reference
                }
            }
*/
            return sch;
        }
    }

} // namespace JsonCons.JsonSchema
