using DataStandardizer.Communication.E164;
using FluentAssertions;

namespace DataStandardizer.Communication.Tests
{
    public class ItuE164InternationalNumberTests
    {
        private const string NumberForGroupsOfCountriesSkipMessage = "There is currently no available Group Identification Code.";
        private const string NumberForTrialsSkipMessage = "There is currently no available Trial Identification Code.";

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
                .WithMessage($"{nameof(ItuE164InternationalNumber)} is uninitialized.");
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
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(8004121234567L);
            
            // act
            var testResult = testSubject.IsNumberForGeographicArea();
            
            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForGlobalService_InternationalNumberForGlobalServices_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGlobalService(8004121234567L);
            
            // act
            var testResult = testSubject.IsNumberForGlobalService();
            
            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForGlobalService_NotInternationalNumberForGlobalServices_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(38831234567L);
            
            // act
            var testResult = testSubject.IsNumberForGlobalService();
            
            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForGroupOfCountries_InternationalNumberForGroupsOfCountries_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(38831234567L);
            
            // act
            var testResult = testSubject.IsNumberForGroupOfCountries();
            
            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForGroupOfCountries_NotInternationalNumberForGroupsOfCountries_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(8831101234567L);
            
            // act
            var testResult = testSubject.IsNumberForGroupOfCountries();
            
            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForNetwork_InternationalNumberForNetworks_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(8831101234567L);
            
            // act
            var testResult = testSubject.IsNumberForNetwork();
            
            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsNumberForNetwork_NotInternationalNumberForNetworks_ReturnsFalse()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(99111234567L);
            
            // act
            var testResult = testSubject.IsNumberForNetwork();
            
            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsNumberForTrial_InternationalNumberForTrials_ReturnsTrue()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(99111234567L);

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
                .WithMessage($"{nameof(ItuE164InternationalNumber)} is uninitialized.");
        }

