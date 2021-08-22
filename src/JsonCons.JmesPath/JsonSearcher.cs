using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JmesPath
{
    sealed class DynamicResources 
    {
    }

    public class JsonSearcher
    {
        /// <summary>
        /// Parses a JMESPath string into a <see cref="JsonSearcher"/>, for "parse once, use many times".
        /// A <see cref="JsonSearcher"/> instance is thread safe and has no mutable state.
        /// </summary>
        /// <param name="jmesPath">A JMESPath string.</param>
        /// <returns>A <see cref="JsonSearcher"/>.</returns>
        /// <exception cref="JmesPathParseException">
        ///   The <paramref name="jmesPath"/> parameter is not a valid JMESPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="jmesPath"/> is <see langword="null"/>.
        /// </exception>
        public static JsonSearcher Parse(string jmesPath)
        {
            if (jmesPath == null)
            {
                throw new ArgumentNullException(nameof(jmesPath));
            }
            var compiler = new JmesPathParser(jmesPath);
            return compiler.Parse();
        }

        Expression _expr;

        internal JsonSearcher(Expression expr)
        {
            _expr = expr;
        }
        
        public JsonDocument Search(JsonElement doc)
        {
            var resources = new DynamicResources();
            IValue temp;
            _expr.TryEvaluate(resources, new JsonElementValue(doc), out temp);
            return JsonDocument.Parse(temp.ToString());
        }
        
    }
}
