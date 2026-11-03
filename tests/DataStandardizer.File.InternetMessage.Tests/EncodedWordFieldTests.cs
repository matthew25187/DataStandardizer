using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of RFC 2047 encoded words in the header fields that the readers and writers handle.
    /// </summary>
    public class EncodedWordFieldTests
    {
        private const string EncodedA = "=?UTF-8?Q?a?=";
        private const string EAcute = "\x00E9";
        private const string UnpairedSurrogate = "\xD83D";

        // ref. RFC 5322 §2.1.1: lines SHOULD be no more than 78 characters.
        private const int TargetLineLength = 78;

        // Each is text long enough to fold, as encoded words alone, as encoded words among plain words, and with white space to keep.
        public static TheoryData<string> LongTexts => new TheoryData<string>
        {
            new string('\x00E9', 200),
            new string('a', 40) + " " + new string('\x00E9', 100) + " " + new string('b', 40),
            "caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait, caf\x00E9 au lait",
            "\x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9   \x00E9 \x00E9  \x00E9",
            "\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00",
        };

        public static TheoryData<string> RefusedTexts => new TheoryData<string>
        {
            "a\r\nBcc: b@c",
            "a\nb",
            "a\x0000",
        };

        [Fact]
        public void WriteTextField_NonAscii_WritesEncodedWords()
        {
            // act
            var testResult = EncodedWordFields.WriteText("Subject", "Re: caf" + EAcute, new ImfOptions());

            // assert
            testResult.Should().Be("Subject: Re: =?UTF-8?B?Y2Fmw6k=?=\r\n");
        }

        [Fact]
        public void WriteTextField_NonAsciiWithUtf8Encoding_WritesTheUtf8()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = EncodedWordFields.WriteText("Subject", "caf" + EAcute, options);

            // assert
            testResult.Should().Be("Subject: caf\x00C3\x00A9\r\n");
        }

        [Fact]
        public void WriteTextField_Ascii_WritesItAsItIs()
        {
            // act
            var testResult = EncodedWordFields.WriteText("Subject", "a b", new ImfOptions());

            // assert
            testResult.Should().Be("Subject: a b\r\n");
        }

        [Fact]
        public void WriteTextField_TextOfTheFormOfAnEncodedWord_EncodesIt()
        {
            // act
            var testResult = EncodedWordFields.WriteText("Subject", EncodedA, new ImfOptions());

            // assert
            testResult.Should().Be("Subject: =?UTF-8?B?PT9VVEYtOD9RP2E/PQ==?=\r\n");
        }

        [Fact]
        public async Task WriteTextFieldAsync_NonAscii_WritesEncodedWords()
        {
            // act
            var testResult = await EncodedWordFields.WriteTextAsync("Subject", "Re: caf" + EAcute, new ImfOptions());

            // assert
            testResult.Should().Be("Subject: Re: =?UTF-8?B?Y2Fmw6k=?=\r\n");
        }

        [Theory]
        [MemberData(nameof(LongTexts))]
        public void WriteTextField_LongText_KeepsLinesWithinTheLimit(string text)
        {
            // act
            var testResult = EncodedWordFields.WriteText("Subject", text, new ImfOptions());

            // assert
            EncodedWordFields.LongestLineLength(testResult).Should().BeLessThanOrEqualTo(TargetLineLength);
        }

        [Theory]
        [MemberData(nameof(LongTexts))]
        public void WriteTextField_LongText_ReadsBackAsTheSameText(string text)
        {
            // arrange
            var written = EncodedWordFields.WriteText("Subject", text, new ImfOptions());

            // act
            var testResult = EncodedWordFields.ReadDecodedValue(written + "\r\n", new ImfOptions());

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(LongTexts))]
        public async Task WriteTextFieldAsync_LongText_ReadsBackAsTheSameText(string text)
        {
            // arrange
            var written = await EncodedWordFields.WriteTextAsync("Subject", text, new ImfOptions());

            // act
            var testResult = EncodedWordFields.ReadDecodedValue(written + "\r\n", new ImfOptions());

            // assert
            EncodedWordFields.LongestLineLength(written).Should().BeLessThanOrEqualTo(TargetLineLength);
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(RefusedTexts))]
        public void WriteTextField_TextThatCannotBeWritten_ThrowsImfFileException(string text)
        {
            // act
            Action testAction = () => EncodedWordFields.WriteText("Subject", text, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Theory]
        [MemberData(nameof(RefusedTexts))]
        public async Task WriteTextFieldAsync_TextThatCannotBeWritten_FailsTheTask(string text)
        {
            // act
            Func<Task> testAction = () => EncodedWordFields.WriteTextAsync("Subject", text, new ImfOptions());

            // assert
            await testAction.Should().ThrowAsync<ImfFileException>();
        }

        [Fact]
        public void WriteTextField_UnpairedSurrogate_ThrowsImfFileException()
        {
            // act
            Action testAction = () => EncodedWordFields.WriteText("Subject", UnpairedSurrogate, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public async Task WriteTextFieldAsync_UnpairedSurrogate_FailsTheTask()
        {
            // act
            Func<Task> testAction = () => EncodedWordFields.WriteTextAsync("Subject", UnpairedSurrogate, new ImfOptions());

            // assert
            await testAction.Should().ThrowAsync<ImfFileException>();
        }

        [Fact]
        public void WriteTextField_NullName_ThrowsArgumentNullException()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteTextField(null, "a");

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
            }
        }

        [Fact]
        public void WriteTextField_NullText_ThrowsArgumentNullException()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteTextField("A", null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("text");
            }
        }

        [Fact]
        public void WriteTextFieldAsync_NullName_ThrowsArgumentNullException()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteTextFieldAsync(null, "a");

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
            }
        }

        [Fact]
        public void WriteTextFieldAsync_NullText_ThrowsArgumentNullException()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteTextFieldAsync("A", null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("text");
            }
        }

        [Fact]
        public void WriteTextField_Disposed_ThrowsObjectDisposedException()
        {
            // arrange
            var writer = new ImfHeaderWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.WriteTextField("A", "a");

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WriteTextFieldAsync_Disposed_ThrowsObjectDisposedException()
        {
            // arrange
            var writer = new ImfHeaderWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.WriteTextFieldAsync("A", "a");

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WriteTextField_AfterTheSectionEnded_ThrowsInvalidOperationException()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                writer.WriteEndOfHeader();

                // act
                Action testAction = () => writer.WriteTextField("A", "a");

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteTextField_OneField_CountsIt()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                writer.WriteTextField("A", EAcute);

                // assert
                writer.FieldsWritten.Should().Be(1);
            }
        }

        [Fact]
        public void AddTextField_NonAscii_WritesEncodedWords()
        {
            // arrange
            var message = EntityWithTextField("caf" + EAcute);

            // act
            var testResult = MessageWrites.Write(message);

            // assert
            testResult.Should().StartWith("Subject: =?UTF-8?B?Y2Fmw6k=?=\r\n");
        }

        [Fact]
        public void AddTextField_NonAsciiWithUtf8Encoding_WritesTheUtf8()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = MessageWrites.Write(EntityWithTextField("caf" + EAcute), options);

            // assert
            testResult.Should().StartWith("Subject: caf\x00C3\x00A9\r\n");
        }

        [Fact]
        public void AddTextField_LineBreakInText_IsRefusedWhenWritten()
        {
            // arrange
            var message = EntityWithTextField("a\r\nBcc: b@c");

            // act
            Action testAction = () => MessageWrites.Write(message);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void AddTextField_UnpairedSurrogate_ThrowsImfFileException()
        {
            // act
            Action testAction = () => EntityWithTextField(UnpairedSurrogate);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void AddTextField_NullName_ThrowsArgumentNullException()
        {
            // arrange
            var source = EntityWithTextField("a");

            // act
            Action testAction = () => source.AddTextField(null, "a");

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
        }

        [Fact]
        public void AddTextField_NullText_ThrowsArgumentNullException()
        {
            // arrange
            var source = EntityWithTextField("a");

            // act
            Action testAction = () => source.AddTextField("A", null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("text");
        }

        [Fact]
        public void AddTextField_RawEntity_ThrowsInvalidOperationException()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new byte[0]);

            // act
            Action testAction = () => source.AddTextField("A", "a");

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetDecodedValue_Rfc2047Subject_GivesTheText()
        {
            // arrange
            var section = "Subject: " + Rfc2047Examples.ExampleSubject + "\r\n\r\n";

            // act
            var testResult = EncodedWordFields.ReadDecodedValue(section, Rfc2047Examples.Options);

            // assert
            testResult.Should().Be(Rfc2047Examples.ExampleSubjectText);
        }

        [Fact]
        public void GetDecodedValue_MalformedWordByDefault_StaysAsItIs()
        {
            // act
            var testResult = EncodedWordFields.ReadDecodedValue("A: =?x-unknown?Q?a?=\r\n\r\n", new ImfOptions());

            // assert
            testResult.Should().Be("=?x-unknown?Q?a?=");
        }

        [Fact]
        public void GetDecodedValue_MalformedWordWithOptIn_DecodesIt()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedEncodedWords = true };

            // act
            var testResult = EncodedWordFields.ReadDecodedValue("A: =?x-unknown?Q?a?=\r\n\r\n", options);

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void GetDecodedValue_BytesThatAreNotUtf8_UsesTheFallbackBeforeTheWords()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitHeaders = true };
            var section = "A: caf\x00E9 " + EncodedA + "\r\n\r\n";

            // act
            var testResult = EncodedWordFields.ReadDecodedValue(section, options);

            // assert
            testResult.Should().Be("caf" + EAcute + " a");
        }

        [Fact]
        public void GetDecodedValue_NoCharsetResolver_ThrowsInvalidOperationException()
        {
            // arrange
            var options = new ImfOptions { CharsetResolver = null };

            // act
            Action testAction = () => EncodedWordFields.ReadDecodedValue("A: a\r\n\r\n", options);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetDecodedValue_ViewNotFromAReader_ThrowsInvalidOperationException()
        {
            // arrange
            var view = default(ImfHeaderFieldView);

            // act
            Action testAction = () => view.GetDecodedValue();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetDecodedValue_ReaderDisposed_ThrowsObjectDisposedException()
        {
            // arrange
            var reader = HeaderSections.CreateReader("A: a\r\n\r\n");
            var view = reader.ReadField().Value;
            reader.Dispose();

            // act
            Action testAction = () => view.GetDecodedValue();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        private static ImfEntitySource EntityWithTextField(string text)
        {
            return ImfEntitySource.FromContent(Communication.Mime.MimeMediaType.Parse("text/plain"), new byte[0])
                .AddTextField("Subject", text);
        }
    }
}
