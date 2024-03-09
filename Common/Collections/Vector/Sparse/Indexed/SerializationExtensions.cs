using System.Numerics;
using Common.Collections.Vector.Serialization;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;

namespace Common.Collections.Vector.Sparse.Indexed;

public static class SerializationExtensions
{
    private static ParquetOptions? _defaultParquetOptions = null;
    public static ParquetOptions DefaultParquetOptions
    {
        set => _defaultParquetOptions = value;
        get => _defaultParquetOptions ?? Collections.Serialization.Common.DefaultParquetOptions;
    }

    private static int? _defaultMemoryStreamCapacity = null;
    public static int DefaultMemoryStreamCapacity
    {
        set => _defaultMemoryStreamCapacity = value; 
        get => _defaultMemoryStreamCapacity ?? Collections.Serialization.Common.DefaultMemoryStreamCapacity;
    }

    private static ParquetSerializerOptions? _defaultSerializerOptions = null;
    public static ParquetSerializerOptions DefaultSerializerOptions
    {
        set => _defaultSerializerOptions = value;
        get => _defaultSerializerOptions ?? Collections.Serialization.Common.DefaultSerializerOptions;
    }
    
    public static IReadOnlyDictionary<Type, ParquetSchema> PrimitiveToSchemaMapping
        => Collections.Serialization.Common.PrimitiveToSchemaMapping;
    
    public static ISerializedVector Serialize<T>(
             this SparseIndexedVector<T> v
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var dataToSerialize = v.GetIndexedElements(false);
        var schema = ParquetSerializer.SerializeAsync(dataToSerialize, memoryStream, actualOptions).Result;
        return new SerializedVector(
             nameof(SparseIndexedVector<T>)
            ,v.Count
            ,new SerializedVectorElements(memoryStream.ToArray())
        );
    }
    
    public static async ValueTask<ISerializedVector> SerializeAsync<T>(
             this SparseIndexedVector<T> v
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var dataToSerialize = v.GetIndexedElements(false);
        var schema = await ParquetSerializer.SerializeAsync(dataToSerialize, memoryStream, actualOptions);
        return new SerializedVector(
             nameof(SparseIndexedVector<T>) 
            ,v.Count
            ,new SerializedVectorElements(memoryStream.ToArray())
        );
    }
}