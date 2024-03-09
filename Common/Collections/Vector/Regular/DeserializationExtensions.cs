using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Serialization;
using Parquet;
using Parquet.Serialization;

namespace Common.Collections.Vector.Regular;

public static class DeserializationExtensions
{
    private static ParquetOptions GetParquetOptions(ParquetSerializerOptions? options)
        => (options ?? SerializationExtensions.DefaultSerializerOptions).ParquetOptions 
           ?? SerializationExtensions.DefaultParquetOptions;
    
    public static Vector<T> Deserialize<T>(
             ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        if (serializedVector.VectorTypeName != nameof(Vector<T>))
        {
            throw new ArgumentException();
        }
        
        var parquetOptions = GetParquetOptions(options);
        
        return new Vector<T>(
            serializedVector
                .SerializedElements
                .Deserialize<Element<T>>(parquetOptions)
                .Select(e => e.Value)
        );
    }
    
    public static async ValueTask<Vector<T>> DeserializeAsync<T>(
             ISerializedVector serializedVector
            ,ParquetSerializerOptions? options = null
        )
        where T : INumber<T>
    {
        if (serializedVector.VectorTypeName != nameof(Vector<T>))
        {
            throw new ArgumentException();
        }
        
        var parquetOptions = GetParquetOptions(options);
        
        return new Vector<T>((
            await serializedVector
                .SerializedElements
                .DeserializeAsync<Element<T>>(parquetOptions)
                ).Select(e => e.Value)
        );
    }
}