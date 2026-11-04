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
    /// Tests of <see cref="ImfReader"/>.
    /// </summary>
    public class ImfReaderTests
    {
        private const string Header = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n";
        private const string Body = "Body\r\n";
        private const string Message = Header + "\r\n" + Body;

        // The bytes of Message, which SplitAcrossRefills splits at chosen points.
        private const int MessageLength = 63;
        private const int HeaderEndsAt = 55;

        // A byte above 127 that is not valid UTF-8, and "\x00E9" in ISO-8859-1.
        private const string Latin1Acute = "\x00E9";
        private const string Acute = "\x00E9";

        private const string LatinSubject = Header + "Subject: " + Latin1Acute + "\r\n\r\n";
        private const string LatinFrom = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: " + Latin1Acute + " <a@b.test>\r\n\r\n";
        private const string SubjectValue = " " + Acute;

        public static TheoryData<int, int> SplitAcrossRefills => new TheoryData<int, int>
        {
            { 1, 1 },
            { 1, 16 },
            { 2, 4096 },
            { HeaderEndsAt, 8 },
            { HeaderEndsAt + 1, 8 },
            { HeaderEndsAt + 2, 8 },
            { MessageLength - 1, 2 },
            { MessageLength, 4096 },
        };

        public static TheoryData<ImfOptions> BadOptions => new TheoryData<ImfOptions>
        {
            new ImfOptions { MaxEntitySize = 0 },
            new ImfOptions { MaxHeaderFieldCount = 0 },
            new ImfOptions { MaxHeaderSectionLength = 0 },
            new ImfOptions { CharsetResolver = null },
        };

        [Theory]
        [MemberData(nameof(Rfc5322Messages.All), MemberType = typeof(Rfc5322Messages))]
        public void ReadMessage_AppendixAMessage_KeepsEachFieldAndTheBody(string message)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                reader.Diagnostics.Should().BeEmpty();
                reader.BytePosition.Should().Be(message.Length);
                testResult.Fields.Should().NotBeEmpty();
                testResult.RawBody.IsEmpty.Should().BeFalse();
            }
        }

        [Fact]
        public void ReadMessage_FieldsAndBody_AreKeptInOrder()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Rfc5322Messages.A11))))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Fields.Should().HaveCount(5);
                testResult.Fields[0].Name.Should().Be("From");
                testResult.Fields[4].Name.Should().Be("Message-ID");
                Octets.ToText(testResult.Fields[1].RawBytes.Span).Should().Be("To: Mary Smith <mary@example.net>\r\n");
                Octets.ToText(testResult.RawBody.Span).Should().Be(Rfc5322Messages.A11Body);
            }
        }

        [Fact]
        public void ReadMessage_NoEmptyLine_HasNoBody()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Header))))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Fields.Should().HaveCount(2);
                testResult.RawBody.IsEmpty.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadMessage_EmptyDataAllowed_HasNothing()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            using (var reader = new ImfReader(new MemoryStream(), options))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Fields.Should().BeEmpty();
                testResult.RawBody.IsEmpty.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadMessage_EmptyData_RecordsTheMissingFields()
        {
            // arrange
            // ref. RFC 5322 §4.5: obs-fields = *(...), so a message can have no fields.
            using (var reader = new ImfReader(new MemoryStream()))
            {
                // act
                reader.ReadMessage();

                // assert
                reader.Diagnostics.Select(diagnostic => diagnostic.FieldName).Should().Equal("Date", "From");
            }
        }

        [Fact]
        public void ReadMessage_BareLineFeed_Throws()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\n\n"))))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadMessage_BareLineFeedWithOption_IsRead()
        {
            // arrange
            var options = new ImfOptions { AcceptBareLineFeed = true };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\n\nBody\n")), options))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.From.ToString().Should().Be("a@b.test");
                Octets.ToText(testResult.RawBody.Span).Should().Be("Body\n");
            }
        }

        [Fact]
        public void ReadMessage_LineFeedIsLineBreakForAContainer_IsRead()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\n\nBody\n")), new ImfOptions(), 16, true))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                reader.Diagnostics.Should().BeEmpty();
                testResult.Fields.Should().HaveCount(2);
                Octets.ToText(testResult.RawBody.Span).Should().Be("Body\n");
            }
        }

        [Fact]
        public void ReadMessage_NotUtf8_Throws()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(LatinSubject))))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadMessage_NotUtf8WithOption_DecodesWithTheFallbackCharacterSet()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitHeaders = true };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(LatinSubject)), options))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Subject.Should().Be(Acute);
                testResult.Fields[2].Value.Should().Be(SubjectValue);
            }
        }

        [Fact]
        public void ReadMessage_DisplayNameNotUtf8WithOption_DecodesWithTheFallbackCharacterSet()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitHeaders = true };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(LatinFrom)), options))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.From[0].DisplayName.Should().Be(Acute);
            }
        }

        [Fact]
        public void ReadMessage_BodyAtMaxEntitySize_IsRead()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = 6 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message)), options))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                Octets.ToText(testResult.RawBody.Span).Should().Be(Body);
            }
        }

        [Fact]
        public void ReadMessage_BodyPastMaxEntitySize_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = 5 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadMessage_HeaderLimitPassed_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxHeaderFieldCount = 1 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(SplitAcrossRefills))]
        public void ReadMessage_SplitAcrossRefills_GivesTheSameMessage(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(Message), BlockStreamMode.SynchronousOnly, firstReadLength);
            using (var reader = new ImfReader(stream, new ImfOptions(), bufferSize, false))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Fields.Should().HaveCount(2);
                Octets.ToText(testResult.Fields[1].RawBytes.Span).Should().Be("From: a@b.test\r\n");
                Octets.ToText(testResult.RawBody.Span).Should().Be(Body);
            }
        }

        [Theory]
        [MemberData(nameof(SplitAcrossRefills))]
        public async Task ReadMessageAsync_SplitAcrossRefills_GivesTheSameMessage(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(Message), BlockStreamMode.AsynchronousOnly, firstReadLength);
            var reader = new ImfReader(stream, new ImfOptions(), bufferSize, false);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await reader.ReadMessageAsync();

                // assert
                testResult.Fields.Should().HaveCount(2);
                Octets.ToText(testResult.Fields[1].RawBytes.Span).Should().Be("From: a@b.test\r\n");
                Octets.ToText(testResult.RawBody.Span).Should().Be(Body);
            });
        }

        [Theory]
        [MemberData(nameof(Rfc5322Messages.All), MemberType = typeof(Rfc5322Messages))]
        public async Task ReadMessageAsync_AppendixAMessage_WritesBackByteForByte(string message)
        {
            // arrange
            var reader = new ImfReader(new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                var read = await reader.ReadMessageAsync();

                // act
                var testResult = ImfMessages.Write(read);

                // assert
                testResult.Should().Be(message);
            });
        }

        [Fact]
        public void ReadMessage_SecondRead_Throws()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message))))
            {
                reader.ReadMessage();

                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task ReadMessageAsync_SecondRead_ThrowsFromTheCall()
        {
            // arrange
            var reader = new ImfReader(new MemoryStream(Octets.Of(Message)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadMessageAsync();

                // act
                Action testAction = () => reader.ReadMessageAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public void ReadMessage_AfterAnError_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxHeaderFieldCount = 1 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message)), options))
            {
                var firstRead = Record.Exception(() => reader.ReadMessage());

                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                firstRead.Should().BeOfType<ImfFileException>();
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadMessage_Disposed_Throws()
        {
            // arrange
            var reader = new ImfReader(new MemoryStream(Octets.Of(Message)));
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadMessage();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var reader = new ImfReader(new MemoryStream(Octets.Of(Message)));
            reader.Dispose();

            // act
            Action testAction = () => { var ignored = reader.Diagnostics; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_Disposed_Throws()
        {
            // arrange
            var reader = new ImfReader(new MemoryStream(Octets.Of(Message)));
            reader.Dispose();

            // act
            Action testAction = () => { var ignored = reader.BytePosition; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream(Octets.Of(Message));
            var reader = new ImfReader(stream);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void ReadMessage_AfterTheReaderIsDisposed_StillHoldsItsContent()
        {
            // arrange
            ImfMessage message;
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message))))
            {
                message = reader.ReadMessage();
            }

            // act
            var testResult = ImfMessages.Write(message);

            // assert
            testResult.Should().Be(Message);
        }

        [Fact]
        public async Task ReadMessageAsync_ReadInProgress_ThrowsFromTheCall()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfReader(stream, new ImfOptions { AllowMissingRequiredFields = true });
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                var read = reader.ReadMessageAsync();

                // act
                Action testAction = () => reader.ReadMessageAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await read;
            });
        }

        [Fact]
        public async Task ReadMessageAsync_DisposedWhileInProgress_FaultsTheTask()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfReader(stream, new ImfOptions { AllowMissingRequiredFields = true });
            var read = reader.ReadMessageAsync();
            reader.Dispose();
            stream.OpenGate();

            // act
            Func<Task> testAction = () => read;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadMessageAsync_AlreadyCanceled_CancelsTheTask()
        {
            // arrange
            var reader = new ImfReader(new MemoryStream(Octets.Of(Message)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();

                    // act
                    Func<Task> testAction = () => reader.ReadMessageAsync(cancellation.Token);

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task ReadMessageAsync_CanceledWhileInProgress_CancelsTheTask()
        {
            // arrange
            var reader = new ImfReader(new GatedStream());
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    var read = reader.ReadMessageAsync(cancellation.Token);
                    cancellation.Cancel();

                    // act
                    Func<Task> testAction = () => read;

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task ReadMessage_AfterACancellation_Throws()
        {
            // arrange
            var reader = new ImfReader(new GatedStream());
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    var read = reader.ReadMessageAsync(cancellation.Token);
                    cancellation.Cancel();
                    await Record.ExceptionAsync(() => read);

                    // act
                    Action testAction = () => reader.ReadMessage();

                    // assert
                    testAction.Should().Throw<InvalidOperationException>();
                }
            });
        }

        [Fact]
        public async Task ReadMessageAsync_StreamFails_FaultsTheTask()
        {
            // arrange
            var reader = new ImfReader(new FailingStream());
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = reader.ReadMessageAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
                Func<Task> awaited = () => testResult;
                await awaited.Should().ThrowAsync<IOException>();
            });
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new ImfReader((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_StreamThatCannotRead_Throws()
        {
            // act
            Action testAction = () => new ImfReader(new WriteOnlyStream());

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfReader(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(BadOptions))]
        public void Constructor_BadOptions_Throws(ImfOptions options)
        {
            // act
            Action testAction = () => new ImfReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }
    }
}
