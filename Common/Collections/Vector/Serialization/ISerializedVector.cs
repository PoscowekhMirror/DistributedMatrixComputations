using Common.Serialization;

namespace Common.Collections.Vector.Serialization;

public interface ISerializedVector // <T> : IDeserializable<ISerializedVector<T>, IVector<T>>
{
    string VectorTypeName { get; }
    int Count { get; }
    ISerializedVectorElements SerializedElements { get; }

    //           IVector<T>  Deserialize     <T>(ParquetOptions parquetOptions) where T : struct, INumber<T>;
    // ValueTask<IVector<T>> DeserializeAsync<T>(ParquetOptions parquetOptions) where T : struct, INumber<T>;
}