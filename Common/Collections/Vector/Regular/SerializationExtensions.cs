using System.Collections.Immutable;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using Common.Collections.Vector.Serialization;
using Common.Collections.Vector.Sparse.Indexed;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Parquet.Serialization;

namespace Common.Collections.Vector.Regular;

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
    
    private static byte[] SerializePrimitive<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        var schema = PrimitiveToSchemaMapping[typeof(T)];
        var values = v.GetValuesOnly(true).ToArray();
        var field = schema.DataFields.First();
        var dataColumn = new DataColumn(field, values);
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        
        using (var writer = ParquetWriter.CreateAsync(schema, memoryStream, options.ParquetOptions).Result)
        {
            writer.CompressionLevel = options.CompressionLevel;
            writer.CompressionMethod = options.CompressionMethod;
            using var rgWriter = writer.CreateRowGroup();
            rgWriter.WriteColumnAsync(dataColumn).Wait();
        }
        
        return memoryStream.ToArray();
    }
    
    private static async ValueTask<byte[]> SerializePrimitiveAsync<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        var schema = PrimitiveToSchemaMapping[typeof(T)];
        var values = v.GetValuesOnly(true).ToArray();
        var field = schema.DataFields.First();
        var dataColumn = new DataColumn(field, values);
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        
        using (var writer = await ParquetWriter.CreateAsync(schema, memoryStream, options.ParquetOptions))
        {
            writer.CompressionLevel = options.CompressionLevel;
            writer.CompressionMethod = options.CompressionMethod;
            using var rgWriter = writer.CreateRowGroup();
            await rgWriter.WriteColumnAsync(dataColumn);
        }
        
        return memoryStream.ToArray();
    }

    private static byte[] SerializeNonPrimitive<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var values = v.GetValuesOnly(true);
        var schema = ParquetSerializer.SerializeAsync(values, memoryStream, options).Result;
        return memoryStream.ToArray();
    }
    
    private static async ValueTask<byte[]> SerializeNonPrimitiveAsync<T>(IVector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var values = v.GetValuesOnly(true);
        var schema = await ParquetSerializer.SerializeAsync(values, memoryStream, options);
        return memoryStream.ToArray();
    }
    
    public static ISerializedVector Serialize<T>(this Vector<T> v, ParquetSerializerOptions? options = null)
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        var data = typeof(T).IsPrimitive 
            ? SerializePrimitive(v, actualOptions) 
            : SerializeNonPrimitive(v, actualOptions);
        return new SerializedVector(
             nameof(Vector<T>)
            ,v.Count
            ,new SerializedVectorElements(data)
        );
    }
    
    public static async ValueTask<ISerializedVector> SerializeAsync<T>(this Vector<T> v, ParquetSerializerOptions? options = null)
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        var data = typeof(T).IsPrimitive 
            ? await SerializePrimitiveAsync(v, actualOptions) 
            : await SerializeNonPrimitiveAsync(v, actualOptions);
        return new SerializedVector(
             nameof(Vector<T>)
            ,v.Count
            ,new SerializedVectorElements(data)
        );
    }
}