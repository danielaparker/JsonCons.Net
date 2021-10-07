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

    public class FailEarlyReporter : ErrorReporter
    {
        internal override void OnError(ValidationOutput o) 
        {
        }
        public FailEarlyReporter()
            : base(true)
        {
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message)
            : base(message)
        {
        }
    }

    public sealed class JsonValidator
    {
        KeywordValidator _root;

        internal JsonValidator(KeywordValidator root)
        {
            _root = root;
        }
 
        public static JsonDocument DefaultUriResolver(Uri uri)
        {
            return JsonDocument.Parse("null");
        }

        public static JsonValidator Create(JsonElement schema)
        {
            var factory = new KeywordValidatorFactory(DefaultUriResolver);
            return JsonValidatorCreator.Create(schema, new Func<Uri,JsonDocument>(DefaultUriResolver));
        }

        public static JsonValidator Create(JsonElement schema,
                                           Func<Uri,JsonDocument> uriResolver)
        {
            return JsonValidatorCreator.Create(schema, uriResolver);
        }

        public bool TryValidate(JsonElement instance)
        {
            var location = new JsonPointer();
            var patch = new List<PatchElement>();
            var reporter = new FailEarlyReporter();
            _root.Validate(instance, location, reporter, patch);
            return reporter.ErrorCount == 0 ? true : false;
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
