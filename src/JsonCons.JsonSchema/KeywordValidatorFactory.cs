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
    static class JsonElementConstants
    {
        internal static JsonElement NullValue {get;}

        static JsonElementConstants()
        {
            using JsonDocument doc = JsonDocument.Parse("null");
            NullValue = doc.RootElement.Clone();
        }
    }

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
                defaultValue = JsonElementConstants.NullValue;
                return false;
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
        Func<Uri,JsonDocument> _uriResolver;
        IDictionary<string,ValidatorRegistry> _validatorRegistries;

        internal KeywordValidatorFactory(Func<Uri,JsonDocument> uriResolver)
        {
            _uriResolver = uriResolver;
            _validatorRegistries = new Dictionary<string,ValidatorRegistry>();

        }

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
            var file = GetOrCreateRegistry(uri.Scheme);

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
            ValidatorRegistry file = GetOrCreateRegistry(uri.Scheme);
            var newUri = SchemaLocation.Append(uri, key);

            if (newUri.HasJsonPointer) 
            {
                var fragment = newUri.Fragment;
                // is there a reference looking for this unknown-keyword, which is thus no longer a unknown keyword but a schema
                ReferenceValidator reference;
                if (file.Unresolved.TryGetValue(fragment, reference))
                {
                    CreateKeywordValidator(value, new List<SchemaLocation>(){newUri}, new List<string>());
                }
                else
                {
                    file.UnprocessedKeywords[fragment] = value;
                }

                // recursively add possible subschemas of unknown keywords
                if (value.ValueKind == JsonValueKind.Object)
                    foreach (var subsch in value.EnumerateObject())
                    {
                        InsertUnknownKeyword(newUri, subsch.Name, subsch.Value);
                    }
            }
        }

        KeywordValidator GetOrCreateReference(SchemaLocation uri)
        {
            var file = GetOrCreateRegistry(uri.Base);

            // a schema already exists
            KeywordValidator sch;
            if (file.Validators.TryGetValue(uri.Fragment, out sch))
            {
                return sch;
            }

            // referencing an unknown keyword, turn it into schema
            //
            // an unknown keyword can only be referenced by a JSONPointer,
            // not by a plain name identifier
            if (uri.HasJsonPointer) 
            {
                string fragment = uri.Fragment;
                var unprocessed_keywords_it = file.UnprocessedKeywords.find(fragment);
                if (unprocessed_keywords_it != file.UnprocessedKeywords.end()) 
                {
                    var subsch = unprocessed_keywords_it->second; 
                    var s = make_keyword_validator(subsch, {{uri}}, {});       //  A JSON Schema MUST be an object or a boolean.
                    file.UnprocessedKeywords.erase(unprocessed_keywords_it);
                    return s;
                }
            }

            // get or create a ReferenceValidator
            KeywordValidator validator;
            if (file.TryGetValue(uri.Fragment, out validator))
            {
                return validator; // Unresolved, use existing reference
            }
            else 
            {
                var orig = new ReferenceValidator(uri.ToString());
                file.Unresolved.Add(uri.Fragment, orig); // Unresolved, create new reference
                return orig;
            }
        }

        ValidatorRegistry GetOrCreateRegistry(string loc)
        {
            ValidatorRegistry registry;
            if (!_validatorRegistries.TryGetValue(loc, out registry))
            {
                registry = new ValidatorRegistry();
                _validatorRegistries.Add(loc, new ValidatorRegistry());
            }
            return registry;
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
    }

} // namespace JsonCons.JsonSchema
