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

    [Theory]
    [InlineData("region", "BU", 2005, 10, 16)]
    [InlineData("language", "en", 2005, 10, 16)]
    [InlineData("extlang", "ase", 2009, 7, 29)]
    public void SubtagRecordAdded_WhenRecordHasAddedField_ReturnsAddedDate(string type, string subtag, int year, int month, int day)
    {
        // arrange
        var record = this.GetSubtagRecord(type, subtag);

        // act
        var testResult = record.Added;

        // assert
        testResult.Should().Be(new DateTime(year, month, day), "the {0} subtag {1} was added on that date", type, subtag);
    }

    [Fact]
    public void SubtagRecordDeprecated_WhenRecordHasDeprecatedField_ReturnsDeprecatedDate()
    {
        // arrange
        var record = this.GetSubtagRecord("region", "BU");

        // act
        var testResult = record.Deprecated;

        // assert
        testResult.Should().Be(new DateTime(1989, 12, 5), "the region subtag BU was deprecated on that date");
    }

    [Fact]
    public void SubtagRecordDeprecated_WhenRecordHasNoDeprecatedField_ReturnsNull()
    {
        // arrange
        var record = this.GetSubtagRecord("language", "en");

        // act
        var testResult = record.Deprecated;

        // assert
        testResult.Should().BeNull("the language subtag en is not deprecated");
    }

    [Theory]
    [InlineData("region", "BU", "MM")]
    [InlineData("extlang", "ase", "ase")]
    public void SubtagRecordPreferredValue_WhenRecordHasPreferredValueField_ReturnsPreferredValue(string type, string subtag, string expectedPreferredValue)
    {
        // arrange
        var record = this.GetSubtagRecord(type, subtag);

        // act
        var testResult = record.PreferredValue;

        // assert
        testResult.Should().Be(expectedPreferredValue, "the {0} subtag {1} has that preferred value", type, subtag);
    }

    [Fact]
    public void SubtagRecordPreferredValue_WhenRecordHasNoPreferredValueField_ReturnsNull()
    {
        // arrange
        var record = this.GetSubtagRecord("language", "en");

        // act
        var testResult = record.PreferredValue;

        // assert
        testResult.Should().BeNull("the language subtag en has no preferred value");
    }

    [Fact]
    public void SubtagRecordSuppressScript_WhenRecordHasSuppressScriptField_ReturnsSuppressScript()
    {
        // arrange
        var record = this.GetSubtagRecord("language", "en");

        // act
        var testResult = record.SuppressScript;

        // assert
        testResult.Should().Be("Latn", "the language subtag en has a Suppress-Script of Latn");
    }

    [Fact]
    public void SubtagRecordSuppressScript_WhenRecordHasNoSuppressScriptField_ReturnsNull()
    {
        // arrange
        var record = this.GetSubtagRecord("language", "jbo");

        // act
        var testResult = record.SuppressScript;

        // assert
        testResult.Should().BeNull("the language subtag jbo has no Suppress-Script");
    }

    [Fact]
    public void TagRecord_WhenGrandfatheredTagHasDateAndPreferredValueFields_ReturnsFieldValues()
    {
        // arrange
        var record = this.GetTagRecord("grandfathered", "zh-guoyu");

        // act
        var added = record.Added;
        var deprecated = record.Deprecated;
        var preferredValue = record.PreferredValue;
        var suppressScript = record.SuppressScript;

        // assert
        added.Should().Be(new DateTime(1999, 12, 18), "the grandfathered tag zh-guoyu was added on that date");
        deprecated.Should().Be(new DateTime(2005, 7, 15), "the grandfathered tag zh-guoyu was deprecated on that date");
        preferredValue.Should().Be("cmn", "the grandfathered tag zh-guoyu has a preferred value of cmn");
        suppressScript.Should().BeNull("the grandfathered tag zh-guoyu has no Suppress-Script");
    }

    private SubtagRegistry.SubtagRegistrySubtagRecord GetSubtagRecord(string type, string subtag)
    {
        var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
        return registry
            .OfType<SubtagRegistry.SubtagRegistrySubtagRecord>()
            .Single(record => record.Type == type && record.Subtag == subtag);
    }

    private SubtagRegistry.SubtagRegistryTagRecord GetTagRecord(string type, string tag)
    {
        var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
        return registry
            .OfType<SubtagRegistry.SubtagRegistryTagRecord>()
            .Single(record => record.Type == type && record.Tag == tag);
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