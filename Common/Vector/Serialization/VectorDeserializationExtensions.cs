using System.Numerics;
using System.Runtime.Serialization;
using Parquet;
using Parquet.Serialization;

namespace Common.Vector.Serialization;

public static class VectorDeserializationExtensions
{
    public static ParquetOptions DefaultParquetOptions { set; get; }
        = VectorSerializationExtensions.DefaultParquetOptions;

    public static ParquetSerializerOptions DefaultDeserializerOptions { set; get; }
        = VectorSerializationExtensions.DefaultSerializerOptions;

    private static ParquetOptions GetParquetOptions(ParquetSerializerOptions? options)
        => (options ?? DefaultDeserializerOptions).ParquetOptions ?? DefaultParquetOptions;
    
    public static VectorDeserializationResult<T> Deserialize<T>(
             this SerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : /*struct,*/ INumber<T>
    {
        var parquetOptions = GetParquetOptions(options);
        
        IVector<T> vector;

        switch (serializedVector.VectorTypeName)
        {
            case nameof(Vector<T>):
                vector = new Vector<T>(
                    serializedVector
                        .SerializedElements
                        .Deserialize<Element<T>>(parquetOptions)
                        .Select(e => e.Value)
                );
                break;

            case nameof(SparseVector<T>):
                vector = new SparseVector<T>(
                     serializedVector.Count
                    ,serializedVector
                         .SerializedElements
                         .Deserialize<IndexedElement<T>>(parquetOptions)
                );
                break;

            default:
                throw new SerializationException();
        }

        return new VectorDeserializationResult<T>(serializedVector.VectorTypeName, vector);
    }
    
    public static async ValueTask<VectorDeserializationResult<T>> DeserializeAsync<T>(
             this SerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : /*struct,*/ INumber<T>
    {
        var parquetOptions = GetParquetOptions(options);

        IVector<T> vector;

        switch (serializedVector.VectorTypeName)
        {
            case nameof(Vector<T>):
                vector = new Vector<T>(
                    (await serializedVector
                        .SerializedElements
                        .DeserializeAsync<Element<T>>(parquetOptions))
                        .Select(e => e.Value)
                );
                break;

            case nameof(SparseVector<T>):
                vector = new SparseVector<T>(
                     serializedVector.Count
                    ,await serializedVector
                         .SerializedElements
                         .DeserializeAsync<IndexedElement<T>>(parquetOptions)
                );
                break;

            default:
                throw new SerializationException();
        }

        return new VectorDeserializationResult<T>(serializedVector.VectorTypeName, vector);
    }
}