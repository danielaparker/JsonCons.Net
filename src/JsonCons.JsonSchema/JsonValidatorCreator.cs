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
        Func<Uri,JsonDocument> _uriResolver;

        internal JsonValidatorCreator()
        {
            _uriResolver = DefaultUriResolver;
        }

        internal JsonValidatorCreator(Func<Uri,JsonDocument> uriResolver)
        {
            _uriResolver = uriResolver;
        }

        internal JsonValidator Create(JsonElement schema)
        {
            return new JsonValidator();
        }

        static JsonDocument DefaultUriResolver(Uri uri)
        {
            if (uri.PathAndQuery.Equals("/draft-07/schema"))
            {
                return SchemaDraft7.Instance.Schema;
            }
            else
            {
                throw new ArgumentException($@"Don't know how to load JSON Schema {uri}");
            }
        }
    }

} // namespace JsonCons.JsonSchema
