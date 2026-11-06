using System.Runtime.CompilerServices;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class GarbageCollectionTests
{
    [Fact]
    public void IsCollected_NothingRefersToTarget_ReturnsTrue()
    {
        // arrange
        var reference = CreateUnreferencedObject();

        // act
        var testResult = GarbageCollection.IsCollected(reference);

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void IsCollected_TargetStillReferenced_ReturnsFalse()
    {
        // arrange
        var target = new object();
        var reference = new WeakReference(target);

        // act
        var testResult = GarbageCollection.IsCollected(reference);

        // assert
        testResult.Should().BeFalse();
        GC.KeepAlive(target);
    }

    // Not inlined, so no local of the caller refers to the object.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUnreferencedObject() => new WeakReference(new object());
}
