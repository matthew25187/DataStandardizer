using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47KeyedSubtagTests
    {
        [Fact]
        public void Bcp47KeyedSubtag_WithNullSubtag_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = new Bcp47KeyedSubtag(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("subtag");
        }

        [Fact]
        public void ExplicitOperator_WithNullString_ThrowsInvalidCastException()
        {
            // act
            Action testAction = () => _ = (Bcp47KeyedSubtag)(string)null;

            // assert
            testAction.Should().Throw<InvalidCastException>();
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

        [Theory]
        [InlineData(""), InlineData("-a"), InlineData(" -a"), InlineData(" \t-a-b")]
        public void Singleton_OnInstanceWithEmptyOrWhitespaceFirstSegment_ReturnsNull(string testSubtag)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Singleton;

            // assert
            testResult.Should().BeNull("the first segment of {0} has no non-whitespace character", testSubtag);
        }

        [Theory]
        [InlineData("a", 'a'), InlineData("a-", 'a'), InlineData("a--b", 'a'), InlineData(" x-y", ' '), InlineData("ab-c", 'a')]
        public void Singleton_OnInstanceWithNonWhitespaceFirstSegment_ReturnsFirstCharacter(string testSubtag, char expectedResult)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Singleton;

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [ClassData(typeof(Subtags_OnInstanceWithEmptySegments_TestCaseFactory))]
        public void Subtags_OnInstanceWithEmptySegments_ReturnsEverySegmentAfterTheFirst(string testSubtag, string[] expectedResult)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Subtags;

            // assert
            testResult.Should().Equal(expectedResult);
        }

        private class Subtags_OnInstanceWithEmptySegments_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "", new string[0] };
                yield return new object[] { "a", new string[0] };
                yield return new object[] { "a-", new[] { "" } };
                yield return new object[] { "-a", new[] { "a" } };
                yield return new object[] { "a--b", new[] { "", "b" } };
                yield return new object[] { "-", new[] { "" } };
                yield return new object[] { "x-" + string.Join("-", Enumerable.Range(0, 40).Select(i => "s" + i)), Enumerable.Range(0, 40).Select(i => "s" + i).ToArray() };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
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

        [Theory]
        [InlineData("u-co-phonebk", "U-CO-PHONEBK"), InlineData("x-whatever", "X-WhatEver"), InlineData("t-hi-latn", "T-hi-Latn")]
        public void Equals_OnInstancesDifferingOnlyInCase_ReturnsTrue(string testSubtag, string otherSubtag)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);
            var otherKeyedSubtag = new Bcp47KeyedSubtag(otherSubtag);

            // act
            var testResult = keyedSubtag.Equals(otherKeyedSubtag);

            // assert
            testResult.Should().BeTrue("subtags are case-insensitive (RFC 5646 section 2.1.1)");
            (keyedSubtag == otherKeyedSubtag).Should().BeTrue();
            (keyedSubtag != otherKeyedSubtag).Should().BeFalse();
            keyedSubtag.Equals((object)otherKeyedSubtag).Should().BeTrue();
            keyedSubtag.GetHashCode().Should().Be(otherKeyedSubtag.GetHashCode(), "equal instances have equal hash codes");
        }

        [Fact]
        public void Equals_OnInstancesWithDifferentSubtags_ReturnsFalse()
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag("u-co-phonebk");
            var otherKeyedSubtag = new Bcp47KeyedSubtag("u-co-pinyin");

            // act
            var testResult = keyedSubtag.Equals(otherKeyedSubtag);

            // assert
            testResult.Should().BeFalse();
            (keyedSubtag == otherKeyedSubtag).Should().BeFalse();
        }

        [Fact]
        public void Equals_OnUninitializedInstances_ReturnsTrue()
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag();
            var otherKeyedSubtag = new Bcp47KeyedSubtag();

            // act
            var testResult = keyedSubtag.Equals(otherKeyedSubtag);

            // assert
            testResult.Should().BeTrue();
            keyedSubtag.GetHashCode().Should().Be(otherKeyedSubtag.GetHashCode());
            keyedSubtag.Equals(new Bcp47KeyedSubtag("u-co-phonebk")).Should().BeFalse();
        }

        [Fact]
        public void ExtensionSubtags_OnTagsDifferingOnlyInCase_AreEqual()
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create("en-u-co-phonebk");
            var otherLanguageTag = Bcp47LanguageTag.Create("en-U-CO-PHONEBK");

            // act
            var testResult = languageTag.ExtensionSubtags[0] == otherLanguageTag.ExtensionSubtags[0];

            // assert
            languageTag.Should().Be(otherLanguageTag);
            testResult.Should().BeTrue("extension subtags compare the way their tags do");
            ((string)otherLanguageTag.ExtensionSubtags[0]).Should().Be("U-CO-PHONEBK", "the subtag keeps the case it was written in");
        }
    }
}
