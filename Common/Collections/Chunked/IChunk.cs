using System.Collections.Immutable;

namespace Common.Collections.Chunked;

public interface IChunk<T> : IList<T>
{
    ReadOnlySpan<T> Values { get; }
    int Capacity { get; }
    bool IsFilled { get; }
}