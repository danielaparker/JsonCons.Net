using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;

namespace JsonCons.JsonSchema
{
    public class ValidationException : Exception
    {
        public ValidationException(string message)
            : base(message)
        {
        }
    }

    public sealed class JsonValidator
    {
        public static JsonValidator Create(JsonElement schema)
        {
            return JsonValidatorCreator.Create(schema);
        }

        public static JsonValidator Create(JsonElement schema,
                                           Func<Uri,JsonDocument> uriResolver)
        {
            return JsonValidatorCreator.Create(schema, uriResolver);
        }

        public bool TryValidate(JsonElement instance)
        {
            return true;
        }

        public void Validate(JsonElement instance, Action<ValidationOutput> reporter)
        {
        }

        public void Validate(JsonElement instance, Action<ValidationOutput> reporter, out JsonDocument patch)
        {
            patch = JsonDocument.Parse("null");
        }
    }

} // namespace JsonCons.JsonSchema
