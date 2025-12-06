using System.ComponentModel;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

public class CsvExtensionsTests
{
    private const int TestMappingConstantValue = 100;

    [Fact]
    public void CreateMapper_DeclarativeMappingFieldIndex_ReturnsFieldMappingWithIndex()
    {
        // arrange
        var testLine = new TestMappedLine();

        // act
        var mapper = testLine.CreateMapper();
        var testResult = mapper.GetValueOrDefault(nameof(TestMappedLine.Id));

        // assert
        testResult.Should().NotBeNull();
        testResult!.FieldIndex.Should().Be(0);
    }

    [Fact]
    public void CreateMapper_DeclarativeMappingFieldNames_ReturnsFieldMappingWithNames()
    {
        // arrange
        var testLine = new TestMappedLine();

        // act
        var mapper = testLine.CreateMapper();
        var testResult = mapper.GetValueOrDefault(nameof(TestMappedLine.Name));

        // assert
        testResult.Should().NotBeNull();
        testResult!.FieldName.Should().Be(nameof(TestMappedLine.Name));
    }

    [Fact]
    public void CreateMapper_DeclarativeMappingConstantValue_ReturnsFieldMappingWithValue()
    {
        // arrange
        var testLine = new TestMappedLine();

        // act
        var mapper = testLine.CreateMapper();
        var testResult = mapper.GetValueOrDefault(nameof(TestMappedLine.Id));

        // assert
        testResult.Should().NotBeNull();
        testResult!.ConstantValue.Should().Be(TestMappingConstantValue);
    }

    [Fact]
    public void CreateMapper_DeclarativeMappingIsOptional_ReturnsFieldMappingWithOptionalFlag()
    {
        // arrange
        var testLine = new TestMappedLine();

        // act
        var mapper = testLine.CreateMapper();
        var testResult = mapper.GetValueOrDefault(nameof(TestMappedLine.Name));

        // assert
        testResult.Should().NotBeNull();
        testResult!.IsOptional.Should().BeTrue();
    }

    [Fact]
    public void CreateMapper_DeclarativeMappingTypeConverter_ReturnsFieldMappingWithTypeConverterType()
    {
        // arrange
        var testLine = new TestMappedLine();

        // act
        var mapper = testLine.CreateMapper();
        var testResult = mapper.GetValueOrDefault(nameof(TestMappedLine.Id));

        // assert
        testResult.Should().NotBeNull();
        testResult!.TypeConverterType.Should().Be(typeof(Int32Converter));
    }

    [Fact]
    public void ToObject_FieldMappedByIndex_CopiesValueFromLineObjectToCustomObject()
    {
        // arrange
        const int identifier = 100;
        var testLine = new TestLine { Id = identifier ,Name = "One"};

        var mapper = new TestModelMapper();

        // act
        var testResult = testLine.ToObject(mapper);

        // assert
        testResult.Id.Should().Be(identifier);
    }

    [Fact]
    public void ToObject_FieldMappedByName_CopiesValueFromLineObjectToCustomObject()
    {
        // arrange
        const string secondValue = "Hello, World!";
        var testLine = new TestLine { Id = 1,Name = secondValue };

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
            .WithMessage($"Property '{nameof(TestModel.Description)}' unable to be mapped.");
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
        var testModel = new TestModel { Id = 1,Name = name };

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

    private class TestLine : CsvFileRecordLine
    {
        public int? Id
        {
            get => GetPropertyValue<int?>();
            set => SetPropertyValue(value);
        }

        public string? Name
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }

        public string? Description
        {
            get => GetPropertyValue<string?>();
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
        public string? Name
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        [CsvFieldMapping("Description", IsOptional = true)]
        public string? Description
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }
    }

    private class TestModel
    {
        public  int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
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