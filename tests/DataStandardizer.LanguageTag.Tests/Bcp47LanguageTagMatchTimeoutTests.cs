// WithTimeout is obsolete but stays tested until v2.0 removes it (#256).
#pragma warning disable CS0618

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of the members that take a match timeout, which have no effect now that a parser checks language tags, and are removed in v2.0 (#256).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagMatchTimeoutTests
    {
        public static TheoryData<MethodInfo> MatchTimeoutMembers => new TheoryData<MethodInfo>
        {
            typeof(Bcp47LanguageTag).GetMethod(nameof(Bcp47LanguageTag.Create), new[] { typeof(string), typeof(TimeSpan) }),
            typeof(Bcp47LanguageTag).GetMethod(nameof(Bcp47LanguageTag.Create), new[] { typeof(string), typeof(SubtagRegistry.SubtagRegistry), typeof(TimeSpan) }),
            typeof(Bcp47LanguageTag).GetMethod(nameof(Bcp47LanguageTag.TryCreate), new[] { typeof(string), typeof(TimeSpan), typeof(Bcp47LanguageTag).MakeByRefType() }),
            typeof(Bcp47LanguageTag).GetMethod(nameof(Bcp47LanguageTag.TryCreate), new[] { typeof(string), typeof(SubtagRegistry.SubtagRegistry), typeof(TimeSpan), typeof(Bcp47LanguageTag).MakeByRefType() }),
            typeof(IBcp47LanguageTagBuilderStepWithTimeout).GetMethod(nameof(IBcp47LanguageTagBuilderStepWithTimeout.WithTimeout)),
            typeof(Bcp47LanguageTagBuilder).GetMethod(nameof(Bcp47LanguageTagBuilder.WithTimeout))
        };

        #region Test: MatchTimeoutMember

        [Theory]
        [MemberData(nameof(MatchTimeoutMembers))]
        public void MatchTimeoutMember_IsObsoleteAsAWarning(MethodInfo testMember)
        {
            // act
            var testResult = testMember.GetCustomAttribute<ObsoleteAttribute>();

            // assert
            testResult.Should().NotBeNull("'{0}' has no effect and is removed in v2.0 (#256)", testMember);
            testResult.IsError.Should().BeFalse("marking the member obsolete in v1.x must not break callers");
            testResult.Message.Should().Contain("removed in version 2.0");
        }

        #endregion

        #region Test: Build_WithTimeout

        [Fact]
        public void Build_WithTimeoutThatIsTooShortToMatch_ProducesBcp47LanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            var testResult = builder.WithTimeout(TimeSpan.FromTicks(1)).UsingLanguageTag("zh-Hant-TW").Build();

            // assert
            ((string)testResult).Should().Be("zh-Hant-TW", "the timeout has no effect");
        }

        [Fact]
        public void Build_WithTimeoutThatIsNotPositive_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.WithTimeout(TimeSpan.Zero).UsingLanguageTag("en").Build();

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>("the timeout is still validated, although it has no effect").WithParameterName("matchTimeout");
        }

        #endregion
    }
}
