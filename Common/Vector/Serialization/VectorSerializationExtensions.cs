using System.Collections.Immutable;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Parquet.Serialization;

namespace Common.Vector.Serialization;

public static class VectorSerializationExtensions
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

    private static IDictionary<Type, ParquetSchema> PrimitiveToSchemaMapping { get; }
        = Assembly
            .GetCallingAssembly()
            .GetTypes()
            .Where(t => t.IsPrimitive)
            .Select(t => new DataField("value", t, false, false, null))
            .Select(f => new KeyValuePair<Type, ParquetSchema>(f.ClrType, new ParquetSchema(f)))
            .ToImmutableSortedDictionary();
    
    private static byte[] SerializePrimitive<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        var schema = PrimitiveToSchemaMapping[typeof(T)];
        var dataColumn = new DataColumn(schema.DataFields.First(), v.GetValuesOnly().ToArray());
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        
        using (var writer = ParquetWriter.CreateAsync(schema, memoryStream, options.ParquetOptions).Result)
        {
            writer.CompressionLevel = options.CompressionLevel;
            writer.CompressionMethod = options.CompressionMethod;
            using (var rgWriter = writer.CreateRowGroup())
            {
                
                rgWriter.WriteColumnAsync(dataColumn).Wait();
            }
        }
        
        return memoryStream.ToArray();
    }
    
    private static async ValueTask<byte[]> SerializePrimitiveAsync<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        var schema = PrimitiveToSchemaMapping[typeof(T)];
        var dataColumn = new DataColumn(schema.DataFields.First(), v.GetValuesOnly().ToArray());
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        
        using (var writer = await ParquetWriter.CreateAsync(schema, memoryStream, options.ParquetOptions))
        {
            writer.CompressionLevel = options.CompressionLevel;
            writer.CompressionMethod = options.CompressionMethod;
            using (var rgWriter = writer.CreateRowGroup())
            {
                
                await rgWriter.WriteColumnAsync(dataColumn);
            }
        }
        
        return memoryStream.ToArray();
    }

    private static byte[] SerializeNonPrimitive<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var schema = ParquetSerializer.SerializeAsync(v.GetValuesOnly(), memoryStream, options).Result;
        return memoryStream.ToArray();
    }
    
    private static async ValueTask<byte[]> SerializeNonPrimitiveAsync<T>(Vector<T> v, ParquetSerializerOptions options) 
        where T : INumber<T>
    {
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        var schema = await ParquetSerializer.SerializeAsync(v.GetValuesOnly(), memoryStream, options);
        return memoryStream.ToArray();
    }
    
    public static SerializedVector Serialize<T>(this Vector<T> v, ParquetSerializerOptions? options = null)
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
    
    public static async ValueTask<SerializedVector> SerializeAsync<T>(this Vector<T> v, ParquetSerializerOptions? options = null)
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
    
    public static SerializedVector Serialize<T>(this SparseVector<T> v, ParquetSerializerOptions? options = null)
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        // TODO: remove extra filter and sort ops?
        var dataToSerialize = 
            v
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index);
        var schema = ParquetSerializer.SerializeAsync(dataToSerialize, memoryStream, actualOptions).Result;
        return new SerializedVector(
             nameof(SparseVector<T>)
            ,v.Count
            ,new SerializedVectorElements(memoryStream.ToArray())
        );
    }
    
    public static async ValueTask<SerializedVector> SerializeAsync<T>(this SparseVector<T> v, ParquetSerializerOptions? options = null)
        where T : INumber<T>
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        using var memoryStream = new MemoryStream(DefaultMemoryStreamCapacity);
        // TODO: remove extra filter and sort ops?
        var dataToSerialize = 
            v
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index);
        var schema = await ParquetSerializer.SerializeAsync(dataToSerialize, memoryStream, actualOptions);
        return new SerializedVector(
             nameof(SparseVector<T>) 
            ,v.Count
            ,new SerializedVectorElements(memoryStream.ToArray())
        );
    }
}