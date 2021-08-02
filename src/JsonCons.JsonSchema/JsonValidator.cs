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

    public sealed class JsonValidator
    {
        public static JsonValidator Create(System.Text.Json.JsonElement schema)
        {
            var creator = new JsonValidatorCreator();
            return creator.Create(schema);
        }
    }

} // namespace JsonCons.JsonSchema
