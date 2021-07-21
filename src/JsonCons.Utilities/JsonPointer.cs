using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.Utilities
{
    public sealed class JsonPointer : IEnumerable<string>, IEquatable<JsonPointer>
    {
        enum JsonPointerState {Start, Escaped, Delim}

        public IReadOnlyList<string> Tokens {get;}

        public JsonPointer(List<string> tokens)
        {
            Tokens = tokens;
        }

        static string UnescapePercent(string source)
        {
            if (source.Length >= 3)
            {
                var buffer = new StringBuilder();
                int end = source.Length - 3;
                int i = 0;
                while (i < end)
                {
                    char c = source[i];
                    switch (c)
                    {
                        case '%':
                            string hex = source.Substring(i+1,2);
                            char ch = (char)int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                            buffer.Append(ch);
                            i += 3;
                            break;
                        default:
                            buffer.Append(c);
                            ++i;
                            break;
                    }
                }
                return buffer.ToString();
            }
            else
            {
                return source;
            }
        }

        public static bool TryParse(string source, out JsonPointer jsonPointer)
        {
            var tokens = new List<string>();

            JsonPointerState state = JsonPointerState.Start;
            int index = 0;
            var buffer = new StringBuilder();

            if (source.Length > 0 && source[0] == '#') 
            {
                source = UnescapePercent(source.Substring(1));
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

        public string ToUriFragment()
        {
            var buffer = new StringBuilder();

            buffer.Append("#");
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
                            switch (c)
                            {
                                case '%':
                                case '^':
                                case '|': 
                                case '\\':
                                case '\"':
                                case ' ':
                                case ':':
                                case '?': 
                                case '#':
                                case '[':
                                case ']':
                                case '@':
                                case '!':
                                case '$': 
                                case '&':
                                case '\'':
                                case '(':
                                case ')':
                                case '*':
                                case '+':
                                case ',':
                                case ';':
                                case '=':
                                    buffer.Append('%');
                                    buffer.Append(((int)c).ToString("X"));
                                    break;
                                default:
                                    buffer.Append(c);
                                    break;
                            }
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

        public bool TryGet(JsonElement root, out JsonElement value)
        {
            value = root;

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

        public static bool TryGet(JsonElement root, string locationStr, out JsonElement value)
        {
            JsonPointer location;
            if (!TryParse(locationStr, out location))
            {
                value = root;
                return false;
            }

            value = root;

            foreach (var token in location)
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

        public static string Escape(string s)
        {
            var result = new StringBuilder();

            foreach (var c in s)
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
