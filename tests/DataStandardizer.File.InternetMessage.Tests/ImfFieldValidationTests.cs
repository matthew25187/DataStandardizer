using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// What the tests of the routes through the grammar need.
    /// </summary>
    internal static class ImfFieldValidation
    {
        /// <summary>
        /// Builds a field as a reader does, with a body that hasn't been checked.
        /// </summary>
        public static ImfHeaderField BuildField(string name, string value)
        {
            return new ImfHeaderField(name, Octets.Of(name + ":" + value + "\r\n"), Octets.Of(value), string.Empty, 0, 1);
        }

        /// <summary>
        /// Counts the mailboxes of parsed addresses, including those in groups.
        /// </summary>
        public static int CountMailboxes(ImfAddressList addresses)
        {
            var count = 0;
            foreach (var address in addresses)
            {
                count += address is ImfGroup group ? group.Mailboxes.Count : 1;
            }

            return count;
        }
    }

    /// <summary>
    /// Tests of the routes through the grammar of RFC 5322 that keep nothing, for the conformance check of a header.  The routes that build a value take the same rows, so that the two can't part.
    /// </summary>
    public class ImfFieldValidationTests
    {
        private const string EncodedWord = "=?utf-8?Q?a?=";

        // ref. RFC 2047 §2: the form of an encoded word, in a character set that isn't resolved here.
        private const string UnknownCharsetWord = "=?x-unknown?Q?a?=";

        public static TheoryData<string, int> ValidAddressLists => new TheoryData<string, int>
        {
            { "a@b.test", 1 },
            { "a@b.test, c@d.test", 2 },
            { "Mary Smith <a@b.test>", 1 },
            { "\"Mary Smith\" <a@b.test>, c@d.test", 2 },
            { "a@[1.2.3.4]", 1 },

            // ref. RFC 6854 §2.1: the mailboxes of a group count, and an empty group has none.
            { "Friends: a@b.test, c@d.test;", 2 },
            { "Friends:;", 0 },
            { "Friends:;, e@f.test", 1 },
            { "Friends: a@b.test;, Others: c@d.test, e@f.test;", 3 },

            // ref. RFC 5322 §4.4: the obsolete forms, with empty elements, source routes, and periods in a phrase.
            { ",a@b.test,,c@d.test,", 2 },
            { "<@x.test,@y.test:a@b.test>", 1 },
            { "Joe Q. Public <a@b.test>", 1 },
            { "a . b @ c . d", 1 },
            { "Friends: ,a@b.test,;", 1 },

            // ref. RFC 2047 §5(3): an encoded word in a phrase.
            { EncodedWord + " <a@b.test>", 1 },
            { "x " + EncodedWord + " y <a@b.test>", 1 },

            // A word in the form of an encoded word that is not valid is kept as it is.
            { "=?bad?= <a@b.test>", 1 },

            // ref. RFC 6532 §3.2: UTF-8.
            { "jos\x00C3\x00A9@b.test", 1 },
        };

        public static TheoryData<string> InvalidAddressLists => new TheoryData<string>
        {
            string.Empty,
            ",",
            "a",
            "a@",
            "@b.test",
            "a@b.test c@d.test",
            "<a@b.test",
            "Friends: a@b.test",
            "Friends: Others: a@b.test;;",
            "a@\"b\"",
            "\"a",
            "(a",
            "a\x00C3@b.test",
        };

        public static TheoryData<string, int> ValidAddresses => new TheoryData<string, int>
        {
            { "a@b.test", 1 },
            { "Mary <a@b.test>", 1 },
            { "Friends: a@b.test;", 1 },
            { "Friends:;", 0 },
            { "Friends: a@b.test, c@d.test;", 2 },
        };

        public static TheoryData<string> InvalidAddresses => new TheoryData<string>
        {
            "a@b.test, c@d.test",
            string.Empty,
        };

        public static TheoryData<string, bool> PhraseLists => new TheoryData<string, bool>
        {
            { "one", true },
            { "one, two three", true },
            { ",one,,", true },
            { string.Empty, true },
            { "Joe Q. Public", true },
            { EncodedWord + ", two", true },
            { "\"one\", two", true },
            { ".", false },
            { "one; two", false },
            { "\"one", false },
        };

        public static TheoryData<string, bool> MessageIds => new TheoryData<string, bool>
        {
            { "<a@b.test>", true },
            { " <a.b@[c]> (comment)", true },
            { "<\"a\"@b.test>", true },
            { "a@b.test", false },
            { "<a>", false },
            { "<a@b.test> <c@d.test>", false },
            { string.Empty, false },
            { "<a\x00C3@b.test>", false },
        };

        public static TheoryData<string, bool> MessageIdLists => new TheoryData<string, bool>
        {
            { "<a@b.test>", true },
            { "<a@b.test> <c@d.test>", true },
            { string.Empty, true },

            // ref. RFC 5322 §4.5.4: phrases, which have no meaning, among the identifiers.
            { "one <a@b.test> two.", true },
            { EncodedWord + " <a@b.test>", true },
            { "<a@b.test", false },
            { ",", false },
            { "\x00C3", false },
        };

        public static TheoryData<string, bool> Paths => new TheoryData<string, bool>
        {
            { "<>", true },
            { " < > ", true },
            { "<a@b.test>", true },
            { "<@x.test:a@b.test>", true },
            { "a@b.test", false },
            { "<a@b.test> c", false },
            { "<> c", false },
            { string.Empty, false },
        };

        public static TheoryData<string, bool, bool, string> ReceivedFields => new TheoryData<string, bool, bool, string>
        {
            { "from a.test by b.test; Fri, 21 Nov 1997 09:55:06 -0600", true, true, " Fri, 21 Nov 1997 09:55:06 -0600" },
            { "from a.test (c) by <d@e.test> with [1.2.3.4] for f@g.test ;x", true, true, "x" },
            { "\"quoted\" word;", true, true, string.Empty },
            { "a.b.c", true, false, string.Empty },
            { string.Empty, true, false, string.Empty },
            { "<>", false, false, string.Empty },
            { "a@", false, false, string.Empty },
            { "\"a\".b", false, false, string.Empty },
            { "@", false, false, string.Empty },
        };

        public static TheoryData<string, string, bool> Fields => new TheoryData<string, string, bool>
        {
            { "From", "a@b.test", true },
            { "FROM", "a@b.test", true },
            { "from", "a", false },
            { "Reply-To", "a", false },
            { "To", "a", false },
            { "Cc", "a", false },
            { "Resent-From", "a", false },
            { "Resent-Reply-To", "a", false },
            { "Resent-To", "a", false },
            { "Resent-Cc", "a", false },

            // ref. RFC 6854 §2.1: a group in a sender field has at most one mailbox.
            { "Sender", "a@b.test", true },
            { "Sender", "Friends:;", true },
            { "Sender", "Friends: a@b.test;", true },
            { "Sender", "Friends: a@b.test, c@d.test;", false },
            { "Sender", "a@b.test, c@d.test", false },
            { "Resent-Sender", "Friends: a@b.test, c@d.test;", false },

            // ref. RFC 5322 §3.6.3 and §4.5.3: Bcc may be empty.
            { "Bcc", string.Empty, true },
            { "Bcc", " , , ", true },
            { "Bcc", "a@b.test", true },
            { "Bcc", "a", false },
            { "Resent-Bcc", string.Empty, true },

            { "Date", "Fri, 21 Nov 1997 09:55:06 -0600", true },
            { "Date", "soon", false },
            { "Resent-Date", "soon", false },
            { "Message-ID", "<a@b.test>", true },
            { "Message-ID", "a", false },
            { "Resent-Message-ID", "a", false },
            { "In-Reply-To", "<a", false },
            { "References", "<a", false },
            { "Keywords", "\"a", false },
            { "Return-Path", "a", false },
            { "Received", "a; Fri, 21 Nov 1997 09:55:06 -0600", true },
            { "Received", "a; soon", false },
            { "Received", "a@", false },

            // Any other name has no grammar here.
            { "Subject", "\"", true },
            { "X-Other", "\"", true },
        };

        [Theory]
        [MemberData(nameof(ValidAddressLists))]
        public void IsValidAddressList_ValidValue_IsAcceptedAndItsMailboxesCounted(string value, int expectedMailboxes)
        {
            // act
            var isValid = ImfAddressParser.IsValidAddressList(Octets.Of(value), new ImfOptions(), out var mailboxes);

            // assert
            isValid.Should().BeTrue();
            mailboxes.Should().Be(expectedMailboxes);
        }

        [Theory]
        [MemberData(nameof(ValidAddressLists))]
        public void TryParseAddressList_ValidValue_IsAccepted(string value, int expectedMailboxes)
        {
            // act
            var isParsed = ImfAddressList.TryParse(Octets.Of(value), new ImfOptions(), out var addresses);

            // assert
            isParsed.Should().BeTrue();
            ImfFieldValidation.CountMailboxes(addresses).Should().Be(expectedMailboxes);
        }

        [Theory]
        [MemberData(nameof(InvalidAddressLists))]
        public void IsValidAddressList_InvalidValue_IsRefused(string value)
        {
            // act
            var isValid = ImfAddressParser.IsValidAddressList(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isValid.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InvalidAddressLists))]
        public void TryParseAddressList_InvalidValue_IsRefused(string value)
        {
            // act
            var isParsed = ImfAddressList.TryParse(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isParsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ValidAddresses))]
        public void IsValidAddress_ValidValue_IsAcceptedAndItsMailboxesCounted(string value, int expectedMailboxes)
        {
            // act
            var isValid = ImfAddressParser.IsValidAddress(Octets.Of(value), new ImfOptions(), out var mailboxes);

            // assert
            isValid.Should().BeTrue();
            mailboxes.Should().Be(expectedMailboxes);
        }

        [Theory]
        [MemberData(nameof(InvalidAddresses))]
        public void IsValidAddress_InvalidValue_IsRefused(string value)
        {
            // act
            var isValid = ImfAddressParser.IsValidAddress(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isValid.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InvalidAddresses))]
        public void TryParseAddress_InvalidValue_IsRefused(string value)
        {
            // act
            var isParsed = ImfAddress.TryParse(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isParsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(PhraseLists))]
        public void IsValidPhraseList_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isValid = ImfAddressParser.IsValidPhraseList(Octets.Of(value), new ImfOptions());

            // assert
            isValid.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(PhraseLists))]
        public void TryParsePhraseList_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isParsed = ImfPhraseList.TryParse(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isParsed.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MessageIds))]
        public void IsValidMessageId_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isValid = ImfAddressParser.IsValidMessageId(Octets.Of(value));

            // assert
            isValid.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MessageIds))]
        public void TryParseMessageId_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isParsed = ImfMessageId.TryParse(Octets.Of(value), out _);

            // assert
            isParsed.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MessageIdLists))]
        public void IsValidMessageIdList_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isValid = ImfAddressParser.IsValidMessageIdList(Octets.Of(value));

            // assert
            isValid.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MessageIdLists))]
        public void TryParseMessageIdList_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isParsed = ImfMessageId.TryParseList(Octets.Of(value), out _);

            // assert
            isParsed.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Paths))]
        public void IsValidPath_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isValid = ImfAddressParser.IsValidPath(Octets.Of(value), new ImfOptions());

            // assert
            isValid.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Paths))]
        public void TryParsePath_Value_FollowsTheGrammar(string value, bool expected)
        {
            // act
            var isParsed = ImfAddressParser.TryParsePath(Octets.Of(value), new ImfOptions(), out _);

            // assert
            isParsed.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ReceivedFields))]
        public void IsValidReceived_Value_FollowsTheGrammarAndFindsTheDate(string value, bool expected, bool expectedHasDate, string expectedDate)
        {
            // act
            var isValid = ImfAddressParser.IsValidReceived(Octets.Of(value), new ImfOptions(), out var hasDate, out var date);

            // assert
            isValid.Should().Be(expected);
            hasDate.Should().Be(expectedHasDate);
            Octets.ToText(date).Should().Be(expectedDate);
        }

        [Theory]
        [MemberData(nameof(ReceivedFields))]
        public void TryParseReceived_Value_FollowsTheGrammarAndFindsTheDate(string value, bool expected, bool expectedHasDate, string expectedDate)
        {
            // act
            var isParsed = ImfAddressParser.TryParseReceived(Octets.Of(value), new ImfOptions(), out _, out var hasDate, out var date);

            // assert
            isParsed.Should().Be(expected);
            hasDate.Should().Be(expectedHasDate);
            Octets.ToText(date).Should().Be(expectedDate);
        }

        [Theory]
        [MemberData(nameof(Fields))]
        public void IsValid_FieldOfEachGrammar_FollowsTheGrammarOfItsName(string name, string value, bool expected)
        {
            // arrange
            var field = ImfFieldValidation.BuildField(name, value);

            // act
            var isValid = ImfFieldValues.IsValid(field, new ImfOptions());

            // assert
            isValid.Should().Be(expected);
        }

        [Fact]
        public void IsValidAddressList_EightBitValueWithoutTheOption_IsRefused()
        {
            // act
            var isValid = ImfAddressParser.IsValidAddressList(Octets.Of("\x00E9 <a@b.test>"), new ImfOptions(), out _);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void IsValidAddressList_EightBitValueWithTheOption_IsAccepted()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var isValid = ImfAddressParser.IsValidAddressList(Octets.Of("\x00E9 <a@b.test>"), options, out var mailboxes);

            // assert
            isValid.Should().BeTrue();
            mailboxes.Should().Be(1);
        }

        [Fact]
        public void IsValidAddressList_EncodedWordInADisplayName_AsksTheResolver()
        {
            // arrange
            var resolver = new CountingCharsetResolver();
            var options = new ImfOptions { CharsetResolver = resolver };

            // act
            ImfAddressParser.IsValidAddressList(Octets.Of(UnknownCharsetWord + " <a@b.test>"), options, out _);

            // assert
            resolver.Count.Should().Be(1);
        }

        [Fact]
        public void IsValidAddressList_NoEncodedWord_DoesNotAskTheResolver()
        {
            // arrange
            var resolver = new CountingCharsetResolver();
            var options = new ImfOptions { CharsetResolver = resolver };

            // act
            ImfAddressParser.IsValidAddressList(Octets.Of("Mary Q. Smith <a@b.test>, Friends: c@d.test;"), options, out _);

            // assert
            resolver.Count.Should().Be(0);
        }

        [Fact]
        public void IsValid_ResolverThatThrowsOnAnEncodedWord_Throws()
        {
            // arrange
            var field = ImfFieldValidation.BuildField("From", UnknownCharsetWord + " <a@b.test>");
            var options = new ImfOptions { CharsetResolver = new ThrowingCharsetResolver() };

            // act
            Action testAction = () => ImfFieldValues.IsValid(field, options);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(ThrowingCharsetResolver.Message);
        }

        [Fact]
        public void IsValid_OptionsWithNoResolver_Throws()
        {
            // arrange
            var field = ImfFieldValidation.BuildField("From", "a@b.test");
            var options = new ImfOptions { CharsetResolver = null };

            // act
            Action testAction = () => ImfFieldValues.IsValid(field, options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("a@b.test", 1)]
        [InlineData("a@b.test, Friends: c@d.test, e@f.test;", 3)]
        [InlineData("a", 0)]
        public void CountMailboxes_Field_CountsThoseOfAValidBody(string value, int expected)
        {
            // arrange
            var field = ImfFieldValidation.BuildField("From", value);

            // act
            var count = ImfFieldValues.CountMailboxes(field, new ImfOptions());

            // assert
            count.Should().Be(expected);
        }

        [Fact]
        public void CountMailboxes_NoField_IsZero()
        {
            // act
            var count = ImfFieldValues.CountMailboxes(null, new ImfOptions());

            // assert
            count.Should().Be(0);
        }

        [Fact]
        public void BuildField_NameAndValue_HoldsThem()
        {
            // act
            var field = ImfFieldValidation.BuildField("From", "a");

            // assert
            field.Name.Should().Be("From");
            Octets.ToText(field.ValueBytes.Span).Should().Be("a");
        }

        [Fact]
        public void CountMailboxes_AddressesAndGroups_CountsEachMailbox()
        {
            // arrange
            var addresses = ImfAddressList.Parse("a@b.test, Friends: c@d.test, e@f.test;");

            // act
            var count = ImfFieldValidation.CountMailboxes(addresses);

            // assert
            count.Should().Be(3);
        }

        private sealed class CountingCharsetResolver : IMimeCharsetResolver
        {
            public int Count { get; private set; }

#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetEncoding(MimeCharset charset, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Encoding encoding)
#else
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
            {
                Count++;
                encoding = null;
                return false;
            }
        }

        private sealed class ThrowingCharsetResolver : IMimeCharsetResolver
        {
            public const string Message = "Asked for a character set.";

#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetEncoding(MimeCharset charset, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Encoding encoding)
#else
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
            {
                throw new InvalidOperationException(Message);
            }
        }
    }
}
