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

        private sealed class LoadedBuild
        {
            private readonly MethodInfo _parse;
            private readonly MethodInfo _tryGetEncoding;
            private readonly object _resolver;

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
            }

            public Assembly Assembly { get; }

            public bool TryGetEncoding(string name, out Encoding encoding)
            {
                var arguments = new[] { _parse.Invoke(null, new object[] { name }), null };
                var result = (bool)_tryGetEncoding.Invoke(_resolver, arguments);
                encoding = (Encoding)arguments[1];
                return result;
            }
        }
    }
}
#endif
