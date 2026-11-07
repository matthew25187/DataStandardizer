using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of when an asynchronous operation of a reader or writer of this package is canceled: only when the caller's token is canceled,
    /// whatever token an <see cref="OperationCanceledException"/> carries.  Otherwise the exception faults the task, as any other exception does.
    /// </summary>
    public class CancellationPrecedenceTests
    {
        private const string CanceledMessage = "*canceled*";
        private const string FieldName = "A";

        // ref. RFC 5322 §2.2 and §2.1: a header section of one field, and the empty line that ends it.
        private const string HeaderSection = "A: 1\r\n\r\n";

        // ref. RFC 5322 §2.1: a message of one field and a body.
        private const string Message = "A: 1\r\n\r\nbody\r\n";

        // ref. RFC 4155 §2 and Appendix A: a mailbox of one message, after its separator line, and the empty line that ends it.
        private const string Mailbox = "From a@b.test Thu Jan  1 00:00:00 2026\nA: 1\n\nbody\n\n";

        private static readonly MboxEnvelope Envelope = new MboxEnvelope(new ImfMailbox("a", "b.test"), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Each way a stream can report a failure.
        private static readonly FailureTiming[] FailureTimings =
        {
            FailureTiming.FromCall, FailureTiming.AsFaultedTask, FailureTiming.AsCanceledTask, FailureTiming.AfterYielding
        };

        // Each OperationCanceledException a stream can fail with while the caller's token isn't canceled.
        private static readonly (string Name, Func<Exception> Create)[] UnrequestedCancellations =
        {
            ("no token", () => new OperationCanceledException()),
            ("another, canceled token", () => new OperationCanceledException(new CancellationToken(true))),
        };

        #region Writers

        // Each asynchronous operation of each writer, over the given stream.  Each reaches the stream.
        private static readonly (string Name, Func<Stream, Func<CancellationToken, Task>> Create)[] WriterOperations =
        {
            ("ImfHeaderWriter.FlushAsync", stream => token => new ImfHeaderWriter(stream).FlushAsync(token)),
            ("ImfHeaderWriter.WriteEndOfHeaderAsync", stream => token => new ImfHeaderWriter(stream).WriteEndOfHeaderAsync(token).AsTask()),
            ("ImfHeaderWriter.WriteFieldAsync of a string", stream => token => new ImfHeaderWriter(stream).WriteFieldAsync(FieldName, "1", ImfHeaderFieldKind.Unstructured, token).AsTask()),
            ("ImfHeaderWriter.WriteFieldAsync of bytes", stream => token => new ImfHeaderWriter(stream).WriteFieldAsync(Octets.Of(FieldName), Octets.Of("1"), ImfHeaderFieldKind.Unstructured, token).AsTask()),
            ("ImfHeaderWriter.WriteFieldAsync of a view", CreateWriteViewAsync),
            ("ImfHeaderWriter.WriteTextFieldAsync", stream => token => new ImfHeaderWriter(stream).WriteTextFieldAsync(FieldName, "1", token).AsTask()),
            ("ImfHeaderWriter.WriteRawFieldAsync", stream => token => new ImfHeaderWriter(stream).WriteRawFieldAsync(Octets.Of("A: 1\r\n"), token).AsTask()),
            ("ImfHeaderWriter.WriteFieldsAsync", stream => token => new ImfHeaderWriter(stream).WriteFieldsAsync(CreateHeaderReader(), cancellationToken: token)),
            ("ImfEntityWriter.FlushAsync", stream => token => new ImfEntityWriter(stream).FlushAsync(token)),
            ("ImfEntityWriter.WriteMessageAsync", stream => token => new ImfEntityWriter(stream).WriteMessageAsync(CreateEntitySource(), cancellationToken: token)),
            ("ImfWriter.FlushAsync", stream => token => new ImfWriter(stream).FlushAsync(token)),
            ("ImfWriter.WriteMessageAsync", stream => token => new ImfWriter(stream).WriteMessageAsync(ImfMessages.Read(Rfc5322Messages.A11), cancellationToken: token)),
            ("EmlxWriter.FlushAsync", stream => token => new EmlxWriter(stream).FlushAsync(token)),
            ("EmlxWriter.WriteEntryAsync", stream => token => new EmlxWriter(stream).WriteEntryAsync(EmlxFiles.Read(EmlxFiles.Plain), cancellationToken: token)),
            ("MboxWriter.FlushAsync", stream => token => new MboxWriter(stream).FlushAsync(token)),
            ("MboxWriter.WriteMessageAsync", stream => token => new MboxWriter(stream).WriteMessageAsync(Envelope, ImfMessages.Read(Rfc5322Messages.A11), token)),
            ("MboxWriter.WriteMessagesAsync", stream => token => new MboxWriter(stream).WriteMessagesAsync(new[] { new MboxEntry(Envelope, ImfMessages.Read(Rfc5322Messages.A11)) }, cancellationToken: token)),
        };

        // Each writer operation, with each way the stream can report a failure, each OperationCanceledException it can fail with while the
        // caller's token isn't canceled, and the exception the task then faults with.  A canceled task carries no exception of its own, so
        // awaiting it gives a TaskCanceledException, whatever exception the stream was given.
        public static TheoryData<string, Func<Stream, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type> WriterStreamCancellationFaults()
        {
            var data = new TheoryData<string, Func<Stream, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type>();
            foreach (var (name, create) in WriterOperations)
            {
                foreach (var timing in FailureTimings)
                {
                    foreach (var (exceptionName, createException) in UnrequestedCancellations)
                    {
                        var expectedType = timing == FailureTiming.AsCanceledTask ? typeof(TaskCanceledException) : typeof(OperationCanceledException);
                        data.Add(name + ", " + timing + ", " + exceptionName, create, timing, createException, expectedType);
                    }
                }
            }

            return data;
        }

        // Each writer operation, with each way the stream can report a failure.
        public static TheoryData<string, Func<Stream, Func<CancellationToken, Task>>, FailureTiming> WriterStreamFailures()
        {
            var data = new TheoryData<string, Func<Stream, Func<CancellationToken, Task>>, FailureTiming>();
            foreach (var (name, create) in WriterOperations)
            {
                foreach (var timing in FailureTimings)
                {
                    data.Add(name + ", " + timing, create, timing);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(WriterStreamCancellationFaults), DisableDiscoveryEnumeration = true)]
        public async Task AsyncWrite_StreamFailsWithOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<Stream, Func<CancellationToken, Task>> create, FailureTiming timing, Func<Exception> createException, Type expectedExceptionType)
        {
            // arrange
            _ = scenario;
            var run = create(new TimedFailingWriteStream(createException, timing));

            // act: wait for the operation to finish without throwing, so that its status can be checked.
            var task = run(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType(expectedExceptionType);
        }

        [Theory]
        [MemberData(nameof(WriterStreamFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncWrite_TokenCanceledAndStreamFailsWithCancellationOfAnotherToken_CancelsTask(string scenario,
            Func<Stream, Func<CancellationToken, Task>> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var stream = new TimedFailingWriteStream(() => new OperationCanceledException(new CancellationToken(true)), timing) { OnFailure = cancellationTokenSource.Cancel };
                var run = create(stream);

                // act: wait for the operation to finish without throwing, so that its status can be checked.
                var task = run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        // Each writer operation that takes a progress callback, given the callback, over a stream that takes every write.  Each also gives
        // a function that writes to the same writer again, synchronously.
        private static readonly (string Name, Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)> Create)[] ProgressOperations =
        {
            ("ImfHeaderWriter.WriteFieldsAsync", progress =>
            {
                var writer = new ImfHeaderWriter(new MemoryStream());
                return (token => writer.WriteFieldsAsync(CreateHeaderReader(), progress, token), () => writer.WriteEndOfHeader());
            }),
            ("ImfEntityWriter.WriteMessageAsync", progress =>
            {
                var writer = new ImfEntityWriter(new MemoryStream());
                return (token => writer.WriteMessageAsync(CreateEntitySource(), progress, token), () => writer.WriteMessage(CreateEntitySource()));
            }),
            ("ImfWriter.WriteMessageAsync", progress =>
            {
                var writer = new ImfWriter(new MemoryStream());
                return (token => writer.WriteMessageAsync(ImfMessages.Read(Rfc5322Messages.A11), progress, token), () => writer.WriteMessage(ImfMessages.Read(Rfc5322Messages.A11)));
            }),
            ("EmlxWriter.WriteEntryAsync", progress =>
            {
                var writer = new EmlxWriter(new MemoryStream());
                return (token => writer.WriteEntryAsync(EmlxFiles.Read(EmlxFiles.Plain), progress, token), () => writer.Flush());
            }),
            ("MboxWriter.WriteMessagesAsync", progress =>
            {
                var writer = new MboxWriter(new MemoryStream());
                return (token => writer.WriteMessagesAsync(new[] { new MboxEntry(Envelope, ImfMessages.Read(Rfc5322Messages.A11)) }, progress, token),
                    () => writer.WriteMessage(Envelope, ImfMessages.Read(Rfc5322Messages.A11)));
            }),
        };

        // Each writer operation that takes a progress callback.
        public static TheoryData<string, Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)>> ProgressFailures()
        {
            var data = new TheoryData<string, Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)>>();
            foreach (var (name, create) in ProgressOperations)
            {
                data.Add(name, create);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncWrite_ProgressThrowsOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)> create)
        {
            // arrange
            _ = scenario;
            var operation = create(new FailingProgress(null, () => new OperationCanceledException()));

            // act: wait for the operation to finish without throwing, so that its status can be checked.
            var task = operation.Run(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType<OperationCanceledException>();
        }

        [Fact]
        public async Task WriteEndOfHeader_AfterProgressOfWriteFieldsAsyncThrowsOperationCanceledWhileTokenIsNotCanceled_WritesTheEmptyLine()
        {
            // arrange
            var output = new MemoryStream();
            var writer = new ImfHeaderWriter(output);
            await Record.ExceptionAsync(() => writer.WriteFieldsAsync(CreateHeaderReader(), new FailingProgress(null, () => new OperationCanceledException())));

            // act
            writer.WriteEndOfHeader();

            // assert
            Octets.ToText(output.ToArray()).Should().Be(HeaderSection);
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncWrite_TokenCanceledAndProgressThrowsCancellationOfNoToken_CancelsTask(string scenario,
            Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(new FailingProgress(cancellationTokenSource.Cancel, () => new OperationCanceledException()));

                // act: wait for the operation to finish without throwing, so that its status can be checked.
                var task = operation.Run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task Write_AfterTokenCanceledAndProgressThrowsCancellationOfNoToken_ThrowsInvalidOperation(string scenario,
            Func<IProgress<ImfWriteProgress>, (Func<CancellationToken, Task> Run, Action WriteAgain)> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(new FailingProgress(cancellationTokenSource.Cancel, () => new OperationCanceledException()));
                await Record.ExceptionAsync(() => operation.Run(cancellationTokenSource.Token));

                // act
                Action testAction = operation.WriteAgain;

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
            }
        }

        #endregion

        #region Readers

        // Each asynchronous read of each reader, over the given stream, which holds the data the reader expects.
        private static readonly (string Name, string Data, Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)> Create)[] ReaderOperations =
        {
            ("ImfHeaderReader.ReadFieldAsync", HeaderSection, stream =>
            {
                var reader = new ImfHeaderReader(stream);
                return (token => reader.ReadFieldAsync(token).AsTask(), () => reader.ReadFieldAsync().AsTask());
            }),
            ("ImfEntityReader.ReadEventAsync", Message, stream =>
            {
                // The event that starts the message needs no data, so the read after it is the first to reach the stream.
                var reader = new ImfEntityReader(stream);
                reader.ReadEvent();
                return (token => reader.ReadEventAsync(token).AsTask(), () => reader.ReadEventAsync().AsTask());
            }),
            ("MboxReader.ReadMessageAsync", Mailbox, stream =>
            {
                var reader = new MboxReader(stream);
                return (token => reader.ReadMessageAsync(token), () => reader.ReadMessageAsync());
            }),
            ("MboxReader.ReadEntityEventsAsync", Mailbox, stream =>
            {
                var reader = new MboxReader(stream);
                return (token => reader.ReadEntityEventsAsync(token), () => reader.ReadMessageAsync());
            }),
        };

        // Each read, with each way the stream can report a failure, each OperationCanceledException it can fail with while the caller's token
        // isn't canceled, and the exception the task then faults with.
        public static TheoryData<string, string, Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)>, FailureTiming, Func<Exception>, Type> ReaderStreamCancellationFaults()
        {
            var data = new TheoryData<string, string, Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)>, FailureTiming, Func<Exception>, Type>();
            foreach (var (name, text, create) in ReaderOperations)
            {
                foreach (var timing in FailureTimings)
                {
                    foreach (var (exceptionName, createException) in UnrequestedCancellations)
                    {
                        var expectedType = timing == FailureTiming.AsCanceledTask ? typeof(TaskCanceledException) : typeof(OperationCanceledException);
                        data.Add(name + ", " + timing + ", " + exceptionName, text, create, timing, createException, expectedType);
                    }
                }
            }

            return data;
        }

        // Each read, with each way the stream can report a failure.
        public static TheoryData<string, string, Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)>, FailureTiming> ReaderStreamFailures()
        {
            var data = new TheoryData<string, string, Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)>, FailureTiming>();
            foreach (var (name, text, create) in ReaderOperations)
            {
                foreach (var timing in FailureTimings)
                {
                    data.Add(name + ", " + timing, text, create, timing);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ReaderStreamCancellationFaults), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_StreamFailsWithOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario, string text,
            Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)> create, FailureTiming timing, Func<Exception> createException,
            Type expectedExceptionType)
        {
            // arrange
            _ = scenario;
            var operation = create(new TimedFailingReadStream(Octets.Of(text), createException, timing));

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var task = operation.Run(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType(expectedExceptionType);
        }

        [Theory]
        [MemberData(nameof(ReaderStreamFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_AfterStreamFailsWithOperationCanceledWhileTokenIsNotCanceled_ReadsTheData(string scenario, string text,
            Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var operation = create(new TimedFailingReadStream(Octets.Of(text), () => new OperationCanceledException(), timing));
            await Record.ExceptionAsync(() => operation.Run(CancellationToken.None));

            // act
            Func<Task> testAction = operation.ReadAgain;

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Theory]
        [MemberData(nameof(ReaderStreamFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_TokenCanceledAndStreamFailsWithCancellationOfAnotherToken_CancelsTask(string scenario, string text,
            Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var stream = new TimedFailingReadStream(Octets.Of(text), () => new OperationCanceledException(new CancellationToken(true)), timing);
            var operation = create(stream);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                stream.OnFailure = cancellationTokenSource.Cancel;

                // act: wait for the read to finish without throwing, so that its status can be checked.
                var task = operation.Run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(ReaderStreamFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_AfterTokenCanceledAndStreamFailsWithCancellationOfNoToken_ThrowsInvalidOperation(string scenario, string text,
            Func<Stream, (Func<CancellationToken, Task> Run, Func<Task> ReadAgain)> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var stream = new TimedFailingReadStream(Octets.Of(text), () => new OperationCanceledException(), timing);
            var operation = create(stream);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                stream.OnFailure = cancellationTokenSource.Cancel;
                await Record.ExceptionAsync(() => operation.Run(cancellationTokenSource.Token));

                // act
                Func<Task> testAction = operation.ReadAgain;

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>().WithMessage(CanceledMessage);
            }
        }

        #endregion

        private static ImfHeaderReader CreateHeaderReader()
        {
            return new ImfHeaderReader(new MemoryStream(Octets.Of(HeaderSection)));
        }

        private static ImfEntitySource CreateEntitySource()
        {
            return ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("body\r\n"));
        }

        // A WriteFieldAsync of a field that a reader has just read.
        private static Func<CancellationToken, Task> CreateWriteViewAsync(Stream stream)
        {
            var reader = CreateHeaderReader();
            var field = reader.ReadField().Value;
            var writer = new ImfHeaderWriter(stream);
            return token => writer.WriteFieldAsync(field, token).AsTask();
        }

        /// <summary>
        /// A progress callback that runs an action, if there is one, then throws the exception a function creates.
        /// </summary>
        private sealed class FailingProgress : IProgress<ImfWriteProgress>
        {
            private readonly Func<Exception> _createException;
            private readonly Action _onReport;

            public FailingProgress(Action onReport, Func<Exception> createException)
            {
                _onReport = onReport;
                _createException = createException;
            }

            public void Report(ImfWriteProgress value)
            {
                _onReport?.Invoke();
                throw _createException();
            }
        }
    }
}
