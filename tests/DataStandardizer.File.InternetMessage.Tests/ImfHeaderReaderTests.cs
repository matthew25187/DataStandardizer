using System;
using System.IO;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfHeaderReaderTests
    {
        // ref. RFC 5322 §2.1.1: a line is at most 998 octets, not counting the CRLF.
        private const int MaxLineLength = 998;

        // ref. RFC 5322 §2.2.3: the example of folding.
        private const string FoldedSubjectExample = "Subject: This\r\n is a test\r\n";
        private const string UnfoldedSubjectExampleValue = " This is a test";

        private const string TwoFieldSection = "A:1\r\nB:2\r\n\r\n";
        private const int TwoFieldSectionLength = 12;
        private const int TwoFields = 2;

        // U+00E9 in ISO-8859-1, which is not valid UTF-8, and the character itself.
        private const string LatinSmallEWithAcuteOctet = "\xE9";
        private const string LatinSmallEWithAcute = "\xE9";

        private const string UnknownCharset = "x-no-such-charset";

        public static TheoryData<string, FieldSnapshot[]> WellFormedSections => new TheoryData<string, FieldSnapshot[]>
        {
            {
                "Subject: Hi\r\n\r\n",
                new[] { new FieldSnapshot("Subject: Hi\r\n", "Subject", " Hi", " Hi", 0, 1) }
            },
            {
                TwoFieldSection,
                new[] { new FieldSnapshot("A:1\r\n", "A", "1", "1", 0, 1), new FieldSnapshot("B:2\r\n", "B", "2", "2", 5, 2) }
            },
            {
                // ref. RFC 5322 §3.5: the empty line and the body may be absent.
                "A:1\r\n",
                new[] { new FieldSnapshot("A:1\r\n", "A", "1", "1", 0, 1) }
            },
            {
                FoldedSubjectExample + "\r\n",
                new[] { new FieldSnapshot(FoldedSubjectExample, "Subject", UnfoldedSubjectExampleValue, UnfoldedSubjectExampleValue, 0, 1) }
            },
            {
                // A fold of several white space characters keeps them all.
                "A:1\r\n \t 2\r\n\r\n",
                new[] { new FieldSnapshot("A:1\r\n \t 2\r\n", "A", "1 \t 2", "1 \t 2", 0, 1) }
            },
            {
                "A:1\r\n\t2\r\n\r\n",
                new[] { new FieldSnapshot("A:1\r\n\t2\r\n", "A", "1\t2", "1\t2", 0, 1) }
            },
            {
                // The line number of a field after a folded one counts the physical lines.
                "A:1\r\n 2\r\n 3\r\nB:4\r\n\r\n",
                new[] { new FieldSnapshot("A:1\r\n 2\r\n 3\r\n", "A", "1 2 3", "1 2 3", 0, 1), new FieldSnapshot("B:4\r\n", "B", "4", "4", 13, 4) }
            },
            {
                "A:\r\n 1\r\n\r\n",
                new[] { new FieldSnapshot("A:\r\n 1\r\n", "A", " 1", " 1", 0, 1) }
            },
            {
                // ref. RFC 5322 §4.2: obs-FWS allows a line of nothing but white space.
                "A:1\r\n \r\n 2\r\n\r\n",
                new[] { new FieldSnapshot("A:1\r\n \r\n 2\r\n", "A", "1  2", "1  2", 0, 1) }
            },
            {
                // ref. RFC 5322 §4.5.8: obs-optional allows white space before the colon.
                "A :1\r\n\r\n",
                new[] { new FieldSnapshot("A :1\r\n", "A", "1", "1", 0, 1) }
            },
            {
                "A \t :1\r\n\r\n",
                new[] { new FieldSnapshot("A \t :1\r\n", "A", "1", "1", 0, 1) }
            },
            {
                "A:\r\n\r\n",
                new[] { new FieldSnapshot("A:\r\n", "A", "", "", 0, 1) }
            },
            {
                "A: \r\n\r\n",
                new[] { new FieldSnapshot("A: \r\n", "A", " ", " ", 0, 1) }
            },
            {
                "A:b:c\r\n\r\n",
                new[] { new FieldSnapshot("A:b:c\r\n", "A", "b:c", "b:c", 0, 1) }
            },
            {
                // ref. RFC 5322 §3.6.8: ftext is %d33-57 and %d59-126, shown here by the first and last characters of each range.
                "!9;~:1\r\n\r\n",
                new[] { new FieldSnapshot("!9;~:1\r\n", "!9;~", "1", "1", 0, 1) }
            },
            {
                // ref. RFC 5322 §4.1: obs-utext allows control characters, which are left to the code that reads the body.
                "A:\0\x01\x7F\r\n\r\n",
                new[] { new FieldSnapshot("A:\0\x01\x7F\r\n", "A", "\0\x01\x7F", "\0\x01\x7F", 0, 1) }
            },
            {
                // ref. RFC 6532 §3.2: UTF-8 in the body, also across a fold.
                "A:\xC3\xA9\r\n \xE2\x82\xAC\r\n\r\n",
                new[] { new FieldSnapshot("A:\xC3\xA9\r\n \xE2\x82\xAC\r\n", "A", "\xC3\xA9 \xE2\x82\xAC", "\xE9 \x20AC", 0, 1) }
            },
        };

        // ref. RFC 3629 §4: the lowest and highest sequence of each form, and the character that each is.
        public static TheoryData<string, string> ValidUtf8 => new TheoryData<string, string>
        {
            { "\xC2\x80", "\x0080" },
            { "\xDF\xBF", "\x07FF" },
            { "\xE0\xA0\x80", "\x0800" },
            { "\xED\x9F\xBF", "\xD7FF" },
            { "\xEE\x80\x80", "\xE000" },
            { "\xEF\xBF\xBF", "\xFFFF" },
            { "\xF0\x90\x80\x80", "\xD800\xDC00" },
            { "\xF4\x8F\xBF\xBF", "\xDBFF\xDFFF" },
        };

        // ref. RFC 3629 §4
        public static TheoryData<string> InvalidUtf8 => new TheoryData<string>
        {
            "\x80",                     // a continuation byte with no lead byte
            "\xBF",
            "\xC0\x80",                 // an overlong form
            "\xC1\xBF",
            "\xC2",                     // a sequence cut short at the end of the body
            "\xC2\x41",                 // a lead byte followed by something else
            "\xE0\x80\x80",             // an overlong form
            "\xE0\x9F\xBF",
            "\xE1\x80",                 // a sequence cut short
            "\xE1\x80\x41",
            "\xED\xA0\x80",             // a surrogate
            "\xF0\x80\x80\x80",         // an overlong form
            "\xF0\x8F\xBF\xBF",
            "\xF1\x80\x80\x41",
            "\xF4\x90\x80\x80",         // above U+10FFFF
            "\xF5\x80\x80\x80",
            "\xFF",
            "\xE2\x82\r\n \xAC",        // ref. RFC 5322 §2.2.3: unfolding keeps the white space, so a fold can't be inside a sequence.
        };

        // The field is rejected, but the line breaks are good, so reading can go on.  Each case gives the byte and line at fault, and the name.
        public static TheoryData<string, long, long, string> MalformedFields => new TheoryData<string, long, long, string>
        {
            { ":1\r\n", 0, 1, null },
            { " A:1\r\n", 0, 1, null },
            { " \r\n", 0, 1, null },
            { "\xEF\xBB\xBF" + "A:1\r\n", 0, 1, null },         // a UTF-8 byte order mark
            { "\xC3\xA9:1\r\n", 0, 1, null },                   // ref. RFC 6532 §3.2: field names stay ASCII.
            { "\x7F:1\r\n", 0, 1, null },
            { "A1\r\n", 2, 1, "A1" },
            { "A B:1\r\n", 2, 1, "A" },
            { "A\x7F:1\r\n", 1, 1, "A" },
            { "A\r\n :1\r\n", 1, 1, "A" },                      // ref. RFC 5322 §4.5.8: only WSP may come before the colon, not a fold.
            { "A:\xFF\r\n", 2, 1, "A" },
            { "A:1\r\n \xFF\r\n", 6, 2, "A" },
        };

        // Where a field ends is unknown, so reading can't go on.  Each case gives the byte and line at fault.
        public static TheoryData<string, long, long> BrokenLines => new TheoryData<string, long, long>
        {
            { "\n", 0, 1 },
            { "A:1\n\r\n", 3, 1 },
            { "A:1\r\n 2\n\r\n", 7, 2 },
            { "\r", 0, 1 },
            { "\rA:1\r\n", 0, 1 },
            { "A:1\r2\r\n", 3, 1 },
            { "A:1\r", 3, 1 },
            { "A:1\r\n 2\r3\r\n", 7, 2 },
            // ref. RFC 5322 §3.6.8: a field ends with CRLF.
            { "A", 1, 1 },
            { "A:1", 3, 1 },
            { "A:1\r\n 2", 7, 2 },
            { "A:1\r\n ", 6, 2 },
        };

        // ref. RFC 5322 §2.2: a bare carriage return is rejected, whatever is said of a bare line feed.
        public static TheoryData<string> BareCarriageReturns => new TheoryData<string>
        {
            "A:1\r2\n",
            "A:1\r",
            "\r\r\n",
        };

        public static TheoryData<string, FieldSnapshot[]> BareLineFeedSections => new TheoryData<string, FieldSnapshot[]>
        {
            {
                "A:1\nB:2\n\n",
                new[] { new FieldSnapshot("A:1\n", "A", "1", "1", 0, 1), new FieldSnapshot("B:2\n", "B", "2", "2", 4, 2) }
            },
            {
                "A:1\n 2\n\n",
                new[] { new FieldSnapshot("A:1\n 2\n", "A", "1 2", "1 2", 0, 1) }
            },
            {
                // Each line break can be either.
                "A:1\r\n 2\n 3\r\n\n",
                new[] { new FieldSnapshot("A:1\r\n 2\n 3\r\n", "A", "1 2 3", "1 2 3", 0, 1) }
            },
        };

        // ref. #412 design decision 14: a message inside an mbox or EMLX file has LF line breaks, and a CR before one is part of the line.
        public static TheoryData<string, FieldSnapshot[]> ContainerSections => new TheoryData<string, FieldSnapshot[]>
        {
            {
                "A:1\nB:2\n\n",
                new[] { new FieldSnapshot("A:1\n", "A", "1", "1", 0, 1), new FieldSnapshot("B:2\n", "B", "2", "2", 4, 2) }
            },
            {
                "A:1\n 2\n\n",
                new[] { new FieldSnapshot("A:1\n 2\n", "A", "1 2", "1 2", 0, 1) }
            },
            {
                "A:1\r\n\n",
                new[] { new FieldSnapshot("A:1\r\n", "A", "1\r", "1\r", 0, 1) }
            },
            {
                "A:1\r\n 2\n\n",
                new[] { new FieldSnapshot("A:1\r\n 2\n", "A", "1\r 2", "1\r 2", 0, 1) }
            },
        };

        public static TheoryData<string> LinesWithinLimit
        {
            get
            {
                var longest = new string('x', MaxLineLength - 2);
                return new TheoryData<string>
                {
                    "A:" + longest + "\r\n\r\n",
                    // A continuation line, whose white space counts.
                    "A:1\r\n " + new string('x', MaxLineLength - 1) + "\r\n\r\n",
                    // The limit is on physical lines, so a field can be longer once unfolded.
                    "A:" + longest + "\r\n " + longest + "\r\n\r\n",
                };
            }
        }

        // Each case gives the line that is too long, and the byte and line at fault.
        public static TheoryData<string, long, long> LinesOverLimit => new TheoryData<string, long, long>
        {
            { "A:" + new string('x', MaxLineLength - 1) + "\r\n\r\n", MaxLineLength, 1 },
            { "A:1\r\n " + new string('x', MaxLineLength) + "\r\n\r\n", 5 + MaxLineLength, 2 },
            { new string('x', MaxLineLength + 1), MaxLineLength, 1 },
        };

        public static TheoryData<ImfOptions> InvalidOptions => new TheoryData<ImfOptions>
        {
            new ImfOptions { MaxHeaderFieldCount = 0 },
            new ImfOptions { MaxHeaderFieldCount = -1 },
            new ImfOptions { MaxHeaderSectionLength = 0 },
            new ImfOptions { MaxHeaderSectionLength = -1 },
            new ImfOptions { AllowEightBitHeaders = true, CharsetResolver = null },
            new ImfOptions { AllowEightBitHeaders = true, HeaderFallbackCharset = default(MimeCharset) },
            new ImfOptions { AllowEightBitHeaders = true, HeaderFallbackCharset = MimeCharset.Parse(UnknownCharset) },
        };

        public static TheoryData<Func<ImfHeaderReader, object>> ReaderMembers => new TheoryData<Func<ImfHeaderReader, object>>
        {
            reader => reader.ReadField(),
            reader => reader.ReadFieldAsync(),
            reader => reader.BytePosition,
            reader => reader.FieldsRead,
            reader => reader.PhysicalLinesRead,
        };

        public static TheoryData<Func<ImfHeaderFieldView, object>> ViewMembers => new TheoryData<Func<ImfHeaderFieldView, object>>
        {
            view => view.BytePosition,
            view => view.LineNumber,
            view => view.NeedsHeaderFallbackCharset,
            view => view.RawBytes.Length,
            view => view.RawBytesMemory,
            view => view.Name.Length,
            view => view.NameMemory,
            view => view.Value.Length,
            view => view.ValueMemory,
            view => view.GetName(),
            view => view.GetValue(),
        };

        #region Fields

        [Theory]
        [MemberData(nameof(WellFormedSections))]
        public void ReadField_WellFormedSection_GivesEachField(string section, FieldSnapshot[] expected)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(ValidUtf8))]
        public void GetValue_ValidUtf8_DecodesIt(string octets, string expected)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:" + octets + "\r\n"))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.DecodedValue.Should().Be(expected);
            }
        }

        [Fact]
        public void Value_NotFolded_IsASliceOfTheRawBytes()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\n"))
            {
                var view = reader.ReadField().Value;

                // act
                var testResult = view.Value.Overlaps(view.RawBytes);

                // assert
                testResult.Should().BeTrue();
            }
        }

        [Fact]
        public void Value_Folded_LeavesTheRawBytesAsTheyWere()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(FoldedSubjectExample))
            {
                var view = reader.ReadField().Value;

                // act
                var testResult = new[] { Octets.ToText(view.Value), Octets.ToText(view.Value), Octets.ToText(view.RawBytes) };

                // assert
                testResult.Should().Equal(UnfoldedSubjectExampleValue, UnfoldedSubjectExampleValue, FoldedSubjectExample);
            }
        }

        [Fact]
        public void Value_SecondFoldedFieldShorterThanTheFirst_HoldsOnlyItsOwnBytes()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:123456\r\n 7\r\nB:1\r\n 2\r\n"))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(
                    new[]
                    {
                        new FieldSnapshot("A:123456\r\n 7\r\n", "A", "123456 7", "123456 7", 0, 1),
                        new FieldSnapshot("B:1\r\n 2\r\n", "B", "1 2", "1 2", 14, 3),
                    },
                    options => options.WithStrictOrdering());
            }
        }

        #endregion

        #region End of the section

        [Theory]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\nA:1\r\n")]
        public void ReadField_NoFields_ReturnsNull(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                var testResult = reader.ReadField();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadField_BodyStartsWithWhiteSpace_DoesNotTakeItForAFold()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\n\r\n body\r\n"))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.Value.Should().Be("1");
            }
        }

        [Fact]
        public void BytePosition_SectionEnded_IsTheStartOfTheBody()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection + "not a field"))
            {
                HeaderSections.ReadAll(reader);

                // act
                var testResult = reader.BytePosition;

                // assert
                testResult.Should().Be(TwoFieldSectionLength);
            }
        }

        [Fact]
        public void Counters_SectionEnded_CountWhatWasRead()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\n 2\r\nB:3\r\n\r\n"))
            {
                HeaderSections.ReadAll(reader);

                // act
                var testResult = new[] { reader.FieldsRead, reader.PhysicalLinesRead };

                // assert
                testResult.Should().Equal(TwoFields, 4);
            }
        }

        [Fact]
        public void ReadField_SectionEnded_KeepsReturningNullWithoutReading()
        {
            // arrange
            const int emptyLineLength = 2;
            var stream = new BlockStream(Octets.Of("\r\nA:1\r\n"), BlockStreamMode.SynchronousOnly, emptyLineLength, 1);
            using (var reader = new ImfHeaderReader(stream))
            {
                // act
                var testResult = new[] { reader.ReadField().HasValue, reader.ReadField().HasValue };

                // assert
                testResult.Should().Equal(false, false);
                stream.BytesGiven.Should().Be(emptyLineLength);
            }
        }

        #endregion

        #region Errors

        [Theory]
        [MemberData(nameof(MalformedFields))]
        public void ReadField_MalformedField_ThrowsWithWhereItWasFound(string section, long bytePosition, long lineNumber, string fieldName)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.BytePosition].Should().Be(bytePosition);
                data[ImfFileException.DataItemName.LineNumber].Should().Be(lineNumber);
                data[ImfFileException.DataItemName.FieldName].Should().Be(fieldName);
            }
        }

        [Fact]
        public void ReadField_AfterMalformedField_GoesOnToTheNextField()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\nno colon\r\nB:\xFF\r\nC:2\r\n\r\n"))
            {
                // act
                var testResult = HeaderSections.ReadOutcomes(reader);

                // assert
                testResult.Should().Equal("A", HeaderSections.Rejected, HeaderSections.Rejected, "C");
            }
        }

        [Theory]
        [MemberData(nameof(BrokenLines))]
        public void ReadField_BrokenLine_ThrowsWithWhereItWasFound(string section, long bytePosition, long lineNumber)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.BytePosition].Should().Be(bytePosition);
                data[ImfFileException.DataItemName.LineNumber].Should().Be(lineNumber);
            }
        }

        [Theory]
        [InlineData("A:1\n")]
        [InlineData("A:1\r2\r\n")]
        [InlineData("A:1")]
        public void ReadField_AfterBrokenLine_ThrowsInvalidOperationException(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                HeaderSections.ReadUntilRejected(reader);

                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ViewMember_AfterRejectedField_ThrowsInvalidOperationException()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\n:2\r\n"))
            {
                var view = reader.ReadField().Value;
                HeaderSections.ReadUntilRejected(reader);

                // act
                Action testAction = () => view.GetName();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region AcceptBareLineFeed

        [Theory]
        [MemberData(nameof(BareLineFeedSections))]
        public void ReadField_BareLineFeedAccepted_ReadsTheFields(string section, FieldSnapshot[] expected)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section, new ImfOptions { AcceptBareLineFeed = true }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            }
        }

        [Theory]
        [InlineData("A:1\nB:2\n\n")]
        [InlineData("A:1\n 2\n\n")]
        [InlineData("A:1\r\n 2\n 3\r\n\n")]
        [InlineData("A:1\r\n\n")]
        public void ReadField_BareLineFeedNotAccepted_Throws(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void BytePosition_BareLineFeedAcceptedAndEmptyLine_IsTheStartOfTheBody()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\n\nnot a field", new ImfOptions { AcceptBareLineFeed = true }))
            {
                HeaderSections.ReadAll(reader);

                // act
                var testResult = reader.BytePosition;

                // assert
                testResult.Should().Be(5);
            }
        }

        [Theory]
        [MemberData(nameof(BareCarriageReturns))]
        public void ReadField_BareCarriageReturnWithBareLineFeedAccepted_Throws(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section, new ImfOptions { AcceptBareLineFeed = true }))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        #endregion

        #region Container line breaks

        [Theory]
        [MemberData(nameof(ContainerSections))]
        public void ReadField_LineFeedIsLineBreak_ReadsTheFields(string section, FieldSnapshot[] expected)
        {
            // arrange
            using (var reader = HeaderSections.CreateContainerReader(section))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(BareCarriageReturns))]
        public void ReadField_BareCarriageReturnWithLineFeedIsLineBreak_Throws(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateContainerReader(section))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadField_CrlfLineWithLineFeedIsLineBreak_IsNotTheEmptyLine()
        {
            // arrange
            using (var reader = HeaderSections.CreateContainerReader("\r\n"))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadField_CarriageReturnWithLineFeedIsLineBreak_CountsTowardsTheLineLength()
        {
            // arrange
            using (var reader = HeaderSections.CreateContainerReader("A:" + new string('x', MaxLineLength - 2) + "\r\n\n"))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        #endregion

        #region AllowLongLines

        [Theory]
        [MemberData(nameof(LinesWithinLimit))]
        public void ReadField_LineWithinLimit_ReadsIt(string section)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle();
            }
        }

        [Theory]
        [MemberData(nameof(LinesOverLimit))]
        public void ReadField_LineOverLimit_ThrowsWithWhereItWasFound(string section, long bytePosition, long lineNumber)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(section))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.BytePosition].Should().Be(bytePosition);
                data[ImfFileException.DataItemName.LineNumber].Should().Be(lineNumber);
            }
        }

        [Fact]
        public void ReadField_LineOverLimitWithLongLinesAllowed_ReadsIt()
        {
            // arrange
            var value = new string('x', MaxLineLength - 1);
            using (var reader = HeaderSections.CreateReader("A:" + value + "\r\n\r\n", new ImfOptions { AllowLongLines = true }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.Value.Should().Be(value);
            }
        }

        #endregion

        #region AllowEightBitHeaders

        [Theory]
        [MemberData(nameof(InvalidUtf8))]
        public void ReadField_InvalidUtf8_ThrowsAtTheStartOfTheSequence(string octets)
        {
            // arrange
            const long startOfBody = 2;
            using (var reader = HeaderSections.CreateReader("A:" + octets + "\r\n"))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.BytePosition].Should().Be(startOfBody);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidUtf8))]
        public void ReadField_InvalidUtf8WithEightBitHeadersAllowed_RecordsThatItNeedsTheFallback(string octets)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:" + octets + "\r\n", new ImfOptions { AllowEightBitHeaders = true }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.NeedsHeaderFallbackCharset.Should().BeTrue();
            }
        }

        [Fact]
        public void GetValue_InvalidUtf8WithEightBitHeadersAllowed_DecodesWithTheDefaultFallback()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:\xE9\r\n \xE9\r\n", new ImfOptions { AllowEightBitHeaders = true }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.Should().BeEquivalentTo(new FieldSnapshot(
                    "A:\xE9\r\n \xE9\r\n",
                    "A",
                    LatinSmallEWithAcuteOctet + " " + LatinSmallEWithAcuteOctet,
                    LatinSmallEWithAcute + " " + LatinSmallEWithAcute,
                    0,
                    1,
                    true));
            }
        }

        [Fact]
        public void GetValue_ValidUtf8WithEightBitHeadersAllowed_DecodesAsUtf8()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:\xC3\xA9\r\n", new ImfOptions { AllowEightBitHeaders = true }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.Should().BeEquivalentTo(
                    new FieldSnapshot("A:\xC3\xA9\r\n", "A", "\xC3\xA9", LatinSmallEWithAcute, 0, 1));
            }
        }

        [Fact]
        public void GetValue_InvalidUtf8WithAnotherFallback_ResolvesItThroughTheResolver()
        {
            // arrange
            var options = new ImfOptions
            {
                AllowEightBitHeaders = true,
                HeaderFallbackCharset = MimeCharset.Parse(UnknownCharset),
                CharsetResolver = new FixedCharsetResolver(new UTF8Encoding(false)),
            };
            using (var reader = HeaderSections.CreateReader("A:\xE9\r\n", options))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().ContainSingle().Which.DecodedValue.Should().Be("\xFFFD");
            }
        }

        [Fact]
        public void ReadField_EightBitFieldNameWithEightBitHeadersAllowed_Throws()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("\xE9:1\r\n", new ImfOptions { AllowEightBitHeaders = true }))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        #endregion

        #region Limits

        [Fact]
        public void ReadField_FieldCountAtLimit_ReadsThem()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection, new ImfOptions { MaxHeaderFieldCount = TwoFields }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().HaveCount(TwoFields);
            }
        }

        [Fact]
        public void ReadField_FieldCountOverLimit_ThrowsAtTheFieldOverIt()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection, new ImfOptions { MaxHeaderFieldCount = TwoFields - 1 }))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.BytePosition].Should().Be(5L);
                data[ImfFileException.DataItemName.LineNumber].Should().Be(2L);
            }
        }

        [Fact]
        public void ReadField_SectionLengthAtLimit_ReadsIt()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection, new ImfOptions { MaxHeaderSectionLength = TwoFieldSectionLength }))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().HaveCount(TwoFields);
            }
        }

        [Fact]
        public void ReadField_SectionLengthOverLimit_ThrowsAtTheOctetOverIt()
        {
            // arrange
            const long lastOctet = TwoFieldSectionLength - 1;
            using (var reader = HeaderSections.CreateReader(TwoFieldSection, new ImfOptions { MaxHeaderSectionLength = TwoFieldSectionLength - 1 }))
            {
                // act
                Action testAction = () => HeaderSections.ReadAll(reader);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.BytePosition].Should().Be(lastOctet);
            }
        }

        [Fact]
        public void ReadField_AfterSectionLengthOverLimit_ThrowsInvalidOperationException()
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection, new ImfOptions { MaxHeaderSectionLength = 1 }))
            {
                HeaderSections.ReadUntilRejected(reader);

                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadField_LongLinesAllowedAndLinePassesSectionLimit_StopsReading()
        {
            // arrange
            const int sectionLimit = 10;
            const int bytesRead = 11;
            var stream = new BlockStream(Octets.Of("A:" + new string('x', 100)), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var reader = new ImfHeaderReader(stream, new ImfOptions { AllowLongLines = true, MaxHeaderSectionLength = sectionLimit }))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.BytesGiven.Should().Be(bytesRead);
            }
        }

        [Fact]
        public void Constructor_Default_IsStrictWithFiniteLimits()
        {
            // act
            var testResult = new ImfOptions();

            // assert
            testResult.AcceptBareLineFeed.Should().BeFalse();
            testResult.AllowEightBitHeaders.Should().BeFalse();
            testResult.AllowLongLines.Should().BeFalse();
            testResult.CharsetResolver.Should().BeSameAs(MimeCharsetResolver.Default);
            testResult.HeaderFallbackCharset.Should().Be(MimeCharset.Iso88591);
            testResult.MaxHeaderFieldCount.Should().Be(1000);
            testResult.MaxHeaderSectionLength.Should().Be(1048576);
        }

        #endregion

        #region Refills

        [Theory]
        [ClassData(typeof(SplitSections))]
        public void ReadField_SplitAcrossRefills_GivesTheSameFields(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(SplitSections.Section, BlockStreamMode.SynchronousOnly, firstReadLength);
            using (var reader = new ImfHeaderReader(stream, new ImfOptions(), bufferSize, false))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(SplitSections.ExpectedFields, options => options.WithStrictOrdering());
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        public void ReadField_EveryReadOfAFewBytes_GivesTheSameFields(int readLength)
        {
            // arrange
            var stream = new BlockStream(SplitSections.Section, BlockStreamMode.SynchronousOnly, readLength, readLength);
            using (var reader = new ImfHeaderReader(stream))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(SplitSections.ExpectedFields, options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public void ReadField_BareCarriageReturnAtEndOfRead_ThrowsOnSeeingWhatFollows()
        {
            // arrange
            const int throughCarriageReturn = 4;
            const long carriageReturnPosition = 3;
            var stream = new BlockStream(Octets.Of("A:1\r2\r\n"), BlockStreamMode.SynchronousOnly, throughCarriageReturn);
            using (var reader = new ImfHeaderReader(stream))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.BytePosition].Should().Be(carriageReturnPosition);
            }
        }

        [Fact]
        public void ReadField_LongLineAcrossRefills_ThrowsAtTheOctetOverTheLimit()
        {
            // arrange
            const int readLength = 100;
            const long octetOverTheLimit = MaxLineLength;
            var stream = new BlockStream(Octets.Of(new string('x', MaxLineLength + 1)), BlockStreamMode.SynchronousOnly, readLength, readLength);
            using (var reader = new ImfHeaderReader(stream))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.BytePosition].Should().Be(octetOverTheLimit);
            }
        }

        #endregion

        #region Construction and lifetime

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfHeaderReader((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageStream");
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfHeaderReader(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_UnreadableStream_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new ImfHeaderReader(new WriteOnlyStream());

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("messageStream");
        }

        [Theory]
        [MemberData(nameof(InvalidOptions))]
        public void Constructor_InvalidOptions_ThrowsArgumentException(ImfOptions options)
        {
            // act
            Action testAction = () => new ImfHeaderReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_UnresolvableFallbackNotNeeded_DoesNotResolveIt()
        {
            // arrange
            var options = new ImfOptions { HeaderFallbackCharset = MimeCharset.Parse(UnknownCharset) };

            // act
            Action testAction = () => new ImfHeaderReader(new MemoryStream(), options).Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerSuppliedStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream();
            var reader = new ImfHeaderReader(stream);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = new ImfHeaderReader(new MemoryStream());
            reader.Dispose();

            // act
            Action testAction = reader.Dispose;

            // assert
            testAction.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(ReaderMembers))]
        public void Member_ReaderDisposed_ThrowsObjectDisposedException(Func<ImfHeaderReader, object> member)
        {
            // arrange
            var reader = HeaderSections.CreateReader(TwoFieldSection);
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region View lifetime

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_ReaderMovedOn_ThrowsInvalidOperationException(Func<ImfHeaderFieldView, object> member)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader(TwoFieldSection))
            {
                var view = reader.ReadField().Value;
                reader.ReadField();

                // act
                Action testAction = () => member(view);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_SectionEnded_ThrowsInvalidOperationException(Func<ImfHeaderFieldView, object> member)
        {
            // arrange
            using (var reader = HeaderSections.CreateReader("A:1\r\n\r\n"))
            {
                var view = reader.ReadField().Value;
                reader.ReadField();

                // act
                Action testAction = () => member(view);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_ReaderDisposed_ThrowsObjectDisposedException(Func<ImfHeaderFieldView, object> member)
        {
            // arrange
            var reader = HeaderSections.CreateReader(TwoFieldSection);
            var view = reader.ReadField().Value;
            reader.Dispose();

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_DefaultView_ThrowsInvalidOperationException(Func<ImfHeaderFieldView, object> member)
        {
            // arrange
            var view = default(ImfHeaderFieldView);

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        #endregion
    }
}
