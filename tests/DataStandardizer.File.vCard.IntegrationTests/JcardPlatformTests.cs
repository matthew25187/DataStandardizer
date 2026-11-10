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
    /// Tests of the netstandard1.1 build of the library.  jCard needs System.Text.Json, which netstandard1.1 does not have, so that build must not hold the jCard types, while it
    /// still reads and writes <c>.vcf</c> data.
    /// </summary>
    /// <remarks>
    /// The project copies the build to the netstandard1.1 folder of its output, from where these tests read it.
    /// </remarks>
    public class JcardPlatformTests
    {
        private static readonly string NetStandard11BuildPath = Path.Combine(AppContext.BaseDirectory, "netstandard1.1", "DataStandardizer.File.vCard.dll");

        [Fact]
        public void TypeDefinitions_NetStandard11Build_HaveTheVcardReaderAndWriter()
        {
            // act
            var testResult = ReadTypeNames(NetStandard11BuildPath);

            // assert
            testResult.Should().Contain(new[] { "VcardReader", "VcardWriter", "VcardValueFormat" });
        }

        [Fact]
        public void TypeDefinitions_NetStandard11Build_HaveNoJcardTypes()
        {
            // act
            var testResult = ReadTypeNames(NetStandard11BuildPath);

            // assert
            testResult.Should().NotContain(name => name.StartsWith("Jcard", StringComparison.Ordinal));
        }

        /// <summary>
        /// Reads the names of the types that an assembly defines.
        /// </summary>
        private static List<string> ReadTypeNames(string path)
        {
            using (var stream = System.IO.File.OpenRead(path))
            using (var peReader = new PEReader(stream))
            {
                var metadata = peReader.GetMetadataReader();
                return metadata.TypeDefinitions
                    .Select(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name))
                    .ToList();
            }
        }
    }
}
#endif
