using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Checks that parsing a value from a span, and formatting one into a span, allocate nothing on the heap.
    /// </summary>
    /// <remarks>
    /// These measure what the runtime allocates, which depends on the JIT and the target framework rather than only on the package, so they are integration tests.
    /// </remarks>
    public class VcardValueAllocationTests
    {
        // Each call parses a value from a span that the factory made before the test runs.
        // ref. RFC 6350 §4.3.4, §4.3.2, §4.3.1, §4.3.5, §4.7, §6.2.2, §6.3.1, §6.6.4, §6.2.7, §6.7.7 and §5.5: the values are their examples.
        public static TheoryData<Func<bool>> DateAndTimeParseCalls()
        {
            var dateAndOrTime = "--1022T1400".ToCharArray();
            var standaloneTime = "T102200-0800".ToCharArray();
            var date = "1985-04".ToCharArray();
            var time = "102200-0800".ToCharArray();
            var dateTime = "19961022T140000".ToCharArray();
            var timestamp = "19961022T140000-0500".ToCharArray();
            var offset = "-0500".ToCharArray();
            var legacyDateAndOrTime = "1987-09-27T08:30:00-06:00".ToCharArray();
            return new TheoryData<Func<bool>>
            {
                () => VcardDateAndOrTime.TryParse(dateAndOrTime, VcardValueFormat.Text, out _),
                () => VcardDateAndOrTime.TryParse(standaloneTime, VcardValueFormat.Text, out _),
                () => VcardDate.TryParse(date, VcardValueFormat.Text, out _),
                () => VcardTime.TryParse(time, VcardValueFormat.Text, out _),
                () => VcardDateTime.TryParse(dateTime, VcardValueFormat.Text, out _),
                () => VcardTimestamp.TryParse(timestamp, VcardValueFormat.Text, out _),
                () => VcardUtcOffset.TryParse(offset, VcardValueFormat.Text, out _),
                () => VcardDateAndOrTime.TryParse(legacyDateAndOrTime, VcardValueFormat.Text, VcardVersion.V3_0, out _),
            };
        }

        public static TheoryData<Func<bool>> StructuredParseCalls()
        {
            var name = "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.".ToCharArray();
            var address = ";;123 Main Street;Any Town;CA;91921-1234;U.S.A.".ToCharArray();
            var organization = "ABC\\, Inc.;North American Division;Marketing".ToCharArray();
            var gender = "M;Fellow".ToCharArray();
            var clientPidMap = "1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b".ToCharArray();
            var pid = "3.1".ToCharArray();
            var destination = new char[64];
            return new TheoryData<Func<bool>>
            {
                () => VcardName.TryParseView(name, VcardValueFormat.Text, out _),
                () => ReadEachItem(name, destination),
                () => ReadEachComponent(organization, destination),
                () => VcardAddress.TryParseView(address, VcardValueFormat.Text, out _),
                () => VcardOrganization.TryParseView(organization, VcardValueFormat.Text, out _),
                () => VcardGender.TryParseParts(gender, VcardValueFormat.Text, out _, out _),
                () => VcardClientPidMap.TryParseParts(clientPidMap, VcardValueFormat.Text, out _, out _),
                () => VcardPid.TryParse(pid, VcardValueFormat.Text, out _),
            };
        }

        public static TheoryData<Func<bool>> OtherParseCalls()
        {
            var text = "a\\,b\\nc".ToCharArray();
            var list = "a\\,b,c,d".ToCharArray();
            var integer = "-1234556790".ToCharArray();
            var boolean = "TRUE".ToCharArray();
            var destination = new char[16];
            return new TheoryData<Func<bool>>
            {
                () => VcardValue.TryParseText(text, VcardValueFormat.Text, destination, out _),
                () => EnumerateEachItem(list),
                () => VcardValue.TryParseInteger(integer, VcardValueFormat.Text, out _),
                () => VcardValue.TryParseBoolean(boolean, VcardValueFormat.Text, out _),
            };
        }

        public static TheoryData<Func<bool>> FormatCalls()
        {
            var dateAndOrTime = VcardDateAndOrTime.Parse("--1022T1400".AsSpan(), VcardValueFormat.Text);
            var timestamp = VcardTimestamp.Parse("19961022T140000-0500".AsSpan(), VcardValueFormat.Text);
            var name = VcardName.Parse("Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.".AsSpan(), VcardValueFormat.Text);
            var address = VcardAddress.Parse(";;123 Main Street;Any Town;CA;91921-1234;U.S.A.".AsSpan(), VcardValueFormat.Text);
            var text = "a,b;c".ToCharArray();
            var destination = new char[128];
            return new TheoryData<Func<bool>>
            {
                () => dateAndOrTime.TryFormat(destination, out _, VcardValueFormat.Text),
                () => timestamp.TryFormat(destination, out _, VcardValueFormat.Text),
                () => VcardValue.TryFormatText(text, destination, out _, VcardValueFormat.Text),
                () => VcardValue.TryFormatBoolean(true, destination, out _, VcardValueFormat.Text),
                () => address.TryFormat(destination, out _, VcardValueFormat.Text),
                () => name.TryFormat(destination, out _, VcardValueFormat.Text),
            };
        }

        [Theory]
        [MemberData(nameof(DateAndTimeParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_DateOrTimeFromSpan_DoesNotAllocate(Func<bool> parseCall)
        {
            // arrange
            Action action = () => parseCall();

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(StructuredParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParseView_StructuredValueFromSpan_DoesNotAllocate(Func<bool> parseCall)
        {
            // arrange
            Action action = () => parseCall();

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(OtherParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_OtherValueFromSpan_DoesNotAllocate(Func<bool> parseCall)
        {
            // arrange
            Action action = () => parseCall();

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(FormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_IntoSpan_DoesNotAllocate(Func<bool> formatCall)
        {
            // arrange
            Action action = () => formatCall();

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(0);
        }

        // A call that fails would allocate nothing, so each fixture has to be shown to succeed.
        [Theory]
        [MemberData(nameof(DateAndTimeParseCalls), DisableDiscoveryEnumeration = true)]
        [MemberData(nameof(StructuredParseCalls), DisableDiscoveryEnumeration = true)]
        [MemberData(nameof(OtherParseCalls), DisableDiscoveryEnumeration = true)]
        [MemberData(nameof(FormatCalls), DisableDiscoveryEnumeration = true)]
        public void Call_AllocationFixture_Succeeds(Func<bool> call)
        {
            // act
            var testResult = call();

            // assert
            testResult.Should().BeTrue();
        }

        /// <summary>
        /// Reads every item of every component of an N value by position, as a caller of <see cref="VcardStructuredValueView"/> can.
        /// </summary>
        /// <returns><c>true</c> if the value parsed and every component had an item.</returns>
        private static bool ReadEachItem(char[] value, char[] destination)
        {
            VcardStructuredValueView view;
            if (!VcardName.TryParseView(value, VcardValueFormat.Text, out view)) return false;

            var itemCount = 0;
            for (var index = 0; index < view.ComponentCount; index++)
            {
                foreach (var item in view.GetComponent(index))
                {
                    view.UnescapeItem(item, destination);
                    itemCount++;
                }
            }

            return itemCount >= view.ComponentCount && view.ComponentCount > 0;
        }

        /// <summary>
        /// Reads every component of an ORG value in one pass, as a caller of <see cref="VcardStructuredValueView"/> can.
        /// </summary>
        /// <returns><c>true</c> if the value parsed and every component had an item.</returns>
        private static bool ReadEachComponent(char[] value, char[] destination)
        {
            VcardStructuredValueView view;
            if (!VcardOrganization.TryParseView(value, VcardValueFormat.Text, out view)) return false;

            var itemCount = 0;
            foreach (var component in view)
            {
                foreach (var item in component)
                {
                    view.UnescapeItem(item, destination);
                    itemCount++;
                }
            }

            return itemCount == view.ComponentCount && itemCount > 0;
        }

        /// <summary>
        /// Splits a list, as a caller of <see cref="VcardValue.EnumerateList"/> does.
        /// </summary>
        /// <returns><c>true</c> if the list had an item that wasn't empty.</returns>
        private static bool EnumerateEachItem(char[] value)
        {
            var length = 0;
            foreach (var item in VcardValue.EnumerateList(value, VcardValueFormat.Text))
            {
                length += item.Length;
            }

            return length > 0;
        }
    }
}
