using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Serialization;
using Parquet;
using Parquet.Serialization;

namespace Common.Collections.Vector.Sparse.Indexed;

public static class DeserializationExtensions
{
    private static ParquetOptions GetParquetOptions(ParquetSerializerOptions? options)
        => (options ?? SerializationExtensions.DefaultSerializerOptions).ParquetOptions 
           ?? SerializationExtensions.DefaultParquetOptions;
    
    public static SparseIndexedVector<T> Deserialize<T>(
             ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        if (serializedVector.VectorTypeName != nameof(SparseIndexedVector<T>))
        {
            throw new ArgumentException();
        }
        
        var parquetOptions = GetParquetOptions(options);
        
        return new SparseIndexedVector<T>(
             serializedVector.Count
            ,serializedVector
                .SerializedElements
                .Deserialize<IndexedElement<T>>(parquetOptions)
        );
    }
    
    public static async ValueTask<SparseIndexedVector<T>> DeserializeAsync<T>(
             ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        if (serializedVector.VectorTypeName != nameof(SparseIndexedVector<T>))
        {
            throw new ArgumentException();
        }
        
        var parquetOptions = GetParquetOptions(options);
        
        return new SparseIndexedVector<T>(
             serializedVector.Count
            ,await serializedVector
                .SerializedElements
                .DeserializeAsync<IndexedElement<T>>(parquetOptions)
        );
    }
}