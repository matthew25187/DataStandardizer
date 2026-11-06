#if !NET9_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The ordered, string-keyed field store of a CSV line on the targets that have no generic
    /// <c>OrderedDictionary&lt;TKey, TValue&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The members are the subset of <c>OrderedDictionary&lt;string, TValue&gt;</c> that <see cref="CsvFileLineBase"/> uses, and
    /// each one behaves as that type's member does: the same results, the same exceptions with the same parameter names, checked in
    /// the same order, and the same rules for when an enumerator is invalidated.  Replacing the value of a field in place doesn't
    /// invalidate an enumerator, and neither does removing a field that isn't present or clearing a store that is already empty.
    /// </para>
    /// <para>
    /// The entries array holds the fields in position order and is the source of truth for position.  The name lookup maps each
    /// field name to its position, and is updated for the fields after the position that <see cref="Insert"/> or
    /// <see cref="RemoveAt"/> changes.
    /// </para>
    /// <para>
    /// The value type is a type parameter so that the store needs no nullable annotations, which the C# 7.3 builds can't use.
    /// </para>
    /// </remarks>
    internal sealed class CsvFieldStore<TValue> : IEnumerable<KeyValuePair<string, TValue>>
    {
        private const int DefaultCapacity = 4;

        private readonly Dictionary<string, int> _positions = new Dictionary<string, int>();
        private Entry[] _entries = new Entry[0];
        private int _count;
        private int _version;

        // Created when first asked for, as most lines are never asked for them.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private KeyCollection? _keys;
        private ValueCollection? _values;
#else
        private KeyCollection _keys;
        private ValueCollection _values;
#endif

        public int Count => _count;

        public IReadOnlyList<string> Keys => _keys ?? (_keys = new KeyCollection(this));

        public IReadOnlyList<TValue> Values => _values ?? (_values = new ValueCollection(this));

        public TValue this[string key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException();
                }

                return value;
            }
            set
            {
                if (_positions.TryGetValue(key, out var index))
                {
                    _entries[index].Value = value;
                    return;
                }

                AddEntry(key, value);
            }
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _entries.Length)
            {
                Array.Resize(ref _entries, capacity);
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            _positions.EnsureCapacity(capacity);
#endif
        }

        public KeyValuePair<string, TValue> GetAt(int index)
        {
            CheckIndex(index);
            return new KeyValuePair<string, TValue>(_entries[index].Key, _entries[index].Value);
        }

        public void SetAt(int index, TValue value)
        {
            CheckIndex(index);
            _entries[index].Value = value;
        }

        public int IndexOf(string key)
        {
            CheckKey(key);
            return _positions.TryGetValue(key, out var index) ? index : -1;
        }

        public bool ContainsKey(string key)
        {
            CheckKey(key);
            return _positions.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
        {
            CheckKey(key);
            if (_positions.TryGetValue(key, out var index))
            {
                value = _entries[index].Value;
                return true;
            }

            value = default;
            return false;
        }

        public void Add(string key, TValue value)
        {
            CheckKey(key);
            if (_positions.ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }

            AddEntry(key, value);
        }

        public void Insert(int index, string key, TValue value)
        {
            if ((uint)index > (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            CheckKey(key);
            if (_positions.ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }

            Grow();
            Array.Copy(_entries, index, _entries, index + 1, _count - index);
            _entries[index].Key = key;
            _entries[index].Value = value;
            _count++;
            _version++;

            _positions.Add(key, index);
            UpdatePositionsFrom(index + 1);
        }

        public bool Remove(string key)
        {
            CheckKey(key);
            if (!_positions.TryGetValue(key, out var index))
            {
                return false;
            }

            RemoveEntryAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            CheckIndex(index);
            RemoveEntryAt(index);
        }

        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }

            Array.Clear(_entries, 0, _count);
            _count = 0;
            _positions.Clear();
            _version++;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void CheckKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private void AddEntry(string key, TValue value)
        {
            Grow();
            _entries[_count].Key = key;
            _entries[_count].Value = value;
            _positions.Add(key, _count);
            _count++;
            _version++;
        }

        private void RemoveEntryAt(int index)
        {
            _positions.Remove(_entries[index].Key);
            _count--;
            Array.Copy(_entries, index + 1, _entries, index, _count - index);
            _entries[_count] = default(Entry);
            _version++;

            // Removal is rare on a CSV line, so the positions of the later fields are updated one by one.
            UpdatePositionsFrom(index);
        }

        private void UpdatePositionsFrom(int index)
        {
            for (var position = index; position < _count; position++)
            {
                _positions[_entries[position].Key] = position;
            }
        }

        private void Grow()
        {
            if (_count == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length == 0 ? DefaultCapacity : _entries.Length * 2);
            }
        }

        private struct Entry
        {
            public string Key;
            public TValue Value;
        }

        /// <summary>
        /// Enumerates the fields in position order.
        /// </summary>
        internal struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
        {
            private readonly CsvFieldStore<TValue> _store;
            private readonly int _version;
            private int _index;
            private KeyValuePair<string, TValue> _current;

            internal Enumerator(CsvFieldStore<TValue> store)
            {
                _store = store;
                _version = store._version;
                _index = 0;
                _current = default(KeyValuePair<string, TValue>);
            }

            public KeyValuePair<string, TValue> Current => _current;

            object IEnumerator.Current => _current;

            public bool MoveNext()
            {
                CheckVersion(_store, _version);
                if (_index < _store._count)
                {
                    _current = new KeyValuePair<string, TValue>(_store._entries[_index].Key, _store._entries[_index].Value);
                    _index++;
                    return true;
                }

                _current = default(KeyValuePair<string, TValue>);
                return false;
            }

            public void Reset()
            {
                CheckVersion(_store, _version);
                _index = 0;
                _current = default(KeyValuePair<string, TValue>);
            }

            public void Dispose()
            {
            }
        }

        private static void CheckVersion(CsvFieldStore<TValue> store, int version)
        {
            if (version != store._version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }
        }

        /// <summary>
        /// The field names, in position order.
        /// </summary>
        private sealed class KeyCollection : IReadOnlyList<string>
        {
            private readonly CsvFieldStore<TValue> _store;

            internal KeyCollection(CsvFieldStore<TValue> store)
            {
                _store = store;
            }

            public int Count => _store._count;

            public string this[int index]
            {
                get
                {
                    _store.CheckIndex(index);
                    return _store._entries[index].Key;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                var version = _store._version;
                for (var index = 0; index < _store._count; index++)
                {
                    CheckVersion(_store, version);
                    yield return _store._entries[index].Key;
                }

                CheckVersion(_store, version);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// The field values, in position order.
        /// </summary>
        private sealed class ValueCollection : IReadOnlyList<TValue>
        {
            private readonly CsvFieldStore<TValue> _store;

            internal ValueCollection(CsvFieldStore<TValue> store)
            {
                _store = store;
            }

            public int Count => _store._count;

            public TValue this[int index]
            {
                get
                {
                    _store.CheckIndex(index);
                    return _store._entries[index].Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                var version = _store._version;
                for (var index = 0; index < _store._count; index++)
                {
                    CheckVersion(_store, version);
                    yield return _store._entries[index].Value;
                }

                CheckVersion(_store, version);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
#endif
