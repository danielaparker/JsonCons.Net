using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPointerLib
{
    public sealed class JsonPointer : IEnumerable<string>, IEquatable<JsonPointer>
    {
        enum JsonPointerState {Start, Escaped, Delim}

        readonly List<string> _tokens;

        public JsonPointer(List<string> tokens)
        {
            _tokens = tokens;
        }

        public static JsonPointer Parse(string str)
        {
            var tokens = new List<string>();

            JsonPointerState state = JsonPointerState.Start;
            int index = 0;
            var buffer = new StringBuilder();

            while (index < str.Length)
            {
                bool done = false;
                while (index < str.Length && !done)
                {
                    switch (state)
                    {
                        case JsonPointerState.Start: 
                            switch (str[index])
                            {
                                case '/':
                                    state = JsonPointerState.Delim;
                                    break;
                                default:
                                    throw new JsonException("Expected slash");
                            };
                            break;
                        case JsonPointerState.Delim: 
                            switch (str[index])
                            {
                                case '/':
                                    done = true;
                                    break;
                                case '~':
                                    state = JsonPointerState.Escaped;
                                    break;
                                default:
                                    buffer.Append(str[index]);
                                    break;
                            };
                            break;
                        case JsonPointerState.Escaped: 
                            switch (str[index])
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
                                    throw new JsonException("Expected '0' or '1'");
                            };
                            break;
                        default:
                            throw new JsonException("Invalid state");
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
            return new JsonPointer(tokens);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
           return (System.Collections.IEnumerator) GetEnumerator();
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            foreach (var token in _tokens)
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

        public bool Equals(JsonPointer other)
        {
            if (other == null)
            {
               return false;
            }
            if (_tokens.Count != other._tokens.Count)
            {
                return false;
            }
            for (int i = 0; i < _tokens.Count; ++i)
            {
                if (!_tokens[i].Equals(other._tokens[i]))
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
            int hashCode = _tokens.GetHashCode();
            foreach (var token in _tokens)
            {
                hashCode += 17*token.GetHashCode();
            }
            return hashCode;
        }

        public bool TryGet(JsonElement root, out JsonElement value)
        {
            value = root;

            foreach (var token in _tokens)
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
    }

} // namespace JsonCons.JsonPointerLib
