using System;

namespace JsonCons.JmesPath
{
    public class JmesPathEvaluator
    {
        /// <summary>
        /// Parses a JMESPath string into a <see cref="JmesPathEvaluator"/>, for "parse once, use many times".
        /// A <see cref="JmesPathEvaluator"/> instance is thread safe and has no mutable state.
        /// </summary>
        /// <param name="jmesPath">A JMESPath string.</param>
        /// <returns>A <see cref="JmesPathEvaluator"/>.</returns>
        /// <exception cref="JmesPathParseException">
        ///   The <paramref name="jmesPath"/> parameter is not a valid JMESPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="jmesPath"/> is <see langword="null"/>.
        /// </exception>
        public static JmesPathEvaluator Parse(string jmesPath)
        {
            if (jmesPath == null)
            {
                throw new ArgumentNullException(nameof(jmesPath));
            }
            var compiler = new JmesPathParser(jmesPath);
            return compiler.Parse();
        }
    }
}
