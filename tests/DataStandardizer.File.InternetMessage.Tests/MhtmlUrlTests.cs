using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MhtmlUrl"/> with the examples of RFC 1808 §5.
    /// </summary>
    public class MhtmlUrlTests
    {
        // ref. RFC 1808 §5: "Within an object with a well-defined base URL of".
        private const string Rfc1808Base = "http://a/b/c/d;p?q#f";

        [Theory]
        // ref. RFC 1808 §5.1.
        [InlineData("g:h", "g:h")]
        [InlineData("g", "http://a/b/c/g")]
        [InlineData("./g", "http://a/b/c/g")]
        [InlineData("g/", "http://a/b/c/g/")]
        [InlineData("/g", "http://a/g")]
        [InlineData("//g", "http://g")]
        [InlineData("?y", "http://a/b/c/d;p?y")]
        [InlineData("g?y", "http://a/b/c/g?y")]
        [InlineData("g?y/./x", "http://a/b/c/g?y/./x")]
        [InlineData("#s", "http://a/b/c/d;p?q#s")]
        [InlineData("g#s", "http://a/b/c/g#s")]
        [InlineData("g#s/./x", "http://a/b/c/g#s/./x")]
        [InlineData("g?y#s", "http://a/b/c/g?y#s")]
        [InlineData(";x", "http://a/b/c/d;x")]
        [InlineData("g;x", "http://a/b/c/g;x")]
        [InlineData("g;x?y#s", "http://a/b/c/g;x?y#s")]
        [InlineData(".", "http://a/b/c/")]
        [InlineData("./", "http://a/b/c/")]
        [InlineData("..", "http://a/b/")]
        [InlineData("../", "http://a/b/")]
        [InlineData("../g", "http://a/b/g")]
        [InlineData("../..", "http://a/")]
        [InlineData("../../", "http://a/")]
        [InlineData("../../g", "http://a/g")]
        // ref. RFC 1808 §5.2.
        [InlineData("", "http://a/b/c/d;p?q#f")]
        [InlineData("../../../g", "http://a/../g")]
        [InlineData("../../../../g", "http://a/../../g")]
        [InlineData("/./g", "http://a/./g")]
        [InlineData("/../g", "http://a/../g")]
        [InlineData("g.", "http://a/b/c/g.")]
        [InlineData(".g", "http://a/b/c/.g")]
        [InlineData("g..", "http://a/b/c/g..")]
        [InlineData("..g", "http://a/b/c/..g")]
        [InlineData("./../g", "http://a/b/g")]
        [InlineData("./g/.", "http://a/b/c/g/")]
        [InlineData("g/./h", "http://a/b/c/g/h")]
        [InlineData("g/../h", "http://a/b/c/h")]
        [InlineData("http:g", "http:g")]
        [InlineData("http:", "http:")]
        public void Resolve_Rfc1808Example_GivesTheExpectedUrl(string url, string expected)
        {
            // act
            var testResult = MhtmlUrl.Resolve(Rfc1808Base, url);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(MhtmlUrl.ThisMessageBase, "ietflogo.gif", "thismessage:/ietflogo.gif")]
        [InlineData("http://a", "g", "http://a/g")]
        [InlineData("http://a/b", "a%2eb/c%20d", "http://a/a%2eb/c%20d")]
        public void Resolve_OtherBase_GivesTheExpectedUrl(string baseUrl, string url, string expected)
        {
            // act
            var testResult = MhtmlUrl.Resolve(baseUrl, url);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("http://a/b#f", "http://a/b")]
        [InlineData("http://a/b", "http://a/b")]
        public void WithoutFragment_Url_LeavesOutTheFragment(string url, string expected)
        {
            // act
            var testResult = MhtmlUrl.WithoutFragment(url);

            // assert
            testResult.Should().Be(expected);
        }
    }
}
