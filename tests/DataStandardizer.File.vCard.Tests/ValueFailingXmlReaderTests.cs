using System;
using System.IO;
using System.Xml;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="ValueFailingXmlReader"/>, which has logic of its own.
    /// </summary>
    public class ValueFailingXmlReaderTests
    {
        [Fact]
        public void Value_TextNode_ThrowsXmlException()
        {
            // arrange
            var reader = new ValueFailingXmlReader(XmlReader.Create(new StringReader("<a>b</a>")));
            reader.Read();
            reader.Read();

            // act
            Func<string> testAction = () => reader.Value;

            // assert
            testAction.Should().Throw<XmlException>();
        }

        [Fact]
        public void Value_AttributeNode_IsTheValueOfTheReaderItReads()
        {
            // arrange
            var reader = new ValueFailingXmlReader(XmlReader.Create(new StringReader("<a b=\"c\"/>")));
            reader.Read();
            reader.MoveToFirstAttribute();

            // act
            var testResult = reader.Value;

            // assert
            testResult.Should().Be("c");
        }
    }
}
