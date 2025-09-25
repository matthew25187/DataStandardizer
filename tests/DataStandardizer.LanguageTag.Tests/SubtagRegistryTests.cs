using System.Text;
using FluentAssertions;

namespace DataStandardizer.LanguageTag.Tests;

public class SubtagRegistryTests : IClassFixture<Bcp47LanguageTagFixture>, IDisposable
{
    private readonly Bcp47LanguageTagFixture _fixture;
    private string? _tempFilePath;

    public SubtagRegistryTests(Bcp47LanguageTagFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CreateFromContent_WhenRegistryContentIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContent(null!);

        // assert
        testAction.Should()
            .Throw<ArgumentNullException>("the Subtag Registry content must not be null")
            .WithParameterName("subtagRegistryContent");
    }

    [Fact]
    public async Task CreateFromContent_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        // ReSharper disable once MethodHasAsyncOverload
        var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(subtagRegistryContent);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
    }

    [Fact]
    public void CreateFromContentAsync_WhenRegistryContentIsNull_ThrowsArgumentNullException()
    {
        // act
        Func<Task> testAction = async () => await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(null!);

        // assert
        testAction.Should()
            .ThrowAsync<ArgumentNullException>("the Subtag Registry content must not be null")
            .WithParameterName("subtagRegistryContent");
    }

    [Fact]
    public async Task CreateFromContentAsync_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        var testResult = await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(subtagRegistryContent);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
    }

    [Fact]
    public void CreateFromFile_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFile(null!);

        // assert
        testAction.Should()
            .Throw<ArgumentNullException>("the Subtag Registry file path must not be null")
            .WithParameterName("subtagRegistryFilePath");
    }

    [Fact]
    public async Task CreateFromFile_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        _tempFilePath = await this.CreateRegistryFileAsync(subtagRegistryContent);
        var expectedRecordCount = await GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        // ReSharper disable once MethodHasAsyncOverload
        var testResult = SubtagRegistry.SubtagRegistry.CreateFromFile(_tempFilePath);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
    }

    [Fact]
    public void CreateFromFileAsync_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
    {
        // act
        Func<Task> testAction = async () => await SubtagRegistry.SubtagRegistry.CreateFromFileAsync(null!);

        // assert
        testAction.Should()
            .ThrowAsync<ArgumentNullException>("the Subtag Registry file path must not be null")
            .WithParameterName("subtagRegistryFilePath");
    }

    [Fact]
    public async Task CreateFromFileAsync_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        _tempFilePath = await CreateRegistryFileAsync(subtagRegistryContent);
        var expectedRecordCount = await GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        var testResult = await SubtagRegistry.SubtagRegistry.CreateFromFileAsync(_tempFilePath);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
    }

    [Fact]
    public void CreateFromStream_WhenStreamIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = SubtagRegistry.SubtagRegistry.CreateFromStream(null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the stream must not be null");
    }

    [Fact]
    public async Task CreateFromStream_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        var subtagRegistryBytes = Encoding.Default.GetBytes(subtagRegistryContent);
        using var subtagRegistryStream = new MemoryStream(subtagRegistryBytes);

        var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        // ReSharper disable once MethodHasAsyncOverload
        var testResult = SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the subtag registry", expectedRecordCount);
    }

    [Fact]
    public void CreateFromStreamAsync_WhenStreamIsNull_ThrowsArgumentNullException()
    {
        // act
        Func<Task> testAction = async () => _ = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(null!);

        // assert
        testAction.Should().ThrowAsync<ArgumentNullException>("the stream must not be null");
    }

    [Fact]
    public async Task CreateFromStreamAsync_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
    {
        // arrange
        var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
        var subtagRegistryBytes = Encoding.Default.GetBytes(subtagRegistryContent);
        using var subtagRegistryStream = new MemoryStream(subtagRegistryBytes);

        var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

        // act
        var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

        // assert
        testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the subtag registry", expectedRecordCount);
    }

    private async Task<string> CreateRegistryFileAsync(string registryContent)
    {
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, registryContent, Encoding.UTF8);
        return tempFilePath;
    }

    private async Task<int> GetRegistryRecordCountAsync(string registryContent)
    {
        var recordCount = 0;
        using (var reader = new StringReader(registryContent))
        {
            var line = await reader.ReadLineAsync();
            while (line != null)
            {
                if (line == "%%") recordCount++;

                line = await reader.ReadLineAsync();
            }
        }

        return recordCount + 1;
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
}