﻿using System;
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
    /// <example>
    /// The following example shows how to get a value at a referenced location in a JSON document.
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// using JsonCons.Utilities;
    /// 
    /// public class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         using var doc = JsonDocument.Parse(@"
    ///     [
    ///       { ""category"": ""reference"",
    ///         ""author"": ""Nigel Rees"",
    ///         ""title"": ""Sayings of the Century"",
    ///         ""price"": 8.95
    ///       },
    ///       { ""category"": ""fiction"",
    ///         ""author"": ""Evelyn Waugh"",
    ///         ""title"": ""Sword of Honour"",
    ///         ""price"": 12.99
    ///       }
    ///     ]
    ///         ");
    ///     
    ///         var options = new JsonSerializerOptions() { WriteIndented = true };
    ///     
    ///         JsonPointer pointer = JsonPointer.Parse("/1/author");
    ///     
    ///         JsonElement element;
    ///     
    ///         if (pointer.TryGetValue(doc.RootElement, out element))
    ///         {
    ///             Console.WriteLine($"{JsonSerializer.Serialize(element, options)}\n");
    ///         }
    ///     }
    /// }
    /// </code>
    /// Output:
    /// 
    /// <code>
    /// "Evelyn Waugh"
    /// </code>
    /// </example>

    public sealed class JsonPointer : IEnumerable<string>, IEquatable<JsonPointer>
    {
        /// <summary>Gets a singleton instance of a <see cref="JsonPointer"/> to the root value of a JSON document.</summary>
        public static JsonPointer Default {get;} = new JsonPointer();

        enum JsonPointerState {Start, Escaped, Delim}

        /// <summary>
        /// Returns a list of (unescaped) reference tokens
        /// </summary>
        public IReadOnlyList<string> Tokens {get;}

        /// <summary>
        /// Constructs a JSON Pointer to the root value of a JSON document
        /// </summary>

        public JsonPointer()
        {
            Tokens = new List<string>();
        }

        /// <summary>
        /// Constructs a JSON Pointer from a list of (unescaped) reference tokens 
        /// </summary>
        /// <param name="tokens">A list of (unescaped) reference tokens.</param>

        public JsonPointer(IReadOnlyList<string> tokens)
        {
            Tokens = tokens;
        }

        /// <summary>
        /// Parses a JSON Pointer represented as a string value or a 
        /// fragment identifier (starts with <c>#</c>) into a <see cref="JsonPointer"/>.
        /// </summary>
        /// <param name="input">A JSON Pointer represented as a string or a fragment identifier.</param>
        /// <returns>A <see cref="JsonPointer"/>.</returns>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="input"/> is invalid.
        /// </exception>
        public static JsonPointer Parse(string input)
        {
            JsonPointer pointer;
            if (!TryParse(input, out pointer))
            {
                throw new ArgumentException("The provided JSON Pointer is invalid.");
            }
            return pointer;
        }

        /// <summary>
        /// Parses a JSON Pointer represented as a string value or a 
        /// fragment identifier (starts with <c>#</c>) into a <see cref="JsonPointer"/>.
        /// </summary>
        /// <param name="input">A JSON Pointer represented as a string or a fragment identifier.</param>
        /// <param name="pointer">The JsonPointer.</param>
        /// <returns><c>true</c> if the input string can be parsed into a list of reference tokens, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryParse(string input, out JsonPointer pointer)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            var tokens = new List<string>();

            if (input.Length == 0 || input.Equals("#")) 
            {
                pointer = new JsonPointer(tokens);
                return true;
            }

            JsonPointerState state = JsonPointerState.Start;
            int index = 0;
            var buffer = new StringBuilder();

            if (input[0] == '#') 
            {
                input = Uri.UnescapeDataString(input);
                index = 1;
            }

            while (index < input.Length)
            {
                bool done = false;
                while (index < input.Length && !done)
                {
                    switch (state)
                    {
                        case JsonPointerState.Start: 
                            switch (input[index])
                            {
                                case '/':
                                    state = JsonPointerState.Delim;
                                    break;
                                default:
                                    pointer = JsonPointer.Default;
                                    return false;
                            };
                            break;
                        case JsonPointerState.Delim: 
                            switch (input[index])
                            {
                                case '/':
                                    done = true;
                                    break;
                                case '~':
                                    state = JsonPointerState.Escaped;
                                    break;
                                default:
                                    buffer.Append(input[index]);
                                    break;
                            };
                            break;
                        case JsonPointerState.Escaped: 
                            switch (input[index])
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
                                    pointer = JsonPointer.Default;
                                    return false;
                            };
                            break;
                        default:
                            pointer = JsonPointer.Default;
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
            pointer = new JsonPointer(tokens);
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a list of reference tokens.
        /// </summary>
        /// <returns>An <c>IEnumerator&lt;string></c> for a list of reference tokens.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return Tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
           return (System.Collections.IEnumerator) GetEnumerator();
        }

        /// <summary>
        /// Returns a JSON Pointer represented as a string value.
        /// </summary>
        /// <returns>A JSON Pointer represented as a string value.</returns>

        public override string ToString()
        {
            var buffer = new StringBuilder();
            foreach (var token in Tokens)
            {
                buffer.Append("/");
                Escape(token, buffer);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a string representing the JSON Pointer as a URI fragment identifier
        /// </summary>
        /// <returns>A JSON Pointer represented as a fragment identifier.</returns>

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

        /// <summary>
        /// Determines whether two JSONPointer objects have the same value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns><c>true</c> if other is a <see cref="JsonPointer"/> and has exactly the same reference tokens as this instance; otherwise, <c>false</c>. 
        /// If other is <c>null</c>, the method returns <c>false</c>.</returns>
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
        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a JSONPointer object, have the same value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(Object other)
        {
            if (other == null)
            {
               return false;
            }

            return Equals((JsonPointer)other);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="JsonPointer"/>
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        /// <returns></returns>
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
        /// Returns <c>true</c> if the provided <see cref="JsonElement"/> contains a value at the referenced location.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> that is to be queried.</param>
        /// <returns><c>true</c> if the provided <see cref="JsonElement"/> contains a value at the referenced location, otherwise <c>false</c>.</returns>
        public bool ContainsValue(JsonElement root)
        {
            JsonElement value = root;

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
        /// Returns <c>true</c> if the provided <see cref="JsonElement"/> contains a value at the referenced location.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> that is to be queried.</param>
        /// <param name="pointer">The JSON string or URI Fragment representation of the JSON pointer.</param>
        /// <returns><c>true</c> if the provided <see cref="JsonElement"/> contains a value at the referenced location, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        public static bool ContainsValue(JsonElement root, string pointer)
        {
            if (pointer == null)
            {
                throw new ArgumentNullException(nameof(pointer));
            }
            JsonPointer location;
            if (!TryParse(pointer, out location))
            {
                return false;
            }
            JsonElement value = root;

            foreach (var token in location.Tokens)
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
        /// Gets the value at the referenced location in the provided <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> that is to be queried.</param>
        /// <param name="value">Contains the value at the referenced location, if found.</param>
        /// <returns><c>true</c> if the value was found at the referenced location, otherwise <c>false</c>.</returns>
        public bool TryGetValue(JsonElement root, out JsonElement value)
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

        /// <summary>
        /// Creates a new JsonPointer by appending a name token to the provided JsonPointer.
        /// </summary>
        /// <param name="pointer">The provided JsonPointer</param>
        /// <param name="token">A name token</param>
        /// <returns>A new JsonPointer</returns>
        public static JsonPointer Append(JsonPointer pointer, string token)
        {
            var tokens = new List<string>(pointer.Tokens);
            tokens.Add(token);
            return new JsonPointer(tokens);
        }

        /// <summary>
        /// Creates a new JsonPointer by appending an index token to the provided JsonPointer.
        /// </summary>
        /// <param name="pointer">The provided JsonPointer</param>
        /// <param name="token">An index token</param>
        /// <returns>A new JsonPointer</returns>
        public static JsonPointer Append(JsonPointer pointer, int token)
        {
            var tokens = new List<string>(pointer.Tokens);
            tokens.Add(token.ToString());
            return new JsonPointer(tokens);
        }

        /// <summary>
        /// Gets the value at the referenced location in the provided <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> that is to be queried.</param>
        /// <param name="pointer">The JSON string or URI Fragment representation of the JSON pointer.</param>
        /// <param name="value">Contains the value at the referenced location, if found.</param>
        /// <returns><c>true</c> if the value was found at the referenced location, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetValue(JsonElement root, string pointer, out JsonElement value)
        {
            if (pointer == null)
            {
                throw new ArgumentNullException(nameof(pointer));
            }
            JsonPointer location;
            if (!TryParse(pointer, out location))
            {
                value = root;
                return false;
            }

            return location.TryGetValue(root, out value);
        }

        /// <summary>
        /// Escapes a JSON Pointer token
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="token"/> is <see langword="null"/>.
        /// </exception>
        public static string Escape(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var buffer = new StringBuilder();
            Escape(token, buffer);
            return buffer.ToString();
        }

        static void Escape(string token, StringBuilder buffer)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

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
    }

} // namespace JsonCons.Utilities
