using System.Numerics;
using Parquet;

namespace Common.Vector.Serialization;

public interface ISerializedVector
{
    string VectorTypeName { get; }
    int Count { get; }
    SerializedVectorElements SerializedElements { get; }

    //           IVector<T>  Deserialize     <T>(ParquetOptions parquetOptions) where T : struct, INumber<T>;
    // ValueTask<IVector<T>> DeserializeAsync<T>(ParquetOptions parquetOptions) where T : struct, INumber<T>;
}