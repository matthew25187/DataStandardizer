using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

// TypeExtensions.GetMethod is obsolete (#626), but its behaviour is still tested until it's removed.
#pragma warning disable CS0618

namespace DataStandardizer.Core.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TypeExtensionsTests
    {
        // Parameter names of TypeExtensions.GetMethod.
        private const string ObjParameterName = "obj";
        private const string MethodNameParameterName = "methodName";

        // Names of the methods that TestBase declares.
        private const string PublicMethodName = nameof(TestBase.PublicMethod);
        private const string ProtectedMethodName = "ProtectedMethod";
        private const string PrivateMethodName = "PrivateMethod";
        private const string UndeclaredMethodName = "UndeclaredMethod";

        [Fact]
        public void GetMethod_WithNullObj_ThrowsArgumentNullException()
        {
            // arrange
            TestDerived obj = null!;

            // act
            Action testAction = () => obj.GetMethod(PublicMethodName);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ObjParameterName);
        }

        [Fact]
        public void GetMethod_WithNullMethodName_ThrowsArgumentNullException()
        {
            // arrange
            var obj = new TestDerived();

            // act
            Action testAction = () => obj.GetMethod(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(MethodNameParameterName);
        }

        [Theory]
        [InlineData(PublicMethodName)]
        [InlineData(ProtectedMethodName)]
        [InlineData(PrivateMethodName)]
        public void GetMethod_WithDefaultFlags_ReturnsBaseMethodOfAnyVisibility(string methodName)
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(methodName);

            // assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(methodName);
            result.DeclaringType.Should().Be(typeof(TestBase));
        }

        [Fact]
        public void GetMethod_WithUndeclaredMethodName_ReturnsNull()
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(UndeclaredMethodName);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetMethod_WithIsPublicOnPublicMethod_ReturnsMethod()
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(PublicMethodName, isPublic: true);

            // assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(PublicMethodName);
            result.DeclaringType.Should().Be(typeof(TestBase));
        }

        [Theory]
        [InlineData(ProtectedMethodName)]
        [InlineData(PrivateMethodName)]
        public void GetMethod_WithIsPublicOnNonPublicMethod_ReturnsNull(string methodName)
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(methodName, isPublic: true);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetMethod_WithIsProtectedOnProtectedMethod_ReturnsMethod()
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(ProtectedMethodName, isProtected: true);

            // assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(ProtectedMethodName);
            result.DeclaringType.Should().Be(typeof(TestBase));
        }

        [Theory]
        [InlineData(PublicMethodName)]
        [InlineData(PrivateMethodName)]
        public void GetMethod_WithIsProtectedOnNonProtectedMethod_ReturnsNull(string methodName)
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(methodName, isProtected: true);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetMethod_WithIsPrivateOnPrivateMethod_ReturnsMethod()
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(PrivateMethodName, isPrivate: true);

            // assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(PrivateMethodName);
            result.DeclaringType.Should().Be(typeof(TestBase));
        }

        [Theory]
        [InlineData(PublicMethodName)]
        [InlineData(ProtectedMethodName)]
        public void GetMethod_WithIsPrivateOnNonPrivateMethod_ReturnsNull(string methodName)
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(methodName, isPrivate: true);

            // assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(PublicMethodName)]
        [InlineData(PrivateMethodName)]
        public void GetMethod_WithIsPublicAndIsPrivate_ReturnsNull(string methodName)
        {
            // arrange
            var obj = new TestDerived();

            // act
            var result = obj.GetMethod(methodName, isPublic: true, isPrivate: true);

            // assert
            result.Should().BeNull();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Found through reflection by the tests.")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Found through reflection by the tests.")]
        public class TestBase
        {
            public void PublicMethod()
            {
            }

            protected void ProtectedMethod()
            {
            }

            private void PrivateMethod()
            {
            }
        }

        public class TestDerived : TestBase
        {
        }
    }
}
