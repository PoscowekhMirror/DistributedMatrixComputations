using System.Collections;
using System.Runtime.Serialization;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Common.Collections.Sync;

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

    private static void ResetStream(Stream s)
    {
        s.Position = 0;
        s.Seek(0, SeekOrigin.Begin);
    }

    private void ResetStream() => ResetStream(FileHandle);

    public static Vector<T> Create(FileStream fileHandle, ParquetOptions? options = null) 
    {
        var actualOptions = options ?? Common.Serialization.Parquet.DefaultParquetOptions;
        
        ParquetSchema? schema = null;
        Dictionary<string, string>? meta = null;
        int rowGroupCount = -1;
        long count = -1L;

        using (var reader = ParquetReader.CreateAsync(fileHandle, actualOptions, true).Result)
        {
            schema        = reader.Schema        ;
            rowGroupCount = reader.RowGroupCount ;
            meta          = reader.CustomMetadata;
            
            // var rowGroupCounts = Enumerable.Repeat(0L, rowGroupCount).ToList();
            count = Enumerable
                .Range(0, rowGroupCount)
                .Select(i => reader.OpenRowGroupReader(i))
                .Select(rgReader => rgReader.RowCount)
                .Sum();
        }

        ResetStream(fileHandle);
        
        return new Vector<T>(fileHandle, actualOptions)
        {
             ParquetSchema = schema
            ,Count = count
            ,MetaData = meta
            ,RowGroupCount = rowGroupCount
            // ,RowGroupSize = rowGroupSize
        };
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
    
    public IEnumerable<T> GetRowGroup(int rowGroupIndex)
    {
        if (Deserializer is null)
        {
            throw new SerializationException();
        }

        DataColumn[]? dataColumns = null;
        using (var reader = ParquetReader.CreateAsync(FileHandle, ParquetOptions, true).Result)
        {
            dataColumns = reader.ReadEntireRowGroupAsync(rowGroupIndex).Result;
        }

        ResetStream();
        
        return Deserializer(dataColumns!);
    }
    public async ValueTask<IEnumerable<T>> GetRowGroupAsync(int rowGroupIndex, CancellationToken cancellationToken = default)
    {
        if (Deserializer is null)
        {
            throw new SerializationException();
        }
        
        DataColumn[]? dataColumns = null;
        using (var reader = await ParquetReader.CreateAsync(FileHandle, ParquetOptions, true, cancellationToken))
        {
            dataColumns = await reader.ReadEntireRowGroupAsync(rowGroupIndex);
        }

        ResetStream();
        
        return Deserializer(dataColumns!);
    }

    public IEnumerator<IEnumerable<T>> GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() => FileHandle.Dispose();
    // public async ValueTask DisposeAsync() => await FileHandle.DisposeAsync();

    internal struct Enumerator : IEnumerator<IEnumerable<T>>
    {
        private readonly Vector<T> _vector;
        private int _rowGroupIndex = -1;
        private IEnumerable<T>? _rowGroupValues = null;


        public IEnumerable<T> Current => _rowGroupValues!;

        object IEnumerator.Current => Current;
        
        public Enumerator(Vector<T> vector) => _vector = vector;


        public bool MoveNext()
        {
            var success = ++_rowGroupIndex >= _vector.RowGroupCount;
            if (!success)
            {
                _rowGroupValues = null;
                return false;
            }
            _rowGroupValues = _vector.GetRowGroup(_rowGroupIndex);
            return true;
        }

        public void Reset()
        {
            _rowGroupValues = null;
            _rowGroupIndex = -1;
        }

        public void Dispose() => _rowGroupValues = null;
    }
}