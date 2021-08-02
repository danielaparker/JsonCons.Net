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
        internal JsonSchemaException(string message)
            : base(message)
        {
        }
    };

    sealed class JsonValidatorCreator
    {
        internal JsonValidatorCreator(Func<Uri,JsonDocument> uriResolver)
        {
        }

        internal JsonValidator Create(JsonElement schema)
        {
            return new JsonValidator();
        }

        internal JsonDocument DefaultUriResolver(Uri uri)
        {
            if (uri.PathAndQuery.Equals("/draft-07/schema"))
            {
                return SchemaDraft7.Instance;
            }
            else
            {
                throw new ArgumentException($@"Don't know how to load JSON Schema {uri}");
            }
        }
    }

} // namespace JsonCons.JsonSchema
