using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the <c>AGENT</c> of vCard 3.0, which is <c>RELATED;TYPE=agent</c> in the model.
    /// </summary>
    public class Vcard30AgentTests
    {
        // ref. RFC 2426 §3.5.4: the second example.
        private const string Rfc2426Agent = "AGENT:BEGIN:VCARD\\nFN:Susan Thomas\\nTEL:+1-919-555-1234\\nEMAIL\\;INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        // The same card in canonical form: its text, escaped once.
        private const string CanonicalAgent = "BEGIN:VCARD\\nVERSION:4.0\\nFN:Susan Thomas\\nTEL:+1-919-555-1234\\nEMAIL;TYPE=INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        // The same card written back as vCard 3.0.  The RFC's example has no VERSION, and this has it.
        private const string WrittenAgent = "AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:Susan Thomas\\nTEL:+1-919-555-1234\\nEMAIL\\;TYPE=INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        private const string CanonicalAgentLine = "RELATED;VALUE=text;TYPE=agent:" + CanonicalAgent;

        private const string AgentUri = "CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com";

        private const string AgentUriLine = "AGENT;VALUE=uri:" + AgentUri;

        [Fact]
        public void ReadCard_EscapedNestedCard_ReadsIntoANestedCard()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Agent);

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Card;

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
            testResult.FormattedNames.Single().Value.Should().Be("Susan Thomas");
            testResult.Emails.Single().Types.Should().Equal("INTERNET");
        }

        [Fact]
        public void ReadCard_EscapedNestedCard_HoldsItAsRelatedWithTypeAgent()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Agent);

            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Related).Single();

            // assert
            testResult.Value.Should().Be(CanonicalAgent);
            Vcard30TestData.ParametersOf(testResult).Should().Equal("VALUE=text", "TYPE=agent");
        }

        [Fact]
        public void WriteCard_NestedCardAsVersion40_WritesRelatedWithTypeAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Rfc2426Agent));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // assert
            testResult.Should().Be(VcardCardTestData.BareCard(Vcard30TestData.FormattedName, Vcard30TestData.Name, CanonicalAgentLine));
        }

        [Fact]
        public void WriteCard_NestedCardAsVersion30_WritesAgentAgain()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Rfc2426Agent));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(WrittenAgent));
        }

        [Fact]
        public void ReadCard_RelatedWithTypeAgentOfVersion40_GivesTheNestedCard()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Rfc2426Agent));
            var version40 = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // act
            var testResult = VcardCardTestData.Read(version40).Agents.Single().Card;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Susan Thomas");
        }

        [Fact]
        public void ReadCard_AgentOfAUri_HasTheUri()
        {
            // arrange
            var text = Vcard30TestData.Card(AgentUriLine);

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single();

            // assert
            testResult.Uri.OriginalString.Should().Be(AgentUri);
            testResult.Card.Should().BeNull();
            testResult.Text.Should().BeNull();
        }

        [Fact]
        public void ReadCard_AgentOfText_HasTheText()
        {
            // arrange
            var text = Vcard30TestData.Card("AGENT;VALUE=text:Jim");

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single();

            // assert
            testResult.Text.Should().Be("Jim");
            testResult.Card.Should().BeNull();
        }

        [Fact]
        public void ReadCard_NestedCardInANestedCard_ReadsEachLevel()
        {
            // arrange: each level of escaping is on top of the one inside it.
            var text = Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nFN:Inner\\nAGENT:BEGIN:VCARD\\\\nFN:Innermost\\\\nEND:VCARD\\\\n\\nEND:VCARD\\n");

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Card.Agents.Single().Card;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Innermost");
        }

        [Fact]
        public void WriteCard_NestedCardInANestedCard_RoundTripsThroughVersion30()
        {
            // arrange
            var first = VcardCardTestData.Read(Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nFN:Inner\\nAGENT:BEGIN:VCARD\\\\nFN:Innermost\\\\nEND:VCARD\\\\n\\nEND:VCARD\\n"));
            var written = Vcard30TestData.Write(first, VcardVersion.V3_0);

            // act
            var testResult = VcardCardTestData.Read(written).Agents.Single().Card.Agents.Single().Card;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Innermost");
        }

        [Fact]
        public void ReadCard_TwoCardsInAnAgent_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nFN:a\\nEND:VCARD\\nBEGIN:VCARD\\nFN:b\\nEND:VCARD\\n");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void Analyze_NestedCardWithWhatVersion30CanNotHold_ReportsItAsLossy()
        {
            // arrange
            var nested = VcardCardTestData.NewCard();
            nested.SetKind(VcardKind.Group);
            var card = VcardCardTestData.NewCard();
            card.AddAgent(nested);

            // act
            var testResult = VcardVersionConverter.Analyze(card, VcardVersion.V3_0);

            // assert
            testResult.Should().ContainSingle().Which.Kind.Should().Be(VcardDiagnosticKind.Lossy);
        }

        [Fact]
        public void AddAgent_Card_GivesItBackFromAgents()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var nested = VcardCardTestData.NewCard();

            // act
            card.AddAgent(nested);

            // assert
            card.Agents.Single().Card.FormattedNames.Single().Value.Should().Be(VcardCardTestData.FormattedNameValue);
            card.Validate().Should().BeEmpty();
        }

        [Fact]
        public void RemoveAgents_CardWithOtherRelated_LeavesTheOther()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("RELATED;TYPE=agent:http://example.com/a", "RELATED;TYPE=friend:http://example.com/b"));

            // act
            var testResult = card.RemoveAgents();

            // assert
            testResult.Should().Be(1);
            card.GetProperties(VcardPropertyNames.Related).Single().Value.Should().Be("http://example.com/b");
        }
    }
}
