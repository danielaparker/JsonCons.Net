using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

namespace JsonCons.JsonSchema
{
    abstract class KeywordValidator 
    {
        static readonly JsonElement nullValue;

        internal string AbsoluteKeywordLocation {get;}

        static KeywordValidator()
        {
            using JsonDocument doc = JsonDocument.Parse("null");
            nullValue = doc.RootElement.Clone();
        }

        internal KeywordValidator(string absoluteKeywordLocation)
        {
            AbsoluteKeywordLocation = absoluteKeywordLocation;
        }

        internal void Validate(JsonElement instance, 
                               SchemaLocation instanceLocation, 
                               ErrorReporter reporter,
                               IList<PatchElement> patch)
        {
            OnValidate(instance,instanceLocation,reporter,patch);
        }

        internal abstract void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch);

        internal virtual bool TryGetDefaultValue(SchemaLocation instanceLocation, 
                                                  JsonElement instance, 
                                                  ErrorReporter reporter,
                                                  out JsonElement defaultValue)
        {
            defaultValue = nullValue;
            return false;
        }
    }

    interface IKeywordValidatorFactory
    {
        KeywordValidator CreateKeywordValidator(JsonElement schema,
                                                IList<SchemaLocation> uris,
                                                IList<string> keys);
    }

    struct PatchElement
    {
        string _op;
        string _path;
        JsonElement _value;

        internal PatchElement(string op, string path, JsonElement value)
        {
            _op = op;
            _path = path;
            _value = value;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("{");
            buffer.Append("\"op\":");
            buffer.Append(JsonSerializer.Serialize(_op));
            buffer.Append("\"path\":");
            buffer.Append($"{JsonSerializer.Serialize(_path)}");
            buffer.Append("\"value\":");
            buffer.Append($"{JsonSerializer.Serialize(_value)}");
            buffer.Append("}");
            return buffer.ToString();
        }
    }

    class StringValidator : KeywordValidator 
    {
        int? _maxLength;
        string _maxLengthLocation;

        int? _minLength;
        string _minLengthLocation;

        Regex _pattern;
        string _patternLocation;

        IFormatValidator _formatValidator; 
        
        string _contentEncoding;
        string _contentEncodingLocation;

        string _contentMediaType;
        string _contentMediaTypeLocation;

        internal StringValidator(string absoluteKeywordLocation,
                                 int? maxLength, string maxLengthLocation,
                                 int? minLength, string minLengthLocation,
                                 Regex pattern, string patternLocation,
                                 IFormatValidator formatValidator, 
                                 string contentEncoding, string contentEncodingLocation,
                                 string contentMediaType, string contentMediaTypeLocation)
            : base(absoluteKeywordLocation)
        {
            _maxLength = maxLength;
            _maxLengthLocation = maxLengthLocation;
            _minLength = minLength;
            _minLengthLocation = minLengthLocation;
            _pattern = pattern;
            _patternLocation = patternLocation;
            _formatValidator = formatValidator;
            _contentEncoding = contentEncoding;
            _contentEncodingLocation = contentEncodingLocation;
            _contentMediaType = contentMediaType;
            _contentMediaTypeLocation = contentMediaTypeLocation;
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch)
        {
            string content = null;
            if (_contentEncoding != null)
            {
                if (_contentEncoding == "base64")
                {
                    string s = instance.GetString();
                    try
                    {
                        content = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
                    }
                    catch (Exception)
                    {
                        reporter.Error(new ValidationOutput("contentEncoding", 
                                                            _contentEncodingLocation, 
                                                            instanceLocation.ToString(), 
                                                            "Content is not a base64 string"));
                        if (reporter.FailEarly)
                        {
                            return;
                        }
                    }
                }
                else if (_contentEncoding.Length != 0)
                {
                    reporter.Error(new ValidationOutput("contentEncoding", 
                                                    _contentEncodingLocation,
                                                    instanceLocation.ToString(), 
                                                    $"Unable to check for contentEncoding '{_contentEncoding}'"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
            else
            {
                content = instance.GetString();
            }
            if (content == null)
            {
                return;
            }

            if (_contentMediaType != null) 
            {
                if (_contentMediaType.Equals("application/Json"))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(content);
                    }
                    catch (Exception e)
                    {
                        reporter.Error(new ValidationOutput("contentMediaType", 
                                                            _contentMediaTypeLocation,
                                                            instanceLocation.ToString(), 
                                                            $"Content is not JSON: {e.Message}"));
                    }
                }
            } 

            if (instance.ValueKind != JsonValueKind.String) 
            {
                return; 
            }

            if (_minLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length < _minLength) 
                {
                    reporter.Error(new ValidationOutput("minLength", 
                                                    _minLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected minLength: {_minLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_maxLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length > _maxLength)
                {
                    reporter.Error(new ValidationOutput("maxLength", 
                                                    _maxLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected maxLength: {_maxLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_pattern != null)
            {
                var match = _pattern.Match(content);
                if (match.Success)
                {
                    reporter.Error(new ValidationOutput("pattern", 
                                                    _patternLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"String '{content}' does not match pattern '{_pattern}'"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_formatValidator != null) 
            {
                _formatValidator.Validate(content, instanceLocation.ToString(), reporter);
                if (reporter.ErrorCount > 0 && reporter.FailEarly)
                {
                    return;
                }
            }
        }

        internal static StringValidator Create(JsonElement schema, IList<SchemaLocation> uris)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            int? maxLength = null;
            string maxLengthLocation = "";
            int? minLength = null;
            string minLengthLocation = "";
            Regex pattern = null;
            string patternLocation = "";
            IFormatValidator formatValidator = null; 
            string contentEncoding = null;
            string contentEncodingLocation = "";
            string contentMediaType = null;
            string contentMediaTypeLocation = "";

            JsonElement element;
            if (schema.TryGetProperty("maxLength", out element))
            {   
                maxLength = element.GetInt32();
                maxLengthLocation = SchemaLocation.Append(absoluteKeywordLocation, "maxLength").ToString();
            }
            if (schema.TryGetProperty("minLength", out element))
            {   
                minLength = element.GetInt32();
                minLengthLocation = SchemaLocation.Append(absoluteKeywordLocation, "minLength").ToString();
            }

            if (schema.TryGetProperty("pattern", out element))
            {   
                string patternString = element.GetString();
                pattern = new Regex(patternString);
                patternLocation = SchemaLocation.Append(absoluteKeywordLocation, "pattern").ToString();
            }
            if (schema.TryGetProperty("format", out element))
            {   
                string format = element.GetString();
                string formatLocation = SchemaLocation.Append(absoluteKeywordLocation, "format").ToString();
                switch (format)
                {
                    case "date-time":
                        formatValidator = new DateTimeValidator(formatLocation);
                        break;
                    case "date":
                        formatValidator = new DateValidator(formatLocation);
                        break;
                    case "time":
                        formatValidator = new TimeValidator(formatLocation);
                        break;
                    case "email":
                        formatValidator = new EmailValidator(formatLocation);
                        break;
                    case "hostname":
                        formatValidator = new HostnameValidator(formatLocation);
                        break;
                    case "ipv4":
                        formatValidator = new Ipv4Validator(formatLocation);
                        break;
                    case "ipv6":
                        formatValidator = new Ipv6Validator(formatLocation);
                        break;
                    case "regex":
                        formatValidator = new RegexValidator(formatLocation);
                        break;
                    default:
                        break;
                }
                formatLocation = SchemaLocation.Append(absoluteKeywordLocation, "format").ToString();
            }
            if (schema.TryGetProperty("contentEncoding", out element))
            {   
                contentEncoding = element.GetString();
                contentEncodingLocation = SchemaLocation.Append(absoluteKeywordLocation, "contentEncoding").ToString();
            }
            if (schema.TryGetProperty("contentMediaType", out element))
            {   
                contentMediaType = element.GetString();
                contentMediaTypeLocation = SchemaLocation.Append(absoluteKeywordLocation, "contentMediaType").ToString();
            }
            return new StringValidator(absoluteKeywordLocation.ToString(),
                                       maxLength, maxLengthLocation,
                                       minLength, minLengthLocation,
                                       pattern, patternLocation,
                                       formatValidator,
                                       contentEncoding, contentEncodingLocation,
                                       contentMediaType, contentMediaTypeLocation);
        }
    }

    class NotValidator : KeywordValidator
    {
        KeywordValidator _rule;

        internal NotValidator(string absoluteKeywordLocation, KeywordValidator rule)
            : base(absoluteKeywordLocation)
        {
            _rule = rule;
        }

        internal static NotValidator Create(IKeywordValidatorFactory validatorFactory, 
                                            JsonElement schema, 
                                            IList<SchemaLocation> uris)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);

            var keys = new List<string>();
            keys.Add("not");
            KeywordValidator rule = validatorFactory.CreateKeywordValidator(schema, uris, keys);
            return new NotValidator(absoluteKeywordLocation.ToString(), rule);
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            CollectingErrorReporter localReporter = new CollectingErrorReporter();
            _rule.Validate(instance, instanceLocation, localReporter, patch);

            if (localReporter.Errors.Count != 0)
            {
                reporter.Error(new ValidationOutput("not", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Instance must not be valid against schema"));
            }
        }

        internal override bool TryGetDefaultValue(SchemaLocation instanceLocation, 
                                                  JsonElement instance, 
                                                  ErrorReporter reporter,
                                                  out JsonElement defaultValue)
        {
            return _rule.TryGetDefaultValue(instanceLocation, instance, reporter, out defaultValue);
        }
    }

    interface ICombiningCriterion 
    {
        string Key {get;}

        bool IsComplete(JsonElement instance, 
                        SchemaLocation instanceLocation, 
                        ErrorReporter reporter, 
                        CollectingErrorReporter localReporter, 
                        int count);
    };

    struct AllOfCriterion : ICombiningCriterion
    {
        public string Key {get {return "allOf";}}

        public bool IsComplete(JsonElement instance, 
                               SchemaLocation instanceLocation, 
                               ErrorReporter reporter, 
                               CollectingErrorReporter localReporter, 
                               int count)
        {
            if (localReporter.Errors.Count == 0)
                reporter.Error(new ValidationOutput(Key, 
                                                    "",
                                                    instanceLocation.ToString(), 
                                                    "At least one keyword_validator failed to match, but all are required to match. ", 
                                                    localReporter.Errors));
            return localReporter.Errors.Count == 0;
        }
    }

    struct AnyOfCriterion : ICombiningCriterion
    {
        public string Key {get {return "anyOf";}}

        public bool IsComplete(JsonElement instance, 
                               SchemaLocation instanceLocation, 
                               ErrorReporter reporter, 
                               CollectingErrorReporter localReporter, 
                               int count)
        {
            return count == 1;
        }
    }

    struct OneOfCriterion : ICombiningCriterion
    {
        public string Key {get {return "oneOf";}}

        public bool IsComplete(JsonElement instance, 
                               SchemaLocation instanceLocation, 
                               ErrorReporter reporter, 
                               CollectingErrorReporter localReporter, 
                               int count)
        {
            if (count > 1)
            {
                reporter.Error(new ValidationOutput("oneOf", 
                    "", 
                    instanceLocation.ToString(), 
                    $"{count} subschemas matched, but exactly one is required to match"));
            }
            return count > 1;
        }
    }

    sealed class CombiningValidator : KeywordValidator
    {
        internal ICombiningCriterion AllOf = new AllOfCriterion();
        internal ICombiningCriterion AnyOf = new AnyOfCriterion();
        internal ICombiningCriterion OneOf = new OneOfCriterion();

        ICombiningCriterion _criterion; 
        IList<KeywordValidator> _validators;

        internal CombiningValidator(string absoluteKeywordLocation,
                                    ICombiningCriterion criterion,
                                    IList<KeywordValidator> validators)
            : base(absoluteKeywordLocation)
        {
            _criterion = criterion;
            _validators = validators;
            // Validate value of allOf, anyOf, and oneOf "MUST be a non-empty array"
        }

        internal static CombiningValidator Create(IKeywordValidatorFactory validatorFactory,
                                                  JsonElement schema, 
                                                  IList<SchemaLocation> uris,
                                                  ICombiningCriterion criterion)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);

            var validators = new List<KeywordValidator>();
            for (int i = 0; i < schema.GetArrayLength(); ++i)
            {
                var keys = new List<string>();
                keys.Add(criterion.Key);
                keys.Add(i.ToString());
                validators.Add(validatorFactory.CreateKeywordValidator(schema[i], uris, keys));
            }

            return new CombiningValidator(absoluteKeywordLocation.ToString(), 
                                                 criterion,
                                                 validators);
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            int count = 0;

            var localReporter = new CollectingErrorReporter();
            foreach (var s in _validators) 
            {
                int mark = localReporter.Errors.Count;
                s.Validate(instance, instanceLocation, localReporter, patch);
                if (mark == localReporter.Errors.Count)
                    count++;

                if (_criterion.IsComplete(instance, instanceLocation, reporter, localReporter, count))
                    return;
            }

            if (count == 0)
            {
                reporter.Error(new ValidationOutput("combined", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 "No KeywordValidator matched, but one of them is required to match", 
                                                 localReporter.Errors));
            }
        }
    }

    static class NumericUtilities 
    {
        internal static bool TryGetInt64(JsonElement element, out Int64 result)
        {
            if (element.ValueKind != JsonValueKind.Number)
            {
                result = 0;
                return false;
            }
            if (!element.TryGetInt64(out result))
            {
                Decimal dec;
                if (!element.TryGetDecimal(out dec))
                {
                    return false;
                }
                Decimal ceil = Decimal.Ceiling(dec);
                if (ceil != dec)
                {
                    return false;
                }
                if (ceil < Int64.MinValue || ceil > Int64.MaxValue)
                {
                    return false;
                }
                result = Decimal.ToInt64(ceil);
            }
            return true;
        }

        internal static bool TryGetDouble(JsonElement element, out double result)
        {
            if (element.ValueKind != JsonValueKind.Number)
            {
                result = 0;
                return false;
            }
            if (!element.TryGetDouble(out result))
            {
                return false;
            }
            return true;
        }

        internal static bool IsMultipleOf(double x, double multipleOf) 
        {
            return x >= multipleOf && multipleOf % x == 0;
        }
    }

    class IntegerValidator : KeywordValidator
    {
        Int64? _maximum;
        string _maximumLocation = "";
        Int64? _minimum;
        string _minimumLocation = "";
        Int64? _exclusiveMaximum;
        string _exclusiveMaximumLocation = "";
        Int64? _exclusiveMinimum;
        string _exclusiveMinimumLocation = "";
        double? _multipleOf;
        string _multipleOfLocation = "";

        internal IntegerValidator(string absoluteKeywordLocation,
                                  Int64? maximum,
                                  string maximumLocation,
                                  Int64? minimum,
                                  string minimumLocation,
                                  Int64? exclusiveMaximum,
                                  string exclusiveMaximumLocation,
                                  Int64? exclusiveMinimum,
                                  string exclusiveMinimumLocation,
                                  double? multipleOf,
                                  string multipleOfLocation)
            : base(absoluteKeywordLocation)
        {
            _maximum = maximum;
            _maximumLocation = maximumLocation;
            _minimum = minimum;
            _minimumLocation = minimumLocation;
            _exclusiveMaximum = exclusiveMaximum;
            _exclusiveMaximumLocation = exclusiveMaximumLocation;
            _exclusiveMinimum = exclusiveMinimum;
            _exclusiveMinimumLocation = exclusiveMinimumLocation;
            _multipleOf = multipleOf;
            _multipleOfLocation = multipleOfLocation;
        }

        internal static IntegerValidator Create(JsonElement sch, 
                                                IList<SchemaLocation> uris, 
                                                ISet<string> keywords)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            Int64? maximum = null;
            string maximumLocation = "";
            Int64? minimum = null;
            string minimumLocation = "";
            Int64? exclusiveMaximum = null;
            string exclusiveMaximumLocation = "";
            Int64? exclusiveMinimum = null;
            string exclusiveMinimumLocation = "";
            double? multipleOf = null;
            string multipleOfLocation = "";

            JsonElement element;
            if (sch.TryGetProperty("maximum", out element)) 
            {
                maximumLocation = SchemaLocation.Append(absoluteKeywordLocation,"maximum").ToString();
                Int64 val;
                if (!NumericUtilities.TryGetInt64(element, out val))
                {
                    throw new JsonSchemaException("'maximum' must be an Int64", maximumLocation);
                }
                maximum = val;
                keywords.Add("maximum");
            }

            if (sch.TryGetProperty("minimum", out element)) 
            {
                minimumLocation = SchemaLocation.Append(absoluteKeywordLocation,"minimum").ToString();
                Int64 val;
                if (!NumericUtilities.TryGetInt64(element, out val))
                {
                    throw new JsonSchemaException("'minimum' must be an Int64", minimumLocation);
                }
                minimum = val;
                keywords.Add("minimum");
            }

            if (sch.TryGetProperty("exclusiveMaximum", out element)) 
            {
                exclusiveMaximumLocation = SchemaLocation.Append(absoluteKeywordLocation,"exclusiveMaximum").ToString();
                Int64 val;
                if (!NumericUtilities.TryGetInt64(element, out val))
                {
                    throw new JsonSchemaException("'exclusiveMaximum' must be an Int64", exclusiveMaximumLocation);
                }
                exclusiveMaximum = val;
                keywords.Add("exclusiveMaximum");
            }

            if (sch.TryGetProperty("exclusiveMinimum", out element)) 
            {
                exclusiveMinimumLocation = SchemaLocation.Append(absoluteKeywordLocation,"exclusiveMinimum").ToString();
                Int64 val;
                if (!NumericUtilities.TryGetInt64(element, out val))
                {
                    throw new JsonSchemaException("'exclusiveMinimum' must be an Int64", exclusiveMinimumLocation);
                }
                exclusiveMinimum = val;
                keywords.Add("exclusiveMinimum");
            }

            if (sch.TryGetProperty("multipleOf", out element)) 
            {
                multipleOfLocation = SchemaLocation.Append(absoluteKeywordLocation, "multipleOf").ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'multipleOf' must be a number", multipleOfLocation);
                }
                multipleOf = val;
                keywords.Add("multipleOf");
            }
            return new IntegerValidator(absoluteKeywordLocation.ToString(),
                                        maximum,
                                        maximumLocation,
                                        minimum,
                                        minimumLocation,
                                        exclusiveMaximum,
                                        exclusiveMaximumLocation,
                                        exclusiveMinimum,
                                        exclusiveMinimumLocation,
                                        multipleOf,
                                        multipleOfLocation);
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            Int64 value;
            if (!NumericUtilities.TryGetInt64(instance, out value))
            {
                reporter.Error(new ValidationOutput("integer", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Instance is not an integer"));
                if (reporter.FailEarly)
                {
                    return;
                }
            }
            if (_multipleOf.HasValue && value != 0) // exclude zero
            {
                if (!NumericUtilities.IsMultipleOf(value, (double)_multipleOf))
                {
                    reporter.Error(new ValidationOutput("multipleOf", 
                                                        _multipleOfLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is not a multiple of _multipleOf"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_maximum.HasValue)
            {
                if (value > (Int64)_maximum)
                {
                    reporter.Error(new ValidationOutput("maximum", 
                                                        _maximumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} exceeds maximum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_minimum != null)
            {
                if (value < _minimum)
                {
                    reporter.Error(new ValidationOutput("minimum", 
                                                        _minimumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is below minimum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_exclusiveMaximum.HasValue)
            {
                if (value >= _exclusiveMaximum)
                {
                    reporter.Error(new ValidationOutput("exclusiveMaximum", 
                                                        _exclusiveMaximumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} exceeds maximum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_exclusiveMinimum.HasValue)
            {
                if (value <= _exclusiveMinimum)
                {
                    reporter.Error(new ValidationOutput("exclusiveMinimum", 
                                                        _exclusiveMinimumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is below minimum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
        }
    }

    class DoubleValidator : KeywordValidator
    {
        double? _maximum;
        string _maximumLocation = "";
        double? _minimum;
        string _minimumLocation = "";
        double? _exclusiveMaximum;
        string _exclusiveMaximumLocation = "";
        double? _exclusiveMinimum;
        string _exclusiveMinimumLocation = "";
        double? _multipleOf;
        string _multipleOfLocation = "";

        internal DoubleValidator(string absoluteKeywordLocation,
                                  double? maximum,
                                  string maximumLocation,
                                  double? minimum,
                                  string minimumLocation,
                                  double? exclusiveMaximum,
                                  string exclusiveMaximumLocation,
                                  double? exclusiveMinimum,
                                  string exclusiveMinimumLocation,
                                  double? multipleOf,
                                  string multipleOfLocation)
            : base(absoluteKeywordLocation)
        {
            _maximum = maximum;
            _maximumLocation = maximumLocation;
            _minimum = minimum;
            _minimumLocation = minimumLocation;
            _exclusiveMaximum = exclusiveMaximum;
            _exclusiveMaximumLocation = exclusiveMaximumLocation;
            _exclusiveMinimum = exclusiveMinimum;
            _exclusiveMinimumLocation = exclusiveMinimumLocation;
            _multipleOf = multipleOf;
            _multipleOfLocation = multipleOfLocation;
        }

        internal static DoubleValidator Create(JsonElement sch, 
                                                IList<SchemaLocation> uris, 
                                                ISet<string> keywords)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            double? maximum = null;
            string maximumLocation = "";
            double? minimum = null;
            string minimumLocation = "";
            double? exclusiveMaximum = null;
            string exclusiveMaximumLocation = "";
            double? exclusiveMinimum = null;
            string exclusiveMinimumLocation = "";
            double? multipleOf = null;
            string multipleOfLocation = "";

            JsonElement element;
            if (sch.TryGetProperty("maximum", out element)) 
            {
                maximumLocation = SchemaLocation.Append(absoluteKeywordLocation,"maximum").ToString().ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'maximum' must be an double", maximumLocation);
                }
                maximum = val;
                keywords.Add("maximum");
            }

            if (sch.TryGetProperty("minimum", out element)) 
            {
                minimumLocation = SchemaLocation.Append(absoluteKeywordLocation,"minimum").ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'minimum' must be an double", minimumLocation);
                }
                minimum = val;
                keywords.Add("minimum");
            }

            if (sch.TryGetProperty("exclusiveMaximum", out element)) 
            {
                exclusiveMaximumLocation = SchemaLocation.Append(absoluteKeywordLocation,"exclusiveMaximum").ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'exclusiveMaximum' must be an double", exclusiveMaximumLocation);
                }
                exclusiveMaximum = val;
                keywords.Add("exclusiveMaximum");
            }

            if (sch.TryGetProperty("exclusiveMinimum", out element)) 
            {
                exclusiveMinimumLocation = SchemaLocation.Append(absoluteKeywordLocation,"exclusiveMinimum").ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'exclusiveMinimum' must be an double", exclusiveMinimumLocation);
                }
                exclusiveMinimum = val;
                keywords.Add("exclusiveMinimum");
            }

            if (sch.TryGetProperty("multipleOf", out element)) 
            {
                multipleOfLocation = SchemaLocation.Append(absoluteKeywordLocation, "multipleOf").ToString();
                double val;
                if (!NumericUtilities.TryGetDouble(element, out val))
                {
                    throw new JsonSchemaException("'multipleOf' must be a number", multipleOfLocation);
                }
                multipleOf = val;
                keywords.Add("multipleOf");
            }
            return new DoubleValidator(absoluteKeywordLocation.ToString(),
                                        maximum,
                                        maximumLocation,
                                        minimum,
                                        minimumLocation,
                                        exclusiveMaximum,
                                        exclusiveMaximumLocation,
                                        exclusiveMinimum,
                                        exclusiveMinimumLocation,
                                        multipleOf,
                                        multipleOfLocation);
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            double value;
            if (!NumericUtilities.TryGetDouble(instance, out value))
            {
                reporter.Error(new ValidationOutput("integer", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Instance is not an integer"));
                if (reporter.FailEarly)
                {
                    return;
                }
            }
            if (_multipleOf.HasValue && value != 0) // exclude zero
            {
                if (!NumericUtilities.IsMultipleOf(value, (double)_multipleOf))
                {
                    reporter.Error(new ValidationOutput("multipleOf", 
                                                        _multipleOfLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is not a multiple of _multipleOf"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_maximum.HasValue)
            {
                if (value > (double)_maximum)
                {
                    reporter.Error(new ValidationOutput("maximum", 
                                                        _maximumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} exceeds maximum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_minimum != null)
            {
                if (value < _minimum)
                {
                    reporter.Error(new ValidationOutput("minimum", 
                                                        _minimumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is below minimum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_exclusiveMaximum.HasValue)
            {
                if (value >= _exclusiveMaximum)
                {
                    reporter.Error(new ValidationOutput("exclusiveMaximum", 
                                                        _exclusiveMaximumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} exceeds maximum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (_exclusiveMinimum.HasValue)
            {
                if (value <= _exclusiveMinimum)
                {
                    reporter.Error(new ValidationOutput("exclusiveMinimum", 
                                                        _exclusiveMinimumLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"{instance} is below minimum of + {_exclusiveMinimum}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
        }
    }

    // NullValidator

    class NullValidator : KeywordValidator
    {
        internal NullValidator(string absoluteKeywordLocation)
            : base(absoluteKeywordLocation)
        {
        }

        internal static NullValidator Create(IList<SchemaLocation> uris)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            return new NullValidator(absoluteKeywordLocation.ToString());
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            if (instance.ValueKind != JsonValueKind.Null)
            {
                reporter.Error(new ValidationOutput("null", 
                                                    this.AbsoluteKeywordLocation, 
                                                    instanceLocation.ToString(), 
                                                    "Expected to be null"));
            }
        }
    }

    class TrueValidator : KeywordValidator
    {
        TrueValidator(string absoluteKeywordLocation)
            : base(absoluteKeywordLocation)
        {
        }

        internal static TrueValidator Create(IList<SchemaLocation> uris)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            return new TrueValidator(absoluteKeywordLocation.ToString());
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
        }
    }

    class FalseValidator : KeywordValidator
    {
        FalseValidator(string absoluteKeywordLocation)
            : base(absoluteKeywordLocation)
        {
        }

        internal static FalseValidator Create(IList<SchemaLocation> uris)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            return new FalseValidator(absoluteKeywordLocation.ToString());
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            reporter.Error(new ValidationOutput("false", 
                                                this.AbsoluteKeywordLocation, 
                                                instanceLocation.ToString(), 
                                                "False schema always fails"));
        }
    }

    class RequiredValidator : KeywordValidator
    {
        IList<string> _items;

        internal RequiredValidator(string absoluteKeywordLocation, 
                                   IList<string> items)
            : base(absoluteKeywordLocation)
        {
            _items = items; 
        }


        internal static RequiredValidator Create(IList<SchemaLocation> uris,
                                                 IList<string> items)
        {
            SchemaLocation absoluteKeywordLocation = SchemaLocation.GetAbsoluteKeywordLocation(uris);
            return new RequiredValidator(absoluteKeywordLocation.ToString(), items);
        }


        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch)
        {
            JsonElement element;
            foreach (var key in _items)
            {
                if (!instance.TryGetProperty(key, out element))
                {
                    reporter.Error(new ValidationOutput("required", 
                                                        this.AbsoluteKeywordLocation, 
                                                        instanceLocation.ToString(), 
                                                        $"Required property '{key}' not found"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
        }
    }

/*
    class ObjectValidator : KeywordValidator
    {
        jsoncons::optional<int> _max_properties;
        string _absolute_max_properties_location;
        jsoncons::optional<int> _min_properties;
        string _absolute_min_properties_location;
        jsoncons::optional<RequiredValidator<Json>> _required;

        std::map<string, KeywordValidator> _properties;
        IList<std::pair<std::regex, KeywordValidator>> _pattern_properties;
        KeywordValidator _additional_properties;

        std::map<string, KeywordValidator> _dependencies;

        KeywordValidator _property_names;

        ObjectValidator(IKeywordValidatorFactory validatorFactory,
                    JsonElement sch,
                    List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), 
              max_properties_(), min_properties_(), 
              additional_properties_(nullptr),
              property_names_(nullptr)
        {
            auto it = sch.find("maxProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                max_properties_ = it.value().template as<int>();
                absolute_max_properties_location_ = SchemaLocation.Append(absoluteKeywordLocation, "maxProperties");
            }

            it = sch.find("minProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                min_properties_ = it.value().template as<int>();
                absolute_min_properties_location_ = SchemaLocation.Append(absoluteKeywordLocation, "minProperties");
            }

            it = sch.find("required");
            if (it != sch.EnumerateObject().end()) 
            {
                auto location = SchemaLocation.Append(absoluteKeywordLocation, "required");
                required_ = RequiredValidator<Json>(location, 
                                                   it.value().template as<IList<string>>());
            }

            it = sch.find("properties");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var prop : it.value().EnumerateObject())
                    properties_.emplace(
                        std::make_pair(
                            prop.key(),
                            validatorFactory.CreateKeywordValidator(prop.value(), uris, {"properties", prop.key()})));
            }

            it = sch.find("patternProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var prop : it.value().EnumerateObject())
                    pattern_properties_.emplace_back(
                        std::make_pair(
                            std::regex(prop.key(), std::regex::ECMAScript),
                            validatorFactory.CreateKeywordValidator(prop.value(), uris, {prop.key()})));
            }

            it = sch.find("additionalProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                additional_properties_ = validatorFactory.CreateKeywordValidator(it.value(), uris, {"additionalProperties"});
            }

            it = sch.find("dependencies");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var dep : it.value().EnumerateObject())
                {
                    switch (dep.value().type()) 
                    {
                        case json_type::array_value:
                        {
                            auto location = SchemaLocation.Append(absoluteKeywordLocation, "required");
                            dependencies_.emplace(dep.key(),
                                                  validatorFactory.make_required_keyword({location},
                                                                                 dep.value().template as<IList<string>>()));
                            break;
                        }
                        default:
                        {
                            dependencies_.emplace(dep.key(),
                                                  validatorFactory.CreateKeywordValidator(dep.value(), uris, {"dependencies", dep.key()}));
                            break;
                        }
                    }
                }
            }

            auto property_names_it = sch.find("propertyNames");
            if (property_names_it != sch.EnumerateObject().end()) 
            {
                property_names_ = validatorFactory.CreateKeywordValidator(property_names_it.value(), uris, {"propertyNames"});
            }
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch)
        {
            if (max_properties_ && instance.Count > *max_properties_)
            {
                string message("Maximum properties: " + std::to_string(*max_properties_));
                message.append(", found: " + std::to_string(instance.Count));
                reporter.Error(new ValidationOutput("maxProperties", 
                                                 absolute_max_properties_location_, 
                                                 instanceLocation.ToString(), 
                                                 std::move(message)));
                if (reporter.FailEarly)
                {
                    return;
                }
            }

            if (min_properties_ && instance.Count < *min_properties_)
            {
                string message("Minimum properties: " + std::to_string(*min_properties_));
                message.append(", found: " + std::to_string(instance.Count));
                reporter.Error(new ValidationOutput("minProperties", 
                                                 absolute_min_properties_location_, 
                                                 instanceLocation.ToString(), 
                                                 std::move(message)));
                if (reporter.FailEarly)
                {
                    return;
                }
            }

            if (required_)
                required_.Validate(instanceLocation, instance, reporter, patch);

            foreach (var property : instance.EnumerateObject()) 
            {
                if (property_names_)
                    property_names_.Validate(instanceLocation, property.key(), reporter, patch);

                bool a_prop_or_pattern_matched = false;
                auto properties_it = properties_.find(property.key());

                // check if it is in "properties"
                if (properties_it != properties_.end()) 
                {
                    a_prop_or_pattern_matched = true;
                    properties_it.second.Validate(instanceLocation.append(property.key()), property.value(), reporter, patch);
                }

                // check all matching "patternProperties"
                for (var schema_pp : pattern_properties_)
                    if (std::regex_search(property.key(), schema_pp.first)) 
                    {
                        a_prop_or_pattern_matched = true;
                        schema_pp.second.Validate(instanceLocation.append(property.key()), property.value(), reporter, patch);
                    }
                // finally, check "additionalProperties" 
                if (!a_prop_or_pattern_matched && additional_properties_) 
                {
                    CollectingErrorReporter localReporter = new CollectingErrorReporter();
                    additional_properties_.Validate(instanceLocation.append(property.key()), property.value(), localReporter, patch);
                    if (!localReporter.Errors.Count != 0)
                    {
                        reporter.Error(new ValidationOutput("additionalProperties", 
                                                         additional_properties_.AbsoluteKeywordLocation, 
                                                         instanceLocation.ToString(), 
                                                         "Additional property \"" + property.key() + "\" found but was invalid."));
                        if (reporter.FailEarly)
                        {
                            return;
                        }
                    }
                }
            }

            // reverse search
            for (auto const& prop : properties_) 
            {
                const auto finding = instance.find(prop.first);
                if (finding == instance.EnumerateObject().end()) 
                { 
                    // If property is not in instance
                    auto default_value = prop.second.TryGetDefaultValue(instanceLocation, instance, reporter);
                    if (default_value) 
                    { 
                        // If default value is available, update patch
                        update_patch(patch, instanceLocation.append(prop.first), std::move(*default_value));
                    }
                }
            }

            foreach (var dep : dependencies_) 
            {
                auto prop = instance.find(dep.first);
                if (prop != instance.EnumerateObject().end()) // if dependency-property is present in instance
                    dep.second.Validate(instanceLocation.append(dep.first), instance, reporter, patch); // Validate
            }
        }
    }

    // ArrayValidator

    class ArrayValidator : KeywordValidator
    {
        jsoncons::optional<int> _max_items;
        string _absolute_max_items_location;
        jsoncons::optional<int> _min_items;
        string _absolute_min_items_location;
        bool unique_items_ = false;
        KeywordValidator _items_schema;
        IList<KeywordValidator> _items;
        KeywordValidator _additional_items;
        KeywordValidator _contains;

        ArrayValidator(IKeywordValidatorFactory validatorFactory, 
                   JsonElement sch, 
                   List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), 
              max_items_(), min_items_(), items_schema_(nullptr), additional_items_(nullptr), contains_(nullptr)
        {
            {
                auto it = sch.find("maxItems");
                if (it != sch.EnumerateObject().end()) 
                {
                    max_items_ = it.value().template as<int>();
                    absolute_max_items_location_ = SchemaLocation.Append(absoluteKeywordLocation, "maxItems");
                }
            }

            {
                auto it = sch.find("minItems");
                if (it != sch.EnumerateObject().end()) 
                {
                    min_items_ = it.value().template as<int>();
                    absolute_min_items_location_ = SchemaLocation.Append(absoluteKeywordLocation, "minItems");
                }
            }

            {
                auto it = sch.find("uniqueItems");
                if (it != sch.EnumerateObject().end()) 
                {
                    unique_items_ = it.value().template as<bool>();
                }
            }

            {
                auto it = sch.find("items");
                if (it != sch.EnumerateObject().end()) 
                {

                    if (it.value().type() == json_type::array_value) 
                    {
                        int c = 0;
                        foreach (var subsch in it.value().array_range())
                            _items.Add(validatorFactory.CreateKeywordValidator(subsch, uris, {"items", std::to_string(c++)}));

                        auto attr_add = sch.find("additionalItems");
                        if (attr_add != sch.EnumerateObject().end()) 
                        {
                            additional_items_ = validatorFactory.CreateKeywordValidator(attr_add.value(), uris, {"additionalItems"});
                        }

                    } 
                    else if (it.value().type() == json_type::object_value ||
                               it.value().type() == json_type::bool_value)
                    {
                        items_schema_ = validatorFactory.CreateKeywordValidator(it.value(), uris, {"items"});
                    }

                }
            }

            {
                auto it = sch.find("contains");
                if (it != sch.EnumerateObject().end()) 
                {
                    contains_ = validatorFactory.CreateKeywordValidator(it.value(), uris, {"contains"});
                }
            }
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch)
        {
            if (max_items_)
            {
                if (instance.Count > *max_items_)
                {
                    string message("Expected maximum item count: " + std::to_string(*max_items_));
                    message.append(", found: " + std::to_string(instance.Count));
                    reporter.Error(new ValidationOutput("maxItems", 
                                                     absolute_max_items_location_, 
                                                     instanceLocation.ToString(), 
                                                     std::move(message)));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (min_items_)
            {
                if (instance.Count < *min_items_)
                {
                    string message("Expected minimum item count: " + std::to_string(*min_items_));
                    message.append(", found: " + std::to_string(instance.Count));
                    reporter.Error(new ValidationOutput("minItems", 
                                                     absolute_min_items_location_, 
                                                     instanceLocation.ToString(), 
                                                     std::move(message)));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (unique_items_) 
            {
                if (!array_has_unique_items(instance))
                {
                    reporter.Error(new ValidationOutput("uniqueItems", 
                                                     this.AbsoluteKeywordLocation, 
                                                     instanceLocation.ToString(), 
                                                     "Array items are not unique"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            int index = 0;
            if (items_schema_)
            {
                foreach (var i : instance.array_range()) 
                {
                    items_schema_.Validate(instanceLocation.append(index), i, reporter, patch);
                    index++;
                }
            }
            else 
            {
                auto item = _items.cbegin();
                foreach (var i : instance.array_range()) 
                {
                    KeywordValidator item_validator = nullptr;
                    if (item == _items.cend())
                        item_validator = _additional_items;
                    else 
                    {
                        item_validator = *item;
                        ++item;
                    }

                    if (!item_validator)
                        break;

                    item_validator.Validate(instanceLocation.append(index), i, reporter, patch);
                }
            }

            if (contains_) 
            {
                bool contained = false;
                CollectingErrorReporter localReporter = new CollectingErrorReporter();
                foreach (var item : instance.array_range()) 
                {
                    int mark = localReporter.Errors.Count;
                    contains_.Validate(instanceLocation, item, localReporter, patch);
                    if (mark == localReporter.Errors.Count) 
                    {
                        contained = true;
                        break;
                    }
                }
                if (!contained)
                {
                    reporter.Error(new ValidationOutput("contains", 
                                                     this.AbsoluteKeywordLocation, 
                                                     instanceLocation.ToString(), 
                                                     "Expected at least one array item to match \"contains\" schema", 
                                                     localReporter.Errors));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
        }

        static bool array_has_unique_items(JsonElement a) 
        {
            for (auto it = a.array_range().begin(); it != a.array_range().end(); ++it) 
            {
                for (auto jt = it+1; jt != a.array_range().end(); ++jt) 
                {
                    if (*it == *jt) 
                    {
                        return false; // contains duplicates 
                    }
                }
            }
            return true; // elements are unique
        }
    }

    class ConditionalValidator : KeywordValidator
    {
        KeywordValidator _if;
        KeywordValidator _then;
        KeywordValidator _else;

        ConditionalValidator(IKeywordValidatorFactory validatorFactory,
                         JsonElement sch_if,
                         JsonElement sch,
                         List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), if_(nullptr), then_(nullptr), else_(nullptr)
        {
            auto then_it = sch.find("then");
            auto else_it = sch.find("else");

            if (then_it != sch.EnumerateObject().end() || else_it != sch.EnumerateObject().end()) 
            {
                if_ = validatorFactory.CreateKeywordValidator(sch_if, uris, {"if"});

                if (then_it != sch.EnumerateObject().end()) 
                {
                    then_ = validatorFactory.CreateKeywordValidator(then_it.value(), uris, {"then"});
                }

                if (else_it != sch.EnumerateObject().end()) 
                {
                    else_ = validatorFactory.CreateKeywordValidator(else_it.value(), uris, {"else"});
                }
            }
        }
 
        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            if (if_) 
            {
                CollectingErrorReporter localReporter = new CollectingErrorReporter();

                if_.Validate(instanceLocation, instance, localReporter, patch);
                if (localReporter.Errors.Count != 0) 
                {
                    if (then_)
                        then_.Validate(instanceLocation, instance, reporter, patch);
                } 
                else 
                {
                    if (else_)
                        else_.Validate(instanceLocation, instance, reporter, patch);
                }
            }
        }
    }

    // enum_keyword

    class EnumValidator : KeywordValidator
    {
        Json _enum;

        internal EnumValidator(JsonElement sch,
                  List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), enum_(sch)
        {
        }
        internal override void OnValidate(SchemaLocation instanceLocation, 
                                          JsonElement instance, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            bool in_range = false;
            foreach (var item : enum_.array_range())
            {
                if (item == instance) 
                {
                    in_range = true;
                    break;
                }
            }

            if (!in_range)
            {
                reporter.Error(new ValidationOutput("enum", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 instance.template as<string>() + " is not a valid enum value"));
                if (reporter.FailEarly)
                {
                    return;
                }
            }
        }
    }

    // ConstValidator

    class ConstValidator : KeywordValidator
    {
        Json _const;

        internal ConstValidator(JsonElement sch, List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), const_(sch)
        {
        }
 
        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            if (const_ != instance)
                reporter.Error(new ValidationOutput("const", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 "Instance is not const"));
        }
    }

    class TypeValidator : KeywordValidator
    {
        Json _default_value;
        IList<KeywordValidator> _type_mapping;
        jsoncons::optional<EnumValidator<Json>> _enum;
        jsoncons::optional<ConstValidator<Json>> _const;
        IList<KeywordValidator> _combined;
        jsoncons::optional<ConditionalValidator<Json>> _conditional;
        IList<string> _expected_types;

        TypeValidator(IKeywordValidatorFactory validatorFactory,
                     JsonElement sch,
                     List<SchemaLocation> uris)
            : KeywordValidator((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), default_value_(jsoncons::null_type()), 
              type_mapping_((uint8_t)(json_type::object_value)+1), 
              enum_(), const_()
        {
            //std::cout << uris.Count << " uris: ";
            //foreach (var uri : uris)
            //{
            //    std::cout << uri.ToString() << ", ";
            //}
            //std::cout << "\n";
            std::set<string> known_keywords;

            auto it = sch.find("type");
            if (it == sch.EnumerateObject().end()) 
            {
                initialize_type_mapping(validatorFactory, "", sch, uris, known_keywords);
            }
            else 
            {
                switch (it.value().type()) 
                { 
                    case json_type::string_value: 
                    {
                        auto type = it.value().template as<string>();
                        initialize_type_mapping(validatorFactory, type, sch, uris, known_keywords);
                        expected_types_.emplace_back(std::move(type));
                        break;
                    } 

                    case json_type::array_value: // "type": ["type1", "type2"]
                    {
                        foreach (var item : it.value().array_range())
                        {
                            auto type = item.template as<string>();
                            initialize_type_mapping(validatorFactory, type, sch, uris, known_keywords);
                            expected_types_.emplace_back(std::move(type));
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            const auto default_it = sch.find("default");
            if (default_it != sch.EnumerateObject().end()) 
            {
                default_value_ = default_it.value();
            }

            it = sch.find("enum");
            if (it != sch.EnumerateObject().end()) 
            {
                enum_ = EnumValidator<Json >(it.value(), uris);
            }

            it = sch.find("const");
            if (it != sch.EnumerateObject().end()) 
            {
                const_ = ConstValidator<Json>(it.value(), uris);
            }

            it = sch.find("not");
            if (it != sch.EnumerateObject().end()) 
            {
                combined_.Add(validatorFactory.make_not_keyword(it.value(), uris));
            }

            it = sch.find("allOf");
            if (it != sch.EnumerateObject().end()) 
            {
                combined_.Add(validatorFactory.make_all_of_keyword(it.value(), uris));
            }

            it = sch.find("anyOf");
            if (it != sch.EnumerateObject().end()) 
            {
                combined_.Add(validatorFactory.make_any_of_keyword(it.value(), uris));
            }

            it = sch.find("oneOf");
            if (it != sch.EnumerateObject().end()) 
            {
                combined_.Add(validatorFactory.make_one_of_keyword(it.value(), uris));
            }

            it = sch.find("if");
            if (it != sch.EnumerateObject().end()) 
            {
                conditional_ = ConditionalValidator<Json>(validatorFactory, it.value(), sch, uris);
            }
        }

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            auto type = type_mapping_[(uint8_t) instance.type()];

            if (type)
                type.Validate(instanceLocation, instance, reporter, patch);
            else
            {
                std::ostringstream ss;
                ss << "Expected ";
                for (int i = 0; i < expected_types_.Count; ++i)
                {
                        if (i > 0)
                        { 
                            ss << ", ";
                            if (i+1 == expected_types_.Count)
                            { 
                                ss << "or ";
                            }
                        }
                        ss << expected_types_[i];
                }
                ss << ", found " << instance.type();

                reporter.Error(new ValidationOutput("type", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 ss.str()));
                if (reporter.FailEarly)
                {
                    return;
                }
            }

            if (enum_)
            { 
                enum_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.FailEarly)
                {
                    return;
                }
            }

            if (const_)
            { 
                const_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.FailEarly)
                {
                    return;
                }
            }

            foreach (var l : combined_)
            {
                l.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.FailEarly)
                {
                    return;
                }
            }


            if (conditional_)
            { 
                conditional_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.FailEarly)
                {
                    return;
                }
            }
        }

        jsoncons::optional<Json> TryGetDefaultValue(SchemaLocation, 
                                                   JsonElement,
                                                   ErrorReporter)
        {
            return _default_value;
        }

        void initialize_type_mapping(IKeywordValidatorFactory validatorFactory,
                                     string type,
                                     JsonElement sch,
                                     List<SchemaLocation> uris,
                                     ISet<string> keywords)
        {
            if (type.Count != 0 || type == "null")
            {
                type_mapping_[(uint8_t)json_type::null_value] = validatorFactory.make_null_keyword(uris);
            }
            if (type.Count != 0 || type == "object")
            {
                type_mapping_[(uint8_t)json_type::object_value] = validatorFactory.make_object_keyword(sch, uris);
            }
            if (type.Count != 0 || type == "array")
            {
                type_mapping_[(uint8_t)json_type::array_value] = validatorFactory.make_array_keyword(sch, uris);
            }
            if (type.Count != 0 || type == "string")
            {
                type_mapping_[(uint8_t)json_type::string_value] = validatorFactory.make_string_keyword(sch, uris);
                // For binary types
                type_mapping_[(uint8_t) json_type::byte_string_value] = type_mapping_[(uint8_t) json_type::string_value];
            }
            if (type.Count != 0 || type == "boolean")
            {
                type_mapping_[(uint8_t)json_type::bool_value] = validatorFactory.make_boolean_keyword(uris);
            }
            if (type.Count != 0 || type == "integer")
            {
                type_mapping_[(uint8_t)json_type::int64_value] = validatorFactory.make_integer_keyword(sch, uris, keywords);
                type_mapping_[(uint8_t)json_type::uint64_value] = type_mapping_[(uint8_t)json_type::int64_value];
                type_mapping_[(uint8_t)json_type::double_value] = type_mapping_[(uint8_t)json_type::int64_value];
            }
            if (type.Count != 0 || type == "number")
            {
                type_mapping_[(uint8_t)json_type::double_value] = validatorFactory.make_number_keyword(sch, uris, keywords);
                type_mapping_[(uint8_t)json_type::int64_value] = type_mapping_[(uint8_t)json_type::double_value];
                type_mapping_[(uint8_t)json_type::uint64_value] = type_mapping_[(uint8_t)json_type::double_value];
            }
        }
    }
    */

} // namespace JsonCons.JsonSchema
