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

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests of the netstandard1.1 build of the library.  xCard uses the XML reader and writer that every target has, so that build holds the xCard types, with their synchronous
    /// members alone, since the asynchronous members of the XML reader and writer need netstandard2.0.
    /// </summary>
    /// <remarks>
    /// The project copies the build to the netstandard1.1 folder of its output, from where these tests read it.
    /// </remarks>
    public class XcardPlatformTests
    {
        private static readonly string NetStandard11BuildPath = Path.Combine(AppContext.BaseDirectory, "netstandard1.1", "DataStandardizer.File.vCard.dll");

        [Theory]
        [InlineData("XcardReader", "ReadCard")]
        [InlineData("XcardReader", "ReadCards")]
        [InlineData("XcardWriter", "WriteCard")]
        [InlineData("XcardWriter", "WriteCards")]
        public void MethodNames_NetStandard11Build_HaveTheSynchronousMember(string typeName, string methodName)
        {
            // act
            var testResult = ReadMethodNames(NetStandard11BuildPath, typeName);

            // assert
            testResult.Should().Contain(methodName);
        }

        [Theory]
        [InlineData("XcardReader", "ReadCardAsync")]
        [InlineData("XcardWriter", "WriteCardAsync")]
        public void MethodNames_NetStandard11Build_HaveNoAsynchronousMember(string typeName, string methodName)
        {
            // act
            var testResult = ReadMethodNames(NetStandard11BuildPath, typeName);

            // assert
            testResult.Should().NotContain(methodName);
        }

        /// <summary>
        /// Reads the names of the methods that a type of an assembly defines.
        /// </summary>
        private static List<string> ReadMethodNames(string path, string typeName)
        {
            using (var stream = System.IO.File.OpenRead(path))
            using (var peReader = new PEReader(stream))
            {
                var metadata = peReader.GetMetadataReader();
                var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition).Single(definition => metadata.GetString(definition.Name) == typeName);
                return type.GetMethods().Select(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name)).ToList();
            }
        }
    }
}
#endif
