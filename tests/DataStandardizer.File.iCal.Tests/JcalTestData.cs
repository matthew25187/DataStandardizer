using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// The examples of RFC 7265, and helpers to read and write jCal for the tests.
    /// </summary>
    internal static class JcalTestData
    {
        /// <summary>
        /// RFC 7265 Appendix B.1, in iCalendar form; the same as RFC 6321's.
        /// </summary>
        public static readonly string Example1Ical = XcalTestData.Example1Ical;

        /// <summary>
        /// RFC 7265 Appendix B.1, in jCal form.  Its iCalendar form is <see cref="XcalTestData.Example1Ical"/>: the RFC's iCalendar form has
        /// <c>DTSTART:20081006</c> with no <c>VALUE=DATE</c>, which RFC 5545 requires for a DATE, and the jCal form's <c>date</c> type is followed.
        /// </summary>
        public const string Example1Jcal =
            @"[""vcalendar"",
  [
    [""calscale"", {}, ""text"", ""GREGORIAN""],
    [""prodid"", {}, ""text"", ""-//Example Inc.//Example Calendar//EN""],
    [""version"", {}, ""text"", ""2.0""]
  ],
  [
    [""vevent"",
      [
        [""dtstamp"", {}, ""date-time"", ""2008-02-05T19:12:24Z""],
        [""dtstart"", {}, ""date"", ""2008-10-06""],
        [""summary"", {}, ""text"", ""Planning meeting""],
        [""uid"", {}, ""text"", ""4088E990AD89CB3DBB484909""]
      ],
      []
    ]
  ]
]";

        /// <summary>
        /// RFC 7265 Appendix B.2, in iCalendar form.  It is <see cref="XcalTestData.Example2Ical"/>, which corrects the RFC 6321 example, with the
        /// PRODID that RFC 7265 gives.
        /// </summary>
        public static readonly string Example2Ical = XcalTestData.Example2Ical.Replace("-//Example Inc.//Example Client//EN", "-//Example Corp.//Example Client//EN");

        /// <summary>
        /// RFC 7265 Appendix B.2, in jCal form.
        /// </summary>
        /// <remarks>
        /// The RFC's jCal form differs from its iCalendar form in four places, all corrected here to follow the iCalendar form: the STANDARD
        /// RRULE has <c>"byday": "1SU"</c> for <c>BYDAY=-1SU</c>; the second VEVENT has the DTSTART of the first, <c>2006-01-02T14:00:00</c> for
        /// <c>20060104T140000</c>, and the SUMMARY <c>Event #2</c> for <c>Event #2 bis</c>.  It also splits the DESCRIPTION over several lines
        /// with <c>+</c> and comments, which JSON does not allow.  Its RDATE gives the PERIOD as one string, <c>"2006-01-02T15:00:00/PT2H"</c>,
        /// not the array that RFC 7265 §3.6.9 requires; that is kept, since the reader accepts it.
        /// </remarks>
        public const string Example2Jcal =
            @"[""vcalendar"",
  [
    [""prodid"", {}, ""text"", ""-//Example Corp.//Example Client//EN""],
    [""version"", {}, ""text"", ""2.0""]
  ],
  [
    [""vtimezone"",
      [
        [""last-modified"", {}, ""date-time"", ""2004-01-10T03:28:45Z""],
        [""tzid"", {}, ""text"", ""US/Eastern""]
      ],
      [
        [""daylight"",
          [
            [""dtstart"", {}, ""date-time"", ""2000-04-04T02:00:00""],
            [""rrule"",
              {},
              ""recur"",
              {
                ""freq"": ""YEARLY"",
                ""byday"": ""1SU"",
                ""bymonth"": 4
              }
            ],
            [""tzname"", {}, ""text"", ""EDT""],
            [""tzoffsetfrom"", {}, ""utc-offset"", ""-05:00""],
            [""tzoffsetto"", {}, ""utc-offset"", ""-04:00""]
          ],
          []
        ],
        [""standard"",
          [
            [""dtstart"", {}, ""date-time"", ""2000-10-26T02:00:00""],
            [""rrule"",
              {},
              ""recur"",
              {
                ""freq"": ""YEARLY"",
                ""byday"": ""-1SU"",
                ""bymonth"": 10
              }
            ],
            [""tzname"", {}, ""text"", ""EST""],
            [""tzoffsetfrom"", {}, ""utc-offset"", ""-04:00""],
            [""tzoffsetto"", {}, ""utc-offset"", ""-05:00""]
          ],
          []
        ]
      ]
    ],
    [""vevent"",
      [
        [""dtstamp"", {}, ""date-time"", ""2006-02-06T00:11:21Z""],
        [""dtstart"",
          { ""tzid"": ""US/Eastern"" },
          ""date-time"",
          ""2006-01-02T12:00:00""
        ],
        [""duration"", {}, ""duration"", ""PT1H""],
        [""rrule"", {}, ""recur"", { ""freq"": ""DAILY"", ""count"": 5 } ],
        [""rdate"",
          { ""tzid"": ""US/Eastern"" },
          ""period"",
          ""2006-01-02T15:00:00/PT2H""
        ],
        [""summary"", {}, ""text"", ""Event #2""],
        [""description"",
         {},
         ""text"",
         ""We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.\nPlease bring your own lunch for the 12 pm meetings.""
        ],
        [""uid"", {}, ""text"", ""00959BC664CA650E933C892C@example.com""]
      ],
      []
    ],
    [""vevent"",
      [
        [""dtstamp"", {}, ""date-time"", ""2006-02-06T00:11:21Z""],
        [""dtstart"",
          { ""tzid"": ""US/Eastern"" },
          ""date-time"",
          ""2006-01-04T14:00:00""
        ],
        [""duration"", {}, ""duration"", ""PT1H""],
        [""recurrence-id"",
          { ""tzid"": ""US/Eastern"" },
          ""date-time"",
          ""2006-01-04T12:00:00""
        ],
        [""summary"", {}, ""text"", ""Event #2 bis""],
        [""uid"", {}, ""text"", ""00959BC664CA650E933C892C@example.com""]
      ],
      []
    ]
  ]
]";

        /// <summary>
        /// <see cref="Example2Jcal"/> with its RDATE PERIOD as the array that RFC 7265 §3.6.9 requires, as a writer writes it.
        /// </summary>
        public static readonly string Example2JcalWithPeriodArray =
            Example2Jcal.Replace("\"2006-01-02T15:00:00/PT2H\"", "[\"2006-01-02T15:00:00\", \"PT2H\"]");

        /// <summary>
        /// Wraps properties and components in the component array of one calendar.
        /// </summary>
        public static string Document(string properties, string components = "")
        {
            return "[\"vcalendar\",[" + properties + "],[" + components + "]]";
        }

        /// <summary>
        /// Builds a jCal document holding one calendar with the required properties and one VJOURNAL holding the given property arrays.
        /// </summary>
        public static string JournalDocument(string journalProperties)
        {
            return Document(
                "[\"prodid\",{},\"text\",\"-//Test//Test//EN\"],[\"version\",{},\"text\",\"2.0\"]",
                "[\"vjournal\",[[\"uid\",{},\"text\",\"j@example.com\"],[\"dtstamp\",{},\"date-time\",\"2026-01-01T00:00:00Z\"]" +
                (journalProperties.Length == 0 ? string.Empty : "," + journalProperties) + "],[]]");
        }

        public static List<IcalCalendar> ReadJcal(string json, IcalOptions options = null)
        {
            using (var reader = new JcalReader(new MemoryStream(Utf8(json)), options ?? new IcalOptions()))
            {
                return reader.ReadCalendars().ToList();
            }
        }

        public static string WriteJcal(IEnumerable<IcalCalendar> calendars, IcalOptions options = null)
        {
            var stream = new MemoryStream();
            using (var writer = new JcalWriter(stream, options ?? new IcalOptions()))
            {
                writer.WriteCalendars(calendars);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Gets the one property of the VJOURNAL in a calendar built by <see cref="JournalDocument"/>, after the UID and DTSTAMP.
        /// </summary>
        public static IcalContentLine JournalProperty(IcalCalendar calendar)
        {
            return calendar.Journals.Single().Properties[2];
        }

        /// <summary>
        /// Gets the content lines of calendars; see <see cref="XcalTestData.Model"/>.
        /// </summary>
        public static List<string> Model(IEnumerable<IcalCalendar> calendars, IcalOptions options = null) => XcalTestData.Model(calendars, options);

        public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// A stream that returns at most a few bytes from each synchronous read, so that a reader must refill its buffer many times, and JSON tokens
    /// are split across reads.
    /// </summary>
    internal sealed class TrickleStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _maxReadLength;
        private int _position;

        public TrickleStream(byte[] data, int maxReadLength)
        {
            _data = data;
            _maxReadLength = maxReadLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public bool IsDisposed { get; private set; }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = Math.Min(Math.Min(count, _maxReadLength), _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, length);
            _position += length;
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A stream whose asynchronous reads wait until the test opens a gate, so a read can be held in progress.  Its synchronous reads throw.
    /// </summary>
    internal sealed class GatedStream : Stream
    {
        private readonly byte[] _data;

        public GatedStream(byte[] data)
        {
            _data = data;
        }

        private readonly TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _position;
        private readonly TaskCompletionSource<bool> _readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets a task that completes when a read starts waiting at the gate.
        /// </summary>
        public Task ReadStarted => _readStarted.Task;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Lets waiting and later reads through.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Synchronous read on the async path.");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#else
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#endif
        {
            _readStarted.TrySetResult(true);
            await TestTasks.WaitAsync(_gate.Task, cancellationToken);
            var count = Math.Min(buffer.Length, _data.Length - _position);
            _data.AsSpan(_position, count).CopyTo(buffer.Span);
            _position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A stream whose asynchronous flushes wait until the test opens a gate, so a flush can be held in progress.  Its synchronous members throw.
    /// </summary>
    internal sealed class GatedWriteStream : Stream
    {
        private readonly MemoryStream _data = new MemoryStream();
        private readonly TaskCompletionSource<bool> _flushStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets a task that completes when a flush starts waiting at the gate.
        /// </summary>
        public Task FlushStarted => _flushStarted.Task;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public byte[] ToArray() => _data.ToArray();

        /// <summary>
        /// Lets waiting and later flushes through.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        public override void Flush()
        {
            throw new InvalidOperationException("Synchronous flush on the async path.");
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            _flushStarted.TrySetResult(true);
            await TestTasks.WaitAsync(_gate.Task, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Synchronous write on the async path.");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _data.Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }
}
