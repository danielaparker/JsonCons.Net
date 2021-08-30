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
    interface IFormatValidator
    {
        void Validate(string value,
                      string instanceLocation, 
                      ErrorReporter reporter);
    }

    class RegexValidator : IFormatValidator
    {
        string _absoluteKeywordLocation;
        Regex _pattern;

        internal RegexValidator(string absoluteKeywordLocation, Regex pattern)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
            _pattern = pattern;
        }

        public void Validate(string value,
                             string instanceLocation, 
                             ErrorReporter reporter) 
        {
            if (!_pattern.IsMatch(value))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation, 
                                                    instanceLocation, 
                                                    $"'{value}' does not match regular expression '{_pattern}'"));
            }
        } 
    }

    class EmailValidator : IFormatValidator
    {
        string _absoluteKeywordLocation;

        internal EmailValidator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string value,
                             string instanceLocation, 
                             ErrorReporter reporter) 
        {
            if (!Check(value))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation, 
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

        static bool IsAText(char c)
        {
            switch (c)
            {
                case '!':
                case '#':
                case '$':
                case '%':
                case '&':
                case '\'':
                case '*':
                case '+':
                case '-':
                case '/':
                case '=':
                case '?':
                case '^':
                case '_':
                case '`':
                case '{':
                case '|':
                case '}':
                case '~':
                    return true;
                default:
                    return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            }
        }

        static bool IsDText( char c)
        {
            return (c >= 33 && c <= 90) || (c >= 94 && c <= 126);
        }

        //  RFC 5322, section 3.4.1
        enum StateKind { LocalPart, Atom, DotAtom, QuotedString, Amp, Domain };

        static bool Check(string s)
        {
            StateKind state = StateKind.LocalPart;
            int partLength = 0;

            foreach (char c in s)
            {
                switch (state)
                {
                    case StateKind.LocalPart:
                    {
                        if (IsAText(c))
                        {
                            state = StateKind.Atom;
                        }
                        else if (c == '"')
                        {
                            state = StateKind.QuotedString;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.DotAtom:
                    {
                        if (IsAText(c))
                        {
                            ++partLength;
                            state = StateKind.Atom;
                        }
                        else
                            return false;
                        break;
                    }
                    case StateKind.Atom:
                    {
                        switch (c)
                        {
                            case '@':
                                state = StateKind.Domain;
                                partLength = 0;
                                break;
                            case '.':
                                state = StateKind.DotAtom;
                                ++partLength;
                                break;
                            default:
                                if (IsAText(c))
                                    ++partLength;
                                else
                                    return false;
                                break;
                        }
                        break;
                    }
                    case StateKind.QuotedString:
                    {
                        if (c == '\"')
                        {
                            state = StateKind.Amp;
                        }
                        else
                        {
                            ++partLength;
                        }
                        break;
                    }
                    case StateKind.Amp:
                    {
                        if (c == '@')
                        {
                            state = StateKind.Domain;
                            partLength = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Domain:
                    {
                        if (IsDText(c))
                        {
                            ++partLength;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                }
            }

            return state == StateKind.Domain && partLength > 0;
        }
    }

    enum DateTimeKind {DateTime,Date,Time}

    class DateValidator : IFormatValidator
    {
        string _absoluteKeywordLocation;

        internal DateValidator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string instanceLocation, 
                             string value,
                             ErrorReporter reporter) 
        {
            if (!DateTimeValidation.Check(value, DateTimeKind.Date))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

    }

    class TimeValidator : IFormatValidator 
    {
        string _absoluteKeywordLocation;

        internal TimeValidator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string instanceLocation, 
                             string value,
                             ErrorReporter reporter) 
        {
            if (!DateTimeValidation.Check(value, DateTimeKind.Time))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

    }

    class DateTimeValidator : IFormatValidator 
    {
        string _absoluteKeywordLocation;

        internal DateTimeValidator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string instanceLocation, 
                             string value,
                             ErrorReporter reporter) 
        {
            if (!DateTimeValidation.Check(value, DateTimeKind.DateTime))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 
    }

    static class DateTimeValidation
    {
        enum StateKind {FullYear,Month,MDay,Hour,Minute,Second,SecFrac,Z,OffsetHour,OffsetMinute}

        // RFC 3339, Section 5.6
        internal static bool Check(string s, DateTimeKind type)
        {
            int piece_length = 0;
            int year = 0;
            int Month = 0;
            int MDay = 0;
            int value = 0;
            StateKind state = (type == DateTimeKind.Time) ? StateKind.Hour : StateKind.FullYear;

            foreach (char c in s)
            {
                switch (state)
                {
                    case StateKind.FullYear:
                    {
                        if (piece_length < 4 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            year = year*10 + (c - '0');
                        }
                        else if (c == '-' && piece_length == 4)
                        {
                            state = StateKind.Month;
                            piece_length = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Month:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            Month = Month*10 + (c - '0');
                        }
                        else if (c == '-' && piece_length == 2 && (Month >=1 && Month <= 12))
                        {
                            state = StateKind.MDay;
                            piece_length = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.MDay:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            MDay = MDay *10 + (c - '0');
                        }
                        else if ((c == 'T' || c == 't') && piece_length == 2 && (MDay <= DateTime.DaysInMonth(year, Month)))
                        {
                            piece_length = 0;
                            state = StateKind.Hour;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Hour:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            value = value*10 + (c - '0');
                        }
                        else if (c == ':' && piece_length == 2 && (value <= 23))
                        {
                            state = StateKind.Minute;
                            value = 0;
                            piece_length = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Minute:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            value = value*10 + (c - '0');
                        }
                        else if (c == ':' && piece_length == 2 && (value <= 59))
                        {
                            state = StateKind.Second;
                            value = 0;
                            piece_length = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Second:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            value = value*10 + (c - '0');
                        }
                        else if (piece_length == 2 && (value <= 60)) // 00-58, 00-59, 00-60 based on leap Second rules
                        {
                            switch (c)
                            {
                                case '.':
                                    value = 0;
                                    state = StateKind.SecFrac;
                                    break;
                                case '+':
                                case '-':
                                    value = 0;
                                    piece_length = 0;
                                    state = StateKind.OffsetHour;
                                    break;
                                case 'z':
                                case 'Z':
                                    state = StateKind.Z;
                                    break;
                                default:
                                    return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.SecFrac:
                    {
                        if (c >= '0' && c <= '9')
                        {
                            value = value*10 + (c - '0');
                        }
                        else
                        {
                            switch (c)
                            {
                                case '+':
                                case '-':
                                    value = 0;
                                    piece_length = 0;
                                    state = StateKind.OffsetHour;
                                    break;
                                case 'Z':
                                case 'z':
                                    state = StateKind.Z;
                                    break;
                                default:
                                    return false;
                            }
                        }
                        break;
                    }
                    case StateKind.OffsetHour:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            value = value*10 + (c - '0');
                        }
                        else if (c == ':' && piece_length == 2 && (value <= 23))
                        {
                            value = 0;
                            piece_length = 0;
                            state = StateKind.OffsetMinute;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.OffsetMinute:
                    {
                        if (piece_length < 2 && (c >= '0' && c <= '9'))
                        {
                            piece_length++;
                            value = value*10 + (c - '0');
                        }
                        else if (c == ':' && piece_length == 2 && (value <= 59))
                        {
                            value = 0;
                            piece_length = 0;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Z:
                        return false;
                }
            }

            if (type == DateTimeKind.Date)
            {
                return state == StateKind.MDay && piece_length == 2 && (MDay >= 1 && MDay <= DateTime.DaysInMonth(year, Month));
            }
            else
            {
                return state == StateKind.OffsetMinute || state == StateKind.Z || state == StateKind.SecFrac;
            }
        }
    }

    class HostnameValidator : IFormatValidator
    {
        // RFC 2673, Section 3.2

        string _absoluteKeywordLocation;

        internal HostnameValidator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string value,
                             string instanceLocation, 
                             ErrorReporter reporter) 
        {
            if (!Check(value))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

        enum StateKind {StartLabel,ExpectLetterOrDigitOrHyphenOrDot}

        // RFC 1034, Section 3.1
        static bool Check(string hostname)
        {
            StateKind state = StateKind.StartLabel;
            int length = hostname.Length - 1;
            int label_length = 0;

            for (int i = 0; i < length; ++i)
            {
                char c = hostname[i];
                switch (state)
                {
                    case StateKind.StartLabel:
                    {
                        if ((c >= 'a' && c <= 'Z') || (c >= 'A' && c <= 'Z'))
                        {
                            ++label_length;
                            state = StateKind.ExpectLetterOrDigitOrHyphenOrDot;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.ExpectLetterOrDigitOrHyphenOrDot:
                    {
                        if (c == '.')
                        {
                            label_length = 0;
                            state = StateKind.StartLabel;
                        }
                        else if (!((c >= 'a' && c <= 'Z') || (c >= 'A' && c <= 'Z') ||
                                   (c >= '0' && c < '9') || c == '-'))
                        {
                            return false;
                        }
                        if (++label_length > 63)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }

            char last = hostname[hostname.Length-1];
            if (!((last >= 'a' && last <= 'Z') || (last >= 'A' && last <= 'Z') || (last >= '0' && last < '9')))
            {
                return false;
            }
            return true;          
        }
    }

    class Ipv4Validator : IFormatValidator
    {
        // RFC 2673, Section 3.2

        string _absoluteKeywordLocation;

        internal Ipv4Validator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string value,
                             string instanceLocation, 
                             ErrorReporter reporter) 
        {
            if (!Check(value))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

        enum StateKind {ExpectIndicatorOrDottedQuad,Decbyte,
                        Bindig, Octdig, Hexdig}

        static bool Check(string s)
        {
            StateKind state = StateKind.ExpectIndicatorOrDottedQuad;

            int digitCount = 0;
            int decbyteCount = 0;
            int value = 0;

            for (int i = 0; i < s.Length; ++i)
            {
                char c = s[i];
                switch (state)
                {
                    case StateKind.ExpectIndicatorOrDottedQuad:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                                state = StateKind.Decbyte;
                                decbyteCount = 0;
                                digitCount = 1;
                                value = 0;
                                break;
                            case 'b':
                                state = StateKind.Bindig;
                                digitCount = 0;
                                break;
                            case 'o':
                                state = StateKind.Octdig;
                                digitCount = 0;
                                break;
                            case 'x':
                                state = StateKind.Hexdig;
                                digitCount = 0;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Bindig:
                    {
                        if (digitCount >= 256)
                        {
                            return false;
                        }
                        switch (c)
                        {
                            case '0':case '1':
                                ++digitCount;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Octdig:
                    {
                        if (digitCount >= 86)
                        {
                            return false;
                        }
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':
                                ++digitCount;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Hexdig:
                    {
                        if (digitCount >= 64)
                        {
                            return false;
                        }
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':
                                ++digitCount;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Decbyte:
                    {
                        if (decbyteCount >= 4)
                        {
                            return false;
                        }
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                            {
                                if (digitCount >= 3)
                                {
                                    return false;
                                }
                                ++digitCount;
                                value = value*10 + (c - '0');
                                if (value > 255)
                                {
                                    return false;
                                }
                                break;
                            }
                            case '.':
                                if (decbyteCount > 3)
                                {
                                    return false;
                                }
                                ++decbyteCount;
                                digitCount = 0;
                                value = 0;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    default:
                        return false;
                }
            }

            switch (state)
            {
                case StateKind.Decbyte:
                    if (digitCount > 0)
                    {
                        ++decbyteCount;
                    }
                    else
                    {
                        return false;
                    }
                    return (decbyteCount == 4) ? true : false;
                case StateKind.Bindig:
                    return digitCount > 0 ? true : false;
                case StateKind.Octdig:
                    return digitCount > 0 ? true : false;
                case StateKind.Hexdig:
                    return digitCount > 0 ? true : false;
                default:
                    return false;
            }
        }
    }

    class Ipv6Validator : IFormatValidator
    {
        string _absoluteKeywordLocation;

        internal Ipv6Validator(string absoluteKeywordLocation)
        {
            _absoluteKeywordLocation = absoluteKeywordLocation;
        }

        public void Validate(string value,
                             string instanceLocation, 
                             ErrorReporter reporter) 
        {
            if (!Check(value))        
            {
                reporter.Error(new ValidationOutput("format", 
                                                    _absoluteKeywordLocation,
                                                    instanceLocation, 
                                                    $"'{value}' is not a valid email address as defined by RFC 5322"));
            }
        } 

        // RFC 2673, Section 3.2
        enum StateKind
        {
            Start, ExpectHexdigOrUnspecified,
            Hexdig, Decdig, ExpectUnspecified, Unspecified
        }

        static bool Check(string s)
        {
            StateKind state = StateKind.Start;

            int digitCount = 0;
            int pieceCount = 0;
            int pieceCount2 = 0;
            bool hasUnspecified = false;
            int decValue = 0;

            for (int i = 0; i < s.Length; ++i)
            {
                char c = s[i];
                switch (state)
                {
                    case StateKind.Start:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':
                                state = StateKind.Hexdig;
                                ++digitCount;
                                pieceCount = 0;
                                break;
                            case ':':
                                if (!hasUnspecified)
                                {
                                    state = StateKind.ExpectUnspecified;
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.ExpectHexdigOrUnspecified:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                                decValue = decValue*10 + (c - '0'); // just in case this piece is followed by a dot
                                state = StateKind.Hexdig;
                                ++digitCount;
                                break;
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':
                                state = StateKind.Hexdig;
                                ++digitCount;
                                break;
                            case ':':
                                if (!hasUnspecified)
                                {
                                    hasUnspecified = true;
                                    state = StateKind.Unspecified;
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.ExpectUnspecified:
                    {
                        if (c == ':')
                        {
                            hasUnspecified = true;
                            state = StateKind.Unspecified;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case StateKind.Hexdig:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':
                                ++digitCount;
                                break;
                            case ':':
                                if (digitCount <= 4)
                                {
                                    ++pieceCount;
                                    digitCount = 0;
                                    decValue = 0;
                                    state = StateKind.ExpectHexdigOrUnspecified;
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            case '.':
                                if (pieceCount == 6 || hasUnspecified)
                                {
                                    ++pieceCount2;
                                    state = StateKind.Decdig;
                                    decValue = 0;
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Decdig:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                                decValue = decValue*10 + (c - '0');
                                ++digitCount;
                                break;
                            case '.':
                                if (decValue > 0xff)
                                {
                                    return false;
                                }
                                digitCount = 0;
                                decValue = 0;
                                ++pieceCount2;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    case StateKind.Unspecified:
                    {
                        switch (c)
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                            case 'A':case 'B':case 'C':case 'D':case 'E':case 'F':
                            case 'a':case 'b':case 'c':case 'd':case 'e':case 'f':
                                state = StateKind.Hexdig;
                                ++digitCount;
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                    default:
                        return false;
                }
            }

            switch (state)
            {
                case StateKind.Unspecified:
                    return pieceCount <= 8;
                case StateKind.Hexdig:
                    if (digitCount <= 4)
                    {
                        ++pieceCount;
                        return digitCount > 0 && (pieceCount == 8 || (hasUnspecified && pieceCount <= 8));
                    }
                    else
                    {
                        return false;
                    }
                case StateKind.Decdig:
                    ++pieceCount2;
                    if (decValue > 0xff)
                    {
                        return false;
                    }
                    return digitCount > 0 && pieceCount2 == 4;
                default:
                    return false;
            }
        }
    }

} // namespace JsonCons.JsonSchema
