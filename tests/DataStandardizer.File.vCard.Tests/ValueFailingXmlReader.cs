using System.Xml;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// An XML reader that reads another, except that the value of a text node throws <see cref="XmlException"/>, as an XML reader that parses the rest of a text node only when
    /// its value is asked for can.
    /// </summary>
    internal sealed class ValueFailingXmlReader : XmlReader
    {
        private readonly XmlReader _inner;

        public ValueFailingXmlReader(XmlReader inner)
        {
            _inner = inner;
        }

        public override int AttributeCount => _inner.AttributeCount;

        public override string BaseURI => _inner.BaseURI;

        public override int Depth => _inner.Depth;

        public override bool EOF => _inner.EOF;

        public override bool IsEmptyElement => _inner.IsEmptyElement;

        public override string LocalName => _inner.LocalName;

        public override string NamespaceURI => _inner.NamespaceURI;

        public override XmlNameTable NameTable => _inner.NameTable;

        public override XmlNodeType NodeType => _inner.NodeType;

        public override string Prefix => _inner.Prefix;

        public override ReadState ReadState => _inner.ReadState;

        public override string Value
        {
            get
            {
                if (_inner.NodeType == XmlNodeType.Text) throw new XmlException("The value of the text node is not well-formed.");

                return _inner.Value;
            }
        }

        public override string GetAttribute(int i) => _inner.GetAttribute(i);

        public override string GetAttribute(string name) => _inner.GetAttribute(name);

        public override string GetAttribute(string name, string namespaceURI) => _inner.GetAttribute(name, namespaceURI);

        public override string LookupNamespace(string prefix) => _inner.LookupNamespace(prefix);

        public override bool MoveToAttribute(string name) => _inner.MoveToAttribute(name);

        public override bool MoveToAttribute(string name, string ns) => _inner.MoveToAttribute(name, ns);

        public override bool MoveToElement() => _inner.MoveToElement();

        public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();

        public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();

        public override bool Read() => _inner.Read();

        public override bool ReadAttributeValue() => _inner.ReadAttributeValue();

        public override void ResolveEntity() => _inner.ResolveEntity();
    }
}
