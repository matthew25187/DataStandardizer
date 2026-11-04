using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MhtmlReader"/>: the fields it doesn't require, what stays as strict as <see cref="ImfReader"/>, and the shapes that browsers and Word produce.
    /// </summary>
    public class MhtmlReaderTests
    {
        private const string NoHeader = "MIME-Version: 1.0\r\nContent-Type: text/html\r\n\r\n<p>page</p>\r\n";
        private const string NoFrom = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nMIME-Version: 1.0\r\nContent-Type: text/html\r\n\r\n<p>page</p>\r\n";
        private const string NoDate = "From: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: text/html\r\n\r\n<p>page</p>\r\n";
        private const string NoSender = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test, c@b.test\r\n\r\n";
        private const string ResentWithoutDate = "Resent-From: a@b.test\r\nContent-Type: text/html\r\n\r\n<p>page</p>\r\n";
        private const string SecondSubject = "Subject: a\r\nSubject: b\r\n\r\n";
        private const string InvalidDate = "Date: not a date\r\n\r\n";
        private const string SnapshotField = "Snapshot-Content-Location";
        private const string SnapshotLocation = " https://example.org/page.html";
        private const string ChromiumPage = "https://example.org/page.html";
        private const string ChromiumLogo = "https://example.org/logo.png";
        private const string WordFileList = "file:///C:/page_files/filelist.xml";
        private const string WordThemeData = "file:///C:/page_files/themedata.thmx";

        // "caf=E9" in MhtmlShapes.Word, where 0xE9 is U+00E9 in windows-1252.
        private const string WordRootText = "<html>caf\x00E9</html>";

        private static readonly ImfOptions ChromiumOptions = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidValues = true };
        private static readonly ImfOptions WordOptions = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true };

        public static TheoryData<string, ImfOptions> RoundTrips => new TheoryData<string, ImfOptions>
        {
            { MhtmlShapes.Rfc2557Section91, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Rfc2557Section92, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Rfc2557Section93, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Rfc2557Section94, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Rfc2557Section95, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Rfc2557Section96, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.AlternativeRoot, new ImfOptions { AllowMissingRequiredFields = true } },
            { MhtmlShapes.Chromium, ChromiumOptions },
            { MhtmlShapes.Word, WordOptions },
        };

        [Theory]
        [InlineData(NoHeader)]
        [InlineData(NoFrom)]
        [InlineData(NoDate)]
        [InlineData(NoSender)]
        public void ReadDocument_MissingOriginationFieldByDefault_Throws(string document)
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document))))
            {
                // act
                Action testAction = () => subject.ReadDocument();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingRequiredField);
            }
        }

        [Fact]
        public void ReadDocument_SecondSubject_RecordsADiagnostic()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(SecondSubject)), EntityEvents.DocumentOptions))
            {
                // act
                subject.ReadDocument();

                // assert
                subject.Diagnostics.Should().Contain(diagnostic => diagnostic.Kind == ImfDiagnosticKind.CardinalityViolation);
            }
        }

        [Fact]
        public void ReadDocument_ResentBlockWithNoDate_ThrowsByDefault()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(ResentWithoutDate))))
            {
                // act
                Action testAction = () => subject.ReadDocument();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingRequiredField);
            }
        }

        [Theory]
        [InlineData(NoHeader, new[] { "Date", "From" })]
        [InlineData(NoFrom, new[] { "From" })]
        [InlineData(NoDate, new[] { "Date" })]
        [InlineData(NoSender, new[] { "Sender" })]
        public void ReadDocument_MissingOriginationField_RecordsADiagnostic(string document, string[] expected)
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), EntityEvents.DocumentOptions))
            {
                // act
                subject.ReadDocument();

                // assert
                subject.Diagnostics.Should().OnlyContain(diagnostic => diagnostic.Kind == ImfDiagnosticKind.MissingRequiredField);
                subject.Diagnostics.Select(diagnostic => diagnostic.FieldName).Should().Equal(expected);
            }
        }

        [Theory]
        [InlineData(NoHeader)]
        [InlineData(NoFrom)]
        [InlineData(NoDate)]
        [InlineData(NoSender)]
        public void ReadMessage_MissingOriginationFieldWithImfReader_Throws(string document)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(document))))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        // ref. RFC 5322 §3.6.6: a block of resent fields is not an origination field.
        [InlineData(InvalidDate, ImfDiagnosticKind.InvalidFieldValue)]
        [InlineData(MhtmlShapes.Chromium, ImfDiagnosticKind.InvalidFieldValue)]
        public void ReadDocument_OtherProblemInTheHeader_Throws(string document, ImfDiagnosticKind expected)
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.ReadDocument();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(MhtmlShapes.Word)]
        [InlineData(MhtmlShapes.Rfc2557Section92Verbatim)]
        public void ReadDocument_InvalidStructure_Throws(string document)
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.ReadDocument();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
            }
        }

        [Fact]
        public void ReadDocument_InvalidStartAllowed_UsesTheFirstPart()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Rfc2557Section92Verbatim)), WordOptions))
            {
                // act
                var testResult = subject.ReadDocument();

                // assert
                EntityTrees.ContentOf(testResult.Root).Should().Be("<p>root</p>");
            }
        }

        [Fact]
        public void ReadDocument_Chromium_KeepsItsFieldsAndFindsItsParts()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Chromium)), ChromiumOptions))
            {
                // act
                var testResult = subject.ReadDocument();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.FieldName.Should().Be("From");
                testResult.Message.Fields.Find(SnapshotField).Value.Should().Be(SnapshotLocation);
                testResult.Root.Should().BeSameAs(testResult.Resources[0].Entity);
                testResult.Resources[0].Uri.OriginalString.Should().Be(ChromiumPage);
                testResult.Resources[1].Uri.OriginalString.Should().Be(ChromiumLogo);
            }
        }

        [Fact]
        public void ReadDocument_Word_RecordsItsProblemsAndFindsItsParts()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Word)), WordOptions))
            {
                // act
                var testResult = subject.ReadDocument();

                // assert
                subject.Diagnostics.Should().HaveCount(3);
                testResult.RootMediaType.Subtype.Should().Be("html");
                testResult.Resources.Should().HaveCount(3);
                testResult.Resources[1].Uri.OriginalString.Should().Be(WordThemeData);
                testResult.Resources[2].Uri.OriginalString.Should().Be(WordFileList);
            }
        }

        [Fact]
        public void GetText_WordRootWithACodePageResolver_DecodesWindows1252()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true, CharsetResolver = new CodePageCharsetResolver() };
            MhtmlDocument document;
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Word)), options))
            {
                document = reader.ReadDocument();
            }

            // act
            var testResult = document.Root.GetText();

            // assert
            testResult.Should().Be(WordRootText);
        }

        [Fact]
        public void GetText_WordRootWithNoResolverForItsCharset_ThrowsAfterReadingSucceeds()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true, CharsetResolver = new NoCharsetResolver() };
            MhtmlDocument document;
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Word)), options))
            {
                document = reader.ReadDocument();
            }

            // act
            Action testAction = () => document.Root.GetText();

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void TryResolve_Word_FindsARelativeReference()
        {
            // arrange
            MhtmlDocument subject;
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Word)), WordOptions))
            {
                subject = reader.ReadDocument();
            }

            // act
            var testResult = subject.TryResolve(new Uri("page_files/filelist.xml", UriKind.Relative), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Uri.OriginalString.Should().Be(WordFileList);
        }

        [Theory]
        [InlineData(MimeShapes.TextOnly)]
        [InlineData(MimeShapes.TextAndHtml)]
        [InlineData(MimeShapes.HtmlWithInlineImage)]
        [InlineData(MimeShapes.MixedWithAttachments)]
        [InlineData(MimeShapes.Forwarded)]
        public void ReadDocument_EmlFile_GivesTheTreeOfImfReader(string document)
        {
            // arrange
            var expected = EntityTrees.Describe(EntityTrees.Read(document));
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), EntityEvents.DocumentOptions))
            {
                // act
                var testResult = subject.ReadDocument();

                // assert
                EntityTrees.Describe(testResult.Message).Should().Equal(expected);
            }
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void ReadMessage_MhtmlFileWithImfReader_GivesTheTreeOfMhtmlReader(string document, ImfOptions options)
        {
            // arrange
            var imfOptions = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidValues = options.AllowInvalidValues };
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), options))
            {
                var expected = EntityTrees.Describe(subject.ReadDocument().Message);

                // act
                var testResult = EntityTrees.Describe(EntityTrees.Read(document, imfOptions));

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void WriteDocument_WhatWasRead_IsTheSameBytes(string document, ImfOptions options)
        {
            // arrange
            var output = new MemoryStream();
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(document)), options))
            using (var subject = new MhtmlWriter(output, options))
            {
                var read = reader.ReadDocument();

                // act
                subject.WriteDocument(read);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(document);
            }
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public async Task ReadDocumentAsync_Document_GivesWhatReadDocumentGives(string document, ImfOptions options)
        {
            // arrange
            var expected = EntityTrees.Describe(EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidValues = options.AllowInvalidValues }));
            var subject = new MhtmlReader(new MemoryStream(Octets.Of(document)), options);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                var testResult = await subject.ReadDocumentAsync();

                // assert
                testResult.Resources.Should().NotBeEmpty();
                EntityTrees.Describe(testResult.Message).Should().Equal(expected);
            });
        }

        [Fact]
        public async Task ReadDocumentAsync_InvalidStructure_FailsTheTask()
        {
            // arrange
            var subject = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.Word)), EntityEvents.DocumentOptions);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadDocumentAsync();

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public async Task ReadDocumentAsync_CanceledToken_Throws()
        {
            // arrange
            var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions);
            var canceled = new CancellationToken(true);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadDocumentAsync(null, canceled);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public void ReadDocument_BaseUri_ResolvesAgainstIt()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(MhtmlShapes.RelativePartsNoBase)), EntityEvents.DocumentOptions))
            {
                // act
                var testResult = subject.ReadDocument(new Uri("http://caller.example/a/b/"));

                // assert
                testResult.Resources[0].Uri.OriginalString.Should().Be("http://caller.example/a/b/page.html");
            }
        }

        [Fact]
        public void ReadDocument_RelativeBaseUri_Throws()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.ReadDocument(new Uri("a", UriKind.Relative));

                // assert
                testAction.Should().Throw<ArgumentException>();
            }
        }

        [Fact]
        public void ReadDocument_SecondRead_Throws()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions))
            {
                subject.ReadDocument();

                // act
                Action testAction = () => subject.ReadDocument();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadDocument_Disposed_Throws()
        {
            // arrange
            var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions);
            subject.Dispose();

            // act
            Action testAction = () => subject.ReadDocument();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions);
            subject.Dispose();

            // act
            Action testAction = () => _ = subject.Diagnostics;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_AfterReading_IsTheLengthOfTheData()
        {
            // arrange
            using (var subject = new MhtmlReader(new MemoryStream(Octets.Of(NoHeader)), EntityEvents.DocumentOptions))
            {
                subject.ReadDocument();

                // act
                var testResult = subject.BytePosition;

                // assert
                testResult.Should().Be(NoHeader.Length);
            }
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new MhtmlReader((Stream)null, EntityEvents.DocumentOptions);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
