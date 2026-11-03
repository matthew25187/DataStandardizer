using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfHeaderField"/> and <see cref="ImfHeaderFieldCollection"/>.
    /// </summary>
    public class ImfHeaderFieldTests
    {
        private const string FoldedField = "Subject  : one\r\n two\r\n";
        private const string FoldedValue = " one two";
        private const string EncodedAcute = "=?UTF-8?Q?=C3=A9?=";
        private const string WrittenAcute = "=?UTF-8?B?w6k=?=";
        private const string EncodedSubjectField = "Subject: " + EncodedAcute + "\r\n";
        private const string Acute = "\x00E9";

        public static TheoryData<string> NotOneField => new TheoryData<string>
        {
            "",
            "\r\n",
            "A: 1",
            "A: 1\r\nB: 2\r\n",
            "A: 1\n",
            "no colon\r\n",
            "A: 1\r\n\r\n",
        };

        [Fact]
        public void FromRaw_FoldedField_GivesItsParts()
        {
            // act
            var testResult = ImfHeaderField.FromRaw(Octets.Of(FoldedField));

            // assert
            testResult.Name.Should().Be("Subject");
            testResult.Value.Should().Be(FoldedValue);
            Octets.ToText(testResult.ValueBytes.Span).Should().Be(FoldedValue);
            Octets.ToText(testResult.RawBytes.Span).Should().Be(FoldedField);
        }

        [Theory]
        [MemberData(nameof(NotOneField))]
        public void FromRaw_NotOneField_Throws(string rawField)
        {
            // act
            Action testAction = () => ImfHeaderField.FromRaw(Octets.Of(rawField));

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void FromRaw_BareLineFeedWithOption_KeepsIt()
        {
            // arrange
            var options = new ImfOptions { AcceptBareLineFeed = true };

            // act
            var testResult = ImfHeaderField.FromRaw(Octets.Of("A: 1\n"), options);

            // assert
            Octets.ToText(testResult.RawBytes.Span).Should().Be("A: 1\n");
        }

        [Fact]
        public void Constructor_View_CopiesTheField()
        {
            // arrange
            using (var reader = new ImfHeaderReader(new MemoryStream(Octets.Of(FoldedField))))
            {
                var view = reader.ReadField().Value;

                // act
                var testResult = new ImfHeaderField(view);

                // assert
                testResult.Name.Should().Be("Subject");
                Octets.ToText(testResult.RawBytes.Span).Should().Be(FoldedField);
            }
        }

        [Fact]
        public void Constructor_NameAndValue_IsNotRaw()
        {
            // act
            var testResult = new ImfHeaderField("A", "1", ImfHeaderFieldKind.Unstructured);

            // assert
            testResult.Name.Should().Be("A");
            testResult.Value.Should().Be("1");
            testResult.RawBytes.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Constructor_InvalidKind_Throws()
        {
            // act
            Action testAction = () => new ImfHeaderField("A", "1", (ImfHeaderFieldKind)99);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FromText_NonAsciiText_HasItsEncodedForm()
        {
            // act
            var testResult = ImfHeaderField.FromText("Subject", Acute);

            // assert
            testResult.Value.Should().Be(WrittenAcute);
        }

        [Fact]
        public void FromText_LineBreak_Throws()
        {
            // act
            Action testAction = () => ImfHeaderField.FromText("Subject", "a\r\nb");

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void GetDecodedValue_EncodedWord_IsDecoded()
        {
            // arrange
            var subject = ImfHeaderField.FromRaw(Octets.Of(EncodedSubjectField));

            // act
            var testResult = subject.GetDecodedValue();

            // assert
            testResult.Should().Be(Acute);
        }

        [Fact]
        public void Add_NullField_Throws()
        {
            // arrange
            var subject = new ImfHeaderFieldCollection();

            // act
            Action testAction = () => subject.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Find_NameInOtherCase_GivesTheFirst()
        {
            // arrange
            var first = new ImfHeaderField("SUBJECT", "1", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { first, new ImfHeaderField("subject", "2", ImfHeaderFieldKind.Unstructured) };

            // act
            var testResult = subject.Find("Subject");

            // assert
            testResult.Should().BeSameAs(first);
        }

        [Fact]
        public void Find_NoSuchField_GivesNull()
        {
            // arrange
            var subject = new ImfHeaderFieldCollection();

            // act
            var testResult = subject.Find("Subject");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void FindAll_NameInOtherCase_GivesEachInOrder()
        {
            // arrange
            var first = new ImfHeaderField("SUBJECT", "1", ImfHeaderFieldKind.Unstructured);
            var second = new ImfHeaderField("subject", "2", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { first, new ImfHeaderField("A", "3", ImfHeaderFieldKind.Unstructured), second };

            // act
            var testResult = subject.FindAll("Subject");

            // assert
            testResult.Should().Equal(first, second);
        }

        [Fact]
        public void RemoveAll_Name_RemovesEachAndCountsThem()
        {
            // arrange
            var other = new ImfHeaderField("A", "3", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { new ImfHeaderField("SUBJECT", "1", ImfHeaderFieldKind.Unstructured), other, new ImfHeaderField("subject", "2", ImfHeaderFieldKind.Unstructured) };

            // act
            var testResult = subject.RemoveAll("Subject");

            // assert
            testResult.Should().Be(2);
            subject.Should().Equal(other);
        }

        [Fact]
        public void Insert_AtStart_GoesBeforeTheOthers()
        {
            // arrange
            var existing = new ImfHeaderField("A", "1", ImfHeaderFieldKind.Unstructured);
            var added = new ImfHeaderField("B", "2", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { existing };

            // act
            subject.Insert(0, added);

            // assert
            subject.Should().Equal(added, existing);
        }

        [Fact]
        public void Replace_Index_KeepsThePosition()
        {
            // arrange
            var other = new ImfHeaderField("A", "1", ImfHeaderFieldKind.Unstructured);
            var added = new ImfHeaderField("B", "2", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { new ImfHeaderField("C", "3", ImfHeaderFieldKind.Unstructured), other };

            // act
            subject.Replace(0, added);

            // assert
            subject.Should().Equal(added, other);
        }

        [Fact]
        public void RemoveAt_Index_RemovesTheField()
        {
            // arrange
            var other = new ImfHeaderField("A", "1", ImfHeaderFieldKind.Unstructured);
            var subject = new ImfHeaderFieldCollection { new ImfHeaderField("C", "3", ImfHeaderFieldKind.Unstructured), other };

            // act
            subject.RemoveAt(0);

            // assert
            subject.Should().Equal(other);
        }

        [Fact]
        public void Contains_NullName_Throws()
        {
            // arrange
            var subject = new ImfHeaderFieldCollection();

            // act
            Action testAction = () => subject.Contains(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteMessage_RawFieldAdded_WritesItByteForByte()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A11);
            subject.Fields.Add(ImfHeaderField.FromRaw(Encoding.ASCII.GetBytes("X-A  :\r\n  1\r\n")));

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Contain("Message-ID: <1234@local.machine.example>\r\nX-A  :\r\n  1\r\n\r\nThis");
        }
    }
}
