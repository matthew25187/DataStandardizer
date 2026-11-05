using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DataStandardizer.Communication.Mime;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Builds cards, and readers and writers of them, for the tests of the card object model.
    /// </summary>
    internal static class VcardCardTestData
    {
        public const string Begin = "BEGIN:VCARD";
        public const string End = "END:VCARD";
        public const string Version40 = "VERSION:4.0";
        public const string FormattedNameLine = "FN:Jane Doe";
        public const string FormattedNameValue = "Jane Doe";

        /// <summary>
        /// The text of two minimal cards, as <see cref="Card"/> gives one.
        /// </summary>
        public static readonly string TwoMinimalCards = Stream(Card(), Card());

        /// <summary>
        /// A card with no <c>FN</c>, which breaks a rule of RFC 6350 §6.2.1, followed by a minimal card.
        /// </summary>
        public static readonly string NonconformingCardThenCard = Stream(BareCard("NOTE:a"), Card());

        /// <summary>
        /// A card with the lines of a minimal vCard 4.0 card around the given properties, and no <c>FN</c> of its own.
        /// </summary>
        public static string BareCard(params string[] properties)
        {
            var lines = new List<string> { Begin, Version40 };
            lines.AddRange(properties);
            lines.Add(End);
            return VcardTestData.Crlf(lines.ToArray());
        }

        /// <summary>
        /// A minimal vCard 4.0 card, with an <c>FN</c>, and the given properties after it.
        /// </summary>
        public static string Card(params string[] properties)
        {
            var lines = new List<string> { FormattedNameLine };
            lines.AddRange(properties);
            return BareCard(lines.ToArray());
        }

        /// <summary>
        /// Puts the text of cards one after another in a stream.
        /// </summary>
        public static string Stream(params string[] cards)
        {
            return string.Concat(cards);
        }

        /// <summary>
        /// Gets the name and value of each property of a card.
        /// </summary>
        public static List<string> NamesAndValuesOf(VcardCard card)
        {
            return card.Properties.Select(line => line.Name + ":" + line.Value).ToList();
        }

        /// <summary>
        /// Reads the first card of some text.
        /// </summary>
        public static VcardCard Read(string text, VcardOptions options = null)
        {
            using (var reader = new VcardReader(new StringReader(text), options ?? new VcardOptions()))
            {
                return reader.ReadCard();
            }
        }

        /// <summary>
        /// Reads a card, and leaves a <see cref="VcardFileException"/> for the test to go on from.
        /// </summary>
        public static void ReadPastFailure(VcardReader reader)
        {
            try
            {
                reader.ReadCard();
            }
            catch (VcardFileException)
            {
                return;
            }

            Assert.Fail("The read was expected to fail.");
        }

        /// <summary>
        /// Reads a card, and leaves the <see cref="ArithmeticException"/> a test's character set resolver throws for the test to go on from.
        /// </summary>
        public static void ReadPastResolverFailure(VcardReader reader)
        {
            try
            {
                reader.ReadCard();
            }
            catch (ArithmeticException)
            {
                return;
            }

            Assert.Fail("The read was expected to fail.");
        }

        /// <summary>
        /// Reads a card asynchronously, and leaves a <see cref="VcardFileException"/> or a cancellation for the test to go on from.
        /// </summary>
        public static async Task ReadPastFailureAsync(VcardReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await reader.ReadCardAsync(cancellationToken);
            }
            catch (VcardFileException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Assert.Fail("The read was expected to fail or be canceled.");
        }

        /// <summary>
        /// Writes a card asynchronously, and leaves a cancellation for the test to go on from.
        /// </summary>
        public static async Task WritePastCancellationAsync(VcardWriter writer, VcardCard card, CancellationToken cancellationToken)
        {
            try
            {
                await writer.WriteCardAsync(card, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Assert.Fail("The write was expected to be canceled.");
        }

        /// <summary>
        /// Writes cards and gives the text.
        /// </summary>
        public static string Write(VcardCard card, VcardOptions options = null)
        {
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options ?? new VcardOptions()))
            {
                writer.WriteCard(card);
            }

            return output.ToString();
        }

        /// <summary>
        /// A card built in code with an <c>FN</c>.
        /// </summary>
        public static VcardCard NewCard()
        {
            var card = new VcardCard();
            card.FormattedNames.Add("Jane Doe");
            return card;
        }

        /// <summary>
        /// A character set resolver that runs an action when it is asked for anything, then says it knows UTF-8.  For a test of what happens part way through checking a card.
        /// </summary>
        internal sealed class CallbackCharsetResolver : IMimeCharsetResolver
        {
            /// <summary>
            /// Gets or sets what to run.
            /// </summary>
            public Action OnResolve { get; set; }

            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                OnResolve();
                encoding = Encoding.UTF8;
                return true;
            }
        }

        /// <summary>
        /// Gets the kinds of a list of diagnostics.
        /// </summary>
        public static List<VcardDiagnosticKind> KindsOf(IEnumerable<VcardDiagnostic> diagnostics)
        {
            return diagnostics.Select(diagnostic => diagnostic.Kind).ToList();
        }

        /// <summary>
        /// Gets each list of alternatives as the raw values of its lines.
        /// </summary>
        public static List<List<string>> ValuesOf(IEnumerable<IReadOnlyList<VcardContentLine>> groups)
        {
            return groups.Select(group => group.Select(line => line.Value).ToList()).ToList();
        }
    }
}
