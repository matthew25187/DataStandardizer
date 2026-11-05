using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The components of a structured value whose components hold lists of text, as N and ADR do.  <see cref="VcardName"/> and <see cref="VcardAddress"/> are built on it.
    /// </summary>
    internal sealed class VcardComponentList
    {
        private static readonly ReadOnlyCollection<string> NoItems = new ReadOnlyCollection<string>(new string[0]);

        private readonly ReadOnlyCollection<string>[] _components;

        private VcardComponentList(ReadOnlyCollection<string>[] components)
        {
            _components = components;
        }

        internal IReadOnlyList<string> this[int index] => _components[index];

        /// <summary>
        /// Gets the number of components.
        /// </summary>
        internal int Count => _components.Length;

        /// <summary>
        /// Gets whether a component has an item with text in it.
        /// </summary>
        internal bool HasValueAt(int index)
        {
            foreach (var item in _components[index])
            {
                if (item.Length != 0) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether a component from a position on has an item with text in it.
        /// </summary>
        internal bool HasValueFrom(int start)
        {
            for (var index = start; index < _components.Length; index++)
            {
                if (HasValueAt(index)) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the items of a component that aren't in another, compared ordinally.
        /// </summary>
        internal IReadOnlyList<string> GetItemsNotIn(int index, int otherIndex)
        {
            var result = new List<string>();
            foreach (var item in _components[index])
            {
                if (!_components[otherIndex].Contains(item))
                {
                    result.Add(item);
                }
            }

            return new ReadOnlyCollection<string>(result);
        }

        /// <summary>
        /// Gets a list in which a component also has the items of another that it doesn't have already.  A list that wouldn't change is returned as it is.
        /// </summary>
        internal VcardComponentList WithItemsOf(int index, int sourceIndex)
        {
            var items = new List<string>(_components[index]);
            var isChanged = false;
            foreach (var item in _components[sourceIndex])
            {
                if (item.Length != 0 && !items.Contains(item))
                {
                    items.Add(item);
                    isChanged = true;
                }
            }

            return isChanged ? WithItems(index, items) : this;
        }

        /// <summary>
        /// Gets a list in which a component has the given items, which have been checked.
        /// </summary>
        internal VcardComponentList WithItems(int index, IList<string> items)
        {
            var components = (ReadOnlyCollection<string>[])_components.Clone();
            components[index] = new ReadOnlyCollection<string>(items);
            return new VcardComponentList(components);
        }

        /// <summary>
        /// Creates a list from the components a caller gave.
        /// </summary>
        /// <exception cref="ArgumentNullException">A component or one of its items is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An item holds a character that a value cannot hold.</exception>
        internal static VcardComponentList Create(params IEnumerable<string>[] components)
        {
            var result = new ReadOnlyCollection<string>[components.Length];
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] is null)
                    throw new ArgumentNullException(nameof(components), "A component is null.  Give an empty list for a component with no items.");

                var items = new List<string>(components[index]);
                foreach (var item in items)
                {
                    if (item is null)
                        throw new ArgumentNullException(nameof(components), "An item is null.");

                    VcardText.CheckCharacters(item.AsSpan(), nameof(components));
                }

                // A component with no text is an empty list, as parsing gives.
                result[index] = items.Count == 0 || (items.Count == 1 && items[0].Length == 0) ? NoItems : new ReadOnlyCollection<string>(items);
            }

            return new VcardComponentList(result);
        }

        /// <summary>
        /// Creates a list from a value that has been checked.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="hasLists"><c>true</c> if a comma separates the items of a component.</param>
        /// <param name="componentCount">The number of components to make, which is at least the number in the value.</param>
        internal static VcardComponentList Parse(ReadOnlySpan<char> value, VcardVersion version, bool hasLists, int componentCount)
        {
            var components = new ReadOnlyCollection<string>[componentCount];
            var rest = value;
            var isLastComponent = false;
            for (var index = 0; index < componentCount; index++)
            {
                if (isLastComponent)
                {
                    components[index] = NoItems;
                    continue;
                }

                var end = VcardText.IndexOfDelimiter(rest, ';', version);
                components[index] = ParseItems(rest.Slice(0, end), version, hasLists);
                isLastComponent = end == rest.Length;
                rest = isLastComponent ? default(ReadOnlySpan<char>) : rest.Slice(end + 1);
            }

            return new VcardComponentList(components);
        }

        /// <summary>
        /// Gets whether every component from <paramref name="start"/> on is empty.
        /// </summary>
        internal bool AreEmptyFrom(int start)
        {
            for (var index = start; index < _components.Length; index++)
            {
                if (_components[index].Count != 0) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets whether no component has more than one item.
        /// </summary>
        internal bool HasSingleItems
        {
            get
            {
                foreach (var component in _components)
                {
                    if (component.Count > 1) return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets whether vCard 2.1 can write every item.
        /// </summary>
        internal bool CanWriteInVersion21
        {
            get
            {
                foreach (var component in _components)
                {
                    foreach (var item in component)
                    {
                        if (!VcardText.CanWriteInVersion21Component(item)) return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the most characters <see cref="Append"/> can write.
        /// </summary>
        internal int MaxLength
        {
            get
            {
                var length = _components.Length;
                foreach (var component in _components)
                {
                    foreach (var item in component)
                    {
                        length += VcardText.GetMaxEscapedLength(item.Length) + 1;
                    }
                }

                return length;
            }
        }

        /// <summary>
        /// Appends the first components, with their delimiters.
        /// </summary>
        internal void Append(ref VcardSpanWriter writer, VcardVersion version, int componentCount)
        {
            for (var index = 0; index < componentCount; index++)
            {
                if (index != 0)
                {
                    writer.Append(';');
                }

                var component = _components[index];
                for (var itemIndex = 0; itemIndex < component.Count; itemIndex++)
                {
                    if (itemIndex != 0)
                    {
                        writer.Append(',');
                    }

                    VcardText.AppendEscaped(ref writer, component[itemIndex].AsSpan(), version, true);
                }
            }
        }

        internal bool EqualsList(VcardComponentList other)
        {
            if (_components.Length != other._components.Length) return false;

            for (var index = 0; index < _components.Length; index++)
            {
                var component = _components[index];
                var otherComponent = other._components[index];
                if (component.Count != otherComponent.Count) return false;

                for (var itemIndex = 0; itemIndex < component.Count; itemIndex++)
                {
                    if (!string.Equals(component[itemIndex], otherComponent[itemIndex], StringComparison.Ordinal)) return false;
                }
            }

            return true;
        }

        internal int GetListHashCode()
        {
            var hash = 17;
            foreach (var component in _components)
            {
                hash = hash * 31 + component.Count;
                foreach (var item in component)
                {
                    hash = hash * 31 + StringComparer.Ordinal.GetHashCode(item);
                }
            }

            return hash;
        }

        private static ReadOnlyCollection<string> ParseItems(ReadOnlySpan<char> component, VcardVersion version, bool hasLists)
        {
            if (component.Length == 0) return NoItems;

            var items = new List<string>();
            if (!hasLists)
            {
                items.Add(VcardText.Unescape(component, version));
            }
            else
            {
                foreach (var item in new VcardListEnumerator(component, false, false))
                {
                    items.Add(VcardText.Unescape(item, version));
                }
            }

            return new ReadOnlyCollection<string>(items);
        }
    }
}
