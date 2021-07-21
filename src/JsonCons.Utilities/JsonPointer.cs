using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.Utilities
{
    /// <summary>
    /// Represents a JSON Pointer as defined by <see href="https://datatracker.ietf.org/doc/html/rfc6901">RFC 6901</see>
    /// </summary>

    public sealed class JsonPointer : IEnumerable<string>, IEquatable<JsonPointer>
    {
        enum JsonPointerState {Start, Escaped, Delim}

        public IReadOnlyList<string> Tokens {get;}

        /// <summary>
        /// Constructs a JSON Pointer from a list of tokens 
        /// </summary>

        public JsonPointer(IReadOnlyList<string> tokens)
        {
            Tokens = tokens;
        }


        public static bool TryParse(string source, out JsonPointer jsonPointer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var tokens = new List<string>();

            JsonPointerState state = JsonPointerState.Start;
            int index = 0;
            var buffer = new StringBuilder();

            if (source.Length > 0 && source[0] == '#') 
            {
                source = Uri.UnescapeDataString(source);
                index = 1;
            }

            while (index < source.Length)
            {
                bool done = false;
                while (index < source.Length && !done)
                {
                    switch (state)
                    {
                        case JsonPointerState.Start: 
                            switch (source[index])
                            {
                                case '/':
                                    state = JsonPointerState.Delim;
                                    break;
                                default:
                                    jsonPointer = null;
                                    return false;
                            };
                            break;
                        case JsonPointerState.Delim: 
                            switch (source[index])
                            {
                                case '/':
                                    done = true;
                                    break;
                                case '~':
                                    state = JsonPointerState.Escaped;
                                    break;
                                default:
                                    buffer.Append(source[index]);
                                    break;
                            };
                            break;
                        case JsonPointerState.Escaped: 
                            switch (source[index])
                            {
                                case '0':
                                    buffer.Append('~');
                                    state = JsonPointerState.Delim;
                                    break;
                                case '1':
                                    buffer.Append('/');
                                    state = JsonPointerState.Delim;
                                    break;
                                default:
                                    jsonPointer = null;
                                    return false;
                            };
                            break;
                        default:
                            jsonPointer = null;
                            return false;
                    }
                    ++index;
                }
                tokens.Add(buffer.ToString());
                buffer.Clear();
            }
            if (buffer.Length > 0)
            {
                tokens.Add(buffer.ToString());
            }
            jsonPointer = new JsonPointer(tokens);
            return true;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
           return (System.Collections.IEnumerator) GetEnumerator();
        }

        /// <summary>
        /// Returns a string representing the JSON Pointer as a string value
        /// </summary>

        public override string ToString()
        {
            var buffer = new StringBuilder();
            foreach (var token in Tokens)
            {
                buffer.Append("/");
                foreach (var c in token)
                {
                    switch (c)
                    {
                        case '~':
                            buffer.Append('~');
                            buffer.Append('0');
                            break;
                        case '/':
                            buffer.Append('~');
                            buffer.Append('1');
                            break;
                        default:
                            buffer.Append(c);
                            break;
                    }
                }
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a string representing the JSON Pointer as a URI fragment identifier
        /// </summary>

        public string ToUriFragment()
        {
            var buffer = new StringBuilder();

            buffer.Append("#");
            foreach (var token in Tokens)
            {
                buffer.Append("/");
                string s = Uri.EscapeUriString(token);
                var span = s.AsSpan();
                for (int i = 0; i < span.Length; ++i)
                {
                    char c = span[i];
                    switch (c)
                    {
                        case '~':
                            buffer.Append('~');
                            buffer.Append('0');
                            break;
                        case '/':
                            buffer.Append('~');
                            buffer.Append('1');
                            break;
                        default:
                            buffer.Append(c);
                            break;
                    }
                }
            }
            return buffer.ToString();
        }

        public bool Equals(JsonPointer other)
        {
            if (other == null)
            {
               return false;
            }
            if (Tokens.Count != other.Tokens.Count)
            {
                return false;
            }
            for (int i = 0; i < Tokens.Count; ++i)
            {
                if (!Tokens[i].Equals(other.Tokens[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(Object other)
        {
            if (other == null)
            {
               return false;
            }

            return Equals(other as JsonPointer);
        }

        public override int GetHashCode()
        {
            int hashCode = Tokens.GetHashCode();
            foreach (var token in Tokens)
            {
                hashCode += 17*token.GetHashCode();
            }
            return hashCode;
        }

        /// <summary>
        /// Evaluates this JSON Pointer on the provided target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(JsonElement target, out JsonElement value)
        {
            value = target;

            foreach (var token in Tokens)
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    if (token == "-")
                    {
                        return false;
                    }
                    int index = 0;
                    if (!int.TryParse(token, out index))
                    {
                        return false;
                    }
                    if (index >= value.GetArrayLength())
                    {
                        return false;
                    }
                    value = value[index];
                }
                else if (value.ValueKind == JsonValueKind.Object)
                {
                    if (!value.TryGetProperty(token, out value))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the value at the referenced location in the specified target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="jsonPointer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGet(JsonElement target, string jsonPointer, out JsonElement value)
        {
            if (jsonPointer == null)
            {
                throw new ArgumentNullException(nameof(jsonPointer));
            }
            JsonPointer location;
            if (!TryParse(jsonPointer, out location))
            {
                value = target;
                return false;
            }

            return location.TryGet(target, out value);
        }

        public static string Escape(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var result = new StringBuilder();

            foreach (var c in token)
            {
                if (c == '~')
                {
                    result.Append('~');
                    result.Append('0');
                }
                else if (c == '/')
                {
                    result.Append('~');
                    result.Append('1');
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }
    }

} // namespace JsonCons.Utilities
