// System.Reflection.Metadata is in the box only on .NET, and the netstandard1.1 build is the same whatever runtime reads it.
#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the netstandard1.1 build of the library.  That target has no <c>ValueTask</c> and no <c>FileStream</c>, so it must have no
    /// asynchronous members and no file-path constructors.
    /// </summary>
    /// <remarks>
    /// The project copies the build to the netstandard1.1 folder of its output, from where these tests read it.
    /// </remarks>
    public class NetStandard11BuildTests
    {
        private const string ReaderTypeName = nameof(ImfHeaderReader);
        private const string SyncReadMethodName = nameof(ImfHeaderReader.ReadField);
        private const string EntityReaderTypeName = nameof(ImfEntityReader);
        private const string EntitySyncReadMethodName = nameof(ImfEntityReader.ReadEvent);
        private const string AsyncSuffix = "Async";
        private const string StreamParameterName = "messageStream";
        private const string FilePathParameterName = "messageFilePath";

        // The constructors of the reader on this target: two public ones over a stream, the internal one for tests and container formats, and the
        // private one they all call.
        private const int ConstructorCount = 4;

        // The constructors of ImfWriter on this target, which also has an internal one for a format with its own conformance rules.
        private const int MessageConstructorCount = 5;

        private static readonly string NetStandard11BuildPath =
            Path.Combine(AppContext.BaseDirectory, "netstandard1.1", "DataStandardizer.File.InternetMessage.dll");

        private const string HeaderWriterTypeName = "ImfHeaderWriter";
        private const string EntityWriterTypeName = "ImfEntityWriter";
        private const string MessageWriterTypeName = "ImfWriter";
        private const string MessageReaderTypeName = nameof(ImfReader);
        private const string MessageSyncReadMethodName = nameof(ImfReader.ReadMessage);
        private const string MhtmlReaderTypeName = nameof(MhtmlReader);
        private const string MhtmlWriterTypeName = nameof(MhtmlWriter);
        private const string MboxReaderTypeName = nameof(MboxReader);
        private const string MboxWriterTypeName = nameof(MboxWriter);
        private const string MailboxStreamParameterName = "mailboxStream";
        private const string MailboxFilePathParameterName = "mailboxFilePath";

        [Fact]
        public void Methods_ReaderInNetStandard11Build_HaveTheSynchronousRead()
        {
            // act
            var testResult = ReadMethodNames(ReaderTypeName);

            // assert
            testResult.Should().Contain(SyncReadMethodName);
        }

        [Fact]
        public void Methods_ReaderInNetStandard11Build_HaveNoAsynchronousMembers()
        {
            // act
            var testResult = ReadMethodNames(ReaderTypeName);

            // assert
            testResult.Should().NotContain(name => name.EndsWith(AsyncSuffix, StringComparison.Ordinal));
        }

        [Fact]
        public void Methods_ReaderInNetStandard11Build_HaveNoFilePathConstructors()
        {
            // act
            var testResult = ReadMethodNames(ReaderTypeName);

            // assert
            testResult.Count(name => name == ".ctor").Should().Be(ConstructorCount);
        }

        [Fact]
        public void Constructors_ReaderInNetStandard11Build_TakeAStreamAndNotAFilePath()
        {
            // act
            var testResult = ReadConstructorParameterNames(ReaderTypeName);

            // assert
            testResult.Should().Contain(StreamParameterName).And.NotContain(FilePathParameterName);
        }

        [Theory]
        [InlineData(HeaderWriterTypeName)]
        [InlineData(EntityWriterTypeName)]
        [InlineData(MessageWriterTypeName)]
        [InlineData(MhtmlReaderTypeName)]
        [InlineData(MhtmlWriterTypeName)]
        [InlineData(MboxReaderTypeName)]
        [InlineData(MboxWriterTypeName)]
        public void Methods_WriterInNetStandard11Build_HaveNoAsynchronousMembers(string typeName)
        {
            // act
            var testResult = ReadMethodNames(typeName);

            // assert
            testResult.Should().NotBeEmpty().And.NotContain(name => name.EndsWith(AsyncSuffix, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(HeaderWriterTypeName)]
        [InlineData(EntityWriterTypeName)]
        [InlineData(MessageWriterTypeName)]
        [InlineData(MhtmlReaderTypeName)]
        [InlineData(MhtmlWriterTypeName)]
        public void Constructors_WriterInNetStandard11Build_TakeAStreamAndNotAFilePath(string typeName)
        {
            // act
            var testResult = ReadConstructorParameterNames(typeName);

            // assert
            testResult.Should().Contain(StreamParameterName).And.NotContain(FilePathParameterName);
        }

        [Theory]
        [InlineData(MboxReaderTypeName)]
        [InlineData(MboxWriterTypeName)]
        public void Constructors_MboxTypesInNetStandard11Build_TakeAStreamAndNotAFilePath(string typeName)
        {
            // act
            var testResult = ReadConstructorParameterNames(typeName);

            // assert
            testResult.Should().Contain(MailboxStreamParameterName).And.NotContain(MailboxFilePathParameterName);
        }

        [Theory]
        [InlineData(HeaderWriterTypeName, ConstructorCount)]
        [InlineData(EntityWriterTypeName, ConstructorCount)]
        [InlineData(MessageWriterTypeName, MessageConstructorCount)]
        public void Methods_WriterInNetStandard11Build_HaveNoFilePathConstructors(string typeName, int expected)
        {
            // act
            var testResult = ReadMethodNames(typeName);

            // assert
            testResult.Count(name => name == ".ctor").Should().Be(expected);
        }

        [Fact]
        public void Methods_EntityReaderInNetStandard11Build_HaveTheSynchronousRead()
        {
            // act
            var testResult = ReadMethodNames(EntityReaderTypeName);

            // assert
            testResult.Should().Contain(EntitySyncReadMethodName);
        }

        [Fact]
        public void Methods_EntityReaderInNetStandard11Build_HaveNoAsynchronousMembers()
        {
            // act
            var testResult = ReadMethodNames(EntityReaderTypeName);

            // assert
            testResult.Should().NotContain(name => name.EndsWith(AsyncSuffix, StringComparison.Ordinal));
        }

        [Fact]
        public void Methods_EntityReaderInNetStandard11Build_HaveNoFilePathConstructors()
        {
            // act
            var testResult = ReadMethodNames(EntityReaderTypeName);

            // assert
            testResult.Count(name => name == ".ctor").Should().Be(ConstructorCount);
        }

        [Fact]
        public void Constructors_EntityReaderInNetStandard11Build_TakeAStreamAndNotAFilePath()
        {
            // act
            var testResult = ReadConstructorParameterNames(EntityReaderTypeName);

            // assert
            testResult.Should().Contain(StreamParameterName).And.NotContain(FilePathParameterName);
        }

        [Fact]
        public void Methods_MessageReaderInNetStandard11Build_HaveTheSynchronousRead()
        {
            // act
            var testResult = ReadMethodNames(MessageReaderTypeName);

            // assert
            testResult.Should().Contain(MessageSyncReadMethodName);
        }

        [Fact]
        public void Methods_MessageReaderInNetStandard11Build_HaveNoAsynchronousMembers()
        {
            // act
            var testResult = ReadMethodNames(MessageReaderTypeName);

            // assert
            testResult.Should().NotContain(name => name.EndsWith(AsyncSuffix, StringComparison.Ordinal));
        }

        [Fact]
        public void Methods_MessageReaderInNetStandard11Build_HaveNoFilePathConstructors()
        {
            // act
            var testResult = ReadMethodNames(MessageReaderTypeName);

            // assert
            testResult.Count(name => name == ".ctor").Should().Be(ConstructorCount);
        }

        [Fact]
        public void Constructors_MessageReaderInNetStandard11Build_TakeAStreamAndNotAFilePath()
        {
            // act
            var testResult = ReadConstructorParameterNames(MessageReaderTypeName);

            // assert
            testResult.Should().Contain(StreamParameterName).And.NotContain(FilePathParameterName);
        }

        /// <summary>
        /// Reads the names of the parameters of the constructors that a type of the assembly defines.
        /// </summary>
        private static List<string> ReadConstructorParameterNames(string typeName)
        {
            using (var stream = System.IO.File.OpenRead(NetStandard11BuildPath))
            using (var peReader = new PEReader(stream))
            {
                var metadata = peReader.GetMetadataReader();
                var type = metadata.TypeDefinitions
                    .Select(metadata.GetTypeDefinition)
                    .Single(definition => metadata.GetString(definition.Name) == typeName);

                return type.GetMethods()
                    .Select(metadata.GetMethodDefinition)
                    .Where(method => metadata.GetString(method.Name) == ".ctor")
                    .SelectMany(method => method.GetParameters())
                    .Select(handle => metadata.GetString(metadata.GetParameter(handle).Name))
                    .ToList();
            }
        }

        /// <summary>
        /// Reads the names of the methods that a type of the assembly defines.
        /// </summary>
        private static List<string> ReadMethodNames(string typeName)
        {
            using (var stream = System.IO.File.OpenRead(NetStandard11BuildPath))
            using (var peReader = new PEReader(stream))
            {
                var metadata = peReader.GetMetadataReader();
                var type = metadata.TypeDefinitions
                    .Select(metadata.GetTypeDefinition)
                    .Single(definition => metadata.GetString(definition.Name) == typeName);

                return type.GetMethods()
                    .Select(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name))
                    .ToList();
            }
        }
    }
}
#endif