        [Fact]
        public void CountryCode_InternationalNumber_ReturnsCountryCode()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(445071234567L);

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
            var testSubject = ItuE164InternationalNumber.CreateNumberForNetwork(ItuE164AssignedCountryCodesForNetworks.GMSS, identificationCode, 1234567L);

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
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)3;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode, groupIdentificationCode, 1234567L);

            // act
            var testResult = ((IItuE164InternationalNumberForGroupsOfCountries)testSubject).GroupIdentificationCode;

            // assert
            testResult.Should().Be(groupIdentificationCode);
        }

        [Fact]
        public void GroupIdentificationCode_NotInternationalNumberForGroupsOfCountries_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(445071234567L);

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
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)1;
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
            const ulong subscriberNumber = 1234567L;
            var testSubject = ItuE164InternationalNumber.CreateNumberForTrial(ItuE164AssignedCountryCodesForTrials.SharedCode, (ItuE164AssignedTrialIdentificationCodesForTrials)1, subscriberNumber);

            // act
            var testResult = ((IItuE164InternationalNumberForTrials)testSubject).SubscriberNumber;

            // assert
            testResult.Should().Be(subscriberNumber);
        }

        [Fact]
        public void SubscriberNumber_NotInternationalNumberForTrials_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(443071234567L);

            // act
            Action testAction = () => _ = ((IItuE164InternationalNumberForTrials)testSubject).SubscriberNumber;

            // assert
            testAction.Should()
                .Throw<NotSupportedException>()
                .WithMessage("This number does not support the Subscriber Number field.");
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void SubscriberNumber_InternationalNumberForGroupsOfCountries_ReturnsSubscriberNumber()
        {
            // arrange
            const ulong subscriberNumber = 1234567L;
            var testSubject = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode, (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)3, subscriberNumber);

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
                .WithMessage("This number does not support the Subscriber Number field.");
        }

        [Fact]
        public void SubscriberNumber_InternationalNumberForNetworks_ReturnsSubscriberNumber()
        {
            // arrange
            const ulong subscriberNumber = 1234567L;
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
                .WithMessage("This number does not support the Subscriber Number field.");
        }

        [Fact]
        public void GlobalSubscriberNumber_InternationalNumberForGlobalServices_ReturnsGlobalSubscriberNumber()
        {
            // arrange
            const ulong globalSubscriberNumber = 8001234567L;
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
                .WithMessage("Number must be 1 to 15 digits.*");
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
            var countryCode = (ItuE164AssignedCountryCodesForGeographicAreas)1000;
            const ulong nationalSignificantNumber = 5081234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGeographicArea(countryCode, nationalSignificantNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{countryCode}' is not a valid Country Code.*");
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

            var expectedResult = ulong.Parse($"{(ushort)countryCode}{nationalSignificantNumber}");

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGeographicArea(countryCode, nationalSignificantNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateNumberForGlobalService_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ulong number = 1234567890123456L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGlobalService(number);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Number must be 1 to 15 digits.*");
        }

        [Fact]
        public void CreateNumberForGlobalService_InternationalNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ulong number = 443071234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGlobalService(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForGlobalService_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForGlobalServices)1000;
            const ulong globalSubscriberNumber = 3031234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGlobalService(countryCode, globalSubscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{countryCode}' is not a valid Country Code.*");
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
            const ulong globalSubscriberNumber = 8001234567L;

            var expectedResult = ulong.Parse($"{(ushort)countryCode}{globalSubscriberNumber}");

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGlobalService(countryCode, globalSubscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateNumberForNetwork_InternationalNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ulong number = 1234567890123456L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(number);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Number must be 1 to 15 digits.*");
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
            var countryCode = (ItuE164AssignedCountryCodesForNetworks)1000;
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC190;
            const ulong subscriberNumber = 1234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{(ushort)countryCode}' is not a valid Country Code.*");
        }

        [Fact]
        public void CreateNumberForNetwork_IdentificationCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForNetworks countryCode = ItuE164AssignedCountryCodesForNetworks.GMSS;
            var identificationCode = (ItuE164AssignedIdentificationCodesForNetworks)10_000;
            const ulong subscriberNumber = 1234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{(ushort)identificationCode}' is not a valid Identification Code.*");
        }

        [Fact]
        public void CreateNumberForNetwork_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForNetworks countryCode = ItuE164AssignedCountryCodesForNetworks.IN1;
            const ItuE164AssignedIdentificationCodesForNetworks identificationCode = ItuE164AssignedIdentificationCodesForNetworks.IC190;
            const ulong subscriberNumber = 123456789012L;

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
            const ulong subscriberNumber = 1234567L;

            var expectedResult = ulong.Parse($"{(ushort)countryCode}{(ushort)identificationCode}{subscriberNumber}");

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForNetwork(countryCode, identificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(12L), InlineData(123L), InlineData(1234L), InlineData(1234567890123456L)]
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
            const ulong number = 38831234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForGroupOfCountries_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForGroupsOfCountries)1000;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)3;
            const ulong subscriberNumber = 1234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{(ushort)countryCode}' is not a valid Country Code.*");
        }

        [Fact]
        public void CreateNumberForGroupOfCountries_GroupIdentificationCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)10;
            const ulong subscriberNumber = 1234567L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{(byte)groupIdentificationCode}' is not a valid Group Identification Code.*");
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void CreateNumberForGroupOfCountries_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)3;
            const ulong subscriberNumber = 123456789012L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Subscriber Number must be 1 to 11 digits.");
        }

        [Fact(Skip = NumberForGroupsOfCountriesSkipMessage)]
        public void CreateNumberForGroupOfCountries_CountryCodeAndGroupIdentificationCodeAndSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForGroupsOfCountries countryCode = ItuE164AssignedCountryCodesForGroupsOfCountries.SharedCode;
            var groupIdentificationCode = (ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)3;
            const ulong subscriberNumber = 1234567L;

            var expectedResult = ulong.Parse($"{(ushort)countryCode}{(byte)groupIdentificationCode}{subscriberNumber}");

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(countryCode, groupIdentificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(12L), InlineData(123L), InlineData(1234567890123456L)]
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
            const ulong number = 99111234567L;

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForTrial(number);

            // assert
            testResult.Number.Should().Be(number);
        }

        [Fact]
        public void CreateNumberForTrial_CountryCodeNotDefined_ThrowsArgumentException()
        {
            // arrange
            var countryCode = (ItuE164AssignedCountryCodesForTrials)1000;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)1;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, null);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage($"'{(ushort)countryCode}' is not a valid Country Code.*");
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
                .WithMessage($"'{(byte)trialIdentificationCode}' is not a valid Trial Identification Code.*");
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void CreateNumberForTrial_SubscriberNumberTooLong_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const ItuE164AssignedCountryCodesForTrials countryCode = ItuE164AssignedCountryCodesForTrials.SharedCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)1;
            const ulong subscriberNumber = 123456789012L;

            // act
            Action testAction = () => _ = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, subscriberNumber);

            // assert
            testAction.Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage("Subscriber Number must be 1 to 11 digits.");
        }

        [Fact(Skip = NumberForTrialsSkipMessage)]
        public void CreateNumberForTrial_CountryCodeAndTrialIdentificationCodeAndSubscriberNumber_ReturnsInternationalNumber()
        {
            // arrange
            const ItuE164AssignedCountryCodesForTrials countryCode = ItuE164AssignedCountryCodesForTrials.SharedCode;
            var trialIdentificationCode = (ItuE164AssignedTrialIdentificationCodesForTrials)1;
            const ulong subscriberNumber = 1234567L;

            var expectedResult = ulong.Parse($"{(ushort)countryCode}{(byte)trialIdentificationCode}{subscriberNumber}");

            // act
            var testResult = ItuE164InternationalNumber.CreateNumberForTrial(countryCode, trialIdentificationCode, subscriberNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("+ 20 607 123 4567", 206071234567L), InlineData("+1 302 1234567", 13021234567L)]
        public void Parse_InternationalNumberForGeographicAreas_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("800 1234567890", 8001234567890L)]
        public void Parse_InternationalNumberForGlobalServices_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("881 8234567890", 8818234567890L)]
        public void Parse_InternationalNumberForNetworks_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData("388 3 1234567", 38831234567L)]
        public void Parse_InternationalNumberForGroupsOfCountries_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory(Skip = NumberForTrialsSkipMessage)]
        [InlineData("991 1 1234567", 99111234567L)]
        public void Parse_InternationalNumberForTrials_ReturnsInternationalNumber(string testNumber, ulong expectedResult)
        {
            // act
            var testResult = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("Not A Number")]
        public void Parse_NotInternationalNumber_ThrowsFormatException(string testNumber)
        {
            // act
            Action testAction = () => _ = ItuE164InternationalNumber.Parse(testNumber);

            // assert
            testAction.Should()
                .Throw<FormatException>()
                .WithMessage("s is not in the correct format.");
        }

        [Theory]
        [InlineData("+ 20 607 123 4567", 206071234567L), InlineData("+1 302 1234567", 13021234567L)]
        public void TryParse_InternationalNumberForGeographicAreas_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("800 1234567890", 8001234567890L)]
        public void TryParse_InternationalNumberForGlobalServices_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("881 9234567890", 8819234567890L)]
        public void TryParse_InternationalNumberForNetworks_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Number.Should().Be(expectedResult);
        }

        [Theory(Skip = NumberForGroupsOfCountriesSkipMessage)]
        [InlineData("388 3 1234567", 38831234567L)]
        public void TryParse_InternationalNumberForGroupsOfCountries_ReturnsTrue(string testNumber, ulong expectedResult)
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
        [InlineData("991 1 1234567", 99111234567L)]
        public void TryParse_InternationalNumberForTrials_ReturnsTrue(string testNumber, ulong expectedResult)
        {
            // arrange
            ItuE164InternationalNumber actualResult;

            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out actualResult);

            // assert
            testResult.Should().BeTrue();
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("Not A Number")]
        public void TryParse_NotInternationalNumber_ReturnsFalse(string testNumber)
        {
            // act
            var testResult = ItuE164InternationalNumber.TryParse(testNumber, out _);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
