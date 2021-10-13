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
    /// <summary>
    /// Defines a custom exception object that is thrown when JSONValidator creation fails.
    /// </summary>    

    public class JsonSchemaException : Exception
    {
        public string AbsoluteKeywordLocation { get;}

        internal JsonSchemaException(string message, string absoluteKeywordLocation)
            : base(message)
        {
            AbsoluteKeywordLocation = absoluteKeywordLocation;
        }

        internal JsonSchemaException(string message)
            : base(message)
        {
            AbsoluteKeywordLocation = "<unknown location>";
        }

        public override string ToString()
        {
            return $"{AbsoluteKeywordLocation}: {this.Message}";
        }
    };

    sealed class JsonValidatorCreator
    {
        Func<string,JsonDocument> _uriResolver;

        internal JsonValidatorCreator()
        {
            _uriResolver = DefaultUriResolver;
        }

        internal JsonValidatorCreator(Func<string,JsonDocument> uriResolver)
        {
            _uriResolver = uriResolver;
        }

        internal static JsonValidator Create(JsonElement schema, Func<string,JsonDocument> uriResolver)
        {
            var factory = new KeywordValidatorFactory(uriResolver);
            factory.LoadRoot(schema);
            var validator = new JsonValidator(factory.Root);

            return validator;
        }

        static JsonDocument DefaultUriResolver(string uri)
        {
            //if (uri == "/draft-07/schema")
            {
                return SchemaDraft7.Instance.Schema;
            }
            //else
            //{
            //    throw new ArgumentException($@"Don't know how to load JSON Schema {uri}");
            //}
        }
    }

} // namespace JsonCons.JsonSchema
