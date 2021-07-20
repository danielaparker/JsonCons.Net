using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;

namespace JsonCons.Utilities
{
     /// <summary>
    /// Captures error message and the related entity and the operation that caused it.
    /// </summary>
    public class JsonPatchException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonPatchException"/>.
        /// </summary>
        /// <param name="operation">The operation that caused the error.</param>
        /// <param name="message">The error message.</param>
        public JsonPatchException(
            string operation,
            string message) : base(message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Operation = operation;
        }

        /// <summary>
        /// Gets the <see cref="string"/> that caused the error.
        /// </summary>
        public string Operation { get; }
    }

    public static class JsonPatch
    {
        public static JsonDocument ApplyPatch(JsonElement target, JsonElement patch)
        {
            var documentBuilder = new JsonDocumentBuilder(target);
            ApplyPatch(ref documentBuilder, patch);
            return documentBuilder.ToJsonDocument();
        }

        static void ApplyPatch(ref JsonDocumentBuilder target, JsonElement patch)
        {
            JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (patch.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Invalid patch");
            }
            
            foreach (var operation in patch.EnumerateArray())
            {
                JsonElement opElement;
                if (!operation.TryGetProperty("op", out opElement))
                {
                    throw new InvalidOperationException("Invalid patch");
                }
                string op = opElement.GetString();

                JsonElement pathElement;
                if (!operation.TryGetProperty("path", out pathElement))
                {
                    throw new JsonPatchException(op, "Invalid patch");
                }
                string path = pathElement.GetString();

                JsonPointer location;
                if (!JsonPointer.TryParse(path, out location))
                {
                    throw new JsonPatchException(op, "Invalid patch");
                }

                if (op =="test")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }

                    JsonDocumentBuilder tested;
                    if (!location.TryGet(target, out tested))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }

                    using (var doc = tested.ToJsonDocument())
                    {
                        if (!comparer.Equals(doc.RootElement, value))
                        {
                            throw new JsonPatchException(op, "Test failed");
                        }
                    }
                }
                else if (op =="add")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }
                    var valueBuilder = new JsonDocumentBuilder(value);
                    if (!location.TryAddIfAbsent(ref target, valueBuilder)) // try insert without replace
                    {
                        if (!location.TryReplace(ref target, valueBuilder)) // try insert without replace
                        {
                            throw new JsonPatchException(op, "Add failed");
                        }
                    }
                }
                else if (op =="remove")
                {
                    if (!location.TryRemove(ref target)) 
                    {
                        throw new JsonPatchException(op, "Add failed");
                    }
                }
                else if (op =="replace")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }
                    var valueBuilder = new JsonDocumentBuilder(value);
                    if (!location.TryReplace(ref target, valueBuilder))
                    {
                        throw new JsonPatchException(op, "Replace failed");
                    }
                }
                else if (op =="move")
                {
                    JsonElement fromElement;
                    if (!operation.TryGetProperty("from", out fromElement))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }
                    string from = fromElement.GetString();

                    JsonPointer fromPointer;
                    if (!JsonPointer.TryParse(from, out fromPointer))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }

                    JsonDocumentBuilder value;
                    if (!fromPointer.TryGet(target, out value))
                    {
                        throw new JsonPatchException(op, "Move failed");
                    }

                    if (!fromPointer.TryRemove(ref target))
                    {
                        throw new JsonPatchException(op, "Move failed");
                    }
                    if (!location.TryAddIfAbsent(ref target, value))
                    {
                        if (!location.TryReplace(ref target, value)) // try insert without replace
                        {
                            throw new JsonPatchException(op, "Move failed");
                        }
                    }
                }
                else if (op =="copy")
                {
                    JsonElement fromElement;
                    if (!operation.TryGetProperty("from", out fromElement))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }
                    string from = fromElement.GetString();
                    JsonPointer fromPointer;
                    if (!JsonPointer.TryParse(from, out fromPointer))
                    {
                        throw new JsonPatchException(op, "Invalid patch");
                    }

                    JsonDocumentBuilder value;
                    if (!fromPointer.TryGet(target, out value))
                    {
                        throw new JsonPatchException(op, "Copy failed");
                    }
                    if (!location.TryAddIfAbsent(ref target, value))
                    {
                        if (!location.TryReplace(ref target, value)) // try insert without replace
                        {
                            throw new JsonPatchException(op, "Move failed");
                        }
                    }
                }
            }
        }

        public static JsonDocument FromDiff(JsonElement source, 
                                            JsonElement target)
        {
            return FromDiff(source, target, "").ToJsonDocument();
        }

        static JsonDocumentBuilder FromDiff(JsonElement source, 
                                            JsonElement target, 
                                            string path)
        {
            var resultBuilder = new JsonDocumentBuilder(JsonValueKind.Array);

            JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

            if (comparer.Equals(source,target))
            {
                return resultBuilder;
            }

            if (source.ValueKind == JsonValueKind.Array && target.ValueKind == JsonValueKind.Array)
            {
                int common = Math.Min(source.GetArrayLength(),target.GetArrayLength());
                for (int i = 0; i < common; ++i)
                {
                    var buffer = new StringBuilder(path); 
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var temp_diff = FromDiff(source[i], target[i], buffer.ToString());
                    foreach (var item in temp_diff.GetList())
                    {
                        resultBuilder.GetList().Add(item);
                    }
                }
                // Element in source, not in target - remove
                for (int i = source.GetArrayLength(); i-- > target.GetArrayLength();)
                {
                    var buffer = new StringBuilder(path); 
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                    valBuilder.GetDictionary().Add("op", new JsonDocumentBuilder("remove"));
                    valBuilder.GetDictionary().Add("path", new JsonDocumentBuilder(buffer.ToString()));
                    resultBuilder.GetList().Add(valBuilder);
                }
                // Element in target, not in source - add, 
                for (int i = source.GetArrayLength(); i < target.GetArrayLength(); ++i)
                {
                    var a = target[i];
                    var buffer = new StringBuilder(path); 
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                    valBuilder.GetDictionary().Add("op", new JsonDocumentBuilder("add"));
                    valBuilder.GetDictionary().Add("path", new JsonDocumentBuilder(buffer.ToString()));
                    valBuilder.GetDictionary().Add("value", new JsonDocumentBuilder(a));
                    resultBuilder.GetList().Add(valBuilder);
                }
            }
            else if (source.ValueKind == JsonValueKind.Object && target.ValueKind == JsonValueKind.Object)
            {
                foreach (var a in source.EnumerateObject())
                {
                    var buffer = new StringBuilder(path);
                    buffer.Append("/"); 
                    buffer.Append(JsonPointer.Escape(a.Name));

                    JsonElement element;
                    if (target.TryGetProperty(a.Name, out element))
                    { 
                        var temp_diff = FromDiff(a.Value, element, buffer.ToString());
                        foreach (var item in temp_diff.GetList())
                        {
                            resultBuilder.GetList().Add(item);
                        }
                    }
                    else
                    {
                        var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                        valBuilder.GetDictionary().Add("op", new JsonDocumentBuilder("remove"));
                        valBuilder.GetDictionary().Add("path", new JsonDocumentBuilder(buffer.ToString()));
                        resultBuilder.GetList().Add(valBuilder);
                    }
                }
                foreach (var a in target.EnumerateObject())
                {
                    JsonElement element;
                    if (!source.TryGetProperty(a.Name, out element))
                    {
                        var buffer = new StringBuilder(path); 
                        buffer.Append("/");
                        buffer.Append(JsonPointer.Escape(a.Name));
                        var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                        valBuilder.GetDictionary().Add("op", new JsonDocumentBuilder("add"));
                        valBuilder.GetDictionary().Add("path", new JsonDocumentBuilder(buffer.ToString()));
                        valBuilder.GetDictionary().Add("value", new JsonDocumentBuilder(a.Value));
                        resultBuilder.GetList().Add(valBuilder);
                    }
                }
            }
            else
            {
                var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                valBuilder.GetDictionary().Add("op", new JsonDocumentBuilder("replace"));
                valBuilder.GetDictionary().Add("path", new JsonDocumentBuilder(path));
                valBuilder.GetDictionary().Add("value", new JsonDocumentBuilder(target));
                resultBuilder.GetList().Add(valBuilder);
            }

            return resultBuilder;
        }
    }

} // namespace JsonCons.Utilities
