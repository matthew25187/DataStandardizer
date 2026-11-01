using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;
using static DataStandardizer.Communication.E164.ItuE164Constants;

namespace DataStandardizer.Communication.Tests.E164
{
    public class ItuE164InternationalNumberTests
    {
        private const string NumberForGroupsOfCountriesSkipMessage = "There is currently no available Group Identification Code.";
        private const string NumberForTrialsSkipMessage = "There is currently no available Trial Identification Code.";

        // Numbers that more than one test uses: for geographic areas in Great Britain, country code 44; for a global service, 800; for a
        // network, 883; and parts of numbers.
        private const ulong GreatBritainNumber = 443071234567L;
        private const ulong OtherGreatBritainNumber = 445071234567L;
        private const ulong GlobalServiceNumber = 8004121234567L;
        private const ulong NetworkNumber = 8831101234567L;
        private const ulong GlobalSubscriberNumber = 8001234567L;
        private const ulong SubscriberNumber = 1234567L;
        private const ulong TwelveDigitSubscriberNumber = 123456789012L;

        // One digit more than ITU-T E.164 §6.1 allows in an international number.
        private const ulong SixteenDigitNumber = 1234567890123456L;

        // The messages of the exceptions that the number types throw.
        private const string UninitializedMessage = "ItuE164InternationalNumber is uninitialized.";
        private const string NumberOf1To15DigitsMessage = "Number must be 1 to 15 digits.*";
        private const string NoSubscriberNumberMessage = "This number does not support the Subscriber Number field.";
        private const string SubscriberNumberOf1To11DigitsMessage = "Subscriber Number must be 1 to 11 digits.";
        private const string UndefinedCountryCodeMessage = "'1000' is not a valid Country Code.*";

        // A country code that ITU-T E.164 doesn't assign.
        private const ushort UndefinedCountryCode = 1000;

        // The group and trial identification codes that the skipped tests assume, as none is assigned yet.
        private const byte GroupIdentificationCode = 3;
        private const byte TrialIdentificationCode = 1;

        // Numbers for a group of countries, country code 388, and for a trial, country code 991, with subscriber number 1234567.
        private const ulong GroupOfCountriesNumber = 38831234567L;
        private const string GroupOfCountriesNumberText = "388 3 1234567";
        private const string PrefixedGroupOfCountriesNumberText = "+388 3 1234567";
        private const ulong TrialNumber = 99111234567L;
        private const string TrialNumberText = "991 1 1234567";
        private const string PrefixedTrialNumberText = "+991 1 1234567";

        // Text that isn't an international number: three digits, which is too few; 16, which is too many; and no digits at all.
        private const string TooShortNumber = "991";
        private const string TooLongNumber = "9911012345678901";
        private const string NotANumber = "Not A Number";

        public static TheoryData<string, ulong> GroupsOfCountriesWithLeadingWhiteSpace => new TheoryData<string, ulong>
        {
            { "38831565550100", 38831565550100L },
            { " 38831565550101", 38831565550101L },
            { "  38831565550102", 38831565550102L },
        };

        public static TheoryData<string, ulong> GroupsOfCountriesWithTrailingWhiteSpace => new TheoryData<string, ulong>
        {
            { "38832075550100", 38832075550100L },
            { "38832075550101 ", 38832075550101L },
            { "38832075550102  ", 38832075550102L },
        };

        public static TheoryData<string, ulong> TrialsWithLeadingWhiteSpace => new TheoryData<string, ulong>
        {
            { "99110865550100", 99110865550100L },
            { " 99110865550101", 99110865550101L },
            { "  99110865550102", 99110865550102L },
        };

        public static TheoryData<string, ulong> TrialsWithTrailingWhiteSpace => new TheoryData<string, ulong>
        {
            { "99110995550100", 99110995550100L },
            { "99110995550101 ", 99110995550101L },
            { "99110995550102  ", 99110995550102L },
        };

