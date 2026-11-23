using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Language.Tests;

/// <summary>
/// Tests of the interfaces that the ISO 639 string enumerations implement.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso639InterfaceTests
{
    [Theory]
    [InlineData(typeof(Iso639Part1Language))]
    [InlineData(typeof(Iso639Part2BLanguage))]
    [InlineData(typeof(Iso639Part2TLanguage))]
    [InlineData(typeof(Iso639Part3Language))]
    [InlineData(typeof(Iso639Part5LanguageFamily))]
    public void IsAssignableFrom_Iso639StructToIConvertible_ReturnsTrue(Type structType)
    {
        // act
        var testResult = typeof(IConvertible).IsAssignableFrom(structType);

        // assert
        testResult.Should().BeTrue();
    }
}
