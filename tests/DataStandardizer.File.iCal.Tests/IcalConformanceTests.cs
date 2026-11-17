using System;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the depth check the writers make as they walk a calendar, which a calendar changed after validation reaches.  A test can't
    /// change a calendar between a writer's validation and its walk without racing another thread, so this calls the check directly.
    /// </summary>
    public class IcalConformanceTests
    {
        [Fact]
        public void CheckWriteDepth_AtTheLimit_DoesNotThrow()
        {
            // act
            Action testAction = () => IcalConformance.CheckWriteDepth(MaxComponentDepth);

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void CheckWriteDepth_PastTheLimit_ThrowsInvalidComponentNesting()
        {
            // act
            Action testAction = () => IcalConformance.CheckWriteDepth(PastMaxComponentDepth);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>()
                .WithMessage("Components are nested more than 64 levels deep; RFC 5545§3.6 allows three.").Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidComponentNesting);
        }
    }
}
