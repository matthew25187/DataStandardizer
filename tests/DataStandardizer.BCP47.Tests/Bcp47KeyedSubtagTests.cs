using System.Collections;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.BCP47.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Bcp47KeyedSubtagTests
{
    [Fact]
    public void Bcp47KeyedSubtag_WithNullSubtag_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = new Bcp47KeyedSubtag(null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName("subtag");
    }

    [Fact]
    public void Singleton_OnInstanceWithNoSubtag_ThrowsInvalidOperationException()
    {
        // arrange
        var keyedSubtag = new Bcp47KeyedSubtag();

        // act
        Action testAction = () => _ = keyedSubtag.Singleton;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("i-ami", 'i'), InlineData("i-bnn", 'i'), InlineData("i-default", 'i'), InlineData("i-enochian", 'i'), InlineData("i-hak", 'i'), InlineData("i-klingon", 'i'), InlineData("i-lux", 'i'), InlineData("i-mingo", 'i'),
     InlineData("i-navajo", 'i'), InlineData("i-pwn", 'i'), InlineData("i-tao", 'i'), InlineData("i-tay", 'i'), InlineData("i-tsu", 'i'), InlineData("u-sd-chzh", 'u')]
    public void Singleton_OnInstanceWithValidSubtag_ReturnsSingletonComponentOfSubtag(string testSubtag, char expectedResult)
    {
        // arrange
        var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

        // act
        var testResult = keyedSubtag.Singleton;

        // assert
        testResult.Should().Be(expectedResult, "{0} is the singleton component of the {1} subtag", expectedResult, testSubtag);
    }

    [Fact]
    public void Subtags_OnInstanceWithNoSubtag_ThrowsInvalidOperationException()
    {
        // arrange
        var keyedSubtag = new Bcp47KeyedSubtag();

        // act
        Action testAction = () => _ = keyedSubtag.Subtags;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [ClassData(typeof(Subtags_OnInstanceWithValidSubtag_TestCaseFactory))]
    public void Subtags_OnInstanceWithValidSubtag_ReturnsSubtagComponentsOfSubtag(string testSubtag, string[] expectedResult)
    {
        // arrange
        var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

        // act
        var testResult = keyedSubtag.Subtags;

        // assert
        testResult.Should().BeEquivalentTo(expectedResult, "{0} are the subtag components of the {1} subtag", string.Join(", ", expectedResult), testSubtag);
    }

    private class Subtags_OnInstanceWithValidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "i-ami", new[] { "ami" } };
            yield return new object[] { "i-bnn", new[] { "bnn" } };
            yield return new object[] { "i-default", new[] { "default" } };
            yield return new object[] { "i-enochian", new[] { "enochian" } };
            yield return new object[] { "i-hak", new[] { "hak" } };
            yield return new object[] { "i-klingon", new[] { "klingon" } };
            yield return new object[] { "i-lux", new[] { "lux" } };
            yield return new object[] { "i-mingo", new[] { "mingo" } };
            yield return new object[] { "i-navajo", new[] { "navajo" } };
            yield return new object[] { "i-pwn", new[] { "pwn" } };
            yield return new object[] { "i-tao", new[] { "tao" } };
            yield return new object[] { "i-tay", new[] { "tay" } };
            yield return new object[] { "i-tsu", new[] { "tsu" } };
            yield return new object[] { "u-sd-chzh", new[] { "sd", "chzh" } };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}