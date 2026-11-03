using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfHeaderWriter"/>.
    /// </summary>
    public class ImfHeaderWriterTests
    {
        private const string FieldName = "Subject";
        private const string ShortValue = "Hi";
        private const string ShortField = "Subject: Hi\r\n";
        private const string RawFolded = "Subject :  Hi\r\n\tthere\r\n";
        private const string BareLineFeedField = "A: 1\n 2\n";

        // The bytes of two copies of ShortField.
        private const int TwoShortFieldsLength = 26;


        // ref. RFC 5322 §2.1.1: the 78 and 998 limits, without the line break.
        private const int MaxLineLength = 998;

        private static readonly string Word70 = new string('a', 70);
        private static readonly string Word75 = new string('a', 75);
        private static readonly string Word80 = new string('a', 80);

        // The value goes on a line of its own, after a space.
        private static readonly string ValueOfLongestLine = new string('a', MaxLineLength - 1);
        private static readonly string ValueOverLongestLine = new string('a', MaxLineLength);
        private static readonly string FieldOfLongestLine = "A:\r\n " + ValueOfLongestLine + "\r\n";

        // ref. RFC 6532 §3.4: "A: " and 75 characters are 78 characters, and the 74 of two octets each make it 151 octets.
        private static readonly string Utf8ValueOf78Characters = new string('\x00E9', 37) + " " + new string('\x00E9', 37);

        // ref. RFC 6532 §3.4: a space and 499 characters of two octets each are 999 octets.
        private static readonly string Utf8ValueOver998Octets = new string('\x00E9', 499);

        private static readonly string FoldedAtLineFeed = "A: " + Word70 + "\n " + Word70 + "\n";
        private static readonly string TwoWords70 = Word70 + " " + Word70;

        public static TheoryData<string, string, ImfHeaderFieldKind, string> Formatted => new TheoryData<string, string, ImfHeaderFieldKind, string>
        {
            // A space is added only where there is none, and to nothing.
            { "A", "1", ImfHeaderFieldKind.Unstructured, "A: 1\r\n" },
            { "A", " 1", ImfHeaderFieldKind.Unstructured, "A: 1\r\n" },
            { "A", "\t1", ImfHeaderFieldKind.Unstructured, "A:\t1\r\n" },
            { "A", "  1", ImfHeaderFieldKind.Unstructured, "A:  1\r\n" },
            { "A", "", ImfHeaderFieldKind.Unstructured, "A:\r\n" },

            // 78 characters fit: "A: " is 3, and the two words 75.
            { "A", new string('a', 40) + " " + new string('b', 34), ImfHeaderFieldKind.Unstructured, "A: " + new string('a', 40) + " " + new string('b', 34) + "\r\n" },

            // 79 don't, so the last space that keeps the line within 78 is used.
            { "A", new string('a', 40) + " " + new string('b', 35), ImfHeaderFieldKind.Unstructured, "A: " + new string('a', 40) + "\r\n " + new string('b', 35) + "\r\n" },

            // The latest space that fits is used, not the first.
            { "A", "a b " + Word70 + " c", ImfHeaderFieldKind.Unstructured, "A: a b " + Word70 + "\r\n c\r\n" },
            { "A", "a b " + Word80, ImfHeaderFieldKind.Unstructured, "A: a b\r\n " + Word80 + "\r\n" },

            // A tab is a place to fold too, and it is kept.
            { "A", Word70 + "\t" + Word70, ImfHeaderFieldKind.Unstructured, "A: " + Word70 + "\r\n\t" + Word70 + "\r\n" },

            // A word too long for its line goes on a line of its own, even the first, and a line past 78 is allowed where it has nowhere to fold.
            { "A", Word80, ImfHeaderFieldKind.Unstructured, "A:\r\n " + Word80 + "\r\n" },
            { "A", Word80 + " b", ImfHeaderFieldKind.Unstructured, "A:\r\n " + Word80 + "\r\n b\r\n" },
            { "A", " " + Word80 + " " + Word80, ImfHeaderFieldKind.Unstructured, "A:\r\n " + Word80 + "\r\n " + Word80 + "\r\n" },

            // More than one fold.
            { "A", Word70 + " " + Word70 + " " + Word70, ImfHeaderFieldKind.Unstructured, "A: " + Word70 + "\r\n " + Word70 + "\r\n " + Word70 + "\r\n" },

            // ref. RFC 5322 §4.2: only the obsolete syntax has a line of only white space, so white space at the end stays on the last line.
            { "A", Word75 + "   ", ImfHeaderFieldKind.Unstructured, "A:\r\n " + Word75 + "   \r\n" },
            { "A", "   ", ImfHeaderFieldKind.Unstructured, "A:   \r\n" },

            // A run of white space may be split, as a line that starts with white space and holds a word is valid.
            { "A", Word70 + "  " + Word70, ImfHeaderFieldKind.Unstructured, "A: " + Word70 + " \r\n " + Word70 + "\r\n" },

            // ref. RFC 5322 §2.2.2: a structured field folds between tokens only.  An unstructured one folds at any white space.
            { "To", Word70 + " \"x y\"", ImfHeaderFieldKind.Structured, "To: " + Word70 + "\r\n \"x y\"\r\n" },
            { "To", "\"" + Word70 + " y z\" b", ImfHeaderFieldKind.Structured, "To:\r\n \"" + Word70 + " y z\"\r\n b\r\n" },
            { "To", "\"" + Word70 + " y z\" b", ImfHeaderFieldKind.Unstructured, "To: \"" + Word70 + " y\r\n z\" b\r\n" },
            { "To", "[" + Word70 + " y z] b", ImfHeaderFieldKind.Structured, "To:\r\n [" + Word70 + " y z]\r\n b\r\n" },

            // ref. RFC 5322 §3.2.2: a comment may hold FWS between its words, but a space in a quoted-pair is not FWS.
            { "To", "(" + Word70 + " y z) b", ImfHeaderFieldKind.Structured, "To: (" + Word70 + " y\r\n z) b\r\n" },
            { "To", "((" + Word70 + ") y z) b", ImfHeaderFieldKind.Structured, "To: ((" + Word70 + ")\r\n y z) b\r\n" },
            { "To", "(" + Word70 + "\\ y\\ z) b", ImfHeaderFieldKind.Structured, "To:\r\n (" + Word70 + "\\ y\\ z)\r\n b\r\n" },

            // A line that starts inside a comment is still inside it: a quote there starts nothing, and a parenthesis closes it, so what follows is outside.
            { "To", "(" + Word70 + " y \"" + Word70 + " y z x v) b", ImfHeaderFieldKind.Structured, "To: (" + Word70 + " y\r\n \"" + Word70 + " y z x\r\n v) b\r\n" },
            { "To", "(" + Word70 + " y z) \"" + Word70 + " y z\" b", ImfHeaderFieldKind.Structured, "To: (" + Word70 + " y\r\n z)\r\n \"" + Word70 + " y z\"\r\n b\r\n" },

            // An escaped quote or parenthesis doesn't end the token.
            { "To", "\"\\\"" + Word70 + " y z\" b", ImfHeaderFieldKind.Structured, "To:\r\n \"\\\"" + Word70 + " y z\"\r\n b\r\n" },
            { "To", "(\\)" + Word70 + " y z) b", ImfHeaderFieldKind.Structured, "To: (\\)" + Word70 + "\r\n y z) b\r\n" },

            // A quote inside a comment, and a parenthesis inside a quoted string, start nothing.
            { "To", "(\"" + Word70 + ") y z", ImfHeaderFieldKind.Structured, "To: (\"" + Word70 + ")\r\n y z\r\n" },
            { "To", "\"(" + Word70 + "\" y z", ImfHeaderFieldKind.Structured, "To: \"(" + Word70 + "\"\r\n y z\r\n" },
        };

        public static TheoryData<string, string> RefusedFields => new TheoryData<string, string>
        {
            // ref. RFC 5322 §3.6.8: field-name = 1*ftext; ftext = %d33-57 / %d59-126
            { "", "1" },
            { "A B", "1" },
            { "A:B", "1" },
            { "A\x7F", "1" },
            { "A\x00E9", "1" },

            // ref. RFC 5322 §3.2.5, and §4 for the obsolete control characters, which are never generated.
            { "A", "1\r\n 2" },
            { "A", "1\r2" },
            { "A", "1\n2" },
            { "A", "1\x00" + "2" },
            { "A", "1\x007F" },

            // Not US-ASCII, and a field body is written as it is given.  WriteTextField encodes text.
            { "A", "caf\x00E9" },

            // ref. RFC 5322 §2.1.1
            { "A", new string('a', MaxLineLength) },
            { "To", "a " + new string('a', MaxLineLength) },
            { new string('A', MaxLineLength), "1" },
        };

        // Each is a field the writer gives, and its name and body once a reader has unfolded it.
        public static TheoryData<string, string> Unfolded => new TheoryData<string, string>
        {
            { "A: 1\r\n", "A: 1" },
            { "A:\r\n", "A:" },
            { "A:  1\r\n", "A:  1" },
            { "A:\t1\r\n", "A:\t1" },
            { "A: a\r\n b\r\n", "A: a b" },
            { "A:\r\n a\r\n\tb\r\n", "A: a\tb" },
            { "A: a \r\n b\r\n", "A: a  b" },
            { "To: \"x y\"\r\n z\r\n", "To: \"x y\" z" },
        };

        public static TheoryData<string> RefusedRawFields => new TheoryData<string>
        {
            "",
            "\r\n",
            ": 1\r\n",
            "A\r\n",
            "A B: 1\r\n",
            "A: 1",
            "A: 1\r\nB: 2\r\n",
            "A: 1\r\n\r\n",
            "A: 1\r\n\r\n 2\r\n",
            "A: 1\r2\r\n",
            "A: 1\n",
            "A: 1\n 2\r\n",
        };

        public static TheoryData<string> FieldsToCopy => new TheoryData<string>
        {
            RawFolded,
            "A:\r\n",
            "A:  \t 1   \r\n",
            "A: 1\r\n \r\n 2\r\n",
            "A: caf\x00C3\x00A9\r\n",
        };

        [Theory]
        [MemberData(nameof(Formatted))]
        public void WriteField_NameAndValue_FoldsTheField(string name, string value, ImfHeaderFieldKind kind, string expected)
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                writer.WriteField(name, value, kind);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Formatted))]
        public void WriteField_Bytes_FoldsTheField(string name, string value, ImfHeaderFieldKind kind, string expected)
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                writer.WriteField(Octets.Of(name), Octets.Of(value), kind);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(expected);
        }

        [Fact]
        public void WriteField_LongestLineWithNothingToFold_IsWritten()
        {
            // act
            var testResult = MessageWrites.WriteField("A", ValueOfLongestLine, ImfHeaderFieldKind.Unstructured);

            // assert
            testResult.Should().Be(FieldOfLongestLine);
        }

        [Fact]
        public void WriteField_ValueOverTheLongestLine_ThrowsAndNamesTheField()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteField("A", ValueOverLongestLine, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.FieldName].Should().Be("A");
            }
        }

        [Theory]
        [MemberData(nameof(RefusedFields))]
        public void WriteField_NameOrValueNotAllowed_ThrowsAndWritesNothing(string name, string value)
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteField(name, value, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteField_AfterARefusal_CanContinue()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                MessageWrites.IgnoreFailure(() => writer.WriteField("A B", ShortValue, ImfHeaderFieldKind.Unstructured));

                // act
                writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(ShortField);
        }

        [Fact]
        public void WriteField_NonAsciiWithUtf8Encoding_WritesTheUtf8()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = MessageWrites.WriteField("A", "caf\x00E9", ImfHeaderFieldKind.Unstructured, options);

            // assert
            testResult.Should().Be("A: caf\x00C3\x00A9\r\n");
        }

        [Fact]
        public void WriteField_InvalidUtf8WithUtf8Encoding_Throws()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, options))
            {
                // act
                Action testAction = () => writer.WriteField(Octets.Of("A"), Octets.Of("caf\x00E9"), ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteField_LoneSurrogate_Throws()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, options))
            {
                // act
                Action testAction = () => writer.WriteField("A", "\xD800", ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteField_Utf8PastTheTargetInOctetsOnly_IsNotFolded()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = MessageWrites.WriteField("A", Utf8ValueOf78Characters, ImfHeaderFieldKind.Unstructured, options);

            // assert
            testResult.Should().NotContain("\r\n ");
        }

        [Fact]
        public void WriteField_Utf8PastTheLongestLineInOctets_Throws()
        {
            // arrange
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, options))
            {
                // act
                Action testAction = () => writer.WriteField("A", Utf8ValueOver998Octets, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(Unfolded))]
        public void ReadField_FieldThatWasWritten_GivesTheBodyBack(string section, string expected)
        {
            // act
            var testResult = MessageWrites.ReadUnfoldedFields(section);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void WriteField_LineFeedSetting_EndsLinesWithLineFeedAndCountsTheSameLimits()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, new ImfOptions(), true))
            {
                // act
                writer.WriteField("A", TwoWords70, ImfHeaderFieldKind.Unstructured);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(FoldedAtLineFeed);
        }

        [Theory]
        [MemberData(nameof(FieldsToCopy))]
        public void WriteField_View_CopiesTheBytes(string field)
        {
            // arrange
            var stream = new MemoryStream();
            using (var reader = HeaderSections.CreateReader(field))
            using (var writer = new ImfHeaderWriter(stream))
            {
                var view = reader.ReadField().Value;

                // act
                writer.WriteField(view);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(field);
        }

        [Fact]
        public void WriteEndOfHeader_LineFeedSetting_WritesALineFeed()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, new ImfOptions(), true))
            {
                // act
                writer.WriteEndOfHeader();
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be("\n");
        }

        [Fact]
        public void WriteField_ViewOfBareLineFeeds_CopiesThemAsTheReaderAcceptedThem()
        {
            // arrange
            var stream = new MemoryStream();
            using (var reader = HeaderSections.CreateReader(BareLineFeedField, new ImfOptions { AcceptBareLineFeed = true }))
            using (var writer = new ImfHeaderWriter(stream))
            {
                var view = reader.ReadField().Value;

                // act
                writer.WriteField(view);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(BareLineFeedField);
        }

        [Fact]
        public void WriteField_ViewFromNoReader_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteField(default(ImfHeaderFieldView));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [MemberData(nameof(FieldsToCopy))]
        public void WriteRawField_OneWholeField_CopiesTheBytes(string field)
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                writer.WriteRawField(Octets.Of(field));
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(field);
        }

        [Theory]
        [MemberData(nameof(RefusedRawFields))]
        public void WriteRawField_NotOneWholeField_ThrowsAndWritesNothing(string field)
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteRawField(Octets.Of(field));

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteRawField_BareLineFeedWithOption_CopiesTheBytes()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, new ImfOptions { AcceptBareLineFeed = true }))
            {
                // act
                writer.WriteRawField(Octets.Of(BareLineFeedField));
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(BareLineFeedField);
        }

        [Fact]
        public void WriteRawField_LineFeedSettingAndLineFeeds_CopiesTheBytes()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, new ImfOptions(), true))
            {
                // act
                writer.WriteRawField(Octets.Of(BareLineFeedField));
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(BareLineFeedField);
        }

        [Fact]
        public void WriteRawField_LineFeedSettingAndCarriageReturn_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream(), new ImfOptions(), true))
            {
                // act
                Action testAction = () => writer.WriteRawField(Octets.Of(ShortField));

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteFields_Reader_CopiesTheRestOfTheFieldsAndReportsEach()
        {
            // arrange
            const string section = "A: 1\r\nB:\r\n 2\r\n\r\nbody";
            var stream = new MemoryStream();
            var progress = new RecordingProgress();
            using (var reader = HeaderSections.CreateReader(section))
            using (var writer = new ImfHeaderWriter(stream))
            {
                // act
                writer.WriteFields(reader, progress);
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be("A: 1\r\nB:\r\n 2\r\n");
            progress.ReportCount.Should().Be(2);
            progress.Last.Should().Be(new ImfWriteProgress(2, 0, 14));
        }

        [Fact]
        public void WriteEndOfHeader_AfterFields_WritesAnEmptyLine()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream))
            {
                writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // act
                writer.WriteEndOfHeader();
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be("Subject: Hi\r\n\r\n");
        }

        [Fact]
        public void WriteField_AfterEndOfHeader_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                writer.WriteEndOfHeader();

                // act
                Action testAction = () => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteEndOfHeader_Twice_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                writer.WriteEndOfHeader();

                // act
                Action testAction = () => writer.WriteEndOfHeader();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void FieldsWritten_AfterTwoFields_CountsThem()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);
                writer.WriteRawField(Octets.Of(ShortField));

                // act
                var testResult = writer.FieldsWritten;

                // assert
                testResult.Should().Be(2);
            }
        }

        [Fact]
        public void BytePosition_AfterTwoFields_CountsTheirBytes()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);
                writer.WriteRawField(Octets.Of(ShortField));

                // act
                var testResult = writer.BytePosition;

                // assert
                testResult.Should().Be(TwoShortFieldsLength);
            }
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new ImfHeaderWriter((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageStream");
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfHeaderWriter(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_StreamThatCannotBeWritten_Throws()
        {
            // act
            Action testAction = () => new ImfHeaderWriter(new ReadOnlyMemoryStream());

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("messageStream");
        }

        [Fact]
        public void WriteField_NullName_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteField(null, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
            }
        }

        [Fact]
        public void WriteField_NullValue_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteField(FieldName, null, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("value");
            }
        }

        [Fact]
        public void WriteField_KindNotAMember_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteField(FieldName, ShortValue, (ImfHeaderFieldKind)2);

                // assert
                testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("kind");
            }
        }

        [Fact]
        public void WriteFields_NullReader_Throws()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteFields(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("reader");
            }
        }

        public static TheoryData<Action<ImfHeaderWriter>> DisposeGuardedMembers => new TheoryData<Action<ImfHeaderWriter>>
        {
            writer => _ = writer.BytePosition,
            writer => _ = writer.FieldsWritten,
            writer => writer.Flush(),
            writer => writer.WriteEndOfHeader(),
            writer => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured),
            writer => writer.WriteField(Octets.Of(FieldName), Octets.Of(ShortValue), ImfHeaderFieldKind.Unstructured),
            writer => writer.WriteField(default(ImfHeaderFieldView)),
            writer => writer.WriteRawField(Octets.Of(ShortField)),
            writer => writer.WriteFields(HeaderSections.CreateReader(ShortField)),
        };

        [Theory]
        [MemberData(nameof(DisposeGuardedMembers))]
        public void Member_DisposedWriter_Throws(Action<ImfHeaderWriter> member)
        {
            // arrange
            var writer = new ImfHeaderWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var writer = new ImfHeaderWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new ImfHeaderWriter(stream);

            // act
            writer.Dispose();

            // assert
            stream.CanWrite.Should().BeTrue();
        }

        [Fact]
        public void WriteField_AfterAFlushFailure_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new FailingWriteStream()))
            {
                MessageWrites.IgnoreFailure(() => writer.Flush());

                // act
                Action testAction = () => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteField_AfterAStreamFailure_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            using (var writer = new ImfHeaderWriter(new FailingWriteStream()))
            {
                MessageWrites.IgnoreFailure(() => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured));

                // act
                Action testAction = () => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }
    }
}
