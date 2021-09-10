using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

namespace JsonCons.JsonSchema
{

    class ReferenceValidator : KeywordValidator
    {
        KeywordValidator _referredValidator;
        internal ReferenceValidator(string id)
            : base(id) 
        {
            _referredValidator = null;
        }

        internal void SetReferredValidator(KeywordValidator target) { _referredValidator = target; }

        internal override void OnValidate(JsonElement instance, 
                                          JsonPointer instanceLocation, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch)
        {
            if (_referredValidator == null)
            {
                reporter.Error(new ValidationOutput("", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Unresolved schema reference"));
                return;
            }

            _referredValidator.Validate(instance, instanceLocation, reporter, patch);
        }

        internal override bool TryGetDefaultValue(JsonPointer instanceLocation, 
                                                  JsonElement instance, 
                                                  ErrorReporter reporter,
                                                  out JsonElement defaultValue)
        {
            if (_referredValidator == null)
            {
                reporter.Error(new ValidationOutput("", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Unresolved schema reference"));
                return;
            }
            return _referredValidator.TryGetDefaultValue(instanceLocation, instance, reporter, out defaultValue);
        }
    }

    class ValidatorRegistry
    {
        internal Dictionary<string, KeywordValidator> Validators {get;} = new Dictionary<string, KeywordValidator>();
        internal Dictionary<string, ReferenceValidator> Unresolved {get;} = new Dictionary<string, ReferenceValidator>();
        internal Dictionary<string, JsonElement> UnprocessedKeywords {get;} = new Dictionary<string, JsonElement>();
    };

    class KeywordValidatorFactory : IKeywordValidatorFactory
    {
        IList<SchemaLocation> UpdateUris(JsonElement schema,
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

        public KeywordValidator CreateKeywordValidator(JsonElement schema,
                                                       IList<SchemaLocation> uris,
                                                       IList<string> keys) 
        {
            IList<SchemaLocation> newUris = UpdateUris(schema, uris, keys);

            JsonElement element;
            KeywordValidator validator = null;

            switch (schema.ValueKind)
            {
                case JsonValueKind.True:
                    validator = TrueValidator.Create(newUris);
                    break;
                case JsonValueKind.False:
                    validator = FalseValidator.Create(newUris);
                    break;
                case JsonValueKind.Object:
                {
                    if (!schema.TryGetProperty("definitions", out JsonElement))
                    {
                        foreach (var def in element.EnumerateObject())
                        {
                            var keys = new List<string>(2);
                            keys.Add("definitions");
                            keys.Add(def.Key);
                            CreateKeywordValidator(def.Value, newUris, keys);
                        }
                    }

                    if (!schema.TryGetProperty("$ref", out JsonElement))
                    { 
                        SchemaLocation relative = new SchemaLocation(element.GetString()); 
                        SchemaLocation id = SchemaLocation.resolve(relative, newUris[newUris.Count-1]);
                        validator = GetOrCreateReference(id);
                    } 
                    else 
                    {
                        validator = TypeValidator.CreateKeywordValidator(this, schema, newUris);
                    }
                    break;
                }
                default:
                    throw new JsonSchemaException($"Invalid JSON-type for a schema for {newUris[0]} expected: boolean or object", "");
                    break;
            }

            foreach (var uri in newUris) 
            { 
                insert(uri, validator);

                if (schema.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in schema.EnumerateObject())
                        InsertUnknownKeyword(uri, item.Key, item.Value); // save unknown keywords for later reference
                }
            }

            return validator;
        }

        void Insert(SchemaLocation uri, KeywordValidator s)
        {
            var file = GetOrCreateFile(uri.Scheme);

            if (file.Validators.ContainsKey(uri.Fragment))
            {
                throw new JsonSchemaException($"Schema with {uri} already inserted.");
            }

            file.Validators.Add(uri.Fragment, s);

            // is there an Unresolved reference to this newly inserted schema?

            ReferenceValidator reference;
            if (file.Unresolved.TryGetValue(uri.Fragment, out reference))
            {
                reference.SetReferredValidator(s);
                file.Unresolved.Remove(uri.Fragment);
            }
        }

        void InsertUnknownKeyword(SchemaLocation uri, string key, JsonElement value)
        {
            var file = GetOrCreateFile(uri.Scheme);
            var newUri = SchemaLocation.Append(uri, key);

            if (newUri.HasJsonPointer) 
            {
                var fragment = newUri.Fragment;
                // is there a reference looking for this unknown-keyword, which is thus no longer a unknown keyword but a schema
                var Unresolved = file.Unresolved.find(fragment);
                if (Unresolved != file.Unresolved.end())
                    make_keyword_validator(value, {{newUri}}, {});
                else // no, nothing ref'd it, keep for later
                    file.UnprocessedKeywords[fragment] = value;

                // recursively add possible subschemas of unknown keywords
                if (value.type() == json_type::object_value)
                    for (const var& subsch : value.object_range())
                    {
                        InsertUnknownKeyword(newUri, subsch.key(), subsch.value());
                    }
            }
        }

        KeywordValidator GetOrCreateReference(const SchemaLocation& uri)
        {
            var &file = GetOrCreateFile(string(uri.base()));

            // a schema already exists
            var sch = file.Validators.find(string(uri.Fragment));
            if (sch != file.Validators.end())
                return sch->second;

            // referencing an unknown keyword, turn it into schema
            //
            // an unknown keyword can only be referenced by a JSONPointer,
            // not by a plain name identifier
            if (uri.has_json_pointer()) 
            {
                string fragment = string(uri.Fragment);
                var unprocessed_keywords_it = file.UnprocessedKeywords.find(fragment);
                if (unprocessed_keywords_it != file.UnprocessedKeywords.end()) 
                {
                    var &subsch = unprocessed_keywords_it->second; 
                    var s = make_keyword_validator(subsch, {{uri}}, {});       //  A JSON Schema MUST be an object or a boolean.
                    file.UnprocessedKeywords.erase(unprocessed_keywords_it);
                    return s;
                }
            }

            // get or create a ReferenceValidator
            var ref = file.Unresolved.lower_bound(string(uri.Fragment));
            if (ref != file.Unresolved.end() && !(file.Unresolved.key_comp()(string(uri.Fragment), ref->first))) 
            {
                return ref->second; // Unresolved, use existing reference
            } 
            else 
            {
                var orig = jsoncons::make_unique<ReferenceValidator<Json>>(uri.string());
                var p = file.Unresolved.insert(ref,
                                              {string(uri.Fragment), orig.get()})
                    ->second; // Unresolved, create new reference

                subschemas_.emplace_back(std::move(orig));
                return p;
            }
        }

        ValidatorRegistry GetOrCreateFile(string loc)
        {
            var file = subschema_registries_.lower_bound(loc);
            if (file != subschema_registries_.end() && !(subschema_registries_.key_comp()(loc, file->first)))
                return file->second;
            else
                return subschema_registries_.insert(file, {loc, {}})->second;
        }
    }

    public static int LowerBound<T>(this IList<T> sortedCollection, T key) where T : IComparable<T> 
    {
        int begin = 0;
        int end = sortedCollection.Count;
        while (end > begin) {
            int index = (begin + end) / 2;
            T el = sortedCollection[index];
            if (el.CompareTo(key) >= 0)
                end = index;
            else
                begin = index + 1;
        }
        return end;
    }

} // namespace JsonCons.JsonSchema
