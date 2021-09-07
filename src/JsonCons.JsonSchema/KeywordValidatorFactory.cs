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
        internal Dictionary<string, KeywordValidator> Validators {get;set;} = new Dictionary<string, KeywordValidator>();
        internal Dictionary<string, ReferenceValidator> Unresolved {get;set;} = new Dictionary<string, ReferenceValidator>();
        internal Dictionary<string, JsonElement> UnprocessedKeywords {get;set;} = new Dictionary<string, JsonElement>();
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
            var file = GetOrCreateFile(uri.base());
            auto schemas_it = file.Validators.lower_bound(uri.Fragment));
            if (schemas_it != file.Validators.end() && !(file.Validators.key_comp()(uri.Fragment, schemas_it->first))) 
            {
                JSONCONS_THROW(schema_error("schema with " + uri.string() + " already inserted"));
                return;
            }

            file.Validators.insert({string(uri.Fragment), s});

            // is there an Unresolved reference to this newly inserted schema?
            auto unresolved_it = file.Unresolved.find(string(uri.Fragment));
            if (unresolved_it != file.Unresolved.end()) 
            {
                unresolved_it->second->SetReferredValidator(s);
                file.Unresolved.erase(unresolved_it);

            }
        }

        void InsertUnknownKeyword(SchemaLocation uri, 
                                    const string& key, 
                                    const Json& value)
        {
            auto &file = GetOrCreateFile(string(uri.base()));
            auto new_u = uri.append(key);
            schema_location new_uri(new_u);

            if (new_uri.has_json_pointer()) 
            {
                auto fragment = string(new_uri.fragment());
                // is there a reference looking for this unknown-keyword, which is thus no longer a unknown keyword but a schema
                auto Unresolved = file.Unresolved.find(fragment);
                if (Unresolved != file.Unresolved.end())
                    make_keyword_validator(value, {{new_uri}}, {});
                else // no, nothing ref'd it, keep for later
                    file.UnprocessedKeywords[fragment] = value;

                // recursively add possible subschemas of unknown keywords
                if (value.type() == json_type::object_value)
                    for (const auto& subsch : value.object_range())
                    {
                        InsertUnknownKeyword(new_uri, subsch.key(), subsch.value());
                    }
            }
        }

        KeywordValidator get_or_create_reference(const schema_location& uri)
        {
            auto &file = GetOrCreateFile(string(uri.base()));

            // a schema already exists
            auto sch = file.Validators.find(string(uri.Fragment));
            if (sch != file.Validators.end())
                return sch->second;

            // referencing an unknown keyword, turn it into schema
            //
            // an unknown keyword can only be referenced by a JSONPointer,
            // not by a plain name identifier
            if (uri.has_json_pointer()) 
            {
                string fragment = string(uri.Fragment);
                auto unprocessed_keywords_it = file.UnprocessedKeywords.find(fragment);
                if (unprocessed_keywords_it != file.UnprocessedKeywords.end()) 
                {
                    auto &subsch = unprocessed_keywords_it->second; 
                    auto s = make_keyword_validator(subsch, {{uri}}, {});       //  A JSON Schema MUST be an object or a boolean.
                    file.UnprocessedKeywords.erase(unprocessed_keywords_it);
                    return s;
                }
            }

            // get or create a ReferenceValidator
            auto ref = file.Unresolved.lower_bound(string(uri.Fragment));
            if (ref != file.Unresolved.end() && !(file.Unresolved.key_comp()(string(uri.Fragment), ref->first))) 
            {
                return ref->second; // Unresolved, use existing reference
            } 
            else 
            {
                auto orig = jsoncons::make_unique<ReferenceValidator<Json>>(uri.string());
                auto p = file.Unresolved.insert(ref,
                                              {string(uri.Fragment), orig.get()})
                    ->second; // Unresolved, create new reference

                subschemas_.emplace_back(std::move(orig));
                return p;
            }
        }

        ValidatorRegistry& GetOrCreateFile(const string& loc)
        {
            auto file = subschema_registries_.lower_bound(loc);
            if (file != subschema_registries_.end() && !(subschema_registries_.key_comp()(loc, file->first)))
                return file->second;
            else
                return subschema_registries_.insert(file, {loc, {}})->second;
        }
    }

} // namespace JsonCons.JsonSchema
