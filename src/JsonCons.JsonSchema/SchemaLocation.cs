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

    class SchemaLocation
    {
        internal SchemaLocation(string uri)
        {
            Uri = new Uri(uri);
        }
        internal SchemaLocation(Uri uri)
        {
            Uri = uri;
        }

        internal Uri Uri {get;}

        internal string Fragment {get {return Uri.Fragment;}}

        internal bool IsAbsoluteUri 
        {
            get {return Uri.IsAbsoluteUri;}
        }

        internal bool HasJsonPointer 
        {
            get {return Uri.Fragment.Length != 0 && Uri.Fragment[0] == '/';}
        }

        internal bool HasIdentifier
        {
            get {return Uri.Fragment.Length != 0 && Uri.Fragment[0] != '/';}
        }

        internal bool TryGetPointer(out string pointer) 
        {
            pointer = Uri.Fragment;
            return HasJsonPointer ? true : false;
        }

        internal static SchemaLocation Resolve(SchemaLocation baseUri, SchemaLocation relativeUri) 
        {
            Uri newUri;
            if (Uri.TryCreate(baseUri.Uri, relativeUri.Uri, out newUri))
            {
            }
            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, string field) 
        {
            if (uri.HasIdentifier)
                return new SchemaLocation(uri.Uri);

            JsonPointer pointer = JsonPointer.Parse(uri.Fragment);
            var tokens = new List<string>();
            foreach (var token in pointer.Tokens)
            {
                tokens.Add(token);
            }
            tokens.Add(field);

            string newUri = uri.Uri.GetLeftPart(UriPartial.Query) + "#" + pointer.ToString();

            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, int index) 
        {
            if (uri.HasIdentifier)
                return new SchemaLocation(uri.Uri);

            JsonPointer pointer = JsonPointer.Parse(uri.Fragment);
            var tokens = new List<string>();
            foreach (var token in pointer.Tokens)
            {
                tokens.Add(token);
            }
            tokens.Add(index.ToString());

            string newUri = uri.Uri.GetLeftPart(UriPartial.Query) + "#" + pointer.ToString();

            return new SchemaLocation(newUri);
        }

        public override string ToString()
        {
            return Uri.ToString();
        }

        internal string GetPathAndQuery()
        {
            return Uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
        }

        internal bool Equals(SchemaLocation uri)
        {
            return Uri.Equals(uri);
        }

        internal int Compare(SchemaLocation uri)
        {
            return Uri.Compare(Uri, uri.Uri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, 
                               StringComparison.Ordinal); 
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }

        internal static SchemaLocation GetAbsoluteKeywordLocation(IList<SchemaLocation> uris)
        {
            foreach (var item in uris)
            {
                if (!item.HasIdentifier && item.IsAbsoluteUri)
                {
                    return item;
                }
            }
            return new SchemaLocation("");
        }
        /*

                string PathAndQuery 
                {
                    get {return _uri.PathAndQuery;}
                }

                int Compare( SchemaLocation& other) 
                {
                    int result = _uri.compare(other._uri);
                    if (result != 0) 
                    {
                        return result;
                    }
                    return result; 
                }

                string string() 
                {
                    string s = _uri.string();
                    return s;
                }

                friend bool operator==( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) == 0;
                }

                friend bool operator!=( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) != 0;
                }

                friend bool operator<( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) < 0;
                }

                friend bool operator<=( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) <= 0;
                }

                friend bool operator>( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) > 0;
                }

                friend bool operator>=( SchemaLocation& lhs,  SchemaLocation& rhs)
                {
                    return lhs.compare(rhs) >= 0;
                }*/
    }

} // namespace JsonCons.JsonSchema
