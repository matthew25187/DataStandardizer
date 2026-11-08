using System;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.E164
{
    public class ItuE164InternationalNumberStructureForNetworksTests
    {
        // Country Code 882, whose network identification codes have two digits, followed by only one digit.
        private const ulong NumberEndingInsideIdentificationCode = 8821;

        // Country Code 883, the four-digit identification code 5120, and a Subscriber Number.
        private const ulong NumberWithFourDigitIdentificationCode = 88351201234;

        [Fact]
        public void IdentificationCode_NumberEndsInsideIdentificationCode_ThrowsInvalidOperationException()
        {
            // arrange
            var numberStructure = new ItuE164InternationalNumberStructureForNetworks(NumberEndingInsideIdentificationCode);

            // act
            Action testAction = () => _ = numberStructure.IdentificationCode;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void IdentificationCode_FourDigitIdentificationCode_ReturnsIdentificationCode()
        {
            // arrange
            var numberStructure = new ItuE164InternationalNumberStructureForNetworks(NumberWithFourDigitIdentificationCode);

            // act
            var actualResult = numberStructure.IdentificationCode;

            // assert
            actualResult.Should().Be(ItuE164AssignedIdentificationCodesForNetworks.IC5120);
        }
    }
}
