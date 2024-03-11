using System.Collections.Immutable;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;
using Common.Serialization;

namespace Common.Collections.Serialization;

public static class Common
{
    private static ParquetOptions? _defaultParquetOptions = null;
    public static ParquetOptions DefaultParquetOptions
    {
        set => _defaultParquetOptions = value;
        get => _defaultParquetOptions ?? global::Common.Serialization.Parquet.DefaultParquetOptions;
    }

    private static int? _defaultMemoryStreamCapacity = null;
    public static int DefaultMemoryStreamCapacity
    {
        set => _defaultMemoryStreamCapacity = value; 
        get => _defaultMemoryStreamCapacity ?? global::Common.Serialization.Parquet.DefaultMemoryStreamCapacity;
    }

    private static ParquetSerializerOptions? _defaultSerializerOptions = null;
    public static ParquetSerializerOptions DefaultSerializerOptions
    {
        set => _defaultSerializerOptions = value;
        get => _defaultSerializerOptions ?? global::Common.Serialization.Parquet.DefaultSerializerOptions;
    }
    
    public static ImmutableSortedDictionary<string, ParquetSchema> PrimitiveToSchemaMapping
        => global::Common.Serialization.Parquet.PrimitiveTypeParquetSchemas;
    
    
}