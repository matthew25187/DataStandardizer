using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// Checks that a member retired from the public surface stays retired.
    /// </summary>
    /// <remarks>
    /// This reads the compiled assembly by reflection, so it is an integration test.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class RemovedMemberTests
    {
        /// <summary>
        /// The method that checked the old hybrid of grammar and ISO code lists. A caller who skips v2.0 must get a compile error, not new semantics under the old name (#502), so the name is never reused.
        /// </summary>
        private const string RetiredMethodName = "IsWellFormedLanguageTagString";

        [Fact]
        public void GetMembers_OfBcp47LanguageTag_DoesNotIncludeIsWellFormedLanguageTagString()
        {
            // act
            var testResult = typeof(Bcp47LanguageTag).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Select(member => member.Name);

            // assert
            testResult.Should().NotContain(RetiredMethodName);
        }
    }
}
