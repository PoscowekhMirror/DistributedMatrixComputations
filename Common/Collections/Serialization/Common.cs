using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;

namespace Common.Collections.Serialization;

public static class Common
{
    public static ParquetOptions DefaultParquetOptions { set; get; } = new() 
    {
        UseDateOnlyTypeForDates = true
    };

    public static int DefaultMemoryStreamCapacity { set; get; } = 32 * 1024 * 1024;
    
    public static ParquetSerializerOptions DefaultSerializerOptions { set; get; } = new()
    {
        CompressionLevel = CompressionLevel.SmallestSize,
        CompressionMethod = CompressionMethod.Gzip,
        RowGroupSize = DefaultMemoryStreamCapacity,
        ParquetOptions = DefaultParquetOptions
    };
    
    public static IReadOnlyDictionary<Type, ParquetSchema> PrimitiveToSchemaMapping { get; }
        = Assembly
            .GetCallingAssembly()
            .GetTypes()
            .Where(t => t.IsPrimitive)
            .Select(t => new DataField("value", t, false, false, null))
            .Select(f => new KeyValuePair<Type, ParquetSchema>(f.ClrType, new ParquetSchema(f)))
            .ToImmutableSortedDictionary()
            .AsReadOnly();
    
    
}