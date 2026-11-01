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

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Tests of the netstandard1.1 build of the library.  jCal needs System.Text.Json, which netstandard1.1 does not have, so that build must
    /// not hold the jCal types, while it still holds xCal.
    /// </summary>
    /// <remarks>
    /// The project copies the build to the netstandard1.1 folder of its output, from where these tests read it.
    /// </remarks>
    public class JcalPlatformTests
    {
        private static readonly string NetStandard11BuildPath =
            Path.Combine(AppContext.BaseDirectory, "netstandard1.1", "DataStandardizer.File.iCal.dll");

        [Fact]
        public void TypeDefinitions_NetStandard11Build_HaveXcalTypes()
        {
            // act
            var testResult = ReadTypeNames(NetStandard11BuildPath);

            // assert
            testResult.Should().Contain(new[] { "XcalReader", "XcalWriter", "IcalValueFormat" });
        }

        [Fact]
        public void TypeDefinitions_NetStandard11Build_HaveNoJcalTypes()
        {
            // act
            var testResult = ReadTypeNames(NetStandard11BuildPath);

            // assert
            testResult.Should().NotContain(name => name.StartsWith("Jcal", StringComparison.Ordinal));
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
