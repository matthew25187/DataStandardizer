#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The kinds of JSON value.
    /// </summary>
    internal enum JcalNodeKind
    {
        String,
        Number,
        True,
        False,
        Null,
        Array,
        Object
    }

    /// <summary>
    /// A JSON value in a jCal document (RFC 7265): the values of a property as they are read, or as they are to be written.
    /// </summary>
    /// <remarks>
    /// A value read from a document knows its place in it, so that a problem can be reported with its JSON path and byte position.
    /// </remarks>
    internal sealed class JcalNode
    {
        private static readonly JcalNode[] NoItems = new JcalNode[0];
        private static readonly string[] NoNames = new string[0];

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private List<JcalNode>? _items;
        private List<string>? _names;
#else
        private List<JcalNode> _items;
        private List<string> _names;
#endif

        /// <summary>
        /// Creates a value to write.
        /// </summary>
        internal JcalNode(JcalNodeKind kind, string text) : this(kind, text, null, -1, null, 0)
        {
        }

        /// <summary>
        /// Creates a value read from a document.
        /// </summary>
        /// <param name="kind">The kind of value.</param>
        /// <param name="text">The text of a string, without its JSON escaping, or of a number; otherwise empty.</param>
        /// <param name="parent">The array or object that holds the value; or <c>null</c> for the root.</param>
        /// <param name="index">The 0-based index of the value in its array or object.</param>
        /// <param name="name">The name of the member, if the value is in an object; otherwise <c>null</c>.</param>
        /// <param name="bytePosition">The 0-based offset of the value's first byte from the start of the data.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal JcalNode(JcalNodeKind kind, string text, JcalNode? parent, int index, string? name, long bytePosition)
#else
        internal JcalNode(JcalNodeKind kind, string text, JcalNode parent, int index, string name, long bytePosition)
#endif
        {
            Kind = kind;
            Text = text;
            Parent = parent;
            Index = index;
            Name = name;
            BytePosition = bytePosition;
        }

        internal long BytePosition { get; }

        internal int Index { get; }

        /// <summary>
        /// Gets the values of an array or object, in order.
        /// </summary>
        internal IReadOnlyList<JcalNode> Items => _items is null ? NoItems : (IReadOnlyList<JcalNode>)_items;

        internal JcalNodeKind Kind { get; }

        /// <summary>
        /// Gets the name of the member, for a value in an object.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal string? Name { get; }
#else
        internal string Name { get; }
#endif

        /// <summary>
        /// Gets the names of the members of an object, in order.
        /// </summary>
        internal IReadOnlyList<string> Names => _names is null ? NoNames : (IReadOnlyList<string>)_names;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal JcalNode? Parent { get; }
#else
        internal JcalNode Parent { get; }
#endif

        internal string Text { get; }

        /// <summary>
        /// Adds a value to an array.
        /// </summary>
        /// <returns>The value.</returns>
        internal JcalNode Add(JcalNode item)
        {
            if (_items is null)
            {
                _items = new List<JcalNode>();
            }

            _items.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a member to an object.
        /// </summary>
        /// <returns>The value.</returns>
        internal JcalNode Add(string name, JcalNode item)
        {
            if (_names is null)
            {
                _names = new List<string>();
            }

            _names.Add(name);
            return Add(item);
        }

        /// <summary>
        /// Gets the JSONPath of the value, such as <c>$[1][2][0]</c> or <c>$[1][2][1]['tzid']</c>.
        /// </summary>
        internal string GetPath()
        {
            var parts = new List<string>();
            for (var node = this; node.Parent != null; node = node.Parent)
            {
                parts.Add(node.Name is null
                    ? string.Format(CultureInfo.InvariantCulture, "[{0}]", node.Index)
                    : "['" + node.Name.Replace("\\", "\\\\").Replace("'", "\\'") + "']");
            }

            var builder = new StringBuilder("$");
            for (var index = parts.Count - 1; index >= 0; index--)
            {
                builder.Append(parts[index]);
            }

            return builder.ToString();
        }
    }
}
#endif
