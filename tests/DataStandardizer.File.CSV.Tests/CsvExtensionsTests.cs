using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{

    public class CsvExtensionsTests
    {
        private const int TestMappingConstantValue = 100;

        // Field names that differ from the property keys they are mapped to, the values in them, and a file that holds them.
        private const string PersonIdFieldName = "person_id";
        private const string PersonNameFieldName = "person_name";
        private const int RenamedId = 5;
        private const string RenamedName = "bob";
        private const string RenamedRecordFile = "5,bob";

        // A name copied between a line and a custom model through a property the model inherits.
        private const string InheritedName = "alice";

        // The field index that HidingMappedLine maps its hiding Name property to.
        private const int HidingNameFieldIndex = 1;

        // The text of RenamedId as a reader stores it for a field that no mapping converts.
        private const string RenamedIdText = "5";

        // A line's field text that isn't a valid Int32.
        private const string InvalidIdText = "five";

        // Values that a mapping's converter, constant or generator supplies instead of the field's.
        private const string ConvertedName = "converted";
        private const int ConstantId = 7;
        private const int GeneratedId = 9;
        private const string ConstantDescription = "constant";
        private const string GeneratedDescription = "generated";
        private const string ModelDescription = "model";

        // The fields of a line whose names no mapping uses, and the position of the second.
        private const string FirstFieldName = "x";
        private const string FirstFieldValue = "a";
        private const string SecondFieldName = "y";
        private const string SecondFieldValue = "b";
        private const int SecondFieldIndex = 1;

        // A field position past the last field of a line with two fields.
        private const int PastLastFieldIndex = 2;

        [Fact]
        public void CreateMapper_DeclarativeMappingFieldIndex_ReturnsFieldMappingWithIndex()
        {
            // arrange
            var testLine = new TestMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Id))
                .WhoseValue.FieldIndex.Should().Be(0);
        }

        [Fact]
        public void CreateMapper_DeclarativeMappingFieldNames_ReturnsFieldMappingWithNames()
        {
            // arrange
            var testLine = new TestMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Name))
                .WhoseValue.FieldName.Should().Be(nameof(TestMappedLine.Name));
        }

        [Fact]
        public void CreateMapper_DeclarativeMappingConstantValue_ReturnsFieldMappingWithValue()
        {
            // arrange
            var testLine = new TestMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Id))
                .WhoseValue.ConstantValue.Should().Be(TestMappingConstantValue);
        }

        [Fact]
        public void CreateMapper_DeclarativeMappingIsOptional_ReturnsFieldMappingWithOptionalFlag()
        {
            // arrange
            var testLine = new TestMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Name))
                .WhoseValue.IsOptional.Should().BeTrue();
        }

        [Fact]
        public void CreateMapper_DeclarativeMappingTypeConverter_ReturnsFieldMappingWithTypeConverterType()
        {
            // arrange
            var testLine = new TestMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Id))
                .WhoseValue.TypeConverterType.Should().Be(typeof(Int32Converter));
        }

        [Fact]
        public void ToObject_FieldMappedByIndex_CopiesValueFromLineObjectToCustomObject()
        {
            // arrange
            const int identifier = 100;
            var testLine = new TestLine { Id = identifier, Name = "One" };

            var mapper = new TestModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(identifier);
        }

        [Fact]
        public void CreateMapper_CalledRepeatedly_ReturnsIndependentMappersWithSameMappings()
        {
            // arrange
            var firstMapper = new TestMappedLine().CreateMapper();
            var testLine = new TestMappedLine();

            // act
            var secondMapper = testLine.CreateMapper();

            // assert
            secondMapper.Should().NotBeSameAs(firstMapper);
            secondMapper.Keys.Should().BeEquivalentTo(firstMapper.Keys);
            secondMapper[nameof(TestMappedLine.Id)].Should().NotBeSameAs(firstMapper[nameof(TestMappedLine.Id)]);
            secondMapper[nameof(TestMappedLine.Id)].Should().BeEquivalentTo(firstMapper[nameof(TestMappedLine.Id)]);
        }

        [Fact]
        public void ToObject_FieldMappedByName_CopiesValueFromLineObjectToCustomObject()
        {
            // arrange
            const string secondValue = "Hello, World!";
            var testLine = new TestLine { Id = 1, Name = secondValue };

            var mapper = new TestModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(secondValue);
        }

        [Fact]
        public void ToObject_OptionalPropertyNotMappedToField_SkipsCopyingPropertyValue()
        {
            // arrange
            var testLine = new TestLine { Id = 10 };

            var mapper = new TestModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().BeNull();
        }

        [Fact]
        public void ToObject_RequiredPropertyNotMappedToField_ThrowsCsvFileException()
        {
            // arrange
            var testLine = new TestLine { Id = 1 };

            var mapper = new TestModelMapper2();

            // act
            Action testAction = () => _ = testLine.ToObject(mapper);

            // assert
            testAction.Should()
                .Throw<CsvFileException>()
                .WithMessage("Property 'Description' unable to be mapped.");
        }

        [Fact]
        public void ToCsvLine_FieldMappedByIndex_CopiesValueFromCustomObjectToLineObject()
        {
            // arrange
            const int identifier = 49;
            var testModel = new TestModel { Id = identifier };

            var mapper = new TestLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Should().NotBeNull();
            testResult.Id.Should().Be(identifier);
        }

        [Fact]
        public void ToCsvLine_FieldMappedByFieldName_CopiesValueFromCustomObjectToLineObject()
        {
            // arrange
            const string name = "Testing";
            var testModel = new TestModel { Id = 1, Name = name };

            var mapper = new TestLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Should().NotBeNull();
            testResult.Name.Should().Be(name);
        }

        [Fact]
        public void ToCsvLine_OptionalPropertyNotMappedToField_SkipsCopyingPropertyValue()
        {
            // arrange
            var testModel = new TestModel { Id = 10 };

            var mapper = new TestLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Name.Should().BeNull();
        }

        [Fact]
        public void ToObject_LineReadWithRenamingMapper_CopiesFieldValuesToCustomObject()
        {
            // arrange
            RenamedLine testLine;
            using (var csvReader = new CsvFileReader<RenamedLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<RenamedLineMapper>();
                testLine = (RenamedLine)csvReader.ReadLine();
            }

            var mapper = new PersonModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.PersonId.Should().Be(RenamedId);
            testResult.PersonName.Should().Be(RenamedName);
        }

        [Fact]
        public void ToCsvLine_RenamingMapper_PropertiesReturnModelValues()
        {
            // arrange
            var testModel = new TestModel { Id = RenamedId, Name = RenamedName };

            var mapper = new RenamedLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Id.Should().Be(RenamedId);
            testResult.Name.Should().Be(RenamedName);
        }

        [Fact]
        public void CreateMapper_PropertyMappedOnBaseType_ReturnsFieldMappingWithIndex()
        {
            // arrange
            var testLine = new DerivedMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Id))
                .WhoseValue.FieldIndex.Should().Be(0);
        }

        [Fact]
        public void CreateMapper_PropertyMappedOnBaseType_ReturnsFieldMappingWithTypeConverterType()
        {
            // arrange
            var testLine = new DerivedMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(TestMappedLine.Id))
                .WhoseValue.TypeConverterType.Should().Be(typeof(Int32Converter));
        }

        [Fact]
        public void CreateMapper_DerivedTypeAddsMappedProperty_ReturnsBaseTypeMappingsFirst()
        {
            // arrange
            var testLine = new ExtendedMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Keys.Should().Equal(
                nameof(ExtendedMappedLine.Id),
                nameof(ExtendedMappedLine.Name),
                nameof(ExtendedMappedLine.Description),
                nameof(ExtendedMappedLine.Email));
        }

        [Fact]
        public void CreateMapper_DerivedTypeHidesMappedPropertyWithOwnMapping_ReturnsHidingFieldMapping()
        {
            // arrange
            var testLine = new HidingMappedLine();

            // act
            var testResult = testLine.CreateMapper();

            // assert
            testResult.Should().ContainKey(nameof(HidingMappedLine.Name))
                .WhoseValue.FieldIndex.Should().Be(HidingNameFieldIndex);
        }

        [Fact]
        public void ToObject_OverrideDeclaresOnlyGetter_CopiesValueThroughOverriddenSetter()
        {
            // arrange
            var testLine = new TestLine { Id = 1, Name = InheritedName };

            var mapper = new GetterOverrideModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(InheritedName);
        }

        [Fact]
        public void ToCsvLine_OverrideDeclaresOnlySetter_CopiesValueThroughOverriddenGetter()
        {
            // arrange
            var testModel = new SetterOverrideModel { Name = InheritedName };

            var mapper = new TestLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Name.Should().Be(InheritedName);
        }

        [Fact]
        public void ToObject_IntermediateTypeHidesPropertyWithPrivateOne_CopiesValueToAccessibleProperty()
        {
            // arrange
            var testLine = new TestLine { Id = 1, Name = InheritedName };

            var mapper = new PrivateHidingDerivedModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(InheritedName);
        }

        [Fact]
        public void ToObject_PropertyInheritedByCustomModel_CopiesValueFromLineObjectToCustomObject()
        {
            // arrange
            var testLine = new TestLine { Id = 1, Name = InheritedName };

            var mapper = new DerivedModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(InheritedName);
        }

        [Fact]
        public void ToCsvLine_PropertyInheritedByCustomModel_CopiesValueFromCustomObjectToLineObject()
        {
            // arrange
            var testModel = new DerivedModel { Id = 1, Name = InheritedName };

            var mapper = new TestLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Name.Should().Be(InheritedName);
        }

        [Fact]
        public void ToObject_FieldTextWithTypeConverter_ConvertsValueWithTypeConverter()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, RenamedIdText } };

            var mapper = new TypeConverterModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(RenamedId);
        }

        [Fact]
        public void ToObject_FieldTextWithConverterDelegate_SetsValueFromConverter()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonNameFieldName, RenamedName } };

            var mapper = new ConverterDelegateModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(ConvertedName);
        }

        [Fact]
        public void ToObject_FieldTextWithoutConverter_ParsesValue()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, RenamedIdText } };

            var mapper = new ParsedModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(RenamedId);
        }

        [Fact]
        public void ToObject_InvalidFieldText_ThrowsCsvFileException()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, InvalidIdText } };

            var mapper = new ParsedModelMapper();

            // act
            Action testAction = () => _ = testLine.ToObject(mapper);

            // assert
            testAction.Should()
                .Throw<CsvFileException>()
                .WithMessage("Encountered invalid value 'five' in CSV field person_id.");
        }

        [Fact]
        public void ToObject_FieldHoldsValueOfOtherType_ThrowsCsvFileException()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, (long)RenamedId } };

            var mapper = new ParsedModelMapper();

            // act
            Action testAction = () => _ = testLine.ToObject(mapper);

            // assert
            testAction.Should()
                .Throw<CsvFileException>()
                .WithMessage("CSV field person_id holds a value of type System.Int64, which property 'Id' of type System.Int32 can't hold.");
        }

        [Fact]
        public void ToObject_FieldMappedByIndex_CopiesValueOfFieldAtThatPosition()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { FirstFieldName, FirstFieldValue }, { SecondFieldName, SecondFieldValue } };

            var mapper = new IndexModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Name.Should().Be(SecondFieldValue);
        }

        [Fact]
        public void ToObject_RequiredFieldIndexPastLastField_ThrowsCsvFileException()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { FirstFieldName, FirstFieldValue }, { SecondFieldName, SecondFieldValue } };

            var mapper = new PastLastFieldModelMapper();

            // act
            Action testAction = () => _ = testLine.ToObject(mapper);

            // assert
            testAction.Should()
                .Throw<CsvFileException>()
                .WithMessage("Property 'Name' unable to be mapped.");
        }

        [Fact]
        public void ToObject_ConstantValue_SetsConstantValue()
        {
            // arrange
            var testLine = new CsvFileRecordLine();

            var mapper = new ConstantModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(ConstantId);
        }

        [Fact]
        public void ToObject_GeneratedValue_SetsGeneratedValue()
        {
            // arrange
            var testLine = new CsvFileRecordLine();

            var mapper = new GeneratedModelMapper();

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(GeneratedId);
        }

        [Fact]
        public void ToObject_ValidatorRejectsValue_ThrowsCsvFileException()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, RenamedIdText } };

            var mapper = new ValidatedModelMapper(false);

            // act
            Action testAction = () => _ = testLine.ToObject(mapper);

            // assert
            testAction.Should()
                .Throw<CsvFileException>()
                .WithMessage("Encountered invalid value '5' in CSV field person_id.");
        }

        [Fact]
        public void ToObject_ValidatorAcceptsValue_SetsConvertedValue()
        {
            // arrange
            var testLine = new CsvFileRecordLine { { PersonIdFieldName, RenamedIdText } };

            var mapper = new ValidatedModelMapper(true);

            // act
            var testResult = testLine.ToObject(mapper);

            // assert
            testResult.Id.Should().Be(RenamedId);
        }

        [Fact]
        public void ToCsvLine_ConstantValue_LineHoldsConstantInsteadOfModelValue()
        {
            // arrange
            var testModel = new TestModel { Id = 1, Description = ModelDescription };

            var mapper = new ConstantLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Description.Should().Be(ConstantDescription);
        }

        [Fact]
        public void ToCsvLine_GeneratedValue_LineHoldsGeneratedValue()
        {
            // arrange
            var testModel = new TestModel { Id = 1 };

            var mapper = new GeneratedLineMapper();

            // act
            var testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Description.Should().Be(GeneratedDescription);
        }

        [Fact]
        public void ToCsvLine_FieldMappedByIndex_LineHoldsFieldsInPositionOrder()
        {
            // arrange
            var testModel = new TestModel { Id = 1, Name = RenamedName };

            var mapper = new PositionLineMapper();

            // act
            ICsvFileLine testResult = testModel.ToCsvLine(mapper);

            // assert
            testResult.Keys.Should().Equal(nameof(TestLine.Name), nameof(TestLine.Id));
        }

        /// <summary>
        /// A line that inherits every mapped property from <see cref="TestMappedLine"/>.
        /// </summary>
        private sealed class DerivedMappedLine : TestMappedLine
        {
        }

        /// <summary>
        /// A line that adds a mapped property to those it inherits from <see cref="TestMappedLine"/>.
        /// </summary>
        private sealed class ExtendedMappedLine : TestMappedLine
        {
            [CsvFieldMapping("Email", IsOptional = true)]
            public string Email
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that hides <see cref="TestMappedLine.Name"/> with a property mapped to another field.
        /// </summary>
        private sealed class HidingMappedLine : TestMappedLine
        {
            [CsvFieldMapping(HidingNameFieldIndex)]
            public new string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private class VirtualNameModel
        {
            public virtual string Name { get; set; }
        }

        /// <summary>
        /// A custom model whose override of <see cref="VirtualNameModel.Name"/> declares only a getter.
        /// </summary>
        private sealed class GetterOverrideModel : VirtualNameModel
        {
            public override string Name => base.Name;
        }

        private sealed class GetterOverrideModelMapper : CsvFileCustomMapperBase<GetterOverrideModel>
        {
            public GetterOverrideModelMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(GetterOverrideModel.Name));
            }
        }

        /// <summary>
        /// A custom model whose override of <see cref="VirtualNameModel.Name"/> declares only a setter.
        /// </summary>
        private sealed class SetterOverrideModel : VirtualNameModel
        {
            public override string Name
            {
                set => base.Name = value;
            }
        }

        /// <summary>
        /// A custom model that hides <see cref="BaseModel.Name"/> with a private property, which its derived types don't inherit.
        /// </summary>
        private class PrivateHidingModel : BaseModel
        {
            private new string Name { get; set; }
        }

        /// <summary>
        /// A custom model whose <c>Name</c> is <see cref="BaseModel.Name"/>, since it doesn't inherit the private one that hides it.
        /// </summary>
        private sealed class PrivateHidingDerivedModel : PrivateHidingModel
        {
        }

        private sealed class PrivateHidingDerivedModelMapper : CsvFileCustomMapperBase<PrivateHidingDerivedModel>
        {
            public PrivateHidingDerivedModelMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(PrivateHidingDerivedModel.Name));
            }
        }

        private class BaseModel
        {
            public string Name { get; set; }
        }

        /// <summary>
        /// A custom model that inherits <see cref="BaseModel.Name"/>.
        /// </summary>
        private sealed class DerivedModel : BaseModel
        {
            public int Id { get; set; }
        }

        private sealed class DerivedModelMapper : CsvFileCustomMapperBase<DerivedModel>
        {
            public DerivedModelMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(DerivedModel.Name));
            }
        }

        /// <summary>
        /// A line whose properties <see cref="RenamedLineMapper"/> maps to fields with other names.
        /// </summary>
        private class RenamedLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private class RenamedLineMapper : CsvFileMapperBase<RenamedLine>
        {
            public RenamedLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
            }
        }

        /// <summary>
        /// A custom model whose property names differ from both the field names and the properties of <see cref="RenamedLine"/>.
        /// </summary>
        private class PersonModel
        {
            public int PersonId { get; set; }
            public string PersonName { get; set; }
        }

        private class PersonModelMapper : CsvFileCustomMapperBase<PersonModel>
        {
            public PersonModelMapper()
            {
                this.Map().Property(x => x.PersonId).HasFieldName(PersonIdFieldName);
                this.Map().Property(x => x.PersonName).HasFieldName(PersonNameFieldName);
            }
        }

        private class TypeConverterModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public TypeConverterModelMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName).ConvertUsing(typeof(Int32Converter));
            }
        }

        private class ConverterDelegateModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public ConverterDelegateModelMapper()
            {
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName)
                    .ConvertUsing((CsvFieldConvertFromString<TestModel, string>)(_ => ConvertedName));
            }
        }

        private class ParsedModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public ParsedModelMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
            }
        }

        private class IndexModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public IndexModelMapper()
            {
                this.Map().Property(x => x.Name).HasFieldIndex(SecondFieldIndex);
            }
        }

        private class PastLastFieldModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public PastLastFieldModelMapper()
            {
                this.Map().Property(x => x.Name).HasFieldIndex(PastLastFieldIndex);
            }
        }

        private class ConstantModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public ConstantModelMapper()
            {
                this.Map().Property(x => x.Id).HasConstantValue(ConstantId);
            }
        }

        private class GeneratedModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public GeneratedModelMapper()
            {
                this.Map().Property(x => x.Id).HasVariableValue(() => GeneratedId);
            }
        }

        /// <summary>
        /// Maps <see cref="TestModel.Id"/> with a validator that gives the result it is created with for every value.
        /// </summary>
        private class ValidatedModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public ValidatedModelMapper(bool isValid)
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName).ConvertUsing(typeof(Int32Converter)).ValidateUsing(_ => isValid);
            }
        }

        private class ConstantLineMapper : CsvFileMapperBase<TestLine>
        {
            public ConstantLineMapper()
            {
                this.Map().Property(x => x.Description).HasConstantValue(ConstantDescription);
            }
        }

        private class GeneratedLineMapper : CsvFileMapperBase<TestLine>
        {
            public GeneratedLineMapper()
            {
                this.Map().Property(x => x.Description).HasVariableValue(() => GeneratedDescription);
            }
        }

        /// <summary>
        /// Maps <see cref="TestLine.Id"/> without an index before <see cref="TestLine.Name"/> at index 0, so <c>Id</c> takes position 1.
        /// </summary>
        private class PositionLineMapper : CsvFileMapperBase<TestLine>
        {
            public PositionLineMapper()
            {
                this.Map().Property(x => x.Id);
                this.Map().Property(x => x.Name).HasFieldIndex(0);
            }
        }

        private class TestLine : CsvFileRecordLine
        {
            public int? Id
            {
                get => GetPropertyValue<int?>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Description
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private class TestMappedLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = TestMappingConstantValue)]
            [TypeConverter(typeof(Int32Converter))]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping("Name", IsOptional = true)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping("Description", IsOptional = true)]
            public string Description
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private class TestModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        private class TestLineMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(TestLine.Name))
                    .IsOptional();
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName(nameof(TestLine.Description))
                    .IsOptional();
            }
        }

        private class TestLineMapper2 : CsvFileMapperBase<TestLine>
        {
            public TestLineMapper2()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(TestLine.Name))
                    .IsOptional();
                this.Map()
                    .Property(x => x.Description);
            }
        }

        private class TestModelMapper : CsvFileCustomMapperBase<TestModel>
        {
            public TestModelMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(TestModel.Name))
                    .IsOptional();
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName(nameof(TestModel.Description))
                    .IsOptional();
            }
        }

        private class TestModelMapper2 : CsvFileCustomMapperBase<TestModel>
        {
            public TestModelMapper2()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(nameof(TestModel.Name))
                    .IsOptional();
                this.Map()
                    .Property(x => x.Description);
            }
        }
    }
}
