// AssemblyLoadContext is only on .NET, and the netstandard1.1 build behaves the same whatever runtime loads it.
#if NETCOREAPP
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// The netstandard1.1 build of the library, loaded into its own <see cref="AssemblyLoadContext"/> beside the build the test project
    /// references, and its <c>MimeCharsetResolver.Default</c>, called through reflection.
    /// </summary>
    /// <remarks>
    /// The project copies the build to the netstandard1.1 folder of its output. It is loaded once and never changed.
    /// </remarks>
    internal static class NetStandard11Build
    {
        private static readonly Lazy<LoadedBuild> Build = new Lazy<LoadedBuild>(() => new LoadedBuild());

        public static Assembly Assembly => Build.Value.Assembly;

        /// <summary>
        /// Calls <c>MimeCharsetResolver.Default.TryGetEncoding</c> of the netstandard1.1 build with the character set named
        /// <paramref name="name"/>.
        /// </summary>
        public static bool TryGetEncoding(string name, out Encoding encoding) => Build.Value.TryGetEncoding(name, out encoding);

        /// <summary>
        /// The encoding that <c>MimeCharsetResolver.Default</c> of the netstandard1.1 build resolves for <paramref name="name"/>, or null.
        /// </summary>
        public static Encoding GetEncoding(string name)
        {
            Build.Value.TryGetEncoding(name, out var encoding);
            return encoding;
        }

        /// <summary>
        /// Calls <c>MimeMediaType.TryParse(string, MimeMediaTypeParseOptions, out MimeMediaType)</c> of the netstandard1.1 build, with the
        /// option named <paramref name="option"/>, and gets the value of the first parameter of the media type it reads.
        /// </summary>
        /// <returns>The value of the first parameter; or null if the media type isn't read.</returns>
        public static string ParseFirstParameterValue(string mediaType, string option) =>
            Build.Value.ParseFirstParameterValue(mediaType, option);

        /// <summary>
        /// Calls <c>MimeEncodedWord.DecodeText(string, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)</c> of the
        /// netstandard1.1 build, for text in a header field, with the option named <paramref name="option"/> and
        /// <c>MimeCharsetResolver.Default</c>.
        /// </summary>
        public static string DecodeText(string text, string option) => Build.Value.DecodeText(text, option);

        private sealed class LoadedBuild
        {
            private readonly MethodInfo _parse;
            private readonly MethodInfo _tryGetEncoding;
            private readonly object _resolver;
            private readonly Type _mediaTypeOptionsType;
            private readonly MethodInfo _tryParseMediaType;
            private readonly PropertyInfo _parameters;
            private readonly PropertyInfo _parameterValue;
            private readonly Type _wordOptionsType;
            private readonly object _textContext;
            private readonly MethodInfo _decodeText;

            public LoadedBuild()
            {
                var path = Path.Combine(AppContext.BaseDirectory, "netstandard1.1", "DataStandardizer.Communication.dll");

                // A context of its own, which leaves every assembly but the one it is asked to load to the default context.
                Assembly = new AssemblyLoadContext("netstandard1.1").LoadFromAssemblyPath(path);

                var charsetType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeCharset", true);
                var resolverType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeCharsetResolver", true);
                _parse = charsetType.GetMethod("Parse", new[] { typeof(string) });
                _tryGetEncoding = resolverType.GetMethod("TryGetEncoding");
                _resolver = resolverType.GetProperty("Default").GetValue(null);

                var mediaType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeMediaType", true);
                _mediaTypeOptionsType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeMediaTypeParseOptions", true);
                _tryParseMediaType = mediaType.GetMethod(
                    "TryParse", new[] { typeof(string), _mediaTypeOptionsType, mediaType.MakeByRefType() });
                _parameters = mediaType.GetProperty("Parameters");
                _parameterValue = Assembly.GetType("DataStandardizer.Communication.Mime.MimeParameter", true).GetProperty("Value");

                var word = Assembly.GetType("DataStandardizer.Communication.Mime.MimeEncodedWord", true);
                var contextType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeEncodedWordContext", true);
                _wordOptionsType = Assembly.GetType("DataStandardizer.Communication.Mime.MimeEncodedWordParseOptions", true);
                _textContext = Enum.Parse(contextType, "Text");
                _decodeText = word.GetMethod(
                    "DecodeText", new[] { typeof(string), contextType, _wordOptionsType, Assembly.GetType(
                        "DataStandardizer.Communication.Mime.IMimeCharsetResolver", true) });
            }

            public Assembly Assembly { get; }

            public bool TryGetEncoding(string name, out Encoding encoding)
            {
                var arguments = new[] { _parse.Invoke(null, new object[] { name }), null };
                var result = (bool)_tryGetEncoding.Invoke(_resolver, arguments);
                encoding = (Encoding)arguments[1];
                return result;
            }

            public string ParseFirstParameterValue(string mediaType, string option)
            {
                var arguments = new[] { mediaType, Enum.Parse(_mediaTypeOptionsType, option), null };
                if (!(bool)_tryParseMediaType.Invoke(null, arguments)) return null;

                var parameters = (System.Collections.IEnumerable)_parameters.GetValue(arguments[2]);
                var enumerator = parameters.GetEnumerator();
                enumerator.MoveNext();
                return (string)_parameterValue.GetValue(enumerator.Current);
            }

            public string DecodeText(string text, string option) =>
                (string)_decodeText.Invoke(null, new[] { text, _textContext, Enum.Parse(_wordOptionsType, option), _resolver });
        }
    }
}
#endif
