using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the helpers in <see cref="ModelTestData"/> whose behaviour the model tests rely on.
    /// </summary>
    public class ModelTestDataTests
    {
        private const int ThreeLevels = 3;

        [Fact]
        public void NestedComponents_ThreeLevels_BuildsAChainOfThree()
        {
            // act
            var testResult = ModelTestData.NestedComponents(ThreeLevels);

            // assert
            var second = testResult.Components.Should().ContainSingle().Subject;
            var third = second.Components.Should().ContainSingle().Subject;
            third.Components.Should().BeEmpty();
            new[] { testResult.Name, second.Name, third.Name }.Should().OnlyContain(name => name == ModelTestData.NestedComponentName);
        }

        [Fact]
        public void NestedComponents_OneLevel_BuildsOneComponent()
        {
            // act
            var testResult = ModelTestData.NestedComponents(1);

            // assert
            testResult.Name.Should().Be(ModelTestData.NestedComponentName);
            testResult.Components.Should().BeEmpty();
        }

        [Fact]
        public void SharedComponents_TwoLevels_HoldsEachChildTwice()
        {
            // act
            var testResult = ModelTestData.SharedComponents(2);

            // assert
            testResult.Components.Should().HaveCount(2);
            var second = testResult.Components[0];
            testResult.Components[1].Should().BeSameAs(second);
            second.Components.Should().HaveCount(2);
            var third = second.Components[0];
            second.Components[1].Should().BeSameAs(third);
            third.Components.Should().BeEmpty();
            new[] { testResult.Name, second.Name, third.Name }.Should().OnlyContain(name => name == ModelTestData.NestedComponentName);
        }

        [Fact]
        public void SharedComponents_NoLevels_BuildsOneComponent()
        {
            // act
            var testResult = ModelTestData.SharedComponents(0);

            // assert
            testResult.Name.Should().Be(ModelTestData.NestedComponentName);
            testResult.Components.Should().BeEmpty();
        }
    }
}
