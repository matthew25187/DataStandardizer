using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="VcardMediaUri"/>: the URI of a <c>PHOTO</c>, <c>LOGO</c> or <c>SOUND</c>, held as text so that it can be longer than <see cref="Uri"/> holds on .NET Framework.
    /// </summary>
    public class VcardMediaUriTests
    {
        private const string HttpUriText = "http://example.com/a.jpg";

        // ref. RFC 2397 §4: the example of a short note.
        private const string DataUriText = "data:,A%20brief%20note";

        [Fact]
        public void FromUri_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => VcardMediaUri.FromUri(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void FromUri_RelativeUri_ThrowsArgumentException()
        {
            // arrange
            var uri = new Uri("a.jpg", UriKind.Relative);

            // act
            Action testAction = () => VcardMediaUri.FromUri(uri);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

#if NET
        [Fact]
        public void FromUri_LongerThanAUriHoldsOnNetFramework_ThrowsArgumentException()
        {
            // arrange: ref. #619.  Such a Uri can't be made on .NET Framework, so every runtime rejects it.
            var uri = new Uri(Vcard30TestData.LargeUri);

            // act
            Action testAction = () => VcardMediaUri.FromUri(uri);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }
#endif

        [Fact]
        public void FromUri_AbsoluteUri_GivesItsText()
        {
            // arrange
            var uri = new Uri(HttpUriText);

            // act
            var testResult = VcardMediaUri.FromUri(uri);

            // assert
            testResult.Text.Should().Be(HttpUriText);
            testResult.Uri.Should().BeSameAs(uri);
        }

        [Fact]
        public void FromData_NotInitialised_ThrowsArgumentException()
        {
            // act
            Action testAction = () => VcardMediaUri.FromData(default(MimeDataUri));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FromData_DataUri_GivesItsText()
        {
            // arrange
            var data = MimeDataUri.Parse(DataUriText);

            // act
            var testResult = VcardMediaUri.FromData(data);

            // assert
            testResult.Text.Should().Be(DataUriText);
        }

        [Fact]
        public void FromData_ShortDataUri_GivesAUri()
        {
            // arrange
            var data = MimeDataUri.Parse(DataUriText);

            // act
            var testResult = VcardMediaUri.FromData(data);

            // assert
            testResult.Uri.OriginalString.Should().Be(DataUriText);
        }

        [Fact]
        public void FromData_LongerThanAUriHoldsOnNetFramework_HasNoUri()
        {
            // arrange: ref. #619.
            var data = MimeDataUri.Create(MimeMediaType.Parse("image/jpeg"), Vcard30TestData.LargeData());

            // act
            var testResult = VcardMediaUri.FromData(data);

            // assert
            testResult.Uri.Should().BeNull();
        }

        [Fact]
        public void TryGetData_LongerThanAUriHoldsOnNetFramework_GivesTheData()
        {
            // arrange: ref. #619.
            var value = VcardMediaUri.FromData(MimeDataUri.Create(MimeMediaType.Parse("image/jpeg"), Vcard30TestData.LargeData()));

            // act
            MimeDataUri data;
            var testResult = value.TryGetData(out data);

            // assert
            testResult.Should().BeTrue();
            data.GetData().Should().Equal(Vcard30TestData.LargeData());
        }

        [Fact]
        public void TryGetData_HttpUri_ReturnsFalse()
        {
            // arrange
            var value = VcardMediaUri.FromUri(new Uri(HttpUriText));

            // act
            var testResult = value.TryGetData(out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(DataUriText, true)]
        [InlineData("DATA:,A%20brief%20note", true)]
        [InlineData(HttpUriText, false)]
        public void IsData_Uri_SaysWhetherItsSchemeIsData(string text, bool expected)
        {
            // arrange
            var value = VcardMediaUri.FromUri(new Uri(text));

            // act
            var testResult = value.IsData;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(HttpUriText, true)]
        [InlineData("http://example.com/A.jpg", false)]
        public void Equals_OtherValue_ComparesTheText(string otherText, bool expected)
        {
            // arrange
            var value = VcardMediaUri.FromUri(new Uri(HttpUriText));
            var other = VcardMediaUri.FromUri(new Uri(otherText));

            // act
            var testResult = value.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_Value_GivesTheText()
        {
            // arrange
            var value = VcardMediaUri.FromUri(new Uri(HttpUriText));

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be(HttpUriText);
        }
    }
}
