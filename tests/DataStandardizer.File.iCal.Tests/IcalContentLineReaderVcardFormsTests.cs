using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests that the forms the shared content-line engine has for vCard 2.1 stay off in File.iCal, which never turns them on (RFC 5545 §3.1).
    /// </summary>
    public class IcalContentLineReaderVcardFormsTests
    {
        // ref. RFC 5545 §3.1: a value may end in "=", and the next line is a line of its own.
        private const string LineEndingInEquals = "DESCRIPTION;ENCODING=QUOTED-PRINTABLE:a=\r\nSUMMARY:b\r\n";

        [Fact]
        public void ReadLines_LineEndingInEquals_IsNotJoinedToTheNextLine()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader(LineEndingInEquals)))
            {
                // act
                var testResult = reader.ReadLines().Select(line => line.Value).ToList();

                // assert
                testResult.Should().Equal("a=", "b");
            }
        }

        [Fact]
        public void ReadLine_ParameterWithNoValue_Throws()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader("TEL;HOME:1\r\n")))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<IcalFileException>();
            }
        }

        [Fact]
        public void ReadLine_Fold_RemovesOneWhiteSpaceCharacter()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader("A:b\r\n  c\r\n")))
            {
                // act
                var testResult = reader.ReadLine().Value;

                // assert
                testResult.Should().Be("b c");
            }
        }

        [Fact]
        public void ReadLines_ValueNamedBase64FollowedByAnotherLine_IsNotContinued()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader("ATTACH;ENCODING=BASE64:SGVsbG8=\r\nSUMMARY:b\r\n")))
            {
                // act
                var testResult = reader.ReadLines().Select(line => line.Value).ToList();

                // assert
                testResult.Should().Equal("SGVsbG8=", "b");
            }
        }
    }
}
