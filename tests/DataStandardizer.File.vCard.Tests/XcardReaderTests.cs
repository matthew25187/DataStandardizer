using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;
using static DataStandardizer.File.vCard.Tests.XcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="XcardReader"/>.
    /// </summary>
    public class XcardReaderTests
    {
        private const string MinimalDocument = "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard></vcards>";
        private const string TwoCards =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard><vcard><fn><text>B</text></fn><note><text>C</text></note></vcard></vcards>";

        // A first card with no FN, which RFC 6350 §6.2.1 requires, then a valid one.
        private const string CardWithoutFnThenValidCard =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><note><text>A</text></note></vcard><vcard><fn><text>B</text></fn></vcard></vcards>";

        // ref. RFC 6351 §5: the example of a group, whose properties are the FN and EMAIL of the group "contact".
        private const string ContactGroupName = "contact";

        private const string FormattedName = "A";

        private const string ContactGroupExample =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn><group name=\"contact\"><note><text>a</text></note></group></vcard></vcards>";

        // ISO-8859-1, which has one octet for each character.
        private const int Latin1CodePage = 28591;

        public static TheoryData<string, string> Properties() => new TheoryData<string, string>
        {
            // ref. RFC 6351 §5: a group element gives its properties its name, as it is.
            { CardStart + "<group name=\"contact\"><email><text>a@example.com</text></email></group>" + CardEnd, "contact.EMAIL:a@example.com" },

            // ref. RFC 6351 Appendix A: value-text-list, one element for each value of NICKNAME and CATEGORIES.
            { CardStart + "<categories><text>a,b</text><text>c</text></categories>" + CardEnd, "CATEGORIES:a\\,b,c" },
            { CardStart + "<nickname><text>Jim</text><text>Jimmie</text></nickname>" + CardEnd, "NICKNAME:Jim,Jimmie" },

            // ref. RFC 6351 Appendix A §6.2.2, §6.3.1, §6.6.4, §6.2.7 and §6.7.7: the structured values.
            {
                CardStart + "<n><surname>van der Harten</surname><given>Rene</given><additional>J.</additional><prefix>Sir</prefix><suffix>R.D.O.N.</suffix></n>" + CardEnd,
                "N:van der Harten;Rene;J.;Sir;R.D.O.N."
            },
            {
                CardStart + "<adr><pobox/><ext/><street>My Street</street><street>Left Side</street><locality>Hometown</locality><region>PA</region><code>18252</code>" +
                "<country>U.S.A.</country></adr>" + CardEnd,
                "ADR:;;My Street,Left Side;Hometown;PA;18252;U.S.A."
            },
            { CardStart + "<org><text>A;B</text><text>C</text></org>" + CardEnd, "ORG:A\\;B;C" },
            { CardStart + "<gender><sex>F</sex><identity>grrrl</identity></gender>" + CardEnd, "GENDER:F;grrrl" },
            { CardStart + "<gender><sex/><identity>it's complicated</identity></gender>" + CardEnd, "GENDER:;it's complicated" },
            { CardStart + "<clientpidmap><sourceid> 1 </sourceid><uri>urn:a,b</uri></clientpidmap>" + CardEnd, "CLIENTPIDMAP:1;urn:a\\,b" },

            // ref. RFC 9554 §2: the components it adds, whose elements are those agreed for this package.
            {
                CardStart + "<n><surname>Doe</surname><given>Jane</given><additional/><prefix/><suffix/><surname2/><generation>III</generation></n>" + CardEnd,
                "N:Doe;Jane;;;;;III"
            },
            {
                CardStart + "<adr><pobox/><ext/><street/><locality>Town</locality><region/><code/><country/><room>12</room><apartment/><floor/><streetnumber>54</streetnumber>" +
                "<streetname/><building/><block/><subdistrict/><district/><landmark/><direction/></adr>" + CardEnd,
                "ADR:;;;Town;;;;12;;;54;;;;;;;"
            },

            // ref. RFC 6351 Appendix A §4.3.4: a DATE-AND-OR-TIME is in a date, date-time or time element, and the time has no "T" in xCard.
            { CardStart + "<bday><date>--0203</date></bday>" + CardEnd, "BDAY:--0203" },
            { CardStart + "<bday><date-time>20090808T1430-0500</date-time></bday>" + CardEnd, "BDAY:20090808T1430-0500" },
            { CardStart + "<bday><time>1230</time></bday>" + CardEnd, "BDAY:T1230" },
            { CardStart + "<bday><text>circa 1800</text></bday>" + CardEnd, "BDAY;VALUE=text:circa 1800" },

            // ref. XML Schema Part 2 §3.2.2, §3.3.13 and §3.2.4: xsd:boolean, xsd:integer and xsd:float, whose white space is collapsed, in the text form of vCard.
            { CardStart + "<x-a><boolean> 1 </boolean></x-a>" + CardEnd, "X-A;VALUE=boolean:TRUE" },
            { CardStart + "<x-a><integer> +42 </integer></x-a>" + CardEnd, "X-A;VALUE=integer:+42" },
            { CardStart + "<x-a><float>1.5E3</float></x-a>" + CardEnd, "X-A;VALUE=float:1500" },
            { CardStart + "<x-a><float>.5</float></x-a>" + CardEnd, "X-A;VALUE=float:0.5" },
            { CardStart + "<tz><utc-offset>-0500</utc-offset></tz>" + CardEnd, "TZ;VALUE=utc-offset:-0500" },
            { CardStart + "<rev><timestamp>19990104T122100Z</timestamp></rev>" + CardEnd, "REV:19990104T122100Z" },
            { CardStart + "<url><uri> http://example.com/a,b </uri></url>" + CardEnd, "URL:http://example.com/a\\,b" },

            // ref. RFC 6351 §6: an unknown element is taken as it is, with no VALUE parameter, and a value element of a property whose type isn't known adds one.
            { CardStart + "<x-coffee-data><unknown>Stenophylla;Guinea\\,Africa</unknown></x-coffee-data>" + CardEnd, "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa" },
            { CardStart + "<x-my-prop><parameters><pref><integer>1</integer></pref></parameters><text>value goes here</text></x-my-prop>" + CardEnd, "X-MY-PROP;PREF=1;VALUE=text:value goes here" },
            { CardStart + "<x-a><text>a,b</text><text>c</text></x-a>" + CardEnd, "X-A;VALUE=text:a\\,b,c" },
            { CardStart + "<note><text>Line one\nLine two</text></note>" + CardEnd, "NOTE:Line one\\nLine two" },
            { CardStart + "<note><text><![CDATA[a<b]]></text></note>" + CardEnd, "NOTE:a<b" },

            // ref. RFC 6351 §5.1 and §6: parameters, with their values in the elements of their types, and unknown for one that isn't recognised.
            { CardStart + "<tel><parameters><type><text>work</text><text>voice</text></type></parameters><uri>tel:+1-555-0100</uri></tel>" + CardEnd, "TEL;TYPE=work|voice;VALUE=uri:tel:+1-555-0100" },
            { CardStart + "<gender><parameters><x-probability><unknown>0.8</unknown></x-probability></parameters><sex>M</sex></gender>" + CardEnd, "GENDER;X-PROBABILITY=0.8:M" },
            { CardStart + "<note><parameters><derived><boolean>1</boolean></derived></parameters><text>a</text></note>" + CardEnd, "NOTE;DERIVED=TRUE:a" },
            { CardStart + "<adr><parameters><tz><uri>urn:a</uri></tz></parameters><pobox/><ext/><street/><locality/><region/><code/><country/></adr>" + CardEnd, "ADR;TZ=urn:a:;;;;;;" },
            { CardStart + "<note><parameters><pref><integer> 1 </integer></pref></parameters><text>a</text></note>" + CardEnd, "NOTE;PREF=1:a" },

            // ref. RFC 6351 §6: a property in another namespace is wrapped as it is in an XML property, in its group.
            { CardStart + "<a xmlns=\"http://www.w3.org/1999/xhtml\" href=\"http://www.example.com\">My web page!</a>" + CardEnd, "XML:<a xmlns=\"http://www.w3.org/1999/xhtml\" href=\"http://www.example.com\">My web page!</a>" },
            { CardStart + "<group name=\"g\"><ext:foo xmlns:ext=\"http://e\" a=\"1\">t<ext:b/></ext:foo></group>" + CardEnd, "g.XML:<ext:foo xmlns:ext=\"http://e\" a=\"1\">t<ext:b /></ext:foo>" },

            // ref. RFC 6351 §5.1: an element or attribute whose name isn't recognised is ignored, as are comments and processing instructions.
            { CardStart + "<note><other:x xmlns:other=\"http://o\"/><text>a</text></note>" + CardEnd, "NOTE:a" },
            { CardStart + "<note><x-unknown/><text>a</text></note>" + CardEnd, "NOTE:a" },
            { CardStart + "<note other=\"1\"><text>a</text></note>" + CardEnd, "NOTE:a" },
            { CardStart + "<note><!-- a comment --><?target data?><text>a</text></note>" + CardEnd, "NOTE:a" },
            { CardStart + "<unnamespaced xmlns=\"\"><text>a</text></unnamespaced>" + CardEnd, "FN:A" },
            { "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><x-other/><vcard><fn><text>A</text></fn></vcard></vcards>", "FN:A" },
        };

        [Theory]
        [MemberData(nameof(Properties))]
        public void ReadCard_Property_GivesTheContentLine(string xml, string expected)
        {
            // act
            var testResult = ReadLastXcardProperty(xml);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<string> StructureErrors() => new TheoryData<string>
        {
            // ref. RFC 6351 §5: the root is vcards in the namespace of xCard, which MUST be present even for one card.
            "<vcard xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><fn><text>A</text></fn></vcard>",
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-3.0\"><vcard><fn><text>A</text></fn></vcard></vcards>",

            // ref. RFC 6351 §5 and §5.1: the vcard element and the namespace take the place of BEGIN, END and VERSION.
            CardStart + "<version><text>4.0</text></version>" + CardEnd,
            CardStart + "<begin><text>VCARD</text></begin>" + CardEnd,
            CardStart + "<end><text>VCARD</text></end>" + CardEnd,

            // ref. RFC 6351 §5.1: names are in lower case.
            CardStart + "<NOTE><text>a</text></NOTE>" + CardEnd,
            CardStart + "<note><parameters><PREF><integer>1</integer></PREF></parameters><text>a</text></note>" + CardEnd,

            // ref. RFC 6351 Appendix A: the parameters element comes first, and the value element gives the type, so there is no value parameter.
            CardStart + "<note><text>a</text><parameters/></note>" + CardEnd,
            CardStart + "<note><parameters/><parameters/><text>a</text></note>" + CardEnd,
            CardStart + "<note><parameters><value><text>text</text></value></parameters><text>a</text></note>" + CardEnd,
            CardStart + "<note><parameters><pref/></parameters><text>a</text></note>" + CardEnd,

            // A property holds elements, a value element holds text, and a property has a value of one type.
            CardStart + "<note>a<text>a</text></note>" + CardEnd,
            CardStart + "<note><text>a<b/></text></note>" + CardEnd,
            CardStart + "<note><x-unknown/></note>" + CardEnd,
            CardStart + "<x-a><text>a</text><integer>1</integer></x-a>" + CardEnd,
            CardStart + "<note><text>a</text><text>b</text></note>" + CardEnd,
            CardStart + "<x-a><unknown>a</unknown><unknown>b</unknown></x-a>" + CardEnd,
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>a<fn><text>A</text></fn></vcard></vcards>",

            // ref. RFC 6351 Appendix A: a group has a name, and holds properties; the name is a group of RFC 6350 §3.3.
            CardStart + "<group name=\"a\"><group name=\"b\"/></group>" + CardEnd,
            CardStart + "<group><note><text>a</text></note></group>" + CardEnd,
            CardStart + "<group name=\"a.b\"><note><text>a</text></note></group>" + CardEnd,

            // ref. RFC 6351 Appendix A: the components of a structured value are in order, and only those of N and ADR repeat.
            CardStart + "<n><given>Jane</given><surname>Doe</surname><additional/><prefix/><suffix/></n>" + CardEnd,
            CardStart + "<gender><sex>M</sex><sex>F</sex></gender>" + CardEnd,
            CardStart + "<n><surname>Doe</surname><text>Jane</text></n>" + CardEnd,
        };

        [Theory]
        [MemberData(nameof(StructureErrors))]
        public void ReadCard_StructureNotOfXcard_ThrowsInvalidStructure(string xml)
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(xml)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        public static TheoryData<string, string> ValuesNotInXcardForm() => new TheoryData<string, string>
        {
            // ref. RFC 6351 Appendix A §4.3.1: value-date has no year alone, and is in the basic format.
            { CardStart + "<bday><date>1985</date></bday>" + CardEnd, "BDAY:1985" },
            { CardStart + "<x-a><date>1985-04-12</date></x-a>" + CardEnd, "X-A;VALUE=date:1985-04-12" },

            // ref. XML Schema Part 2 §3.2.2.1 and §3.2.4.1: TRUE isn't an xsd:boolean, and INF isn't a FLOAT of vCard.
            { CardStart + "<x-a><boolean>TRUE</boolean></x-a>" + CardEnd, "X-A;VALUE=boolean:TRUE" },
            { CardStart + "<x-a><float>INF</float></x-a>" + CardEnd, "X-A;VALUE=float:INF" },

            // ref. RFC 6351 Appendix A: each component of RFC 6350 has its element, GENDER has a sex element, and a CLIENTPIDMAP source is an xsd:positiveInteger.
            { CardStart + "<n><surname>Doe</surname><prefix>Dr.</prefix></n>" + CardEnd, "N:Doe;;;Dr.;" },
            { CardStart + "<gender><identity>a</identity></gender>" + CardEnd, "GENDER:;a" },
            { CardStart + "<gender><sex>m</sex></gender>" + CardEnd, "GENDER:m" },

            // Once a component of RFC 9554 is there, each of its components has an element, as the writer writes them.
            { CardStart + "<n><surname>Doe</surname><given>Jane</given><additional/><prefix/><suffix/><generation>III</generation></n>" + CardEnd, "N:Doe;Jane;;;;;III" },
            {
                CardStart + "<adr><pobox/><ext/><street/><locality>Town</locality><region/><code/><country/><room>12</room><streetnumber>54</streetnumber></adr>" + CardEnd,
                "ADR:;;;Town;;;;12;;;54;;;;;;;"
            },
            { CardStart + "<clientpidmap><sourceid>0</sourceid><uri>urn:a</uri></clientpidmap>" + CardEnd, "CLIENTPIDMAP:0;urn:a" },

            // ref. RFC 6351 Appendix A §5.2: the value of PREF is an integer element.
            { CardStart + "<note><parameters><pref><text>1</text></pref></parameters><text>a</text></note>" + CardEnd, "NOTE;PREF=1:a" },
        };

        [Theory]
        [MemberData(nameof(ValuesNotInXcardForm))]
        public void ReadCard_ValueNotInXcardForm_ThrowsInvalidValue(string xml, string _)
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(xml)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidValue);
            }
        }

        [Theory]
        [MemberData(nameof(ValuesNotInXcardForm))]
        public void ReadCard_ValueNotInXcardFormWithInvalidValuesAllowed_KeepsTheValue(string xml, string expected)
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };

            // act
            var testResult = ReadLastXcardProperty(xml, options);

            // assert
            testResult.Should().Be(expected);
        }

        // Found by fuzzing.  An XML declaration whose version is followed by sixteen U+00A0 makes the XmlReader of .NET 10 throw an ArgumentException of its own.
        private const string DeclarationWithGarbledVersion = "<?xml version=\"1.0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\x00A0\" encoding=\"UTF-8\"?><avc>";

        [Fact]
        public void ReadCard_DeclarationWithGarbledVersion_Throws()
        {
            // arrange
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(DeclarationWithGarbledVersion);
            using (var reader = new XcardReader(new MemoryStream(bytes)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadCard_DocumentTypeDefinition_Throws()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader("<!DOCTYPE vcards []><vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"/>")))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadCard_DocumentTypeDefinitionFromAnXmlReaderThatParsesIt_Throws()
        {
            // arrange
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
            using (var xmlReader = XmlReader.Create(new StringReader("<!DOCTYPE vcards []><vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"/>"), settings))
            using (var reader = new XcardReader(xmlReader))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        [Fact]
        public void ReadCard_StructureError_GivesTheLineAndPositionOfTheElement()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader("<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n<vcard>\n  <NOTE/>\n</vcard>\n</vcards>")))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                var data = testAction.Should().Throw<VcardFileException>().Which.Data;
                (data[VcardFileException.DataItemName.LineNumber], data[VcardFileException.DataItemName.LinePosition]).Should().Be((3L, 4L));
            }
        }

        [Fact]
        public void ReadCard_TextValueThatFailsToRead_ThrowsVcardFileException()
        {
            // arrange
            using (var reader = new XcardReader(new ValueFailingXmlReader(XmlReader.Create(new StringReader(MinimalDocument)))))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>().WithInnerException<XmlException>();
            }
        }

        [Fact]
        public void ReadCard_AfterATextValueFailedToRead_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = new XcardReader(new ValueFailingXmlReader(XmlReader.Create(new StringReader(MinimalDocument)))))
            {
                ReadPastFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_NotWellFormedXml_ThrowsWithTheLine()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader("<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n<vcard>\n</vcards>")))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.LineNumber].Should().Be(3L);
            }
        }

        [Fact]
        public void ReadCard_AfterAStructureError_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader("<vcard xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"/>")))
            {
                ReadPastFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_AfterACardThatFailsItsCheck_ReadsTheNextCard()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(CardWithoutFnThenValidCard)))
            {
                ReadPastFailure(reader);

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.FormattedNames[0].Value.Should().Be("B");
            }
        }

        [Fact]
        public void ReadCards_TwoCards_ReadsBoth()
        {
            // act
            var testResult = Describe(ReadXcard(TwoCards));

            // assert
            testResult.Should().BeEquivalentTo(new[] { new[] { "FN:A" }, new[] { "FN:B", "NOTE:C" } }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void Counters_TwoCardsRead_CountTheCardsAndPropertiesWithoutVersion()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(TwoCards)))
            {
                // act
                var cards = reader.ReadCards().ToList();
                var testResult = (reader.CardsRead, reader.PropertiesRead);

                // assert
                testResult.Should().Be((2L, 3L));
            }
        }

        [Fact]
        public void ReadCard_AfterTheLastCard_ReturnsNull()
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(MinimalDocument)))
            {
                reader.ReadCard();

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.Should().BeNull();
            }
        }

        public static TheoryData<string> DocumentsWithNoCard() => new TheoryData<string>
        {
            // ref. RFC 6351 Appendix A: vcards holds one or more vcard elements.
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"/>",
            "<?xml version=\"1.0\"?>",
        };

        [Theory]
        [MemberData(nameof(DocumentsWithNoCard))]
        public void ReadCard_DocumentWithNoCard_Throws(string xml)
        {
            // arrange
            using (var reader = new XcardReader(new StringReader(xml)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadCard_FromAnXmlReaderInALargerDocument_LeavesItOnTheEndOfVcards()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader("<outer><vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard></vcards><after/></outer>")))
            {
                xmlReader.ReadStartElement("outer");
                using (var reader = new XcardReader(xmlReader))
                {
                    // act
                    var cards = reader.ReadCards().ToList();
                    var testResult = (cards.Count, xmlReader.NodeType, xmlReader.LocalName);

                    // assert
                    testResult.Should().Be((1, XmlNodeType.EndElement, XcardNamesForTests.VCards));
                }
            }
        }

        [Fact]
        public void ReadCard_StreamInLatin1WithTheEncodingOption_DecodesIt()
        {
            // arrange
            var latin1 = Encoding.GetEncoding(Latin1CodePage);
            var bytes = latin1.GetBytes("<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>\x00E9</text></fn></vcard></vcards>");
            var options = new VcardOptions { MaxLineLength = null, Encoding = latin1 };
            using (var reader = new XcardReader(new MemoryStream(bytes), options))
            {
                // act
                var testResult = reader.ReadCard().FormattedNames[0].Value;

                // assert
                testResult.Should().Be("\x00E9");
            }
        }

        public static TheoryData<Action> ConstructorsWithBadArguments() => new TheoryData<Action>
        {
            () => new XcardReader((Stream)null),
            () => new XcardReader(new MemoryStream(), null),
            () => new XcardReader((TextReader)null),
            () => new XcardReader(new StringReader(string.Empty), new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 }),
            () => new XcardReader((XmlReader)null),
            () => new XcardReader(XmlReader.Create(new StringReader(string.Empty)), new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 }),
            () => new XcardReader(new WriteOnlyStream()),
        };

        [Theory]
        [MemberData(nameof(ConstructorsWithBadArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_BadArgument_ThrowsArgumentException(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ReadCard_Disposed_ThrowsObjectDisposed()
        {
            // arrange
            var reader = new XcardReader(new StringReader(MinimalDocument));
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadCard();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadCard_MinimalDocument_HasTheFormattedName()
        {
            // act
            var testResult = ReadXcard(MinimalDocument)[0].FormattedNames[0].Value;

            // assert
            testResult.Should().Be(FormattedName);
        }

        [Fact]
        public void ReadCard_GroupExample_IsInTheGroup()
        {
            // act
            var testResult = ReadXcard(ContactGroupExample)[0].Properties[1].Group;

            // assert
            testResult.Should().Be(ContactGroupName);
        }

        /// <summary>
        /// Reads a card, and leaves a <see cref="VcardFileException"/> for the test to go on from.
        /// </summary>
        private static void ReadPastFailure(XcardReader reader)
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
        /// The element names of xCard the tests use.
        /// </summary>
        private static class XcardNamesForTests
        {
            public const string VCards = "vcards";
        }

        /// <summary>
        /// A stream that can only be written to.
        /// </summary>
        private sealed class WriteOnlyStream : MemoryStream
        {
            public override bool CanRead => false;
        }
    }
}
