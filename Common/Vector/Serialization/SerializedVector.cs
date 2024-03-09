﻿namespace Common.Vector.Serialization;

public readonly record struct SerializedVector(
         string VectorTypeName
        ,int Count
        ,SerializedVectorElements SerializedElements
    ) : ISerializedVector
{
    /*
    public IVector<T> Deserialize<T>(ParquetOptions parquetOptions) 
        where T : struct, INumber<T> 
        => throw new NotImplementedException();

    public async ValueTask<IVector<T>> DeserializeAsync<T>(ParquetOptions parquetOptions) 
        where T : struct, INumber<T> 
        => throw new NotImplementedException()
    */
}