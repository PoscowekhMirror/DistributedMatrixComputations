using Parquet;
using Parquet.Serialization;

namespace Common.Vector.Serialization;

public readonly record struct SerializedVectorElements(
         /*ParquetSchema DataSchema
        ,*/byte[] Data
    ) : ISerializedVectorElements
{
    public IEnumerable<T> Deserialize<T>(ParquetOptions parquetOptions)
        where T : new()
    {
        using var dataStream = new MemoryStream(Data, false);
        return ParquetSerializer.DeserializeAsync<T>(dataStream, parquetOptions).Result;
    }

    public async ValueTask<IEnumerable<T>> DeserializeAsync<T>(ParquetOptions parquetOptions) 
        where T : new()
    {
        using var dataStream = new MemoryStream(Data, false);
        return await ParquetSerializer.DeserializeAsync<T>(dataStream, parquetOptions);
    }
}