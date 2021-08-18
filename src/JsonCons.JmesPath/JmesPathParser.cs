using System;

namespace JsonCons.JmesPath
{
    /// <summary>
    /// Defines a custom exception object that is thrown when JMESPath parsing fails.
    /// </summary>    

    public class JmesPathParseException : Exception
    {
        /// <summary>
        /// The line in the JMESPath string where a parse error was detected.
        /// </summary>
        public int LineNumber {get;}

        /// <summary>
        /// The column in the JMESPath string where a parse error was detected.
        /// </summary>
        public int ColumnNumber {get;}

        internal JmesPathParseException(string message, int line, int column)
            : base(message)
        {
            LineNumber = line;
            ColumnNumber = column;
        }

        /// <summary>
        /// Returns an error message that describes the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString ()
        {
            return $"{base.Message} at line {LineNumber} and column {ColumnNumber}";
        }
    };


    ref struct JmesPathParser
    {
        ReadOnlySpan<char> _span;
        int _index;
        int _column;
        int _line;

        internal JmesPathParser(string input)
        {
            _span = input.AsSpan();
            _index = 0;
            _column = 1;
            _line = 1;
        }

        internal JmesPathEvaluator Parse()
        {
            return new JmesPathEvaluator();
        }
    }
}
