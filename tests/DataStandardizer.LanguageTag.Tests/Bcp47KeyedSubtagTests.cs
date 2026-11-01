using System;
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
        #region Declarations

        // The irregular grandfathered tags that start with the singleton i (RFC 5646 §2.1, §2.2.8).
        private const string GrandfatheredAmi = "i-ami";
        private const string GrandfatheredBnn = "i-bnn";
        private const string GrandfatheredDefault = "i-default";
        private const string GrandfatheredEnochian = "i-enochian";
        private const string GrandfatheredHak = "i-hak";
        private const string GrandfatheredKlingon = "i-klingon";
        private const string GrandfatheredLux = "i-lux";
        private const string GrandfatheredMingo = "i-mingo";
        private const string GrandfatheredNavajo = "i-navajo";
        private const string GrandfatheredPwn = "i-pwn";
        private const string GrandfatheredTao = "i-tao";
        private const string GrandfatheredTay = "i-tay";
        private const string GrandfatheredTsu = "i-tsu";

        /// <summary>
        /// A Unicode locale extension with the subdivision key (RFC 5646 §2.2.6; RFC 6067), from the RFC 5646 Appendix A example <c>gsw-u-sd-chzh</c>.
        /// </summary>
        private const string SubdivisionExtension = "u-sd-chzh";

        /// <summary>
        /// A Unicode locale extension with the collation key (RFC 5646 §2.2.6; RFC 6067).
        /// </summary>
        private const string PhonebookCollationExtension = "u-co-phonebk";

        /// <summary>
        /// <see cref="PhonebookCollationExtension"/> in upper case, which is the same subtag (RFC 5646 §2.1.1).
        /// </summary>
        private const string PhonebookCollationExtensionUpperCase = "U-CO-PHONEBK";

        private const string EmptyFirstSegment = "-a";
        private const string EmptyLastSegment = "a-";
        private const string EmptyMiddleSegment = "a--b";

        /// <summary>
        /// Each way of comparing two instances for equality.
        /// </summary>
        private static readonly Dictionary<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>> EqualityOperations = new Dictionary<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>>
        {
            { "Equals(Bcp47KeyedSubtag)", (left, right) => left.Equals(right) },
            { "Equals(object)", (left, right) => left.Equals((object)right) },
            { "operator ==", (left, right) => left == right }
        };

        public static TheoryData<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>, Bcp47KeyedSubtag, Bcp47KeyedSubtag> InstancesDifferingOnlyInCase =>
            CreateEqualityCases(
                (new Bcp47KeyedSubtag(PhonebookCollationExtension), new Bcp47KeyedSubtag(PhonebookCollationExtensionUpperCase)),
                (new Bcp47KeyedSubtag("x-whatever"), new Bcp47KeyedSubtag("X-WhatEver")),
                (new Bcp47KeyedSubtag("t-hi-latn"), new Bcp47KeyedSubtag("T-hi-Latn")));

        public static TheoryData<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>, Bcp47KeyedSubtag, Bcp47KeyedSubtag> InstancesWithDifferentSubtags =>
            CreateEqualityCases(
                (new Bcp47KeyedSubtag(PhonebookCollationExtension), new Bcp47KeyedSubtag("u-co-pinyin")),
                (new Bcp47KeyedSubtag(), new Bcp47KeyedSubtag(PhonebookCollationExtension)));

        public static TheoryData<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>, Bcp47KeyedSubtag, Bcp47KeyedSubtag> UninitializedInstances =>
            CreateEqualityCases((new Bcp47KeyedSubtag(), new Bcp47KeyedSubtag()));

        private static TheoryData<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>, Bcp47KeyedSubtag, Bcp47KeyedSubtag> CreateEqualityCases(params (Bcp47KeyedSubtag Left, Bcp47KeyedSubtag Right)[] pairs)
        {
            var data = new TheoryData<string, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool>, Bcp47KeyedSubtag, Bcp47KeyedSubtag>();
            foreach (var operation in EqualityOperations)
            foreach (var pair in pairs)
            {
                data.Add(operation.Key, operation.Value, pair.Left, pair.Right);
            }

            return data;
        }

        #endregion

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
        [InlineData(GrandfatheredAmi, 'i'), InlineData(GrandfatheredBnn, 'i'), InlineData(GrandfatheredDefault, 'i'), InlineData(GrandfatheredEnochian, 'i'), InlineData(GrandfatheredHak, 'i'),
         InlineData(GrandfatheredKlingon, 'i'), InlineData(GrandfatheredLux, 'i'), InlineData(GrandfatheredMingo, 'i'), InlineData(GrandfatheredNavajo, 'i'), InlineData(GrandfatheredPwn, 'i'),
         InlineData(GrandfatheredTao, 'i'), InlineData(GrandfatheredTay, 'i'), InlineData(GrandfatheredTsu, 'i'), InlineData(SubdivisionExtension, 'u')]
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
        [InlineData(""), InlineData(EmptyFirstSegment), InlineData(" -a"), InlineData(" \t-a-b")]
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
        [InlineData("a", 'a'), InlineData(EmptyLastSegment, 'a'), InlineData(EmptyMiddleSegment, 'a'), InlineData(" x-y", ' '), InlineData("ab-c", 'a')]
        public void Singleton_OnInstanceWithNonWhitespaceFirstSegment_ReturnsFirstCharacter(string testSubtag, char expectedResult)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Singleton;

            // assert
            testResult.Should().Be(expectedResult);
        }

        /// <summary>
        /// Subtags with empty segments, each with every segment after its first. The last has 40 segments after its singleton, more than
        /// a subtag would be split into without growing its buffer.
        /// </summary>
        public static TheoryData<string, string[]> SubtagsWithEmptySegments
        {
            get
            {
                const int manySegmentCount = 40;
                var manySegments = Enumerable.Range(0, manySegmentCount).Select(index => "s" + index).ToArray();
                return new TheoryData<string, string[]>
                {
                    { "", new string[0] },
                    { "a", new string[0] },
                    { EmptyLastSegment, new[] { "" } },
                    { EmptyFirstSegment, new[] { "a" } },
                    { EmptyMiddleSegment, new[] { "", "b" } },
                    { "-", new[] { "" } },
                    { "x-" + string.Join("-", manySegments), manySegments }
                };
            }
        }

        [Theory]
        [MemberData(nameof(SubtagsWithEmptySegments))]
        public void Subtags_OnInstanceWithEmptySegments_ReturnsEverySegmentAfterTheFirst(string testSubtag, string[] expectedResult)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Subtags;

            // assert
            testResult.Should().Equal(expectedResult);
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

        public static TheoryData<string, string[]> SubtagsWithComponents => new TheoryData<string, string[]>
        {
            { GrandfatheredAmi, new[] { "ami" } },
            { GrandfatheredBnn, new[] { "bnn" } },
            { GrandfatheredDefault, new[] { "default" } },
            { GrandfatheredEnochian, new[] { "enochian" } },
            { GrandfatheredHak, new[] { "hak" } },
            { GrandfatheredKlingon, new[] { "klingon" } },
            { GrandfatheredLux, new[] { "lux" } },
            { GrandfatheredMingo, new[] { "mingo" } },
            { GrandfatheredNavajo, new[] { "navajo" } },
            { GrandfatheredPwn, new[] { "pwn" } },
            { GrandfatheredTao, new[] { "tao" } },
            { GrandfatheredTay, new[] { "tay" } },
            { GrandfatheredTsu, new[] { "tsu" } },
            { SubdivisionExtension, new[] { "sd", "chzh" } }
        };

        [Theory]
        [MemberData(nameof(SubtagsWithComponents))]
        public void Subtags_OnInstanceWithValidSubtag_ReturnsSubtagComponentsOfSubtag(string testSubtag, string[] expectedResult)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);

            // act
            var testResult = keyedSubtag.Subtags;

            // assert
            testResult.Should().Equal(expectedResult, "the subtag components of {0} are the segments after its singleton", testSubtag);
        }

        [Theory]
        [MemberData(nameof(InstancesDifferingOnlyInCase), DisableDiscoveryEnumeration = true)]
        public void Equals_OnInstancesDifferingOnlyInCase_ReturnsTrue(string operationName, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool> operation, Bcp47KeyedSubtag keyedSubtag, Bcp47KeyedSubtag otherKeyedSubtag)
        {
            // act
            var testResult = operation(keyedSubtag, otherKeyedSubtag);

            // assert
            testResult.Should().BeTrue("subtags are case-insensitive (RFC 5646 §2.1.1), so {0} finds {1} and {2} equal", operationName, keyedSubtag, otherKeyedSubtag);
        }

        [Theory]
        [InlineData(PhonebookCollationExtension, PhonebookCollationExtensionUpperCase), InlineData("x-whatever", "X-WhatEver")]
        public void InequalityOperator_OnInstancesDifferingOnlyInCase_ReturnsFalse(string testSubtag, string otherSubtag)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);
            var otherKeyedSubtag = new Bcp47KeyedSubtag(otherSubtag);

            // act
            var testResult = keyedSubtag != otherKeyedSubtag;

            // assert
            testResult.Should().BeFalse("subtags are case-insensitive (RFC 5646 §2.1.1)");
        }

        [Theory]
        [InlineData(PhonebookCollationExtension, PhonebookCollationExtensionUpperCase), InlineData("x-whatever", "X-WhatEver"), InlineData("t-hi-latn", "T-hi-Latn")]
        public void GetHashCode_OnInstancesDifferingOnlyInCase_ReturnsEqualHashCodes(string testSubtag, string otherSubtag)
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag(testSubtag);
            var otherHashCode = new Bcp47KeyedSubtag(otherSubtag).GetHashCode();

            // act
            var testResult = keyedSubtag.GetHashCode();

            // assert
            testResult.Should().Be(otherHashCode, "equal instances have equal hash codes");
        }

        [Theory]
        [MemberData(nameof(InstancesWithDifferentSubtags), DisableDiscoveryEnumeration = true)]
        public void Equals_OnInstancesWithDifferentSubtags_ReturnsFalse(string operationName, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool> operation, Bcp47KeyedSubtag keyedSubtag, Bcp47KeyedSubtag otherKeyedSubtag)
        {
            // act
            var testResult = operation(keyedSubtag, otherKeyedSubtag);

            // assert
            testResult.Should().BeFalse("{0} must find {1} and {2} different", operationName, keyedSubtag, otherKeyedSubtag);
        }

        [Theory]
        [MemberData(nameof(UninitializedInstances), DisableDiscoveryEnumeration = true)]
        public void Equals_OnUninitializedInstances_ReturnsTrue(string operationName, Func<Bcp47KeyedSubtag, Bcp47KeyedSubtag, bool> operation, Bcp47KeyedSubtag keyedSubtag, Bcp47KeyedSubtag otherKeyedSubtag)
        {
            // act
            var testResult = operation(keyedSubtag, otherKeyedSubtag);

            // assert
            testResult.Should().BeTrue("{0} must find two uninitialized instances equal", operationName);
        }

        [Fact]
        public void GetHashCode_OnUninitializedInstances_ReturnsEqualHashCodes()
        {
            // arrange
            var keyedSubtag = new Bcp47KeyedSubtag();
            var otherHashCode = new Bcp47KeyedSubtag().GetHashCode();

            // act
            var testResult = keyedSubtag.GetHashCode();

            // assert
            testResult.Should().Be(otherHashCode);
        }

        [Fact]
        public void ExtensionSubtags_OnTagsDifferingOnlyInCase_AreEqual()
        {
            // arrange
            var extensionSubtag = Bcp47LanguageTag.Create("en-u-co-phonebk").ExtensionSubtags[0];
            var otherExtensionSubtag = Bcp47LanguageTag.Create("en-U-CO-PHONEBK").ExtensionSubtags[0];

            // act
            var testResult = extensionSubtag == otherExtensionSubtag;

            // assert
            testResult.Should().BeTrue("extension subtags compare the way their tags do");
        }

        [Fact]
        public void ExtensionSubtags_OnTagInUpperCase_KeepsTheCaseOfTheSubtag()
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create("EN-U-CO-PHONEBK");

            // act
            string testResult = languageTag.ExtensionSubtags[0];

            // assert
            testResult.Should().Be(PhonebookCollationExtensionUpperCase, "the subtag keeps the case it was written in");
        }
    }
}
