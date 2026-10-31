// AssemblyLoadContext is only on .NET, and the netstandard1.1 build behaves the same whatever runtime loads it.
#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// Tests of the built-in encodings of the netstandard1.1 build of the library, which hand-codes US-ASCII, ISO-8859-1 and UTF-32, as
    /// netstandard1.1 has no <c>Encoding.ASCII</c>, <c>Encoding.Latin1</c> or <c>UTF32Encoding</c>.
    /// </summary>
    /// <remarks>
    /// The tests load that build into its own <see cref="AssemblyLoadContext"/>, beside the build the test project references, and call it
    /// through reflection.
    /// </remarks>
    public class NetStandard11CharsetTests
    {
        private static readonly Lazy<NetStandard11Resolver> Resolver = new Lazy<NetStandard11Resolver>(() => new NetStandard11Resolver());

        public static IEnumerable<object[]> BuiltInNames() => MimeCharsetResolverTestData.BuiltInNames();

        public static IEnumerable<object[]> ByteOrderMarks() => MimeCharsetResolverTestData.ByteOrderMarks();

        [Fact]
        public void Resolver_IsFromNetStandard11Build()
        {
            Resolver.Value.Assembly.Should().NotBeSameAs(typeof(DataStandardizer.Communication.Mime.MimeCharset).Assembly);
            Resolver.Value.Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>().FrameworkName
                .Should().Be(".NETStandard,Version=v1.1");
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void TryGetEncoding_BuiltInNameInAnyCase_DecodesAndEncodes(string name, string text, byte[] bytes)
        {
            var encoding = Resolve(name);

            encoding.GetString(bytes).Should().Be(text);
            encoding.GetBytes(text).Should().Equal(bytes);
            encoding.GetPreamble().Should().BeEmpty();
            Resolve(name.ToUpperInvariant()).Should().BeSameAs(encoding);
            Resolve(name.ToLowerInvariant()).Should().BeSameAs(encoding);
            MimeCharsetResolverTests.DecodeInBlocks(encoding, bytes, 1).Should().Be(text);
            MimeCharsetResolverTests.DecodeInBlocks(encoding, bytes, 3).Should().Be(text);
            MimeCharsetResolverTests.EncodeInBlocks(encoding, text, 1).Should().Equal(bytes);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void TryGetEncoding_UnmarkedSchemeWithByteOrderMark_UsesItsByteOrderAndRemovesIt(string name, byte[] mark, string text,
            byte[] bytes)
        {
            var encoding = Resolve(name);
            var marked = mark.Concat(bytes).ToArray();

            encoding.GetString(marked).Should().Be(text);
            MimeCharsetResolverTests.DecodeInBlocks(encoding, marked, 1).Should().Be(text);
        }

        [Fact]
        public void TryGetEncoding_Latin1_RoundTripsEveryByte()
        {
            var encoding = Resolve("latin1");
            var bytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

            var text = encoding.GetString(bytes);

            text.Select(c => (int)c).Should().Equal(Enumerable.Range(0, 256));
            encoding.GetBytes(text).Should().Equal(bytes);
            encoding.GetBytes("\u0100\uD83D\uDE00\uD83D").Should().Equal((byte)'?', (byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void TryGetEncoding_UsAscii_ReplacesCharactersOutsideRange()
        {
            var encoding = Resolve("US-ASCII");

            encoding.GetString(new byte[] { 0x41, 0x80, 0xFF }).Should().Be("A\uFFFD\uFFFD");
            encoding.GetBytes("A\u0080\uD83D\uDE00").Should().Equal((byte)'A', (byte)'?', (byte)'?', (byte)'?');
        }

        [Theory]
        // Above U+10FFFF, a surrogate, and a code unit that is negative as a signed integer.
        [InlineData("UTF-32BE", new byte[] { 0x00, 0x11, 0x00, 0x00 })]
        [InlineData("UTF-32BE", new byte[] { 0x00, 0x00, 0xD8, 0x00 })]
        [InlineData("UTF-32BE", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData("UTF-32LE", new byte[] { 0x00, 0xDC, 0x00, 0x00 })]
        // An incomplete code unit at the end.
        [InlineData("UTF-32BE", new byte[] { 0x00, 0x00 })]
        [InlineData("UTF-32LE", new byte[] { 0x41 })]
        public void TryGetEncoding_Utf32_ReplacesInvalidCodeUnit(string name, byte[] bytes)
        {
            var encoding = Resolve(name);

            encoding.GetString(bytes).Should().Be("\uFFFD");
            MimeCharsetResolverTests.DecodeInBlocks(encoding, bytes, 1).Should().Be("\uFFFD");
        }

        public static IEnumerable<object[]> UnpairedSurrogates()
        {
            // An attribute can't hold these, as it keeps strings as UTF-8.
            var replacement = new byte[] { 0x00, 0x00, 0xFF, 0xFD };
            var letter = new byte[] { 0x00, 0x00, 0x00, 0x41 };
            yield return new object[] { "\uD83D", replacement };
            yield return new object[] { "\uDE00A", replacement.Concat(letter).ToArray() };
            yield return new object[] { "\uD83DA", replacement.Concat(letter).ToArray() };
            yield return new object[] { "A\uDE00\uD83D", letter.Concat(replacement).Concat(replacement).ToArray() };
        }

        [Theory]
        [MemberData(nameof(UnpairedSurrogates))]
        public void TryGetEncoding_Utf32_WritesUnpairedSurrogateAsReplacementCharacter(string text, byte[] bytes)
        {
            var encoding = Resolve("UTF-32BE");

            encoding.GetBytes(text).Should().Equal(bytes);
            MimeCharsetResolverTests.EncodeInBlocks(encoding, text, 1).Should().Equal(bytes);
        }

        [Fact]
        public void TryGetEncoding_UnknownName_ReturnsFalse()
        {
            Resolver.Value.TryGetEncoding("x-no-such-charset", out var encoding).Should().BeFalse();

            encoding.Should().BeNull();
        }

        private static Encoding Resolve(string name)
        {
            Resolver.Value.TryGetEncoding(name, out var encoding).Should().BeTrue("{0} should resolve", name);
            return encoding;
        }

        /// <summary>
        /// <c>MimeCharsetResolver.Default</c> of the netstandard1.1 build.
        /// </summary>
        private sealed class NetStandard11Resolver
        {
            private readonly MethodInfo _parse;
            private readonly MethodInfo _tryGetEncoding;
            private readonly object _resolver;

            public NetStandard11Resolver()
            {
                var path = FindNetStandard11Build();
                var context = new IsolatedContext(path);
                Assembly = context.LoadFromAssemblyPath(path);

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

            /// <summary>
            /// Finds the netstandard1.1 build of the library, which the test project builds alongside the one it references.
            /// </summary>
            private static string FindNetStandard11Build()
            {
                // The test assembly is in tests/<project>/bin/<configuration>/<framework>.
                var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
                var configuration = outputDirectory.Parent.Name;
                var repository = outputDirectory;
                while (repository != null && !System.IO.File.Exists(Path.Combine(repository.FullName, "DataStandardizer.sln")))
                {
                    repository = repository.Parent;
                }

                repository.Should().NotBeNull("the tests run within the repository");
                var path = Path.Combine(repository.FullName, "src", "DataStandardizer.Communication", "bin", configuration, "netstandard1.1",
                    "DataStandardizer.Communication.dll");
                System.IO.File.Exists(path).Should().BeTrue("the netstandard1.1 build should be at {0}", path);
                return path;
            }
        }

        /// <summary>
        /// Loads the netstandard1.1 build of the library, and leaves every other assembly to the default context.
        /// </summary>
        private sealed class IsolatedContext : AssemblyLoadContext
        {
            private readonly string _path;

            public IsolatedContext(string path)
                : base("netstandard1.1")
            {
                _path = path;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return assemblyName.Name == Path.GetFileNameWithoutExtension(_path) ? LoadFromAssemblyPath(_path) : null;
            }
        }
    }
}
#endif
