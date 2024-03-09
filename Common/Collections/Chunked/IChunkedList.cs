namespace Common.Collections.Chunked;

public interface IChunkedList<T> : IReadOnlyList<T>, ICollection<T>
{
    int ChunkSize { get; }
    int ChunkCount { get; }
    IReadOnlyList<IChunk<T>> Chunks { get; }
}