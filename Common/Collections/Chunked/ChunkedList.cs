using System.Collections;

namespace Common.Collections.Chunked;

public sealed class ChunkedList<T> : IChunkedList<T>
{
    private IList<IChunk<T>> _chunks;
    internal IList<IChunk<T>> InternalChunks => _chunks;
    public IReadOnlyList<IChunk<T>> Chunks => _chunks.AsReadOnly();
    
    public int ChunkSize { private init; get; }
    public int Count { private set; get; }
    public int ChunkCount => _chunks.Count;

    public bool IsReadOnly => false;

    private static string OutOfRangeExceptionString(int index, int rangeStartIndex, int rangeEndIndex)
        => $"{nameof(index)} {index} out of range [{rangeStartIndex}-{rangeEndIndex}]";

    private void CheckRange(int index, int rangeStartIndex = 0, int rangeEndIndex = -1)
    {
        var actualRangeEndIndex = rangeEndIndex == -1 ? Count - 1 : rangeEndIndex;
        if (index < rangeStartIndex || index >= actualRangeEndIndex)
        {
            throw new IndexOutOfRangeException(
                OutOfRangeExceptionString(index, rangeStartIndex, actualRangeEndIndex)
            );
        }
    }

    private int GetChunkIndex(int index) => index / ChunkSize;
    private int GetChunkElementIndex(int index) => index % ChunkSize;
    
    public T this[int index]
    {
        get
        {
            CheckRange(index);
            return _chunks[GetChunkIndex(index)][GetChunkElementIndex(index)];
        }
        set
        {
            CheckRange(index);
            _chunks[GetChunkIndex(index)][GetChunkElementIndex(index)] = value;
        }
    }

    public ChunkedList(int chunkSize)
    {
        if (chunkSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }
        
        ChunkSize = chunkSize;
        _chunks = new List<IChunk<T>>();
        Count = 0;
    }
    public ChunkedList(int chunkSize, IEnumerable<T> elements) 
    {
        if (chunkSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }
        
        ChunkSize = chunkSize;
        _chunks = elements
            .Chunk(ChunkSize)
            .Select(chunk => new Chunk<T>(chunk) as IChunk<T>)
            .ToList();
        Count = _chunks.Select(chunk => chunk.Count).Sum();
    }
    internal ChunkedList(int chunkSize, in IList<IChunk<T>> chunks)
    {
        if (chunkSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }
        
        ChunkSize = chunkSize;
        _chunks = chunks;
        for (int i = 0; i < _chunks.Count; i++)
        {
            var chunk = _chunks[i];
            if (chunk.Capacity != chunkSize)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(chunks)}[{i}].{nameof(chunk.Capacity)},{nameof(chunkSize)}"
                );
            }
            if (i < _chunks.Count - 1 && !chunk.IsFilled)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(chunks)}[{i}].{nameof(chunk.Count)},{nameof(chunks)}[{i}].{nameof(chunk.Capacity)}"
                );
            }
            Count += chunk.Count;
        }
    }

    public void Add(T item)
    {
        if (!_chunks.Any())
        {
            _chunks.Add(new Chunk<T>(ChunkSize, new [] {item}));
            return;
        }

        var lastChunk = _chunks.Last();
        if (lastChunk.IsFilled)
        {
            _chunks.Add(new Chunk<T>(ChunkSize, new [] {item}));
            return;
        }
        
        lastChunk.Add(item);
    }
    
    private int IndexOf(T item) 
        => _chunks
            .Select(chunk => chunk.IndexOf(item))
            .FirstOrDefault(index => index != -1, -1);

    private void RemoveAt(int index)
    {
        CheckRange(index);
        _chunks[GetChunkIndex(index)].RemoveAt(GetChunkElementIndex(index));
    }
    
    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index == -1)
        {
            return false;
        }
        RemoveAt(index);
        return true;
    }

    public bool Contains(T item) => IndexOf(item) == -1;

    public void CopyTo(T[] array, int arrayIndex)
    {
        var offset = arrayIndex;
        foreach (var chunk in _chunks)
        {
            chunk.CopyTo(array, offset);
            offset += chunk.Count;
        }
    }
    
    public void Clear()
    {
        _chunks = new List<IChunk<T>>();
        Count = 0;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    internal struct Enumerator : IEnumerator<T>
    {
        internal IList<IChunk<T>> Chunks { private init; get; }
        internal int ChunkIndex { private set; get; } = 0;
        internal int ChunkElementIndex { private set; get; } = -1;

        public T Current => Chunks[ChunkIndex][ChunkElementIndex];
        object IEnumerator.Current => Current;
        
        public Enumerator(IList<IChunk<T>> chunks) => Chunks = chunks;
        public Enumerator(ChunkedList<T> list) : this(list._chunks) { }
        
        public bool MoveNext()
        {
            ChunkElementIndex += 1;

            if (ChunkElementIndex == Chunks[ChunkElementIndex].Count)
            {
                ChunkElementIndex = 0;
                ChunkIndex += 1;
            }

            return ChunkIndex > Chunks.Count || ChunkElementIndex > Chunks[ChunkIndex].Count;
        }

        public void Reset()
        {
            ChunkIndex = 0;
            ChunkElementIndex = -1;
        }

        public void Dispose()
        {
            
        }
    }
}