        [Fact]
        public void Equals_PhoneNumbersAreEqualInValue_ReturnsTrue()
        {
            // arrange
            const ulong testNumber = 226071234567L;
            var testSubject1 = ItuE164InternationalNumber.CreateNumberForGeographicArea(testNumber);
            var testSubject2 = ItuE164InternationalNumber.CreateNumberForGeographicArea(testNumber);

            // act
            var testResult = testSubject1.Equals(testSubject2);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_PhoneNumbersAreNotEqualInValue_ReturnsFalse()
        {
            // arrange
            var testSubject1 = ItuE164InternationalNumber.CreateNumberForGeographicArea(226071234567L);
            var testSubject2 = ItuE164InternationalNumber.CreateNumberForGlobalService(226091234567L);

            // act
            var testResult = testSubject1.Equals(testSubject2);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void CastToInteger_InstanceIsNotInitialized_ThrowsInvalidCastException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumber();

            // act
            Action testAction = () => _ = (ulong)testSubject;

            // assert
            testAction.Should()
                .Throw<InvalidCastException>()
                .WithMessage(UninitializedMessage);
        }

        [Fact]
        public void IsNumberForGeographicArea_InternationalNumberForGeographicAreas_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(443031234567L);

            // act
            var testResult = testSubject.IsNumberForGeographicArea();

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForGeographicArea_NotInternationalNumberForGeographicAreas_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(GlobalServiceNumber);

            // act
            var testResult = testSubject.IsNumberForGeographicArea();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForGlobalService_InternationalNumberForGlobalServices_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(GlobalServiceNumber);

            // act
            var testResult = testSubject.IsNumberForGlobalService();

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForGlobalService_NotInternationalNumberForGlobalServices_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(GroupOfCountriesNumber);

            // act
            var testResult = testSubject.IsNumberForGlobalService();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForGroupOfCountries_InternationalNumberForGroupsOfCountries_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(GroupOfCountriesNumber);

            // act
            var testResult = testSubject.IsNumberForGroupOfCountries();

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForGroupOfCountries_NotInternationalNumberForGroupsOfCountries_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(NetworkNumber);

            // act
            var testResult = testSubject.IsNumberForGroupOfCountries();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForNetwork_InternationalNumberForNetworks_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(NetworkNumber);

            // act
            var testResult = testSubject.IsNumberForNetwork();

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForNetwork_NotInternationalNumberForNetworks_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(TrialNumber);

            // act
            var testResult = testSubject.IsNumberForNetwork();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForTrial_InternationalNumberForTrials_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(TrialNumber);

            // act
            var testResult = testSubject.IsNumberForTrial();

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForTrial_NotInternationalNumberForTrials_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(16175550199L);

            // act
            var testResult = testSubject.IsNumberForTrial();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Number_InternationalNumber_ReturnsFullPhoneNumber()
        {
            // arrange
            const ulong testNumber = 4451671234567L;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(testNumber);

            // act
            var testResult = testSubject.Number;

            // assert
            testResult.Should().Be(testNumber);
        }

        [Fact]
        public void Number_InstanceIsNotInitialized_ThrowsInvalidOperationException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumber();

            // act
            Action testAction = () => _ = testSubject.Number;

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(UninitializedMessage);
        }

        [Fact]
        public void CountryCode_InternationalNumber_ReturnsCountryCode()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(OtherGreatBritainNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForGeographicAreas)testSubject).CountryCode;

