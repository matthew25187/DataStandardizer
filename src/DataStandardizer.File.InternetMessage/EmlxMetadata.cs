using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The metadata of an EMLX file, from the XML property list that ends it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every key and value is kept, in order, whether or not this package knows it.  Metadata that was read and hasn't changed is written back byte for byte.  A change writes the whole property list
    /// again, as Apple's tools lay it out, with the keys that weren't changed still in order.
    /// </para>
    /// <para>
    /// EMLX is proprietary and undocumented, so the well-known keys come from community documentation, and a key whose value isn't of the kind expected gives <c>null</c> from its property.  The value
    /// itself is still in <see cref="Entries"/>.  If a key is repeated, a lookup gives the last, as Apple's parser does, and all of them are kept.
    /// </para>
    /// </remarks>
    public sealed class EmlxMetadata
    {
        private const string DateLastViewedKey = "date-last-viewed";
        private const string DateReceivedKey = "date-received";
        private const string FlagsKey = "flags";
        private const string RemoteIdKey = "remote-id";
        private const string SubjectKey = "subject";

        // ref. the EMLX import code of Thunderbird: bits 10 to 15 hold the number of attachments, and bits 16 to 22 the priority.
        private const int AttachmentCountShift = 10;
        private const long AttachmentCountMask = 0x3F;
        private const int PriorityLevelShift = 16;
        private const long PriorityLevelMask = 0x7F;

        private const EmlxMessageFlags KnownFlags = EmlxMessageFlags.Read | EmlxMessageFlags.Deleted | EmlxMessageFlags.Answered | EmlxMessageFlags.Encrypted | EmlxMessageFlags.Flagged
            | EmlxMessageFlags.Recent | EmlxMessageFlags.Draft | EmlxMessageFlags.Forwarded | EmlxMessageFlags.Redirected | EmlxMessageFlags.Signed | EmlxMessageFlags.Junk | EmlxMessageFlags.NotJunk;

        // The range of seconds since 1970 that a DateTimeOffset can hold.
        private const long MinUnixSeconds = -62135596800;
        private const long MaxUnixSeconds = 253402300799;
        private const long TicksPerSecond = 10000000;

        private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly List<KeyValuePair<string, EmlxPlistValue>> _entries;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private byte[]? _rawBytes;
#else
        private byte[] _rawBytes;
#endif

        /// <summary>
        /// Creates metadata with no entries.
        /// </summary>
        public EmlxMetadata() : this(new List<KeyValuePair<string, EmlxPlistValue>>(), null, true)
        {
        }

        private EmlxMetadata(List<KeyValuePair<string, EmlxPlistValue>> entries, [AllowNull] byte[] rawBytes, bool isWellFormed)
        {
            _entries = entries;
            _rawBytes = rawBytes;
            IsWellFormed = isWellFormed;
            Entries = new ReadOnlyCollection<KeyValuePair<string, EmlxPlistValue>>(_entries);
        }

        /// <summary>
        /// Gets the keys and values, in order.  The list is read-only.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> Entries { get; }

        /// <summary>
        /// Gets a flag that is <c>false</c> if this came from a file whose property list was missing or not well formed, under <see cref="ImfOptions.AllowInvalidEmlxMetadata"/>.  Any change makes it
        /// <c>true</c>, and replaces those bytes.
        /// </summary>
        public bool IsWellFormed { get; private set; }

        /// <summary>
        /// Gets the bytes that were read, as they were, until a change is made.  It is empty if nothing was read, or the file had no property list.
        /// </summary>
        public ReadOnlyMemory<byte> RawBytes => _rawBytes is null ? default(ReadOnlyMemory<byte>) : new ReadOnlyMemory<byte>(_rawBytes);

        /// <summary>
        /// Gets or sets the whole integer of the <c>flags</c> key.  Setting <c>null</c> removes the key.
        /// </summary>
        public long? Flags
        {
            get => FindValue(FlagsKey)?.TryGetInt64(out var value) == true ? value : (long?)null;
            set => SetOrRemove(FlagsKey, value.HasValue ? EmlxPlistValue.FromInteger(value.Value) : null);
        }

        /// <summary>
        /// Gets the single-bit flags of the <c>flags</c> key that are known, or <c>null</c> if it has none.  These come from community documentation, as <see cref="EmlxMessageFlags"/> describes.  To
        /// change them, set <see cref="Flags"/>.
        /// </summary>
        public EmlxMessageFlags? MessageFlags
        {
            get
            {
                var flags = Flags;
                return flags.HasValue ? (EmlxMessageFlags)flags.Value & KnownFlags : (EmlxMessageFlags?)null;
            }
        }

        /// <summary>
        /// Gets the number of attachments that the <c>flags</c> key holds, or <c>null</c> if it has none.  This comes from community documentation.
        /// </summary>
        public int? AttachmentCount
        {
            get
            {
                var flags = Flags;
                return flags.HasValue ? (int)((flags.Value >> AttachmentCountShift) & AttachmentCountMask) : (int?)null;
            }
        }

        /// <summary>
        /// Gets the priority that the <c>flags</c> key holds, or <c>null</c> if it has none.  This comes from community documentation.
        /// </summary>
        public int? PriorityLevel
        {
            get
            {
                var flags = Flags;
                return flags.HasValue ? (int)((flags.Value >> PriorityLevelShift) & PriorityLevelMask) : (int?)null;
            }
        }

        /// <summary>
        /// Gets or sets the date and time in the <c>date-received</c> key, in UTC.  A value is read from a <c>date</c>, or from a number of seconds since 1970, and set as a whole number of seconds
        /// since 1970.  Setting <c>null</c> removes the key.
        /// </summary>
        public DateTimeOffset? DateReceived
        {
            get => GetDate(DateReceivedKey);
            set => SetDate(DateReceivedKey, value);
        }

        /// <summary>
        /// Gets or sets the date and time in the <c>date-last-viewed</c> key, in UTC.  A value is read from a <c>date</c>, or from a number of seconds since 1970, and set as a whole number of seconds
        /// since 1970.  Setting <c>null</c> removes the key.
        /// </summary>
        public DateTimeOffset? DateLastViewed
        {
            get => GetDate(DateLastViewedKey);
            set => SetDate(DateLastViewedKey, value);
        }

        /// <summary>
        /// Gets or sets the text of the <c>remote-id</c> key, which is the identifier of the message on its server, from a <c>string</c> or an <c>integer</c>.  Setting <c>null</c> removes the key.
        /// </summary>
        /// <exception cref="ArgumentException">On set: the value holds a character that XML doesn't allow.</exception>
        [AllowNull, MaybeNull]
        public string RemoteId
        {
            get
            {
                var value = FindValue(RemoteIdKey);
                return !(value is null) && (value.Kind == EmlxPlistValueKind.String || value.Kind == EmlxPlistValueKind.Integer) ? value.Text : null;
            }
            set => SetOrRemove(RemoteIdKey, value is null ? null : EmlxPlistValue.FromString(value));
        }

        /// <summary>
        /// Gets or sets the text of the <c>subject</c> key.  Setting <c>null</c> removes the key.
        /// </summary>
        /// <exception cref="ArgumentException">On set: the value holds a character that XML doesn't allow.</exception>
        [AllowNull, MaybeNull]
        public string Subject
        {
            get
            {
                var value = FindValue(SubjectKey);
                return !(value is null) && value.Kind == EmlxPlistValueKind.String ? value.Text : null;
            }
            set => SetOrRemove(SubjectKey, value is null ? null : EmlxPlistValue.FromString(value));
        }

        /// <summary>
        /// Gets the value of a key.
        /// </summary>
        /// <param name="key">The key, whose case matters.</param>
        /// <param name="value">Receives the value, or <c>null</c> if the key isn't there.</param>
        /// <returns><c>true</c> if the key is there.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out EmlxPlistValue value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            value = FindValue(key);
            return !(value is null);
        }

        /// <summary>
        /// Sets the value of a key.  A key that is there keeps its place, and any repeats of it go.  A new key goes at the end.
        /// </summary>
        /// <param name="key">The key, whose case matters.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> holds a character that XML doesn't allow.</exception>
        public void Set(string key, EmlxPlistValue value)
        {
            EmlxPlistValue.CheckText(key, nameof(key));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            MarkChanged();
            var entry = new KeyValuePair<string, EmlxPlistValue>(key, value);
            var index = _entries.FindIndex(existing => existing.Key == key);
            if (index < 0)
            {
                _entries.Add(entry);
                return;
            }

            _entries[index] = entry;
            for (var later = _entries.Count - 1; later > index; later--)
            {
                if (_entries[later].Key == key) _entries.RemoveAt(later);
            }
        }

        /// <summary>
        /// Removes a key, and any repeats of it.
        /// </summary>
        /// <param name="key">The key, whose case matters.</param>
        /// <returns><c>true</c> if the key was there.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool Remove(string key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (FindValue(key) is null) return false;

            MarkChanged();
            _entries.RemoveAll(existing => existing.Key == key);
            return true;
        }

        /// <summary>
        /// Makes metadata from a property list that is well formed.
        /// </summary>
        internal static EmlxMetadata FromRead(List<KeyValuePair<string, EmlxPlistValue>> entries, byte[] rawBytes)
        {
            return new EmlxMetadata(entries, rawBytes, true);
        }

        /// <summary>
        /// Makes metadata that keeps the bytes of a property list that is missing or not well formed.
        /// </summary>
        internal static EmlxMetadata FromInvalid(byte[] rawBytes)
        {
            return new EmlxMetadata(new List<KeyValuePair<string, EmlxPlistValue>>(), rawBytes, false);
        }

        /// <summary>
        /// Gets the bytes that a file holds for this.
        /// </summary>
        internal byte[] GetBytes()
        {
            return _rawBytes ?? EmlxPlist.Write(Entries);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private EmlxPlistValue? FindValue(string key)
#else
        private EmlxPlistValue FindValue(string key)
#endif
        {
            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                if (_entries[index].Key == key) return _entries[index].Value;
            }

            return null;
        }

        private void SetOrRemove(string key, [AllowNull] EmlxPlistValue value)
        {
            if (value is null) Remove(key);
            else Set(key, value);
        }

        private void MarkChanged()
        {
            _rawBytes = null;
            IsWellFormed = true;
        }

        private DateTimeOffset? GetDate(string key)
        {
            var value = FindValue(key);
            if (value is null) return null;

            if (value.TryGetDate(out var date)) return date;

            if (!value.TryGetDouble(out var seconds) || double.IsNaN(seconds) || seconds < MinUnixSeconds || seconds > MaxUnixSeconds) return null;

            return UnixEpoch.AddTicks((long)(seconds * TicksPerSecond));
        }

        // Rounds down, so a time before 1970 stays within its second.
        private static long ToUnixSeconds(DateTimeOffset value)
        {
            var ticks = (value - UnixEpoch).Ticks;
            var seconds = ticks / TicksPerSecond;
            return ticks % TicksPerSecond < 0 ? seconds - 1 : seconds;
        }

        private void SetDate(string key, DateTimeOffset? value)
        {
            SetOrRemove(key, value.HasValue ? EmlxPlistValue.FromInteger(ToUnixSeconds(value.Value)) : null);
        }
    }
}
