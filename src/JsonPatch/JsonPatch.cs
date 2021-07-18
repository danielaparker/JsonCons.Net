using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.JsonLib;
using JsonCons.JsonPointerLib;

namespace JsonCons.JsonPatchLib
{
     /// <summary>
    /// Captures error message and the related entity and the operation that caused it.
    /// </summary>
    public class JsonPatchError
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonPatchError"/>.
        /// </summary>
        /// <param name="operation">The operation that caused the error.</param>
        /// <param name="message">The error message.</param>
        public JsonPatchError(
            string operation,
            string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Operation = operation;
            ErrorMessage = message;
        }

        /// <summary>
        /// Gets the <see cref="string"/> that caused the error.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }
    }

    public static class JsonPatch
    {
        public static JsonDocument ApplyPatch(JsonElement target, JsonElement patch, Action<JsonPatchError> errorReporter)
        {
            var unpacked = UnpackedJsonElement.Unpack(target);
            ApplyPatch(ref unpacked, patch, errorReporter);
            return unpacked.ToJsonDocument();
        }

        static void ApplyPatch(ref UnpackedJsonElement target, JsonElement patch, Action<JsonPatchError> errorReporter)
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
                    errorReporter(new JsonPatchError(op, "Invalid patch"));
                    return;
                }
                string path = pathElement.GetString();

                var location = JsonPointer.Parse(path);

                if (op =="test")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }

                    UnpackedJsonElement tested;
                    if (!location.TryGet(target, out tested))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }

                    using (var doc = tested.ToJsonDocument())
                    {
                        if (!comparer.Equals(doc.RootElement, value))
                        {
                            errorReporter(new JsonPatchError(op, "Test failed"));
                            return;
                        }
                    }
                }
                else if (op =="add")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }
                    var unpackedValue = UnpackedJsonElement.Unpack(value);
                    if (!location.TryAddIfAbsent(ref target, unpackedValue)) // try insert without replace
                    {
                        if (!location.TryReplace(ref target, unpackedValue)) // try insert without replace
                        {
                            errorReporter(new JsonPatchError(op, "Add failed"));
                            return;
                        }
                    }
                }
                else if (op =="remove")
                {
                    if (!location.TryRemove(ref target)) 
                    {
                        errorReporter(new JsonPatchError(op, "Add failed"));
                        return;
                    }
                }
                else if (op =="replace")
                {
                    JsonElement value;
                    if (!operation.TryGetProperty("value", out value))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }
                    var unpackedValue = UnpackedJsonElement.Unpack(value);
                    if (!location.TryReplace(ref target, unpackedValue))
                    {
                        errorReporter(new JsonPatchError(op, "Replace failed"));
                        return;
                    }
                }
                else if (op =="move")
                {
                    JsonElement fromElement;
                    if (!operation.TryGetProperty("from", out fromElement))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }
                    string from = fromElement.GetString();

                    var fromPointer = JsonPointer.Parse(from);
                    UnpackedJsonElement value;
                    if (!fromPointer.TryGet(target, out value))
                    {
                        errorReporter(new JsonPatchError(op, "Move failed"));
                        return;
                    }

                    if (!fromPointer.TryRemove(ref target))
                    {
                        errorReporter(new JsonPatchError(op, "Move failed"));
                        return;
                    }
                    if (!location.TryAddIfAbsent(ref target, value))
                    {
                        if (!location.TryReplace(ref target, value)) // try insert without replace
                        {
                            errorReporter(new JsonPatchError(op, "Move failed"));
                            return;
                        }
                    }
                }
                else if (op =="copy")
                {
                    JsonElement fromElement;
                    if (!operation.TryGetProperty("from", out fromElement))
                    {
                        errorReporter(new JsonPatchError(op, "Invalid patch"));
                        return;
                    }
                    string from = fromElement.GetString();
                    var fromPointer = JsonPointer.Parse(from);

                    UnpackedJsonElement value;
                    if (!fromPointer.TryGet(target, out value))
                    {
                        errorReporter(new JsonPatchError(op, "Copy failed"));
                        return;
                    }
                    if (!location.TryAddIfAbsent(ref target, value))
                    {
                        if (!location.TryReplace(ref target, value)) // try insert without replace
                        {
                            errorReporter(new JsonPatchError(op, "Move failed"));
                            return;
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

        static UnpackedJsonElement FromDiff(JsonElement source, 
                                            JsonElement target, 
                                            string path)
        {
            var result = new UnpackedJsonElement(JsonValueKind.Array);

            JsonElementEqualityComparer comparer = JsonElementEqualityComparer.Instance;

            if (comparer.Equals(source,target))
            {
                return result;
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
                        result.GetList().Add(item);
                    }
                }
                // Element in source, not in target - remove
                for (int i = source.GetArrayLength(); i-- > target.GetArrayLength();)
                {
                    var buffer = new StringBuilder(path); 
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var val = new UnpackedJsonElement(JsonValueKind.Object);
                    val.GetDictionary().Add("op", new UnpackedJsonElement("remove"));
                    val.GetDictionary().Add("path", new UnpackedJsonElement(buffer.ToString()));
                    result.GetList().Add(val);
                }
                // Element in target, not in source - add, 
                for (int i = source.GetArrayLength(); i < target.GetArrayLength(); ++i)
                {
                    var a = target[i];
                    var buffer = new StringBuilder(path); 
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var val = new UnpackedJsonElement(JsonValueKind.Object);
                    val.GetDictionary().Add("op", new UnpackedJsonElement("add"));
                    val.GetDictionary().Add("path", new UnpackedJsonElement(buffer.ToString()));
                    val.GetDictionary().Add("value", UnpackedJsonElement.Unpack(a));
                    result.GetList().Add(val);
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
                            result.GetList().Add(item);
                        }
                    }
                    else
                    {
                        var val = new UnpackedJsonElement(JsonValueKind.Object);
                        val.GetDictionary().Add("op", new UnpackedJsonElement("remove"));
                        val.GetDictionary().Add("path", new UnpackedJsonElement(buffer.ToString()));
                        result.GetList().Add(val);
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
                        var val = new UnpackedJsonElement(JsonValueKind.Object);
                        val.GetDictionary().Add("op", new UnpackedJsonElement("add"));
                        val.GetDictionary().Add("path", new UnpackedJsonElement(buffer.ToString()));
                        val.GetDictionary().Add("value", UnpackedJsonElement.Unpack(a.Value));
                        result.GetList().Add(val);
                    }
                }
            }
            else
            {
                var val = new UnpackedJsonElement(JsonValueKind.Object);
                val.GetDictionary().Add("op", new UnpackedJsonElement("replace"));
                val.GetDictionary().Add("path", new UnpackedJsonElement(path));
                val.GetDictionary().Add("value", UnpackedJsonElement.Unpack(target));
                result.GetList().Add(val);
            }

            return result;
        }
    }

} // namespace JsonCons.JsonPointerLib
