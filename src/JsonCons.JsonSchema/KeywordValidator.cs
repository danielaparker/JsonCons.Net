using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

#nullable enable        

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
        internal int? MaxLength {get;} = null;
        internal string? MaxLengthLocation {get;} = null;

        internal int? MinLength {get;} = null;
        internal string? MinLengthLocation {get;} = null;

        internal Regex? Pattern {get;} = null;
        internal string? PatternLocation {get;} = null;

        IFormatValidator? FormatValidator {get;} = null; 
        
        internal string? ContentEncoding {get;} = null;
        internal string? ContentEncodingLocation {get;} = null;

        internal string? ContentMediaType {get;} = null;
        internal string? ContentMediaTypeLocation {get;} = null;

        internal StringValidator(string absoluteKeywordLocation,
                                 int? maxLength, string? maxLengthLocation,
                                 int? minLength, string? minLengthLocation,
                                 Regex? pattern, string? patternLocation,
                                 IFormatValidator? formatValidator, 
                                 string? contentEncoding, string? contentEncodingLocation,
                                 string? contentMediaType, string? contentMediaTypeLocation)
            : base(absoluteKeywordLocation)
        {
            MaxLength = maxLength;
            MaxLengthLocation = maxLengthLocation;
            MinLength = minLength;
            MinLengthLocation = minLengthLocation;
            Pattern = pattern;
            PatternLocation = patternLocation;
            FormatValidator = formatValidator;
            ContentEncoding = contentEncoding;
            ContentEncodingLocation = contentEncodingLocation;
            ContentMediaType = contentMediaType;
            ContentMediaTypeLocation = contentMediaTypeLocation;
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch)
        {
            string? content = null;
            if (ContentEncoding != null)
            {
                if (ContentEncoding == "base64")
                {
                    string? s = instance.GetString();
                    try
                    {
                        content = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
                    }
                    catch (Exception)
                    {
                        reporter.Error(new ValidationOutput("contentEncoding", 
                                                            ContentEncodingLocation, 
                                                            instanceLocation.ToString(), 
                                                            "Content is not a base64 string"));
                        if (reporter.FailEarly)
                        {
                            return;
                        }
                    }
                }
                else if (ContentEncoding.Length != 0)
                {
                    reporter.Error(new ValidationOutput("contentEncoding", 
                                                    ContentEncodingLocation,
                                                    instanceLocation.ToString(), 
                                                    $"Unable to check for contentEncoding '{ContentEncoding}'"));
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

            if (ContentMediaType != null) 
            {
                if (ContentMediaType.Equals("application/Json"))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(content);
                    }
                    catch (Exception e)
                    {
                        reporter.Error(new ValidationOutput("contentMediaType", 
                                                            ContentMediaTypeLocation,
                                                            instanceLocation.ToString(), 
                                                            $"Content is not JSON: {e.Message}"));
                    }
                }
            } 

            if (instance.ValueKind != JsonValueKind.String) 
            {
                return; 
            }

            if (MinLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length < MinLength) 
                {
                    reporter.Error(new ValidationOutput("minLength", 
                                                    MinLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected minLength: {MinLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (MaxLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length > MaxLength)
                {
                    reporter.Error(new ValidationOutput("maxLength", 
                                                    MaxLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected maxLength: {MaxLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (Pattern != null)
            {
                var match = Pattern.Match(content);
                if (match.Success)
                {
                    reporter.Error(new ValidationOutput("pattern", 
                                                    PatternLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"String '{content}' does not match pattern '{Pattern}'"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (FormatValidator != null) 
            {
                FormatValidator.Validate(content, instanceLocation.ToString(), reporter);
                if (reporter.ErrorCount > 0 && reporter.FailEarly)
                {
                    return;
                }
            }
        }

        internal static StringValidator Create(JsonElement schema, IList<SchemaLocation> uris)
        {
            string absoluteKeywordLocation = (uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "";
            int? maxLength = null;
            string? maxLengthLocation = null;
            int? minLength = null;
            string? minLengthLocation = null;
            Regex? pattern = null;
            string? patternLocation = null;
            IFormatValidator? formatValidator = null; 
            string? contentEncoding = null;
            string? contentEncodingLocation = null;
            string? contentMediaType = null;
            string? contentMediaTypeLocation = null;

            JsonElement element;
            if (schema.TryGetProperty("maxLength", out element))
            {   
                maxLength = element.GetInt32();
                maxLengthLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "maxLength");
            }
            if (schema.TryGetProperty("minLength", out element))
            {   
                minLength = element.GetInt32();
                minLengthLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "minLength");
            }

            if (schema.TryGetProperty("pattern", out element))
            {   
                string? patternString = element.GetString();
                pattern = new Regex(patternString);
                patternLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "pattern");
            }
            if (schema.TryGetProperty("format", out element))
            {   
                string? format = element.GetString();
                string formatLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "format");
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
                formatLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "format");
            }
            if (schema.TryGetProperty("contentEncoding", out element))
            {   
                contentEncoding = element.GetString();
                contentEncodingLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "contentEncoding");
            }
            if (schema.TryGetProperty("contentMediaType", out element))
            {   
                contentMediaType = element.GetString();
                contentMediaTypeLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "contentMediaType");
            }
            return new StringValidator(absoluteKeywordLocation,
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
            string absoluteKeywordLocation = (uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "";

            var keys = new List<string>();
            keys.Add("not");
            KeywordValidator rule = validatorFactory.CreateKeywordValidator(schema, uris, keys);
            return new NotValidator(absoluteKeywordLocation, rule);
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
            string absoluteKeywordLocation = (uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "";

            var validators = new List<KeywordValidator>();
            for (int i = 0; i < schema.GetArrayLength(); ++i)
            {
                var keys = new List<string>();
                keys.Add(criterion.Key);
                keys.Add(i.ToString());
                validators.Add(validatorFactory.CreateKeywordValidator(schema[i], uris, keys));
            }

            return new CombiningValidator(absoluteKeywordLocation, 
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
/*
    template <class T, class Json>
    T get_number(JsonElement val, const string_view& keyword) 
    {
        if (!val.is_number())
        {
            std::string message(keyword);
            message.append(" must be a number value");
            JSONCONS_THROW(schema_error(message));
        }
        return val.template as<T>();
    }

    template <class Json,class T>
    class numericic_type_validator : KeywordValidator
    {
        jsoncons::optional<T> maximum_;
        std::string absolute_maximum_location_;
        jsoncons::optional<T> minimum_;
        std::string absolute_minimum_location_;
        jsoncons::optional<T> exclusive_maximum_;
        std::string absolute_exclusive_maximum_location_;
        jsoncons::optional<T> exclusive_minimum_;
        std::string absolute_exclusive_minimum_location_;
        jsoncons::optional<double> multiple_of_;
        std::string absolute_multiple_of_location_;

    public:
        numericic_type_validator(JsonElement sch, 
                    List<SchemaLocation> uris, 
                    std::set<std::string>& keywords)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), 
              maximum_(), minimum_(),exclusive_maximum_(), exclusive_minimum_(), multiple_of_()
        {
            auto it = sch.find("maximum");
            if (it != sch.EnumerateObject().end()) 
            {
                maximum_ = get_number<T>(it.value(), "maximum");
                absolute_maximum_location_ = make_absolute_keyword_location(uris,"maximum");
                keywords.insert("maximum");
            }

            it = sch.find("minimum");
            if (it != sch.EnumerateObject().end()) 
            {
                minimum_ = get_number<T>(it.value(), "minimum");
                absolute_minimum_location_ = make_absolute_keyword_location(uris,"minimum");
                keywords.insert("minimum");
            }

            it = sch.find("exclusiveMaximum");
            if (it != sch.EnumerateObject().end()) 
            {
                exclusive_maximum_ = get_number<T>(it.value(), "exclusiveMaximum");
                absolute_exclusive_maximum_location_ = make_absolute_keyword_location(uris,"exclusiveMaximum");
                keywords.insert("exclusiveMaximum");
            }

            it = sch.find("exclusiveMinimum");
            if (it != sch.EnumerateObject().end()) 
            {
                exclusive_minimum_ = get_number<T>(it.value(), "exclusiveMinimum");
                absolute_exclusive_minimum_location_ = make_absolute_keyword_location(uris,"exclusiveMinimum");
                keywords.insert("exclusiveMinimum");
            }

            it = sch.find("multipleOf");
            if (it != sch.EnumerateObject().end()) 
            {
                multiple_of_ = get_number<double>(it.value(), "multipleOf");
                absolute_multiple_of_location_ = make_absolute_keyword_location(uris,"multipleOf");
                keywords.insert("multipleOf");
            }
        }

    protected:

        void apply_kewords(T value,
                           SchemaLocation instanceLocation, 
                           JsonElement instance, 
                           ErrorReporter reporter) const 
        {
            if (multiple_of_ && value != 0) // exclude zero
            {
                if (!is_multiple_of(value, *multiple_of_))
                {
                    reporter.Error(new ValidationOutput("multipleOf", 
                                                     absolute_multiple_of_location_, 
                                                     instanceLocation.ToString(), 
                                                     instance.template as<std::string>() + " is not a multiple of " + std::to_string(*multiple_of_)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }

            if (maximum_)
            {
                if (value > *maximum_)
                {
                    reporter.Error(new ValidationOutput("maximum", 
                                                     absolute_maximum_location_, 
                                                     instanceLocation.ToString(), 
                                                     instance.template as<std::string>() + " exceeds maximum of " + std::to_string(*maximum_)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }

            if (minimum_)
            {
                if (value < *minimum_)
                {
                    reporter.Error(new ValidationOutput("minimum", 
                                                     absolute_minimum_location_, 
                                                     instanceLocation.ToString(), 
                                                     instance.template as<std::string>() + " is below minimum of " + std::to_string(*minimum_)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }

            if (exclusive_maximum_)
            {
                if (value >= *exclusive_maximum_)
                {
                    reporter.Error(new ValidationOutput("exclusiveMaximum", 
                                                     absolute_exclusive_maximum_location_, 
                                                     instanceLocation.ToString(), 
                                                     instance.template as<std::string>() + " exceeds maximum of " + std::to_string(*exclusive_maximum_)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }

            if (exclusive_minimum_)
            {
                if (value <= *exclusive_minimum_)
                {
                    reporter.Error(new ValidationOutput("exclusiveMinimum", 
                                                     absolute_exclusive_minimum_location_, 
                                                     instanceLocation.ToString(), 
                                                     instance.template as<std::string>() + " is below minimum of " + std::to_string(*exclusive_minimum_)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }
        }
    private:
        static bool is_multiple_of(T x, double multiple_of) 
        {
            double rem = std::remainder(x, multiple_of);
            double eps = std::nextafter(x, 0) - x;
            return std::fabs(rem) < std::fabs(eps);
        }
    }

    template <class Json>
    class integer_keyword : numericic_type_validator<Json,int64_t>
    {
    public:
        integer_keyword(JsonElement sch, 
                          List<SchemaLocation> uris, 
                          std::set<std::string>& keywords)
            : numericic_type_validator<Json, int64_t>(sch, uris, keywords)
        {
        }
    private:
        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            if (!(instance.template is_integer<int64_t>() || (instance.is_double() && static_cast<double>(instance.template as<int64_t>()) == instance.template as<double>())))
            {
                reporter.Error(new ValidationOutput("integer", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 "Instance is not an integer"));
                if (reporter.fail_early())
                {
                    return;
                }
            }
            int64_t value = instance.template as<int64_t>(); 
            this.apply_kewords(value, instanceLocation, instance, reporter);
        }
    }

    template <class Json>
    class number_validator : numericic_type_validator<Json,double>
    {
    public:
        number_validator(JsonElement sch,
                          List<SchemaLocation> uris, 
                          std::set<std::string>& keywords)
            : numericic_type_validator<Json, double>(sch, uris, keywords)
        {
        }
    private:
        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch) 
        {
            if (!(instance.template is_integer<int64_t>() || instance.is_double()))
            {
                reporter.Error(new ValidationOutput("number", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 "Instance is not a number"));
                if (reporter.fail_early())
                {
                    return;
                }
            }
            double value = instance.template as<double>(); 
            this.apply_kewords(value, instanceLocation, instance, reporter);
        }
    }

    // null_validator

    template <class Json>
    class null_validator : KeywordValidator
    {
    public:
        null_validator(List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "")
        {
        }
    private:
        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            if (!instance.is_null())
            {
                reporter.Error(new ValidationOutput("null", 
                                                 this.AbsoluteKeywordLocation, 
                                                 instanceLocation.ToString(), 
                                                 "Expected to be null"));
            }
        }
    }

    template <class Json>
    class boolean_validator : KeywordValidator
    {
    public:
        boolean_validator(List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "")
        {
        }
    private:
        internal override void OnValidate(JsonElement,
                                          SchemaLocation,
                                          ErrorReporter,
                                          IList<PatchElement> patch) 
        {
        }

    }

    template <class Json>
    class true_validator : KeywordValidator
    {
        true_validator(List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "")
        {
        }
    private:
        internal override void OnValidate(JsonElement,
                                          SchemaLocation,
                                          ErrorReporter,
                                          IList<PatchElement> patch) 
        {
        }
    }

    template <class Json>
    class false_validator : KeywordValidator
    {
        false_validator(List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : "")
        {
        }
    private:
        internal override void OnValidate(SchemaLocation instanceLocation,
                                          JsonElement,
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch) 
        {
            reporter.Error(new ValidationOutput("false", 
                                             this.AbsoluteKeywordLocation, 
                                             instanceLocation.ToString(), 
                                             "False schema always fails"));
        }
    }

    template <class Json>
    class required_KeywordValidator : KeywordValidator
    {
        IList<std::string> items_;

    public:
        required_KeywordValidator(List<SchemaLocation> uris,
                         const IList<std::string>& items)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), items_(items) {}
        required_KeywordValidator(string absolute_keyword_location, const IList<std::string>& items)
            : base(absolute_keyword_location), items_(items) {}

        required_KeywordValidator(const required_KeywordValidator&) = delete;
        required_KeywordValidator(required_KeywordValidator&&) = default;
        required_KeywordValidator& operator=(const required_KeywordValidator&) = delete;
        required_KeywordValidator& operator=(required_KeywordValidator&&) = default;
    private:

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter,
                                          IList<PatchElement> patch)  final
        {
            foreach (var key : items_)
            {
                if (instance.find(key) == instance.object_range().end())
                {
                    reporter.Error(new ValidationOutput("required", 
                                                     this.AbsoluteKeywordLocation, 
                                                     instanceLocation.ToString(), 
                                                     "Required property \"" + key + "\" not found"));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }
        }
    }

    template <class Json>
    class object_validator : KeywordValidator
    {
        jsoncons::optional<int> max_properties_;
        std::string absolute_max_properties_location_;
        jsoncons::optional<int> min_properties_;
        std::string absolute_min_properties_location_;
        jsoncons::optional<required_KeywordValidator<Json>> required_;

        std::map<std::string, KeywordValidator> properties_;
    #if defined(JSONCONS_HAS_STD_REGEX)
        IList<std::pair<std::regex, KeywordValidator>> pattern_properties_;
    #endif
        KeywordValidator additional_properties_;

        std::map<std::string, KeywordValidator> dependencies_;

        KeywordValidator property_names_;

    public:
        object_validator(IKeywordValidatorFactory validatorFactory,
                    JsonElement sch,
                    List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), 
              max_properties_(), min_properties_(), 
              additional_properties_(nullptr),
              property_names_(nullptr)
        {
            auto it = sch.find("maxProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                max_properties_ = it.value().template as<int>();
                absolute_max_properties_location_ = make_absolute_keyword_location(uris, "maxProperties");
            }

            it = sch.find("minProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                min_properties_ = it.value().template as<int>();
                absolute_min_properties_location_ = make_absolute_keyword_location(uris, "minProperties");
            }

            it = sch.find("required");
            if (it != sch.EnumerateObject().end()) 
            {
                auto location = make_absolute_keyword_location(uris, "required");
                required_ = required_KeywordValidator<Json>(location, 
                                                   it.value().template as<IList<std::string>>());
            }

            it = sch.find("properties");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var prop : it.value().object_range())
                    properties_.emplace(
                        std::make_pair(
                            prop.key(),
                            validatorFactory.CreateKeywordValidator(prop.value(), uris, {"properties", prop.key()})));
            }

    #if defined(JSONCONS_HAS_STD_REGEX)
            it = sch.find("patternProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var prop : it.value().object_range())
                    pattern_properties_.emplace_back(
                        std::make_pair(
                            std::regex(prop.key(), std::regex::ECMAScript),
                            validatorFactory.CreateKeywordValidator(prop.value(), uris, {prop.key()})));
            }
    #endif

            it = sch.find("additionalProperties");
            if (it != sch.EnumerateObject().end()) 
            {
                additional_properties_ = validatorFactory.CreateKeywordValidator(it.value(), uris, {"additionalProperties"});
            }

            it = sch.find("dependencies");
            if (it != sch.EnumerateObject().end()) 
            {
                foreach (var dep : it.value().object_range())
                {
                    switch (dep.value().type()) 
                    {
                        case json_type::array_value:
                        {
                            auto location = make_absolute_keyword_location(uris, "required");
                            dependencies_.emplace(dep.key(),
                                                  validatorFactory.make_required_keyword({location},
                                                                                 dep.value().template as<IList<std::string>>()));
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
    private:

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch)
        {
            if (max_properties_ && instance.Count > *max_properties_)
            {
                std::string message("Maximum properties: " + std::to_string(*max_properties_));
                message.append(", found: " + std::to_string(instance.Count));
                reporter.Error(new ValidationOutput("maxProperties", 
                                                 absolute_max_properties_location_, 
                                                 instanceLocation.ToString(), 
                                                 std::move(message)));
                if (reporter.fail_early())
                {
                    return;
                }
            }

            if (min_properties_ && instance.Count < *min_properties_)
            {
                std::string message("Minimum properties: " + std::to_string(*min_properties_));
                message.append(", found: " + std::to_string(instance.Count));
                reporter.Error(new ValidationOutput("minProperties", 
                                                 absolute_min_properties_location_, 
                                                 instanceLocation.ToString(), 
                                                 std::move(message)));
                if (reporter.fail_early())
                {
                    return;
                }
            }

            if (required_)
                required_.Validate(instanceLocation, instance, reporter, patch);

            foreach (var property : instance.object_range()) 
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

    #if defined(JSONCONS_HAS_STD_REGEX)

                // check all matching "patternProperties"
                for (var schema_pp : pattern_properties_)
                    if (std::regex_search(property.key(), schema_pp.first)) 
                    {
                        a_prop_or_pattern_matched = true;
                        schema_pp.second.Validate(instanceLocation.append(property.key()), property.value(), reporter, patch);
                    }
    #endif

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
                        if (reporter.fail_early())
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
                if (finding == instance.object_range().end()) 
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
                if (prop != instance.object_range().end()) // if dependency-property is present in instance
                    dep.second.Validate(instanceLocation.append(dep.first), instance, reporter, patch); // Validate
            }
        }
    }

    // array_validator

    template <class Json>
    class array_validator : KeywordValidator
    {
        jsoncons::optional<int> max_items_;
        std::string absolute_max_items_location_;
        jsoncons::optional<int> min_items_;
        std::string absolute_min_items_location_;
        bool unique_items_ = false;
        KeywordValidator items_schema_;
        IList<KeywordValidator> items_;
        KeywordValidator additional_items_;
        KeywordValidator contains_;

    public:
        array_validator(IKeywordValidatorFactory validatorFactory, 
                   JsonElement sch, 
                   List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), 
              max_items_(), min_items_(), items_schema_(nullptr), additional_items_(nullptr), contains_(nullptr)
        {
            {
                auto it = sch.find("maxItems");
                if (it != sch.EnumerateObject().end()) 
                {
                    max_items_ = it.value().template as<int>();
                    absolute_max_items_location_ = make_absolute_keyword_location(uris, "maxItems");
                }
            }

            {
                auto it = sch.find("minItems");
                if (it != sch.EnumerateObject().end()) 
                {
                    min_items_ = it.value().template as<int>();
                    absolute_min_items_location_ = make_absolute_keyword_location(uris, "minItems");
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
                            items_.Add(validatorFactory.CreateKeywordValidator(subsch, uris, {"items", std::to_string(c++)}));

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
    private:

        internal override void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter, 
                                          IList<PatchElement> patch)
        {
            if (max_items_)
            {
                if (instance.Count > *max_items_)
                {
                    std::string message("Expected maximum item count: " + std::to_string(*max_items_));
                    message.append(", found: " + std::to_string(instance.Count));
                    reporter.Error(new ValidationOutput("maxItems", 
                                                     absolute_max_items_location_, 
                                                     instanceLocation.ToString(), 
                                                     std::move(message)));
                    if (reporter.fail_early())
                    {
                        return;
                    }
                }
            }

            if (min_items_)
            {
                if (instance.Count < *min_items_)
                {
                    std::string message("Expected minimum item count: " + std::to_string(*min_items_));
                    message.append(", found: " + std::to_string(instance.Count));
                    reporter.Error(new ValidationOutput("minItems", 
                                                     absolute_min_items_location_, 
                                                     instanceLocation.ToString(), 
                                                     std::move(message)));
                    if (reporter.fail_early())
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
                    if (reporter.fail_early())
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
                auto item = items_.cbegin();
                foreach (var i : instance.array_range()) 
                {
                    KeywordValidator item_validator = nullptr;
                    if (item == items_.cend())
                        item_validator = additional_items_;
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
                    if (reporter.fail_early())
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

    template <class Json>
    class conditional_keyword : base
    {
        KeywordValidator if_;
        KeywordValidator then_;
        KeywordValidator else_;

    public:
        conditional_keyword(IKeywordValidatorFactory validatorFactory,
                         JsonElement sch_if,
                         JsonElement sch,
                         List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), if_(nullptr), then_(nullptr), else_(nullptr)
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
    private:
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

    template <class Json>
    class enum_keyword : base
    {
        Json enum_;

    public:
        enum_keyword(JsonElement sch,
                  List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), enum_(sch)
        {
        }
    private:
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
                                                 instance.template as<std::string>() + " is not a valid enum value"));
                if (reporter.fail_early())
                {
                    return;
                }
            }
        }
    }

    // const_keyword

    template <class Json>
    class const_keyword : base
    {
        Json const_;

    public:
        const_keyword(JsonElement sch, List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), const_(sch)
        {
        }
    private:
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

    template <class Json>
    class type_keyword : base
    {
        Json default_value_;
        IList<KeywordValidator> type_mapping_;
        jsoncons::optional<enum_keyword<Json>> enum_;
        jsoncons::optional<const_keyword<Json>> const_;
        IList<KeywordValidator> combined_;
        jsoncons::optional<conditional_keyword<Json>> conditional_;
        IList<std::string> expected_types_;

    public:
        type_keyword(const type_keyword&) = delete;
        type_keyword& operator=(const type_keyword&) = delete;
        type_keyword(type_keyword&&) = default;
        type_keyword& operator=(type_keyword&&) = default;

        type_keyword(IKeywordValidatorFactory validatorFactory,
                     JsonElement sch,
                     List<SchemaLocation> uris)
            : base((uris.Count != 0 && uris[uris.Count-1].IsAbsoluteUri) ? uris[uris.Count-1].ToString() : ""), default_value_(jsoncons::null_type()), 
              type_mapping_((uint8_t)(json_type::object_value)+1), 
              enum_(), const_()
        {
            //std::cout << uris.Count << " uris: ";
            //foreach (var uri : uris)
            //{
            //    std::cout << uri.ToString() << ", ";
            //}
            //std::cout << "\n";
            std::set<std::string> known_keywords;

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
                        auto type = it.value().template as<std::string>();
                        initialize_type_mapping(validatorFactory, type, sch, uris, known_keywords);
                        expected_types_.emplace_back(std::move(type));
                        break;
                    } 

                    case json_type::array_value: // "type": ["type1", "type2"]
                    {
                        foreach (var item : it.value().array_range())
                        {
                            auto type = item.template as<std::string>();
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
                enum_ = enum_keyword<Json >(it.value(), uris);
            }

            it = sch.find("const");
            if (it != sch.EnumerateObject().end()) 
            {
                const_ = const_keyword<Json>(it.value(), uris);
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
                conditional_ = conditional_keyword<Json>(validatorFactory, it.value(), sch, uris);
            }
        }
    private:

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
                if (reporter.fail_early())
                {
                    return;
                }
            }

            if (enum_)
            { 
                enum_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.fail_early())
                {
                    return;
                }
            }

            if (const_)
            { 
                const_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.fail_early())
                {
                    return;
                }
            }

            foreach (var l : combined_)
            {
                l.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.fail_early())
                {
                    return;
                }
            }


            if (conditional_)
            { 
                conditional_.Validate(instanceLocation, instance, reporter, patch);
                if (reporter.Error_count() > 0 && reporter.fail_early())
                {
                    return;
                }
            }
        }

        jsoncons::optional<Json> TryGetDefaultValue(SchemaLocation, 
                                                   JsonElement,
                                                   ErrorReporter)
        {
            return default_value_;
        }

        void initialize_type_mapping(IKeywordValidatorFactory validatorFactory,
                                     string type,
                                     JsonElement sch,
                                     List<SchemaLocation> uris,
                                     std::set<std::string>& keywords)
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
