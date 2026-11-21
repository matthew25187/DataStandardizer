using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the <c>AGENT</c> of vCard 2.1, whose card follows its line in the data (vCard 2.1 §2.5.4), and which is <c>RELATED;TYPE=agent</c> in the model.
    /// </summary>
    public class Vcard21AgentTests
    {
        // ref. vCard 2.1 §2.5.4: the example, with the card on the lines after the AGENT line.
        private static readonly string Version21AgentExample = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John",
            "AGENT:",
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Friday;Fred",
            "TEL;WORK;VOICE:+1-213-555-1234",
            "TEL;WORK;FAX:+1-213-555-5678",
            "END:VCARD",
            "TEL;HOME:+1-213-555-0000",
            "END:VCARD");

        // ref. vCard 2.1 §2.9: "AGENT" [params] ":" vcard CRLF, so the card starts on the AGENT line.
        private static readonly string Version21AgentOnItsLine = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John",
            "AGENT:BEGIN:VCARD",
            "VERSION:2.1",
            "N:Friday;Fred",
            "TEL;WORK;VOICE:+1-213-555-1234",
            "TEL;WORK;FAX:+1-213-555-5678",
            "END:VCARD",
            "TEL;HOME:+1-213-555-0000",
            "END:VCARD");

        // What a writer of vCard 2.1 writes for either.
        private static readonly string WrittenAgent = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John;;;",
            "AGENT:BEGIN:VCARD",
            "VERSION:2.1",
            "N:Friday;Fred;;;",
            "TEL;WORK;VOICE:+1-213-555-1234",
            "TEL;WORK;FAX:+1-213-555-5678",
            "END:VCARD",
            "TEL;HOME:+1-213-555-0000",
            "END:VCARD");

        // The card in canonical form: its text, escaped once.
        private const string CanonicalAgent = "BEGIN:VCARD\\nVERSION:4.0\\nN:Friday;Fred;;;\\nTEL;TYPE=work\\,voice:+1-213-555-1234\\nTEL;TYPE=work\\,fax:+1-213-555-5678\\nEND:VCARD\\n";


        public static TheoryData<string> AgentExampleData()
        {
            return new TheoryData<string> { Version21AgentExample, Version21AgentOnItsLine };
        }

        [Theory]
        [MemberData(nameof(AgentExampleData))]
        public void ReadCard_NestedCard_HoldsItAsRelatedWithTypeAgent(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Related).Single();

            // assert
            testResult.Value.Should().Be(CanonicalAgent);
            Vcard30TestData.ParametersOf(testResult).Should().Equal("VALUE=text", "TYPE=agent");
        }

        [Theory]
        [MemberData(nameof(AgentExampleData))]
        public void ReadCard_NestedCard_ReadsIntoANestedCardAndGoesOnAfterIt(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Agents.Single().Card.Telephones.Select(telephone => telephone.Value.Text).Should().Equal("+1-213-555-1234", "+1-213-555-5678");
            testResult.Telephones.Single().Value.Text.Should().Be("+1-213-555-0000");
        }

        [Theory]
        [MemberData(nameof(AgentExampleData))]
        public void WriteCard_Version21_WritesTheCardAfterTheAgentLine(string text)
        {
            // arrange
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(WrittenAgent);
        }

        [Fact]
        public void WriteCard_Version40_WritesRelatedWithTypeAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Version21AgentExample);
            card.FormattedNames.Add("John Public");

            // act
            var testResult = Vcard30TestData.Unfold(VcardCardTestData.Write(card));

            // assert
            testResult.Should().Contain("\r\nRELATED;VALUE=text;TYPE=agent:BEGIN:VCARD\\nVERSION:4.0\\nN:Friday;Fred;;;\\nTEL;TYPE=work\\,voice:+1-213-555-1234\\nTEL;TYPE=work\\,fax:+1-213-555-5678\\nEND:VCARD\\n\r\n");
        }

        [Fact]
        public void WriteCard_Version30_WritesAnAgentThatHoldsTheCardAsText()
        {
            // arrange
            var card = VcardCardTestData.Read(Version21AgentExample);

            // act
            var testResult = Vcard30TestData.Unfold(Vcard30TestData.Write(card, VcardVersion.V3_0, allowMissingRequired: true));

            // assert
            testResult.Should().Contain("\r\nAGENT:BEGIN:VCARD\\nVERSION:3.0\\nN:Friday\\;Fred\\;\\;\\;\\n");
        }

        [Fact]
        public void WriteCard_Version30AgentAsVersion21_WritesTheCardAfterTheAgentLine()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:Fred\\nN:Friday\\;Fred\\;\\;\\;\\nEND:VCARD\\n"));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().EndWith("\r\nAGENT:BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Fred\r\nN:Friday;Fred;;;\r\nEND:VCARD\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void ReadCard_NestedCardWithNoName_ThrowsMissingRequiredProperty()
        {
            // arrange
            var text = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "FN:Fred", "END:VCARD");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void ReadCard_NestedCardWithNoVersion_ReadsItAsVersion21UnderAllowMissingRequiredProperties()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowMissingRequiredProperties = true };
            var text = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "N:Friday;Fred", "TEL;WORK:1", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text, options).Agents.Single().Card.Telephones.Single().Types;

            // assert
            testResult.Should().Equal("work");
        }

        [Fact]
        public void WriteCard_NestedCardWithNoName_ThrowsMissingRequiredProperty()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card());
            card.AddAgent(VcardCardTestData.Read(VcardCardTestData.Card()));

            // act
            Action testAction = () => Vcard21TestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        // ref. vCard 2.1 §2.1.8: a URL, and a Content-ID as a cid URI (RFC 2392 §2).
        private const string AgentUrlLine = "AGENT;VALUE=URL:http://example.com/fred.vcf";
        private const string AgentContentIdLine = "AGENT;VALUE=CONTENT-ID:<fred@host1.com>";

        public static TheoryData<string, string> AgentElsewhereData()
        {
            return new TheoryData<string, string>
            {
                { AgentUrlLine, "http://example.com/fred.vcf" },
                { AgentContentIdLine, "cid:fred@host1.com" }
            };
        }

        [Theory]
        [MemberData(nameof(AgentElsewhereData))]
        public void ReadCard_AgentElsewhere_HoldsItsUri(string line, string expectedUri)
        {
            // arrange
            var text = Vcard21TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Uri;

            // assert
            testResult.Should().Be(new Uri(expectedUri));
        }

        public static TheoryData<string, string> AgentElsewhereLineData()
        {
            return new TheoryData<string, string>
            {
                { AgentUrlLine, "BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nAGENT;VALUE=URL:http://example.com/fred.vcf\r\nEND:VCARD\r\n" },
                { AgentContentIdLine, "BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nAGENT;VALUE=CONTENT-ID:<fred@host1.com>\r\nEND:VCARD\r\n" }
            };
        }

        [Theory]
        [MemberData(nameof(AgentElsewhereLineData))]
        public void WriteCard_AgentElsewhere_WritesItsLocation(string line, string expected)
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card(line));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteCard_AgentThatIsTextOfNoCard_ThrowsLossy()
        {
            // arrange
            // ref. RFC 2426 §3.5.4: an AGENT can be reset to text, where vCard 2.1 §2.5.4 holds only a card.
            var card = VcardCardTestData.Read(Vcard30TestData.Card("AGENT;VALUE=text:Fred"));

            // act
            Action testAction = () => Vcard21TestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.Lossy);
        }

        [Fact]
        public void WriteCard_PreferredAgent_WritesPref()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card("AGENT;PREF;VALUE=URL:http://example.com/fred.vcf"));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nAGENT;VALUE=URL;PREF:http://example.com/fred.vcf\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void ReadCard_AgentWithAGroup_ReadsItsCard()
        {
            // arrange
            // ref. vCard 2.1 §2.9: the group is on the AGENT item, and the vcard of its value has none.
            var text = Vcard21TestData.Card("a.AGENT:BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Related).Single();

            // assert
            testResult.Group.Should().Be("a");
            testResult.Value.Should().Be("BEGIN:VCARD\\nVERSION:4.0\\nN:Friday;Fred;;;\\nEND:VCARD\\n");
        }

        [Fact]
        public void WriteCard_AgentLineLongerThanAPlainLine_WritesItsValueAsItIs()
        {
            // arrange
            // The value that starts the card is never quoted-printable, since a reader looks for it as it is.
            var card = VcardCardTestData.Read(Vcard21TestData.Card("AGENT;X-ABCDEFGHIJKLMNOPQRSTUVWXYZ=ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ:BEGIN:VCARD", "VERSION:2.1", "N:b", "END:VCARD"));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nAGENT;X-ABCDEFGHIJKLMNOPQRSTUVWXYZ=ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ:BEGIN:VCARD\r\nVERSION:2.1\r\nN:b;;;;\r\nEND:VCARD\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void ReadCard_CardInTheCardOfAnAgent_ReadsBoth()
        {
            // arrange
            var text = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:b", "AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:c", "END:VCARD", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Card.Agents.Single().Card.Name.Value.FamilyNames;

            // assert
            testResult.Should().Equal("c");
        }

        [Fact]
        public void WriteCard_CardInTheCardOfAnAgent_WritesBoth()
        {
            // arrange
            var text = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:b", "AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:c", "END:VCARD", "END:VCARD");
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nAGENT:BEGIN:VCARD\r\nVERSION:2.1\r\nN:b;;;;\r\nAGENT:BEGIN:VCARD\r\nVERSION:2.1\r\nN:c;;;;\r\nEND:VCARD\r\nEND:VCARD\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void ReadCard_DataEndsInTheCardOfAnAgent_ThrowsInvalidStructure()
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", "VERSION:2.1", "N:a", "AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:b", "END:VCARD");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
        }

        [Fact]
        public void ReadCard_CardsNestedPastTheLimit_ThrowsInvalidStructure()
        {
            // arrange
            var text = Vcard21TestData.Card(
                "AGENT:BEGIN:VCARD", "N:1", "AGENT:BEGIN:VCARD", "N:2", "AGENT:BEGIN:VCARD", "N:3", "AGENT:BEGIN:VCARD", "N:4",
                "AGENT:BEGIN:VCARD", "N:5", "AGENT:BEGIN:VCARD", "N:6", "AGENT:BEGIN:VCARD", "N:7", "AGENT:BEGIN:VCARD", "N:8",
                "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD");

            // act
            Action testAction = () => VcardCardTestData.Read(text, new VcardOptions { MaxLineLength = null, AllowMissingRequiredProperties = true });

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
        }

        // The names of a chain of cards, each held by an AGENT of the one before it, one past the limit of nesting: the card named "8" is nested in eight cards.
        private static readonly string[] NamesOfCardsNestedPastTheLimit = { "N:1", "N:2", "N:3", "N:4", "N:5", "N:6", "N:7", "N:8" };

        // What a writer of vCard 2.1 writes for the chain, under AllowLossyVersionConversion: the AGENT of the card named "7" is left out, since its card is nested too deeply.
        private static readonly string WrittenCardsNestedToTheLimit = VcardTestData.Crlf(
            "BEGIN:VCARD", "VERSION:2.1", "N:Doe;Jane;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:1;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:2;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:3;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:4;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:5;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:6;;;;",
            "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:7;;;;",
            "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD", "END:VCARD",
            "END:VCARD");

        public static TheoryData<VcardCard> CardsNestedPastTheLimitData()
        {
            // AddAgent holds a copy of the card, so the chain is built from its innermost card out.
            var held = VcardCardTestData.Read(Vcard21TestData.BareCard(NamesOfCardsNestedPastTheLimit[NamesOfCardsNestedPastTheLimit.Length - 1]));
            for (var index = NamesOfCardsNestedPastTheLimit.Length - 2; index >= 0; index--)
            {
                var outer = VcardCardTestData.Read(Vcard21TestData.BareCard(NamesOfCardsNestedPastTheLimit[index]));
                outer.AddAgent(held);
                held = outer;
            }

            var card = VcardCardTestData.Read(Vcard21TestData.Card());
            card.AddAgent(held);
            return new TheoryData<VcardCard> { card };
        }

        [Theory]
        [MemberData(nameof(CardsNestedPastTheLimitData))]
        public void WriteCard_CardsNestedPastTheLimit_ThrowsLossy(VcardCard card)
        {
            // arrange

            // act
            Action testAction = () => Vcard21TestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.Lossy);
        }

        [Theory]
        [MemberData(nameof(CardsNestedPastTheLimitData))]
        public void WriteCard_CardsNestedPastTheLimitUnderAllowLossyVersionConversion_WritesOnlyTheCardsWithinTheLimit(VcardCard card)
        {
            // arrange

            // act
            var testResult = Vcard21TestData.Write(card, allowLossy: true);

            // assert
            testResult.Should().Be(WrittenCardsNestedToTheLimit);
        }

        [Fact]
        public void ReadCard_CardsNestedToTheLimit_ReadsTheInnermostCard()
        {
            // arrange

            // act
            var testResult = VcardCardTestData.Read(WrittenCardsNestedToTheLimit)
                .Agents.Single().Card.Agents.Single().Card.Agents.Single().Card.Agents.Single().Card
                .Agents.Single().Card.Agents.Single().Card.Agents.Single().Card.Name.Value.FamilyNames;

            // assert
            testResult.Should().Equal("7");
        }

        [Fact]
        public void ReadCard_AgentWithNoCardAfterIt_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard21TestData.Card("AGENT:", "TEL:1");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        // ref. vCard 2.1 §2.1.8 and §2.5.1: a TEL at a URL, which RFC 6350 has no form for with a TYPE without a name, so a reader keeps the line as it is.
        private const string KeptLegacyLine = "TEL;WORK;VALUE=URL:http://example.com/tel";

        // The value of KeptLegacyLine.
        private const string TelephoneUrl = "http://example.com/tel";

        // ref. vCard 2.1 §2.1.4: "=ZZ" isn't a quoted-printable octet, so only AllowInvalidValues keeps the line, as it is.
        private const string UndecodableLine = "NOTE;ENCODING=QUOTED-PRINTABLE:=ZZ";

        private static readonly string AgentWithKeptLegacyLine = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", KeptLegacyLine, "END:VCARD");

        private static readonly string AgentWithUndecodableLine = Vcard21TestData.Card("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", UndecodableLine, "END:VCARD");

        // The cards in canonical form, with the line as it was read: a TYPE in vCard 4.0 is a parameter with a name (RFC 6350 §5.6), so the writer of the text names it.
        private const string CanonicalAgentWithKeptLegacyLine = "BEGIN:VCARD\\nVERSION:4.0\\nN:Friday;Fred;;;\\nTEL;TYPE=WORK;VALUE=URL:http://example.com/tel\\nEND:VCARD\\n";
        private const string CanonicalAgentWithUndecodableLine = "BEGIN:VCARD\\nVERSION:4.0\\nN:Friday;Fred;;;\\nNOTE;ENCODING=QUOTED-PRINTABLE:=ZZ\\nEND:VCARD\\n";

        // What a writer of vCard 2.1 writes for the card with the kept line.
        private static readonly string WrittenAgentWithKeptLegacyLine = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Doe;Jane;;;",
            "AGENT:BEGIN:VCARD",
            "VERSION:2.1",
            "N:Friday;Fred;;;",
            KeptLegacyLine,
            "END:VCARD",
            "END:VCARD");

        private static readonly VcardOptions AllowInvalidValues = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };

        public static TheoryData<string, VcardOptions, string> HeldLegacyLineData()
        {
            return new TheoryData<string, VcardOptions, string>
            {
                { AgentWithKeptLegacyLine, null, CanonicalAgentWithKeptLegacyLine },
                { AgentWithUndecodableLine, AllowInvalidValues, CanonicalAgentWithUndecodableLine }
            };
        }

        [Theory]
        [MemberData(nameof(HeldLegacyLineData))]
        public void ReadCard_NestedCardWithALineKeptInItsForm_HoldsTheLineAsItIs(string text, VcardOptions options, string expectedValue)
        {
            // act
            var testResult = VcardCardTestData.Read(text, options).GetProperties(VcardPropertyNames.Related).Single();

            // assert
            testResult.Value.Should().Be(expectedValue);
            Vcard30TestData.ParametersOf(testResult).Should().Equal("VALUE=text", "TYPE=agent");
        }

        public static TheoryData<string, VcardOptions> HeldLegacyCardData()
        {
            return new TheoryData<string, VcardOptions>
            {
                { AgentWithKeptLegacyLine, null },
                { AgentWithUndecodableLine, AllowInvalidValues }
            };
        }

        [Theory]
        [MemberData(nameof(HeldLegacyCardData))]
        public void ReadCards_NestedCardWithALineKeptInItsForm_ReadsTheCardAfterIt(string text, VcardOptions options)
        {
            // arrange
            var stream = VcardCardTestData.Stream(text, Vcard21TestData.Card(KeptLegacyLine));

            // act
            var testResult = VcardCardTestData.ReadAll(stream, options);

            // assert
            testResult.Should().HaveCount(2);
            testResult.Last().Properties.Last().Value.Should().Be(TelephoneUrl);
        }

        [Fact]
        public void ReadCard_NestedCardWithAnUndecodableValueByDefault_ThrowsInvalidValue()
        {
            // act
            Action testAction = () => VcardCardTestData.Read(AgentWithUndecodableLine);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void WriteCard_NestedCardWithALineKeptInItsFormAsVersion21_WritesTheLineAsItIs()
        {
            // arrange
            var card = VcardCardTestData.Read(AgentWithKeptLegacyLine);

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(WrittenAgentWithKeptLegacyLine);
        }
    }
}
