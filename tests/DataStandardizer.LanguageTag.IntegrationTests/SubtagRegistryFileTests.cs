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

        /// <summary>
        /// The message for the first line of the fixture, which ends with a line feed alone, as every line of the registry that IANA publishes does.
        /// </summary>
        private const string LineFeedMessage = "Line 1 ends with a line feed alone, not the CRLF that ends every line (ref. RFC 5646 §3.1.1).";

        [Fact]
        public void CreateFromFile_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.CreateFromFile(registryFile.Path, SubtagRegistryLoadOptions.AcceptBareLineFeed);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                var testResult = await SubtagRegistry.CreateFromFileAsync(registryFile.Path, SubtagRegistryLoadOptions.AcceptBareLineFeed, CancellationToken.None);

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
                Func<Task> testAction = () => SubtagRegistry.CreateFromFileAsync(registryFile.Path, SubtagRegistryLoadOptions.AcceptBareLineFeed, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }

        [Fact]
        public void CreateFromFile_WhenIanaRegistryHasLineFeedsAlone_ThrowsFormatException()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                Action testAction = () => SubtagRegistry.CreateFromFile(registryFile.Path);

                // assert
                testAction.Should().Throw<FormatException>("every line ends with CRLF by default (ref. RFC 5646 §3.1.1), and IANA publishes the registry with line feeds alone")
                    .WithMessage(LineFeedMessage);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenIanaRegistryHasLineFeedsAlone_ThrowsFormatException()
        {
            // arrange
            using (var registryFile = new TemporaryFile(IanaSubtagRegistryResource.ReadResourceText()))
            {
                // act
                Func<Task> testAction = () => SubtagRegistry.CreateFromFileAsync(registryFile.Path);

                // assert
                (await testAction.Should().ThrowAsync<FormatException>("every line ends with CRLF by default (ref. RFC 5646 §3.1.1), and IANA publishes the registry with line feeds alone"))
                    .WithMessage(LineFeedMessage);
            }
        }
    }
}