            // assert
            testResult.Should().Be(44);
        }

        [Fact]
        public void CountryCode_InstanceIsNotInitialized_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumber();

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumber)testSubject).CountryCode;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Country Code field.");
        }

        [Fact]
        public void NationalSignificantNumber_InternationalNumberForGeographicAreas_ReturnsNationalSignificantNumber()
        {
            // arrange
            const ulong nationalSignificantNumber = 5071234567L;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(ItuE164AssignedCountryCodesForGeographicAreas.GB, nationalSignificantNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForGeographicAreas)testSubject).NationalSignificantNumber;

            // assert
            ((ulong)testResult).Should().Be(nationalSignificantNumber);
        }

        [Fact]
        public void NationalSignificantNumber_NotInternationalNumberForGeographicAreas_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(8001052367L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForGeographicAreas)testSubject).NationalSignificantNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the National Significant Number field.");
        }

        [Fact]
        public void IdentificationCode_InternationalNumberForNetworks_ReturnsIdentificationCode()
        {
            // arrange
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC9;
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(ItuE164AssignedCountryCodesForNetworks.GMSS, identificationCode, SubscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForNetworks)testSubject).IdentificationCode;

            // assert
            testResult.Should().Be(identificationCode);
        }

        [Fact]
        public void IdentificationCode_NotInternationalNumberForNetworks_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(8005671234L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForNetworks)testSubject).IdentificationCode;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Identification Code field.");
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void GroupIdentificationCode_InternationalNumberForGroupsOfCountries_ReturnsGroupIdentificationCode()
        {
            // arrange
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)GroupIdentificationCode;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode, groupIdentificationCode, SubscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForGroupsOfCountries)testSubject).GroupIdentificationCode;

            // assert
            testResult.Should().Be(groupIdentificationCode);
        }

        [Fact]
        public void GroupIdentificationCode_NotInternationalNumberForGroupsOfCountries_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(OtherGreatBritainNumber);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForGroupsOfCountries)testSubject).GroupIdentificationCode;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Group Identification Code field.");
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void TrialIdentificationCode_InternationalNumberForTrials_ReturnsTrialIdentificationCode()
        {
            // arrange
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)TrialIdentificationCode;
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(ItuE164AssignedCountryCodesForTrials.SharedCode, trialIdentificationCode, null);

            // act
            var testResult = ((IItuE164InternationalNumberForTrials)testSubject).TrialIdentificationCode;

            // assert
            testResult.Should().Be(trialIdentificationCode);
        }

        [Fact]
        public void TrialIdentificationCode_NotInternationalNumberForTrials_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(804071234567L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForTrials)testSubject).TrialIdentificationCode;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Trial Identification Code field.");
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void SubscriberNumber_InternationalNumberForTrials_ReturnsSubscriberNumber()
        {
            // arrange
            const ulong subscriberNumber = SubscriberNumber;
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(ItuE164AssignedCountryCodesForTrials.SharedCode, (ItuE164AssignedTrialIdentificationCodesForTrials)TrialIdentificationCode, subscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForTrials)testSubject).SubscriberNumber;

            // assert
            testResult.Should().Be(subscriberNumber);
        }

        [Fact]
        public void SubscriberNumber_NotInternationalNumberForTrials_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(GreatBritainNumber);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForTrials)testSubject).SubscriberNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage(NoSubscriberNumberMessage);
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void SubscriberNumber_InternationalNumberForGroupsOfCountries_ReturnsSubscriberNumber()
        {
            // arrange
            const ulong subscriberNumber = SubscriberNumber;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode, (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)GroupIdentificationCode, subscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForGroupsOfCountries)testSubject).SubscriberNumber;

            // assert
            testResult.Should().Be(subscriberNumber);
        }

        [Fact]
        public void SubscriberNumber_NotInternationalNumberForGroupsOfCountries_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(613571234567L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForGroupsOfCountries)testSubject).SubscriberNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage(NoSubscriberNumberMessage);
        }

        [Fact]
        public void SubscriberNumber_InternationalNumberForNetworks_ReturnsSubscriberNumber()
        {
            // arrange
            const ulong subscriberNumber = SubscriberNumber;
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(ItuE164AssignedCountryCodesForNetworks.GMSS, ItuE164AssignedIdentificationCodesForNetworks.IC8, subscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForNetworks)testSubject).SubscriberNumber;

            // assert
            ((ulong)testResult).Should().Be(subscriberNumber);
        }

        [Fact]
        public void SubscriberNumber_NotInternationalNumberForNetworks_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(654621234567L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForNetworks)testSubject).SubscriberNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage(NoSubscriberNumberMessage);
        }

        [Fact]
        public void GlobalSubscriberNumber_InternationalNumberForGlobalServices_ReturnsGlobalSubscriberNumber()
        {
            // arrange
            const ulong globalSubscriberNumber = GlobalSubscriberNumber;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(ItuE164AssignedCountryCodesForGlobalServices.SNAC, globalSubscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForGlobalServices)testSubject).GlobalSubscriberNumber;

            // assert
            ((ulong)testResult).Should().Be(globalSubscriberNumber);
        }

        [Fact]
        public void GlobalSubscriberNumber_NotInternationalNumberForGlobalServices_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(617501234567L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForGlobalServices)testSubject).GlobalSubscriberNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Global Subscriber Number field.");
        }

        [Fact]
        public void CreateNumberForGeographicArea_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ulong testNumber = 1234567890123456;

            // act
            Action testAction = () => ItuE164InternationalNumber.CreateNumberForGeographicArea(testNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(NumberOf1To15DigitsMessage);
        }

        [Fact]
        public void CreateNumberForGeographicArea_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong testNumber = 274031234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGeographicArea(testNumber);

            // assert
            testResult.Number.Should().Be(testNumber);
        }

        [Fact]
        public void CreateNumberForGeographicArea_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForGeographicAreas)UndefinedCountryCode;
            const ulong nationalSignificantNumber = 5081234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGeographicArea(countryCode, nationalSignificantNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(UndefinedCountryCodeMessage);
        }

        [Fact]
        public void CreateNumberForGeographicArea_NationalSignificantNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGeographicAreas countryCode = ItuE164AssignedCountryCodesForGeographicAreas.US;
            const ulong nationalSignificantNumber = 123456789012345L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGeographicArea(countryCode, nationalSignificantNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("National Significant Number must be 1 to 14 digits.*");
        }

        [Fact]
        public void CreateNumberForGeographicArea_CountryCodeAndNationalSignificantNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGeographicAreas countryCode = ItuE164AssignedCountryCodesForGeographicAreas.GB;
            const ulong nationalSignificantNumber = 5091234567L;

            const ulong expectedResult = 445091234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGeographicArea(countryCode, nationalSignificantNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateNumberForGlobalService_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ulong number = SixteenDigitNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGlobalService(number);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(NumberOf1To15DigitsMessage);
        }

        [Fact]
        public void CreateNumberForGlobalService_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong number = GreatBritainNumber;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGlobalService(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForGlobalService_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForGlobalServices)UndefinedCountryCode;
            const ulong globalSubscriberNumber = 3031234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGlobalService(countryCode, globalSubscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(UndefinedCountryCodeMessage);
        }

        [Fact]
        public void CreateNumberForGlobalService_GlobalSubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGlobalServices countryCode = ItuE164AssignedCountryCodesForGlobalServices.SNAC;
            const ulong globalSubscriberNumber = 9001234567890L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGlobalService(countryCode, globalSubscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Global Subscriber Number must be 1 to 12 digits.*");
        }

        [Fact]
        public void CreateNumberForGlobalService_CountryCodeAndGlobalSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGlobalServices countryCode = ItuE164AssignedCountryCodesForGlobalServices.IFS;
            const ulong globalSubscriberNumber = GlobalSubscriberNumber;

            const ulong expectedResult = 8008001234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGlobalService(countryCode, globalSubscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateNumberForNetwork_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ulong number = SixteenDigitNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(number);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(NumberOf1To15DigitsMessage);
        }

        [Fact]
        public void CreateNumberForNetwork_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong number = 8813091234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForNetwork(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForNetwork_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForNetworks)UndefinedCountryCode;
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC190;
            const ulong subscriberNumber = SubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(UndefinedCountryCodeMessage);
        }

        [Fact]
        public void CreateNumberForNetwork_IdentificationCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForNetworks countryCode = ItuE164AssignedCountryCodesForNetworks.GMSS;
            var identificationCode = (ItuE164AssignedIdentificationCodesForNetworks)10_000;
            const ulong subscriberNumber = SubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage("'10000' is not a valid Identification Code.*");
        }

        [Fact]
        public void CreateNumberForNetwork_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForNetworks countryCode = ItuE164AssignedCountryCodesForNetworks.IN1;
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC190;
            const ulong subscriberNumber = TwelveDigitSubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Subscriber Number must be 1 to 9 digits.*");
        }

        [Fact]
        public void CreateNumberForNetwork_CountryCodeAndIdentificationCodeAndSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForNetworks countryCode = ItuE164AssignedCountryCodesForNetworks.GMSS;
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC110;
            const ulong subscriberNumber = SubscriberNumber;

            const ulong expectedResult = 8811101234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(12L), InlineData(123L), InlineData(1234L), InlineData(SixteenDigitNumber)]
        public void CreateNumberForGroupOfCountries_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException(ulong testNumber)
        {
            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Number must be 5 to 15 digits.*");
        }

        [Fact]
        public void CreateNumberForGroupOfCountries_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong number = GroupOfCountriesNumber;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForGroupOfCountries_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForGroupsOfCountries)UndefinedCountryCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)GroupIdentificationCode;
            const ulong subscriberNumber = SubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(UndefinedCountryCodeMessage);
        }

        [Fact]
        public void CreateNumberForGroupOfCountries_GroupIdentificationCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)10;
            const ulong subscriberNumber = SubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage("'10' is not a valid Group Identification Code.*");
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void CreateNumberForGroupOfCountries_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)GroupIdentificationCode;
            const ulong subscriberNumber = TwelveDigitSubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(SubscriberNumberOf1To11DigitsMessage);
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void CreateNumberForGroupOfCountries_CountryCodeAndGroupIdentificationCodeAndSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)GroupIdentificationCode;
            const ulong subscriberNumber = SubscriberNumber;

            const ulong expectedResult = GroupOfCountriesNumber;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(12L), InlineData(123L), InlineData(SixteenDigitNumber)]
        public void CreateNumberForTrial_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException(ulong testNumber)
        {
            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(testNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Number must be 4 to 15 digits.*");
        }

        [Fact]
        public void CreateNumberForTrial_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong number = TrialNumber;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForTrial(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForTrial_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForTrials)UndefinedCountryCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)TrialIdentificationCode;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, null);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(UndefinedCountryCodeMessage);
        }

        [Fact]
        public void CreateNumberForTrial_TrialIdentificationCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForTrials countryCode = ItuE164AssignedCountryCodesForTrials.SharedCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)10;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, null);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage("'10' is not a valid Trial Identification Code.*");
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void CreateNumberForTrial_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForTrials countryCode = ItuE164AssignedCountryCodesForTrials.SharedCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)TrialIdentificationCode;
            const ulong subscriberNumber = TwelveDigitSubscriberNumber;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(SubscriberNumberOf1To11DigitsMessage);
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void CreateNumberForTrial_CountryCodeAndTrialIdentificationCodeAndSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForTrials countryCode = ItuE164AssignedCountryCodesForTrials.SharedCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)TrialIdentificationCode;
            const ulong subscriberNumber = SubscriberNumber;

            const ulong expectedResult = TrialNumber;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForGeographicAreasTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void Parse_InternationalNumberForGeographicAreas_ReturnsInternationalNumber(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testInput, ItuE164InternationalNumberStyles.Any);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForGlobalServicesTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void Parse_InternationalNumberForGlobalServices_ReturnsInternationalNumber(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testInput, ItuE164InternationalNumberStyles.Any);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForNetworksTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void Parse_InternationalNumberForNetworks_ReturnsInternationalNumber(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testInput, ItuE164InternationalNumberStyles.Any);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData(GroupOfCountriesNumberText, GroupOfCountriesNumber)]
        public void Parse_InternationalNumberForGroupsOfCountriesNoPrefix_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData(PrefixedGroupOfCountriesNumberText, GroupOfCountriesNumber)]
        public void Parse_InternationalNumberForGroupsOfCountriesWithPrefix_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [MemberData(nameof(GroupsOfCountriesWithLeadingWhiteSpace))]
        public void Parse_InternationalNumberForGroupsOfCountriesWithLeadingWhiteSpace_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.AllowLeadingWhite);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [MemberData(nameof(GroupsOfCountriesWithTrailingWhiteSpace))]
        public void Parse_InternationalNumberForGroupsOfCountriesWithTrailingWhiteSpace_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.AllowTrailingWhite);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [InlineData(TrialNumberText, TrialNumber)]
        public void Parse_InternationalNumberForTrialsNoPrefix_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [InlineData(PrefixedTrialNumberText, TrialNumber)]
        public void Parse_InternationalNumberForTrialsWithPrefix_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [MemberData(nameof(TrialsWithLeadingWhiteSpace))]
        public void Parse_InternationalNumberForTrialsWithLeadingWhiteSpace_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.AllowLeadingWhite);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [MemberData(nameof(TrialsWithTrailingWhiteSpace))]
        public void Parse_InternationalNumberForTrialsWithTrailingWhiteSpace_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.AllowTrailingWhite);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TooShortNumber, "The input string '991' was not in a correct format."),
         InlineData(TooLongNumber, "The input string '9911012345678901' was not in a correct format."),
         InlineData(NotANumber, "The input string 'Not A Number' was not in a correct format.")]
        public void Parse_InvalidInput_ThrowsFormatException(string testNumber, string expectedMessage)
        {
            // act
            Action testAction = () => _ = ItuE164InternationalNumber.Parse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber);

            // assert
            testAction.Should()
                .Throw<FormatException>()
                .WithMessage(expectedMessage);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForGeographicAreasTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void TryParse_InternationalNumberForGeographicAreas_ReturnsInternationalNumber(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testInput, ItuE164InternationalNumberStyles.Any, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForGlobalServicesTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void TryParse_InternationalNumberForGlobalServices_ReturnsTrue(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testInput, ItuE164InternationalNumberStyles.Any, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseTestCaseGenerator.InternationalNumberForNetworksTestCases), MemberType = typeof(ParseTestCaseGenerator))]
        public void TryParse_InternationalNumberForNetworks_ReturnsTrue(string testInput, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testInput, ItuE164InternationalNumberStyles.Any, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData(GroupOfCountriesNumberText, GroupOfCountriesNumber)]
        public void TryParse_InternationalNumberForGroupsOfCountriesNoPrefix_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData(PrefixedGroupOfCountriesNumberText, GroupOfCountriesNumber)]
        public void TryParse_InternationalNumberForGroupsOfCountriesWithPrefix_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [MemberData(nameof(GroupsOfCountriesWithLeadingWhiteSpace))]
        public void TryParse_InternationalNumberForGroupsOfCountriesWithLeadingWhiteSpace_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.AllowLeadingWhite, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [MemberData(nameof(GroupsOfCountriesWithTrailingWhiteSpace))]
        public void TryParse_InternationalNumberForGroupsOfCountriesWithTrailingWhiteSpace_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.AllowTrailingWhite, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [InlineData(TrialNumberText, TrialNumber)]
        public void TryParse_InternationalNumberForTrialsNoPrefix_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [InlineData(PrefixedTrialNumberText, TrialNumber)]
        public void TryParse_InternationalNumberForTrialsWithPrefix_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [MemberData(nameof(TrialsWithLeadingWhiteSpace))]
        public void TryParse_InternationalNumberForTrialsWithLeadingWhiteSpace_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.AllowLeadingWhite, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [MemberData(nameof(TrialsWithTrailingWhiteSpace))]
        public void TryParse_InternationalNumberForTrialsWithTrailingWhiteSpace_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.AllowTrailingWhite, out var actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TooShortNumber),
         InlineData(TooLongNumber),
         InlineData(NotANumber)]
        public void TryParse_NotInternationalNumber_ReturnsFalse(string testNumber)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, ItuE164InternationalNumberStyles.InternationalNumber, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public class ParseTestCaseGenerator
        {
            private static readonly string[] Prefixes = new string[] { string.Empty, InternationalPrefixSymbol.ToString() };
            private static readonly string[] Separators = new string[] { string.Empty, " ", "-" };
            private static readonly string[] WhiteSpace = new string[] { string.Empty, " ", "  ", "\t", "\t\t" };

            public static IEnumerable<object[]> InternationalNumberForGeographicAreasTestCases
            {
                get
                {
                    const int areaCode = 411;
                    var subscriberNumberIndex = 0;
                    foreach (var countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGeographicAreas)).Cast<ushort>().Distinct())
                    {
                        foreach (var separator in Separators)
                        {
                            foreach (var leadingWhiteSpace in WhiteSpace)
                            {
                                foreach (var trailingWhiteSpace in WhiteSpace)
                                {
                                    foreach (var prefix in Prefixes)
                                    {
                                        var subscriberNumber = 5550000 + (100 + subscriberNumberIndex++ % 100);
                                        var testInput = string.Concat(leadingWhiteSpace, prefix, leadingWhiteSpace, countryCode, separator, areaCode, separator, subscriberNumber, trailingWhiteSpace);

                                        var expectedResult = countryCode * 10_000_000_000L + areaCode * 10_000_000L + subscriberNumber;

                                        yield return new object[] { testInput, expectedResult };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public static IEnumerable<object[]> InternationalNumberForGlobalServicesTestCases
            {
                get
                {
                    var subscriberNumberIndex = 0;
                    foreach (var countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGlobalServices)).Cast<ushort>())
                    {
                        foreach (var separator in Separators)
                        {
                            foreach (var leadingWhiteSpace in WhiteSpace)
                            {
                                foreach (var trailingWhiteSpace in WhiteSpace)
                                {
                                    foreach (var prefix in Prefixes)
                                    {
                                        var subscriberNumber = 5550000 + (100 + subscriberNumberIndex++ % 100);
                                        var testInput = string.Concat(leadingWhiteSpace, prefix, leadingWhiteSpace, countryCode, separator, subscriberNumber, trailingWhiteSpace);

                                        var expectedResult = countryCode * 10_000_000L + subscriberNumber;

                                        yield return new object[] { testInput, expectedResult };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public static IEnumerable<object[]> InternationalNumberForNetworksTestCases
            {
                get
                {
                    var identificationCodeLookup = (
                            from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForNetworks)).Cast<ushort>()
                            from identificationCode in Enum.GetValues(typeof(ItuE164AssignedIdentificationCodesForNetworks)).Cast<ItuE164AssignedIdentificationCodesForNetworks>()
                            let sharedCodeAttribute = identificationCode.GetType().GetFields()
                                .First(@field => @field.GetValue(identificationCode)?.Equals(identificationCode) ?? false)
                                .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                            where sharedCodeAttribute.CountryCode == countryCode
                            select new { countryCode, identificationCode = (ushort)identificationCode })
                        .ToLookup(kvp => kvp.countryCode, kvp => kvp.identificationCode);
                    var subscriberNumberIndex = 0;
                    foreach (var kvp in identificationCodeLookup)
                    {
                        foreach (var identificationCode in kvp)
                        {
                            foreach (var separator in Separators)
                            {
                                foreach (var leadingWhiteSpace in WhiteSpace)
                                {
                                    foreach (var trailingWhiteSpace in WhiteSpace)
                                    {
                                        foreach (var prefix in Prefixes)
                                        {
                                            var subscriberNumber = 5550000 + (100 + subscriberNumberIndex++ % 100);
                                            var testInput = string.Concat(leadingWhiteSpace, prefix, leadingWhiteSpace, kvp.Key, separator, identificationCode, separator, subscriberNumber, trailingWhiteSpace);

                                            var expectedResult = ulong.Parse($"{kvp.Key}{identificationCode}{subscriberNumber}");

                                            yield return new object[] { testInput, expectedResult };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
