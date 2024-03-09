namespace Common.Collections.Chunked;

public interface IChunkedList<T> : IList<T>
{
    int ChunkSize { get; }
    int ChunkCount { get; }
    IReadOnlyList<IChunk<T>> Chunks { get; }
}