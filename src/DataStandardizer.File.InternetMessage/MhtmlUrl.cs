using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Resolves relative URLs as RFC 1808 §4 does, on their text, for <see cref="MhtmlDocument"/>.
    /// </summary>
    /// <remarks>
    /// RFC 2557 §5 cites RFC 1808, and its §8.2 compares the results octet by octet without translating <c>%</c> escapes.  <see cref="Uri"/> follows a later edition, and changes the
    /// text it is given in ways that vary with the runtime and the operating system, so it can't do this.
    /// </remarks>
    // TODO: RFC 3986 §5.2 replaced this algorithm.  The results differ for a reference such as "../../../g", which RFC 1808 §5.2 keeps above the root, so revisit this if RFC 2557 is
    // ever revised to cite it.
    internal static class MhtmlUrl
    {
        /// <summary>
        /// The base of RFC 2557 §5 (e), for when nothing gives an absolute URL.
        /// </summary>
        internal const string ThisMessageBase = "thismessage:/";

        /// <summary>
        /// Determines if a URL starts with a scheme name (RFC 1808 §2.4.2), and so is absolute.
        /// </summary>
        internal static bool IsAbsolute(string url)
        {
            return GetSchemeLength(url) > 0;
        }

        /// <summary>
        /// Determines if a URL has a scheme.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="scheme">The scheme name, without its colon.</param>
        internal static bool HasScheme(string url, string scheme)
        {
            return GetSchemeLength(url) == scheme.Length && string.Compare(url, 0, scheme, 0, scheme.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Gets a URL without its fragment identifier, which RFC 1808 §2.1 says is not a part of the URL, so that it identifies the same resource (RFC 1808 §2.4.1).
        /// </summary>
        internal static string WithoutFragment(string url)
        {
            var index = url.IndexOf('#');
            return index < 0 ? url : url.Substring(0, index);
        }

        /// <summary>
        /// Resolves a URL against a base URL.
        /// </summary>
        /// <param name="baseUrl">The absolute base URL.</param>
        /// <param name="url">The URL, absolute or relative.</param>
        /// <returns>The absolute URL.</returns>
        internal static string Resolve(string baseUrl, string url)
        {
            // ref. RFC 1808 §4 step 2 (a) and (b).
            if (url.Length == 0) return baseUrl;
            if (IsAbsolute(url)) return url;

            var reference = new Parts(url);
            var parent = new Parts(baseUrl);

            // ref. RFC 1808 §4 step 3: a network location leaves everything after the scheme as it is.
            if (!(reference.NetLoc is null)) return Combine(parent.Scheme, reference.NetLoc, reference.Path, reference.Params, reference.Query, reference.Fragment);

            string path;
            var parameters = reference.Params;
            var query = reference.Query;
            if (reference.Path.Length > 0 && reference.Path[0] == '/')
            {
                // ref. RFC 1808 §4 step 4.
                path = reference.Path;
            }
            else if (reference.Path.Length == 0)
            {
                // ref. RFC 1808 §4 step 5.
                path = parent.Path;
                if (parameters is null)
                {
                    parameters = parent.Params;
                    if (query is null) query = parent.Query;
                }
            }
            else
            {
                // ref. RFC 1808 §4 step 6.
                path = RemoveDotSegments(parent.Path.Substring(0, parent.Path.LastIndexOf('/') + 1) + reference.Path);
            }

            return Combine(parent.Scheme, parent.NetLoc, path, parameters, query, reference.Fragment);
        }

        private static string RemoveDotSegments(string path)
        {
            var segments = path.Split('/');
            var output = new List<string>(segments.Length);
            var hasRoot = path[0] == '/';
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                var isLast = index == segments.Length - 1;
                if (segment == ".")
                {
                    // ref. RFC 1808 §4 step 6 (a) and (b).
                    if (isLast) output.Add(string.Empty);
                }
                else if (segment == ".." && output.Count > (hasRoot ? 1 : 0) && output[output.Count - 1] != "..")
                {
                    // ref. RFC 1808 §4 step 6 (c) and (d).
                    output.RemoveAt(output.Count - 1);
                    if (isLast) output.Add(string.Empty);
                }
                else
                {
                    output.Add(segment);
                }
            }

            return string.Join("/", output);
        }

        private static string Combine(string scheme, [AllowNull] string netLoc, string path, [AllowNull] string parameters, [AllowNull] string query, [AllowNull] string fragment)
        {
            var output = new StringBuilder();
            output.Append(scheme).Append(':');
            if (!(netLoc is null))
            {
                output.Append("//").Append(netLoc);

                // A network location ends at a slash, so that a relative path doesn't run into it.
                if (path.Length > 0 && path[0] != '/') output.Append('/');
            }

            output.Append(path);
            if (!(parameters is null)) output.Append(';').Append(parameters);
            if (!(query is null)) output.Append('?').Append(query);
            if (!(fragment is null)) output.Append('#').Append(fragment);

            return output.ToString();
        }

        private static int GetSchemeLength(string url)
        {
            for (var index = 0; index < url.Length; index++)
            {
                var c = url[index];
                if (c == ':') return index;

                // ref. RFC 1808 §2.2: scheme = 1*( alpha | digit | "+" | "-" | "." )
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '+' || c == '-' || c == '.')) return 0;
            }

            return 0;
        }

        /// <summary>
        /// The components of a URL, parsed in the order of RFC 1808 §2.4.  A component that is <c>null</c> is absent, and a path keeps the slash that precedes it.
        /// </summary>
        private readonly struct Parts
        {
            internal Parts(string url)
            {
                var rest = url;
                Fragment = TakeAfter(ref rest, '#');

                var schemeLength = GetSchemeLength(rest);
                Scheme = schemeLength > 0 ? rest.Substring(0, schemeLength) : string.Empty;
                if (schemeLength > 0) rest = rest.Substring(schemeLength + 1);

                NetLoc = null;
                if (rest.StartsWith("//", StringComparison.Ordinal))
                {
                    var end = rest.IndexOf('/', 2);
                    NetLoc = end < 0 ? rest.Substring(2) : rest.Substring(2, end - 2);
                    rest = end < 0 ? string.Empty : rest.Substring(end);
                }

                Query = TakeAfter(ref rest, '?');
                Params = TakeAfter(ref rest, ';');
                Path = rest;
            }

            internal string Scheme { get; }

            [AllowNull, MaybeNull]
            internal string NetLoc { get; }

            internal string Path { get; }

            [AllowNull, MaybeNull]
            internal string Params { get; }

            [AllowNull, MaybeNull]
            internal string Query { get; }

            [AllowNull, MaybeNull]
            internal string Fragment { get; }

            [return: MaybeNull]
            private static string TakeAfter(ref string rest, char delimiter)
            {
                var index = rest.IndexOf(delimiter);
                if (index < 0) return null;

                var taken = rest.Substring(index + 1);
                rest = rest.Substring(0, index);
                return taken;
            }
        }
    }
}
