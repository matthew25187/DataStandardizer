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
        // ref. RFC 2426 §3.5.4: the second example, whose card has no VERSION or N and a parameter with no name, which §4 and §5 don't allow.
        private const string Rfc2426Agent = "AGENT:BEGIN:VCARD\\nFN:Susan Thomas\\nTEL:+1-919-555-1234\\nEMAIL\\;INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        // The same agent, with a card that follows RFC 2426 §4 and §5.  It is also what a writer of vCard 3.0 gives for it.
        private const string ValidAgent = "AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:Susan Thomas\\nN:Thomas\\;Susan\\;\\;\\;\\nTEL:+1-919-555-1234\\nEMAIL\\;TYPE=INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        // The same card in canonical form: its text, escaped once.
        private const string CanonicalAgent = "BEGIN:VCARD\\nVERSION:4.0\\nFN:Susan Thomas\\nN:Thomas;Susan;;;\\nTEL:+1-919-555-1234\\nEMAIL;TYPE=INTERNET:sthomas@host.com\\nEND:VCARD\\n";

        private const string CanonicalAgentLine = "RELATED;VALUE=text;TYPE=agent:" + CanonicalAgent;

        private const string AgentUri = "CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com";

        private const string AgentUriLine = "AGENT;VALUE=uri:" + AgentUri;

        // An AGENT whose card has a comma that isn't escaped in a TEXT value, once its own escaping is removed.
        private const string AgentWithInvalidValue = "AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:a\\nN:a\\;\\;\\;\\;\\nNOTE:a\\,b\\nEND:VCARD\\n";

        private const int MaxNestingLevels = 8;

        private static readonly VcardOptions AcceptRfc2426Example = new VcardOptions { AllowMissingRequiredProperties = true, AcceptLegacySyntax = true };

        [Fact]
        public void ReadCard_EscapedNestedCard_ReadsIntoANestedCard()
        {
            // arrange
            var text = Vcard30TestData.Card(ValidAgent);

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
            var text = Vcard30TestData.Card(ValidAgent);

            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Related).Single();

            // assert
            testResult.Value.Should().Be(CanonicalAgent);
            Vcard30TestData.ParametersOf(testResult).Should().Equal("VALUE=text", "TYPE=agent");
        }

        [Fact]
        public void ReadCard_Rfc2426ExampleByDefault_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Agent);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void ReadCard_Rfc2426ExampleUnderItsOptions_ReadsIntoANestedCard()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Agent);

            // act
            var testResult = VcardCardTestData.Read(text, AcceptRfc2426Example).Agents.Single().Card;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Susan Thomas");
            testResult.Emails.Single().Types.Should().Equal("INTERNET");
        }

        [Fact]
        public void ReadCard_NestedCardWithAnInvalidValueByDefault_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard30TestData.Card(AgentWithInvalidValue);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void ReadCard_NestedCardWithAnInvalidValueUnderAllowInvalidValues_IsRead()
        {
            // arrange
            var text = Vcard30TestData.Card(AgentWithInvalidValue);
            var options = new VcardOptions { AllowInvalidValues = true };

            // act
            var testResult = VcardCardTestData.Read(text, options).Agents;

            // assert
            testResult.Single().Card.Should().NotBeNull();
        }

        [Fact]
        public void WriteCard_NestedCardAsVersion40_WritesRelatedWithTypeAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(ValidAgent));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // assert
            testResult.Should().Be(VcardCardTestData.BareCard(Vcard30TestData.FormattedName, Vcard30TestData.Name, CanonicalAgentLine));
        }

        [Fact]
        public void WriteCard_NestedCardAsVersion30_WritesAgentAgain()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(ValidAgent));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(ValidAgent));
        }

        [Fact]
        public void WriteCard_NestedCardWithNoNameByDefault_ThrowsMissingRequiredProperty()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Rfc2426Agent), AcceptRfc2426Example);

            // act
            Action testAction = () => Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void WriteCard_NestedCardWithNoNameUnderAllowMissingRequiredProperties_WritesIt()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Rfc2426Agent), AcceptRfc2426Example);

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0, false, true);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:Susan Thomas\\nTEL:+1-919-555-1234\\nEMAIL\\;TYPE=INTERNET:sthomas@host.com\\nEND:VCARD\\n"));
        }

        [Fact]
        public void ReadCard_RelatedWithTypeAgentOfVersion40_GivesTheNestedCard()
        {
            // arrange
            var text = VcardCardTestData.Card(CanonicalAgentLine);

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Card;

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
            // arrange
            var text = Vcard30TestData.Card(Vcard30TestData.NestedAgentLine(2));

            // act
            var testResult = VcardCardTestData.Read(text).Agents.Single().Card.Agents.Single().Card;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("a");
        }

        [Fact]
        public void WriteCard_NestedCardInANestedCard_RoundTripsThroughVersion30()
        {
            // arrange
            var line = Vcard30TestData.NestedAgentLine(2);
            var card = VcardCardTestData.Read(Vcard30TestData.Card(line));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(line));
        }

        [Fact]
        public void ReadCard_NestingAtTheLimit_IsAccepted()
        {
            // arrange
            var text = Vcard30TestData.Card(Vcard30TestData.NestedAgentLine(MaxNestingLevels));

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Agents.Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_NestingPastTheLimit_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard30TestData.Card(Vcard30TestData.NestedAgentLine(MaxNestingLevels + 1));

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void ReadCard_TwoCardsInAnAgent_ThrowsInvalidValue()
        {
            // arrange
            var text = Vcard30TestData.Card("AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:a\\nN:a\\;\\;\\;\\;\\nEND:VCARD\\nBEGIN:VCARD\\nVERSION:3.0\\nFN:b\\nN:b\\;\\;\\;\\;\\nEND:VCARD\\n");

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
