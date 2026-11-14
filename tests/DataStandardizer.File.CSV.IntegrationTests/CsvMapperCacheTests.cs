using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class CsvMapperCacheTests
{
    // The field of the line that is written.
    private const string FieldName = "One";
    private const string FieldValue = "1";

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
        var line = (ICsvFileLine)Activator.CreateInstance(lineType)!;

        // A record has one or more fields, so the line is given one to be written.
        line.Add(FieldName, FieldValue);
        using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(new StringWriter()))
        {
            csvWriter.WriteLine(line);
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
