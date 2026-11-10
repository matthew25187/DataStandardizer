using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardPropertyInfoTests
    {
        public static TheoryData<string, VcardVersion, bool> DefinedData()
        {
            // ref. RFC 6350, RFC 2426, RFC 2739, RFC 4770, vCard 2.1: the versions are not additive.  The extensions of RFC 6474, RFC 6715, RFC 8605 and RFC 9554 are of vCard 4.0 alone.
            return new TheoryData<string, VcardVersion, bool>
            {
                { "FN", VcardVersion.V4_0, true },
                { "FN", VcardVersion.V3_0, true },
                { "FN", VcardVersion.V2_1, true },
                { "KIND", VcardVersion.V4_0, true },
                { "KIND", VcardVersion.V3_0, false },
                { "KIND", VcardVersion.V2_1, false },
                { "NICKNAME", VcardVersion.V4_0, true },
                { "NICKNAME", VcardVersion.V3_0, true },
                { "NICKNAME", VcardVersion.V2_1, false },
                { "MAILER", VcardVersion.V4_0, false },
                { "MAILER", VcardVersion.V3_0, true },
                { "MAILER", VcardVersion.V2_1, true },
                { "CLASS", VcardVersion.V4_0, false },
                { "CLASS", VcardVersion.V3_0, true },
                { "CLASS", VcardVersion.V2_1, false },
                { "BIRTHPLACE", VcardVersion.V4_0, true },
                { "BIRTHPLACE", VcardVersion.V3_0, false },
                { "BIRTHPLACE", VcardVersion.V2_1, false },
                { "DEATHPLACE", VcardVersion.V4_0, true },
                { "DEATHPLACE", VcardVersion.V3_0, false },
                { "DEATHPLACE", VcardVersion.V2_1, false },
                { "DEATHDATE", VcardVersion.V4_0, true },
                { "DEATHDATE", VcardVersion.V3_0, false },
                { "DEATHDATE", VcardVersion.V2_1, false },
                { "EXPERTISE", VcardVersion.V4_0, true },
                { "EXPERTISE", VcardVersion.V3_0, false },
                { "EXPERTISE", VcardVersion.V2_1, false },
                { "HOBBY", VcardVersion.V4_0, true },
                { "HOBBY", VcardVersion.V3_0, false },
                { "HOBBY", VcardVersion.V2_1, false },
                { "INTEREST", VcardVersion.V4_0, true },
                { "INTEREST", VcardVersion.V3_0, false },
                { "INTEREST", VcardVersion.V2_1, false },
                { "ORG-DIRECTORY", VcardVersion.V4_0, true },
                { "ORG-DIRECTORY", VcardVersion.V3_0, false },
                { "ORG-DIRECTORY", VcardVersion.V2_1, false },
                { "CONTACT-URI", VcardVersion.V4_0, true },
                { "CONTACT-URI", VcardVersion.V3_0, false },
                { "CONTACT-URI", VcardVersion.V2_1, false },
                { "fn", VcardVersion.V4_0, true }
            };
        }

        [Theory]
        [MemberData(nameof(DefinedData))]
        public void IsDefinedIn_PropertyAndVersion_SaysSo(string name, VcardVersion version, bool expected)
        {
            // arrange
            var info = VcardPropertyInfo.Find(name);

            // act
            var testResult = info.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<string, VcardCardinality> CardinalityData()
        {
            // ref. RFC 6350 §6, RFC 6474 §2, RFC 6715 §2 and RFC 8605 §2.1.
            return new TheoryData<string, VcardCardinality>
            {
                { "BEGIN", VcardCardinality.ExactlyOne },
                { "VERSION", VcardCardinality.ExactlyOne },
                { "FN", VcardCardinality.AtLeastOne },
                { "N", VcardCardinality.AtMostOne },
                { "UID", VcardCardinality.AtMostOne },
                { "TEL", VcardCardinality.Any },
                { "BIRTHPLACE", VcardCardinality.AtMostOne },
                { "DEATHPLACE", VcardCardinality.AtMostOne },
                { "DEATHDATE", VcardCardinality.AtMostOne },
                { "EXPERTISE", VcardCardinality.Any },
                { "HOBBY", VcardCardinality.Any },
                { "INTEREST", VcardCardinality.Any },
                { "ORG-DIRECTORY", VcardCardinality.Any },
                { "CONTACT-URI", VcardCardinality.Any }
            };
        }

        [Theory]
        [MemberData(nameof(CardinalityData))]
        public void Cardinality_Property_IsWhatTheRfcSays(string name, VcardCardinality expected)
        {
            // arrange
            var info = VcardPropertyInfo.Find(name);

            // act
            var testResult = info.Cardinality;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Find_NoVersionDefinesIt_ReturnsNull()
        {
            // arrange
            const string Name = "X-A";

            // act
            var testResult = VcardPropertyInfo.Find(Name);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Find_Null_ThrowsArgumentNullException()
        {
            // arrange
            const string Name = null;

            // act
            Action testAction = () => VcardPropertyInfo.Find(Name);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsDefinedIn_UnknownVersion_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const VcardVersion UnknownVersion = (VcardVersion)99;
            var info = VcardPropertyInfo.Find("FN");

            // act
            Action testAction = () => info.IsDefinedIn(UnknownVersion);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void All_EveryProperty_HasANameItCanBeFoundBy()
        {
            // arrange
            var all = VcardPropertyInfo.All;

            // act
            var testResult = all.Select(info => VcardPropertyInfo.Find(info.Name)).ToList();

            // assert
            testResult.Should().Equal(all);
        }

        [Fact]
        public void ToString_Info_GivesTheName()
        {
            // arrange
            var info = VcardPropertyInfo.Find("FN");

            // act
            var testResult = info.ToString();

            // assert
            testResult.Should().Be("FN");
        }
    }
}
