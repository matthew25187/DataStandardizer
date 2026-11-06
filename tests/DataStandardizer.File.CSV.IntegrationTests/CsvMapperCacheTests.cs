using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class CsvMapperCacheTests
{
    [Fact]
    public void Clear_AfterLineOfCollectibleTypeWritten_ReleasesTypeForUnloading()
    {
        // arrange
        var lineAssembly = WriteLineOfCollectibleType();

        // act
        CsvMapperCache.Clear();

        // assert
        GarbageCollection.IsCollected(lineAssembly).Should().BeTrue();
    }

    /// <summary>
    /// Writes a line whose type is defined in a new collectible assembly, which caches the line type's mapper and property bindings.
    /// </summary>
    /// <returns>A weak reference to the collectible assembly.</returns>
    /// <remarks>
    /// Not inlined, so no local of the test refers to the line, its type or its assembly.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference WriteLineOfCollectibleType()
    {
        var lineType = CreateCollectibleLineType();
        using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(new StringWriter()))
        {
            csvWriter.WriteLine((ICsvFileLine)Activator.CreateInstance(lineType)!);
        }

        return new WeakReference(lineType.Assembly);
    }

    private static Type CreateCollectibleLineType()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CollectibleLines"), AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("CollectibleLines");
        var typeBuilder = moduleBuilder.DefineType("CollectibleLine", TypeAttributes.Public | TypeAttributes.Sealed, typeof(CsvFileRecordLine));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        return typeBuilder.CreateType();
    }
}
