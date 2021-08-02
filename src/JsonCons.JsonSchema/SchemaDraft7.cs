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

    public sealed class SchemaDraft7
    {
        /// <summary>
        /// Gets a singleton instance of <see cref="SchemaDraft7"/>. This property is read-only.
        /// </summary>

        public static SchemaDraft7 Instance { get; } = new SchemaDraft7();

        JsonDocument _schemaDoc;

        internal SchemaDraft7()
        {
        }
    }

} // namespace JsonCons.JsonSchema
