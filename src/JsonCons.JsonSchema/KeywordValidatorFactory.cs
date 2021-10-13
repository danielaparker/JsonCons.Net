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
        Func<string,JsonDocument> _uriResolver;
        IDictionary<string,ValidatorRegistry> _validatorRegistries;
        KeywordValidator _root;

        internal KeywordValidator Root {get{return _root;}}

        internal KeywordValidatorFactory(Func<string,JsonDocument> uriResolver)
        {
            _uriResolver = uriResolver;
            _validatorRegistries = new Dictionary<string,ValidatorRegistry>();

        }

        internal void LoadRoot(JsonElement sch)
        {
            if (sch.ValueKind == JsonValueKind.Object)
            {
                JsonElement schemaIdElement;
                if (sch.TryGetProperty("$schema", out schemaIdElement))
                {
                    string s = schemaIdElement.GetString();
                }
            }
            Load(sch);
        }

        internal void Load(JsonElement sch)
        {
            _validatorRegistries.Clear();
            _root = CreateKeywordValidator(sch, new List<SchemaLocation>(){new SchemaLocation("#")}, new List<string>(){});

            // Load all external schemas that have not already been loaded

            int loadedCount = 0;
            do 
            {
                loadedCount = 0;

                var locations = new List<string>();
                foreach (var item in _validatorRegistries)
                    locations.Add(item.Key);

                foreach (var loc in locations) 
                {
                    if (_validatorRegistries[loc].Validators.Count == 0) // registry for this file is empty
                    { 
                        if (_uriResolver != null) 
                        {
                            JsonDocument externalSchema = _uriResolver(loc);
                            CreateKeywordValidator(externalSchema.RootElement, new List<SchemaLocation>{new SchemaLocation(loc)}, new List<string>{});
                            ++loadedCount;
                        } 
                        else 
                        {
                            throw new Exception($"External schema reference '{loc}' needs to be loaded, but no resolver provided");
                        }
                    }
                }
            } 
            while (loadedCount > 0);

            foreach (var file in _validatorRegistries)
            {
                if (file.Value.Unresolved.Count != 0)
                {
                    throw new Exception($"after all files have been parsed, '{(file.Key == "" ? "<root>" : file.Key)}' has still undefined references.");
                }
            }
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
                    if (schema.TryGetProperty("definitions", out element))
                    {
                        foreach (var def in element.EnumerateObject())
                        {
                            var keys2 = new List<string>(2);
                            keys2.Add("definitions");
                            keys2.Add(def.Name);
                            CreateKeywordValidator(def.Value, newUris, keys2);
                        }
                    }

                    if (schema.TryGetProperty("$ref", out element))
                    { 
                        SchemaLocation relative = new SchemaLocation(element.GetString()); 
                        SchemaLocation id = SchemaLocation.Resolve(relative, newUris[newUris.Count-1]);
                        validator = GetOrCreateReference(id);
                    } 
                    else 
                    {
                        validator = CreateKeywordValidator(schema, newUris, new List<string>() { });
                    }
                    break;
                }
                default:
                    throw new JsonSchemaException($"Invalid JSON-type for a schema for {newUris[0]} expected: boolean or object", "");
            }

            foreach (var uri in newUris) 
            { 
                if (uri.IsAbsoluteUri)
                {
                    Insert(uri, validator);
                    if (schema.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var item in schema.EnumerateObject())
                            InsertUnknownKeyword(uri, item.Name, item.Value); // save unknown keywords for later reference
                    }
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

            if (newUri.HasPointer) 
            {
                var fragment = newUri.Fragment;
                // is there a reference looking for this unknown-keyword, which is thus no longer a unknown keyword but a schema
                ReferenceValidator reference;
                if (file.Unresolved.TryGetValue(fragment, out reference))
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
            var file = GetOrCreateRegistry(uri.AbsolutePath);

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
            if (uri.HasPointer) 
            {
                string fragment = uri.Fragment;
                JsonElement subsch;
                if (file.UnprocessedKeywords.TryGetValue(fragment, out subsch))
                {
                    var s = CreateKeywordValidator(subsch, new List<SchemaLocation>(){uri}, new List<string>(){});       //  A JSON Schema MUST be an object or a boolean.
                    file.UnprocessedKeywords.Remove(fragment);
                    return s;
                }
            }

            // get or create a ReferenceValidator
            ReferenceValidator validator;
            if (file.Unresolved.TryGetValue(uri.Fragment, out validator))
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

    }

    static class Utilities 
    {
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

    };

} // namespace JsonCons.JsonSchema
