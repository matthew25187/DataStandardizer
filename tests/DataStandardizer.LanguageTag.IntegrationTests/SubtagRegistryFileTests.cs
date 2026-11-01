using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// Tests of loading a Subtag Registry from a file, which need a real file.
    /// </summary>
    public class SubtagRegistryFileTests
    {
        /// <summary>
        /// The number of records in the IANA registry fixture: its File-Date record and the 9,274 records after it, each after a <c>%%</c> line (RFC 5646 §3.1.1).
        /// </summary>
        private const int IanaRegistryRecordCount = 9275;

        [Fact]
        public void CreateFromFile_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.SubtagRegistry.CreateFromFile(registryFile.Path);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromFileAsync(registryFile.Path);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFileAsync(registryFile.Path, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }
    }
}
