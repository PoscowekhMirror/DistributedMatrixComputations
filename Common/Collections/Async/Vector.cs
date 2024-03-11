using System.Runtime.Serialization;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Common.Collections.Async;

public sealed class Vector<T> : IVector<T>
{
    private FileStream FileHandle { init; get; }
    private ParquetOptions ParquetOptions { init; get; }
    public Func<DataColumn[], IEnumerable<T>>? Deserializer { private set; get; }
    public ParquetSchema ParquetSchema { private init; get; }
    public long Count { private init; get; }
    public int RowGroupCount { private init; get; }
    // public int RowGroupSize { private init; get; }
    public IReadOnlyDictionary<string, string> MetaData { private init; get; }

    private Vector(FileStream fileHandle, ParquetOptions parquetOptions)
    {
        FileHandle = fileHandle;
        ParquetOptions = parquetOptions;
    }

    public static async ValueTask<Vector<T>> CreateAsync(FileStream fileHandle, ParquetOptions? options = null)
    {
        var actualOptions = options ?? Common.Serialization.Parquet.DefaultParquetOptions;
        ParquetSchema? schema = null;
        int rowGroupCount = -1;
        Dictionary<string, string>? meta = null;
        long count = -1L;

        using (var reader = await ParquetReader.CreateAsync(fileHandle, actualOptions, true))
        {
            schema = reader.Schema;
            rowGroupCount = reader.RowGroupCount;
            meta = reader.CustomMetadata;
            // var rowGroupCounts = Enumerable.Repeat(0L, rowGroupCount).ToList();
            count = Enumerable
                .Range(0, rowGroupCount)
                .Select(i => reader.OpenRowGroupReader(i))
                .Select(rgReader => rgReader.RowCount)
                .Sum();
        }
        var result = new Vector<T>(fileHandle, actualOptions)
        {
             ParquetSchema = schema
            ,Count = count
            ,MetaData = meta
            ,RowGroupCount = rowGroupCount
            // ,RowGroupSize = rowGroupSize
        };
        return result;
    }

    public void SetDeserializer(Func<DataColumn[], IEnumerable<T>> deserializer)
    {
        if (Deserializer is not null)
        {
            throw new ArgumentException($"{nameof(Deserializer)} is not null");
        }
        Deserializer = deserializer;
    }
    
    public IEnumerable<T> GetRowGroup(int rowGroupIndex) => GetRowGroupAsync(rowGroupIndex).Result;

    public async ValueTask<IEnumerable<T>> GetRowGroupAsync(int rowGroupIndex, CancellationToken cancellationToken = default)
    {
        if (Deserializer is null)
        {
            throw new SerializationException();
        }
        using var reader = await ParquetReader.CreateAsync(FileHandle, ParquetOptions, true, cancellationToken);
        return Deserializer(await reader.ReadEntireRowGroupAsync(rowGroupIndex));
    }

    public IAsyncEnumerator<IEnumerable<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(this, cancellationToken);

    public async ValueTask DisposeAsync() => await FileHandle.DisposeAsync();

    internal struct Enumerator : IAsyncEnumerator<IEnumerable<T>>
    {
        private readonly Vector<T> _vector;
        private CancellationToken _cancellationToken;
        private int _rowGroupIndex = -1;
        private IEnumerable<T>? _rowGroupValues = null;

        public Enumerator(Vector<T> v, CancellationToken cancellationToken)
        {
            _vector = v;
            _cancellationToken = cancellationToken;
        }

        public IEnumerable<T> Current => _rowGroupValues!;
        
        public async ValueTask<bool> MoveNextAsync()
        {
            var success = ++_rowGroupIndex >= _vector.RowGroupCount;
            if (!success)
            {
                return false;
            }
            _rowGroupValues = await _vector.GetRowGroupAsync(_rowGroupIndex);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            _rowGroupValues = null;
        }
    }
}