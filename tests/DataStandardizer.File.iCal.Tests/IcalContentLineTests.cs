using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalContentLineTests
    {
        private const string Attendee = "ATTENDEE";
        private const string CommonName = "CN";
        private const string AnnName = "Ann";
        private const string AnnAddress = "mailto:a@example.com";

        #region Read-only lists

        [Fact]
        public void Parameters_LineBuiltInCode_CannotBeCastToArray()
        {
            // arrange
            var line = new IcalContentLine(Attendee, new[] { new IcalContentLineParameter(CommonName, AnnName) }, AnnAddress);

            // act
            var testResult = line.Parameters;

            // assert
            testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
            testResult.Should().BeAssignableTo<ICollection<IcalContentLineParameter>>().Which.IsReadOnly.Should().BeTrue();
            testResult[0].Values.Should().NotBeAssignableTo<string[]>();
            testResult[0].Values.Should().BeAssignableTo<ICollection<string>>().Which.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void Parameters_LineWithNoParameters_CannotBeCastToArray()
        {
            // arrange
            var line = new IcalContentLine("SUMMARY", "Party");

            // act
            var testResult = line.Parameters;

            // assert
            testResult.Should().BeEmpty();
            testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
        }

        [Fact]
        public void Parameters_LineRead_CannotBeCastToArray()
        {
            // arrange
            using (var reader = CreateReader(Crlf("ATTENDEE;CN=Ann;MEMBER=\"mailto:x@example.com\",\"mailto:y@example.com\":mailto:a@example.com")))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.Parameters;

                // assert
                testResult.Should().HaveCount(2);
                testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
                testResult.Should().BeAssignableTo<ICollection<IcalContentLineParameter>>().Which.IsReadOnly.Should().BeTrue();
                testResult[1].Values.Should().Equal("mailto:x@example.com", "mailto:y@example.com");
                testResult[1].Values.Should().NotBeAssignableTo<string[]>();
                testResult[1].Values.Should().BeAssignableTo<ICollection<string>>().Which.IsReadOnly.Should().BeTrue();
            }
        }

        [Fact]
        public void Values_ParameterBuiltInCode_DoesNotChangeWithSourceArray()
        {
            // arrange
            var values = new[] { AnnName };
            var parameter = new IcalContentLineParameter(CommonName, values);

            // act
            values[0] = "Mallory";

            // assert
            parameter.Values.Should().Equal(AnnName);
            parameter.Values.Should().NotBeAssignableTo<string[]>();
        }

        [Fact]
        public void Parameters_ReadTwice_ReturnsSameList()
        {
            // arrange
            var line = new IcalContentLine(Attendee, new[] { new IcalContentLineParameter(CommonName, AnnName) }, AnnAddress);
            var firstRead = line.Parameters;

            // act
            var testResult = line.Parameters;

            // assert
            testResult.Should().BeSameAs(firstRead);
            testResult[0].Values.Should().BeSameAs(firstRead[0].Values);
        }

        #endregion
    }
}
