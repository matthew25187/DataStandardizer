using System;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.E164
{
    public class ItuE164InternationalNumberStructureBaseTests
    {
        // Shared Country Codes that the test enumerations use: 388 and 991 have identification codes, and 999 is not a listed Country Code.
        private const ushort FirstCountryCode = 388;
        private const ushort SecondCountryCode = 991;
        private const ushort UnlistedCountryCode = 999;

        private enum TestCountryCodes : ushort
        {
            First = FirstCountryCode,
            Second = SecondCountryCode
        }

        // Declared out of numeric order, with the underlying type of the generated identification code enumerations for groups of countries and trials.
        private enum TestIdentificationCodes : byte
        {
            [ItuE164SharedCode(FirstCountryCode)]
            Three = 3,

            [ItuE164SharedCode(FirstCountryCode)]
            One = 1,

            [ItuE164SharedCode(SecondCountryCode)]
            Two = 2,

            [ItuE164SharedCode(UnlistedCountryCode)]
            Four = 4
        }

        // Two names for one value, both with the same shared code.
        private enum AliasedIdentificationCodes : byte
        {
            [ItuE164SharedCode(FirstCountryCode)]
            One = 1,

            [ItuE164SharedCode(FirstCountryCode)]
            AlsoOne = 1
        }

        // A member that the generator failed to give a shared code.
        private enum UnattributedIdentificationCodes : byte
        {
            [ItuE164SharedCode(FirstCountryCode)]
            One = 1,

            Two = 2
        }

        private enum EmptyIdentificationCodes : byte
        {
        }

        [Fact]
        public void ComposeIdentificationCodeLookup_EnumerationWithMembers_PairsEachMemberWithItsSharedCountryCode()
        {
            // arrange
            var countryCodeEnumType = typeof(TestCountryCodes);
            var identificationCodeEnumType = typeof(TestIdentificationCodes);

            // act
            var actualResult = ItuE164InternationalNumberStructureBase.ComposeIdentificationCodeLookup(countryCodeEnumType, identificationCodeEnumType);

            // assert
            actualResult.Count.Should().Be(2);
            actualResult[FirstCountryCode].Should().Equal(1, 3);
            actualResult[SecondCountryCode].Should().Equal(2);
            actualResult.Contains(UnlistedCountryCode).Should().BeFalse();
        }

        [Fact]
        public void ComposeIdentificationCodeLookup_AliasedMembers_ListsValueOnce()
        {
            // arrange
            var countryCodeEnumType = typeof(TestCountryCodes);
            var identificationCodeEnumType = typeof(AliasedIdentificationCodes);

            // act
            var actualResult = ItuE164InternationalNumberStructureBase.ComposeIdentificationCodeLookup(countryCodeEnumType, identificationCodeEnumType);

            // assert
            actualResult.Count.Should().Be(1);
            actualResult[FirstCountryCode].Should().Equal(1);
        }

        [Fact]
        public void ComposeIdentificationCodeLookup_MemberWithoutSharedCode_ThrowsInvalidOperationException()
        {
            // arrange
            var countryCodeEnumType = typeof(TestCountryCodes);
            var identificationCodeEnumType = typeof(UnattributedIdentificationCodes);

            // act
            Action testAction = () => ItuE164InternationalNumberStructureBase.ComposeIdentificationCodeLookup(countryCodeEnumType, identificationCodeEnumType);

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Identification code 'Two' of UnattributedIdentificationCodes has no shared Country Code.");
        }

        [Fact]
        public void ComposeIdentificationCodeLookup_EnumerationWithoutMembers_ReturnsEmptyLookup()
        {
            // arrange
            var countryCodeEnumType = typeof(TestCountryCodes);
            var identificationCodeEnumType = typeof(EmptyIdentificationCodes);

            // act
            var actualResult = ItuE164InternationalNumberStructureBase.ComposeIdentificationCodeLookup(countryCodeEnumType, identificationCodeEnumType);

            // assert
            actualResult.Count.Should().Be(0);
        }
    }
}
