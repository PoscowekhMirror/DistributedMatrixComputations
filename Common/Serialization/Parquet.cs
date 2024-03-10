using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Parquet.Serialization;

namespace Common.Serialization;

public static class Parquet
{
    public static ParquetOptions DefaultParquetOptions { set; get; } = new() 
    {
        UseDateOnlyTypeForDates = true
    };

    public static int DefaultRowGroupSize { set; get; } = 32 * 1024 * 1024;
    public static int DefaultMemoryStreamCapacity { set; get; } = DefaultRowGroupSize;
    
    public static ParquetSerializerOptions DefaultSerializerOptions { set; get; } = new()
    {
        CompressionLevel = CompressionLevel.SmallestSize,
        CompressionMethod = CompressionMethod.Gzip,
        RowGroupSize = DefaultRowGroupSize,
        ParquetOptions = DefaultParquetOptions
    };
    
    public static IEnumerable<Type> Primitives { get; }
        = Assembly
            .GetCallingAssembly()
            .GetTypes()
            .Where(t => t.IsPrimitive);

    public static ImmutableSortedDictionary<Type, ParquetSchema> PrimitiveTypeParquetSchemas { get; }
        = Primitives
            .Select(type =>
                new KeyValuePair<Type, ParquetSchema>(
                    type
                    , new ParquetSchema(new List<Field>()
                    {
                        new DataField("value", type, type.IsNullable())
                    })
                )
            )
            .ToImmutableSortedDictionary();

    public static byte[] Serialize<T>(this IEnumerable<T> values) 
        => SerializeAsync(values).Result;

    public static void Serialize<T>(this IEnumerable<T> values, Stream stream) 
        => SerializeAsync(values, stream).Wait();

    private static async ValueTask<byte[]> SerializeNonPrimitiveAsync<T>(
             this IEnumerable<T> values
            ,ParquetSerializerOptions options
        )
    {
        using var stream = new MemoryStream(DefaultMemoryStreamCapacity);
        await ParquetSerializer.SerializeAsync(values, stream, options);
        return stream.ToArray();
    }
    
    private static async Task SerializeNonPrimitiveAsync<T>(
             this IEnumerable<T> values
            ,Stream stream
            ,ParquetSerializerOptions options
        ) 
        => await ParquetSerializer.SerializeAsync(values, stream, options);

    private static async ValueTask<byte[]> SerializePrimitiveAsync<T>(
             this IEnumerable<T> values
            ,ParquetSchema schema
            ,ParquetSerializerOptions options
        )
    {
        var field = schema.DataFields.First();
        var valuesChunks = values
            .Chunk(options.RowGroupSize!.Value)
            .Select(data => new DataColumn(field, data))
            .ToImmutableArray();
        
        using var stream = new MemoryStream(DefaultMemoryStreamCapacity);
        using (var writer = await ParquetWriter.CreateAsync(schema, stream, options.ParquetOptions))
        {
            writer.CompressionMethod = options.CompressionMethod;
            writer.CompressionLevel  = options.CompressionLevel ;
            foreach (var chunk in valuesChunks)
            {
                using var rgWriter = writer.CreateRowGroup();
                await rgWriter.WriteColumnAsync(chunk);
            }
        }
        return stream.ToArray();
    }

    private static async Task SerializePrimitiveAsync<T>(
             this IEnumerable<T> values
            ,ParquetSchema schema
            ,Stream stream 
            ,ParquetSerializerOptions options
        )
    {
        var field = schema.DataFields.First();
        var valuesChunks = values
            .Chunk(options.RowGroupSize!.Value)
            .Select(data => new DataColumn(field, data))
            .ToImmutableArray();

        using var writer = await ParquetWriter.CreateAsync(schema, stream, options.ParquetOptions);
        writer.CompressionMethod = options.CompressionMethod;
        writer.CompressionLevel  = options.CompressionLevel ;
        foreach (var chunk in valuesChunks)
        {
            using var rgWriter = writer.CreateRowGroup();
            await rgWriter.WriteColumnAsync(chunk);
        }
    }
    
    public static async ValueTask<byte[]> SerializeAsync<T>(
             this IEnumerable<T> values
            ,ParquetSerializerOptions? options = null
        )
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        actualOptions.ParquetOptions ??= DefaultParquetOptions;
        actualOptions.RowGroupSize   ??= DefaultRowGroupSize  ;

        var type = typeof(T);
        if (type.IsPrimitive)
        {
            return await SerializePrimitiveAsync(values, PrimitiveTypeParquetSchemas[type], actualOptions);
        }
        
        return await SerializeNonPrimitiveAsync(values, actualOptions);
    }
    
    public static async Task SerializeAsync<T>(
             this IEnumerable<T> values
            ,Stream stream
            ,ParquetSerializerOptions? options = null
        )
    {
        var actualOptions = options ?? DefaultSerializerOptions;
        actualOptions.ParquetOptions ??= DefaultParquetOptions;
        actualOptions.RowGroupSize   ??= DefaultRowGroupSize  ;

        var type = typeof(T);
        if (type.IsPrimitive)
        {
            await SerializePrimitiveAsync(values, PrimitiveTypeParquetSchemas[type], stream, actualOptions);
            return;
        }
        
        await SerializeNonPrimitiveAsync(values, stream, actualOptions);
    }
}