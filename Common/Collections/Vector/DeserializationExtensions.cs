using System.Numerics;
using System.Runtime.Serialization;
using Common.Collections.Vector.Serialization;
using Common.Collections.Vector.Sparse.Indexed;
using Parquet;
using Parquet.Serialization;

namespace Common.Collections.Vector;

public static class DeserializationExtensions
{
    private static ParquetOptions GetParquetOptions(ParquetSerializerOptions? options)
        => (options ?? SerializationExtensions.DefaultSerializerOptions).ParquetOptions 
           ?? SerializationExtensions.DefaultParquetOptions;
    
    public static IDeserializedVector<T> Deserialize<T>(
             this ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        var parquetOptions = GetParquetOptions(options);
        
        IVector<T> vector;

        switch (serializedVector.VectorTypeName)
        {
            case nameof(Regular.Vector<T>):
                vector = Regular.DeserializationExtensions.Deserialize<T>(serializedVector, options);
                break;

            case nameof(SparseIndexedVector<T>):
                vector = Sparse.Indexed.DeserializationExtensions.Deserialize<T>(serializedVector, options);
                break;

            default:
                throw new SerializationException();
        }

        return new DeserializedVector<T>(serializedVector.VectorTypeName, vector);
    }
    
    public static async ValueTask<IDeserializedVector<T>> DeserializeAsync<T>(
             this ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : /*struct,*/ INumber<T>
    {
        var parquetOptions = GetParquetOptions(options);

        IVector<T> vector;

        switch (serializedVector.VectorTypeName)
        {
            case nameof(Regular.Vector<T>):
                vector = await Regular.DeserializationExtensions.DeserializeAsync<T>(serializedVector, options);
                break;

            case nameof(SparseIndexedVector<T>):
                vector = await Sparse.Indexed.DeserializationExtensions.DeserializeAsync<T>(serializedVector, options);
                break;

            default:
                throw new SerializationException();
        }

        return new DeserializedVector<T>(serializedVector.VectorTypeName, vector);
    }
}