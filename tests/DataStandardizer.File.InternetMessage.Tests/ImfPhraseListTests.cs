using System;
using System.Collections;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfPhraseListTests
    {
        private const string EncodedWord = "=?utf-8?Q?a?=";

        public static TheoryData<string, string[]> Parts => new TheoryData<string, string[]>
        {
            { "a", new[] { "a" } },
            { "a, b", new[] { "a", "b" } },
            { "a b, \"c d\", \"e,f\"", new[] { "a b", "c d", "e,f" } },
            { "a(b),\r\n (c)d", new[] { "a", "d" } },
            { "\"\"", new[] { "" } },

            // ref. RFC 6532 §3.2
            { "jos\x00E9", new[] { "jos\x00E9" } },

            // ref. RFC 5322 §4.1: obs-phrase-list
            { "", new string[0] },
            { " (a) ", new string[0] },
            { ",", new string[0] },
            { ",a", new[] { "a" } },
            { "a,", new[] { "a" } },
            { "a, (b) ,, c", new[] { "a", "c" } },

            // ref. RFC 5322 §4.1: obs-phrase
            { "a. b, c", new[] { "a. b", "c" } },

            // ref. RFC 2047 §5(3): an encoded word is kept as written.
            { EncodedWord, new[] { EncodedWord } },
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            { "", "" },
            { "a", "a" },
            { "\"a\",\"b c\",\"d,e\"", "a, b c, \"d,e\"" },
            { "a. b,, c", "\"a. b\", c" },
            { EncodedWord + ", \"" + EncodedWord + "\"", EncodedWord + ", \"" + EncodedWord + "\"" },
            { EncodedWord + " \"" + EncodedWord + "\"", EncodedWord + " \"" + EncodedWord + "\"" },
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            ".",
            ".a",
            "a; b",
            "a@b",
            "a <b>",
            "a:",
            "(a",
            "\"a",
            "a\r\n",
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { "a, b", "\"a\",(c)b", true },
            { "a", "A", false },
            { "a, b", "b, a", false },
            { "a", "a, b", false },
            { EncodedWord, "\"" + EncodedWord + "\"", false },
            { EncodedWord + " \"=?utf-8?Q?b?=\"", "\"" + EncodedWord + "\" =?utf-8?Q?b?=", false },
        };

        [Theory]
        [MemberData(nameof(Parts))]
        public void Parse_String_GivesItsPhrases(string value, string[] expected)
        {
            // act
            var testResult = ImfPhraseList.Parse(value);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(Parts))]
        public void TryParse_String_GivesItsPhrases(string value, string[] expected)
        {
            // act
            var testResult = ImfPhraseList.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(expected);
        }

        [Fact]
        public void Parse_Bytes_GivesItsPhrases()
        {
            // arrange
            var value = Octets.Of("a, jos\x00C3\x00A9");

            // act
            var testResult = ImfPhraseList.Parse(value);

            // assert
            testResult.Should().Equal("a", "jos\x00E9");
        }

        [Fact]
        public void TryParse_Bytes_GivesItsPhrases()
        {
            // arrange
            var value = Octets.Of("a");

            // act
            var testResult = ImfPhraseList.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal("a");
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfPhraseList.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfPhraseList.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("\x00FF");

            // act
            Action testAction = () => ImfPhraseList.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfPhraseList.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfPhraseList.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfPhraseList.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_Constructed_GivesTheModernForm()
        {
            // arrange
            var subject = new ImfPhraseList(new[] { "a b", "c,d", "=?a?=" });

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be("a b, \"c,d\", \"=?a?=\"");
        }

        [Fact]
        public void Count_TwoPhrases_IsTwo()
        {
            // arrange
            var subject = ImfPhraseList.Parse("a, b");

            // act
            var testResult = subject.Count;

            // assert
            testResult.Should().Be(2);
        }

        [Fact]
        public void Indexer_Position_GivesThePhrase()
        {
            // arrange
            var subject = ImfPhraseList.Parse("a, b");

            // act
            var testResult = subject[1];

            // assert
            testResult.Should().Be("b");
        }

        [Fact]
        public void GetEnumerator_NonGeneric_GivesThePhrases()
        {
            // arrange
            IEnumerable subject = ImfPhraseList.Parse("a");

            // act
            var enumerator = subject.GetEnumerator();
            var testResult = enumerator.MoveNext();
            var current = enumerator.Current;

            // assert
            testResult.Should().BeTrue();
            current.Should().Be("a");
        }

        [Fact]
        public void Constructor_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfPhraseList(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("phrases");
        }

        [Fact]
        public void Constructor_NoPhrases_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new ImfPhraseList(new string[0]);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("phrases");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("a\r\nBcc: b@c")]
        public void Constructor_InvalidPhrase_ThrowsArgumentException(string phrase)
        {
            // act
            Action testAction = () => new ImfPhraseList(new[] { phrase });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("phrases");
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_List_ComparesItsPhrasesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfPhraseList.Parse(left);
            var other = ImfPhraseList.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void EqualityOperator_List_ComparesItsPhrasesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfPhraseList.Parse(left);
            var other = ImfPhraseList.Parse(right);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void InequalityOperator_List_ComparesItsPhrasesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfPhraseList.Parse(left);
            var other = ImfPhraseList.Parse(right);

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().Be(!expected);
        }

        [Fact]
        public void Equals_ObjectOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfPhraseList.Parse("a");
            object other = ImfPhraseList.Parse("\"a\"");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfPhraseList.Parse("a");

            // act
            var testResult = subject.Equals((ImfPhraseList)null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_EqualLists_AreTheSame()
        {
            // arrange
            var subject = ImfPhraseList.Parse("a, b");
            var other = ImfPhraseList.Parse("\"a\",b");

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(other.GetHashCode());
        }
    }
}
