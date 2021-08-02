using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;

#nullable enable        

namespace JsonCons.JsonSchema
{

    public sealed class ValidationOutput
    {
        string InstanceLocation {get;}
        string Message {get;}
        string Keyword {get;}
        string AbsoluteKeywordLocation {get;}
        List<ValidationOutput> NestedErrors {get;}

        public ValidationOutput(string instanceLocation,
                                string message,
                                string keyword,
                                string absoluteKeywordLocation)
        {
            InstanceLocation = instanceLocation;
            Message = message;
            Keyword = keyword;
            AbsoluteKeywordLocation = absoluteKeywordLocation;
        }

        public ValidationOutput(string instanceLocation,
                                string message,
                                string keyword,
                                string absoluteKeywordLocation,
                                List<ValidationOutput> nestedErrors)
        {
            InstanceLocation = instanceLocation;
            Message = message;
            Keyword = keyword;
            AbsoluteKeywordLocation = absoluteKeywordLocation;
            NestedErrors = nestedErrors;
        }

    }

    public sealed class JsonValidator
    {
        public static JsonValidator Create(JsonElement schema)
        {
            var creator = new JsonValidatorCreator();
            return creator.Create(schema);
        }

        public static JsonValidator Create(JsonElement schema,
                                           Func<Uri,JsonDocument> uriResolver)
        {
            var creator = new JsonValidatorCreator(uriResolver);
            return creator.Create(schema);
        }

        bool TryValidate(JsonElement instance)
        {
            return true;
        }

        void Validate(JsonElement instance, Action<ValidationOutput> reporter)
        {
        }

        void Validate(JsonElement instance, Action<ValidationOutput> reporter, out JsonDocument patch)
        {
        }
    }

} // namespace JsonCons.JsonSchema
