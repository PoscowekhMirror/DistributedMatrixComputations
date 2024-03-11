﻿using Parquet.Data;
using Parquet.Schema;

namespace Common.Collections.Sync;

public interface IVector<T> : 
     IEnumerable<IEnumerable<T>>
    // ,IAsyncDisposable
    ,IDisposable
{
    public Func<DataColumn[], IEnumerable<T>>? Deserializer { get; }
    ParquetSchema ParquetSchema { get; }
    long Count { get; }
    int RowGroupCount { get; }
    // int RowGroupSize { get; }

    void SetDeserializer(Func<DataColumn[], IEnumerable<T>> deserializer);
    
    public IEnumerable<T> GetRowGroup(int rowGroupIndex);
    public ValueTask<IEnumerable<T>> GetRowGroupAsync(int rowGroupIndex, CancellationToken cancellationToken);
}