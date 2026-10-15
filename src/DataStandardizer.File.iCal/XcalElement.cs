using System.Collections.Generic;
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An element of an xCal document in the xCal namespace, with its child elements and text.  <see cref="XcalReader"/> builds one for each
    /// property it reads, and <see cref="XcalWriter"/> builds one for each calendar it writes.
    /// </summary>
    internal sealed class XcalElement
    {
        private static readonly List<XcalElement> NoChildren = new List<XcalElement>();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private List<XcalElement>? _children;
        private StringBuilder? _text;
#else
        private List<XcalElement> _children;
        private StringBuilder _text;
#endif
        private bool _hasNonWhitespaceText;

        internal XcalElement(string name) : this(name, 0, 0)
        {
        }

        internal XcalElement(string name, long lineNumber, long linePosition)
        {
            Name = name;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        /// <summary>
        /// Gets the child elements, in document order.
        /// </summary>
        internal IReadOnlyList<XcalElement> Children => _children ?? NoChildren;

        /// <summary>
        /// Gets a flag indicating if the element's text holds anything other than XML white space.
        /// </summary>
        internal bool HasNonWhitespaceText => _hasNonWhitespaceText;

        /// <summary>
        /// Gets the 1-based line of the XML document on which the element starts; or 0 if not known.
        /// </summary>
        internal long LineNumber { get; }

        /// <summary>
        /// Gets the 1-based position on its line at which the element starts; or 0 if not known.
        /// </summary>
        internal long LinePosition { get; }

        /// <summary>
        /// Gets the local name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the text the element holds directly, not counting that of its child elements.
        /// </summary>
        internal string Text => _text is null ? string.Empty : _text.ToString();

        /// <summary>
        /// Adds a child element.
        /// </summary>
        /// <returns>The child.</returns>
        internal XcalElement Add(XcalElement child)
        {
            if (_children is null)
            {
                _children = new List<XcalElement>();
            }

            _children.Add(child);
            return child;
        }

        /// <summary>
        /// Adds a child element holding text.
        /// </summary>
        /// <returns>The child.</returns>
        internal XcalElement Add(string name, string text)
        {
            var child = Add(new XcalElement(name));
            child.AppendText(text);
            return child;
        }

        internal void AppendText(string text)
        {
            if (text.Length == 0) return;

            if (_text is null)
            {
                _text = new StringBuilder(text);
            }
            else
            {
                _text.Append(text);
            }

            if (!_hasNonWhitespaceText)
            {
                foreach (var character in text)
                {
                    if (!IcalValueFormatting.IsXmlWhitespace(character))
                    {
                        _hasNonWhitespaceText = true;
                        break;
                    }
                }
            }
        }
    }
}
