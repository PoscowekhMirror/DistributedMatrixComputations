﻿using System.Numerics;
using Parquet;
using Parquet.Schema;

namespace Common.Vector.Serialization;

public interface ISerializedVectorElements
{
    // ParquetSchema DataSchema { get; }
    byte[] Data { get; }

              IEnumerable<T>  Deserialize     <T>(ParquetOptions parquetOptions) where T : new();
    ValueTask<IEnumerable<T>> DeserializeAsync<T>(ParquetOptions parquetOptions) where T : new();
}