namespace Common.Collections.Chunked;

public interface IChunk<T> : System.Collections.Generic.IList<T>
{
    ReadOnlySpan<T> Values { get; }
    int Capacity { get; }
    bool IsFilled { get; }
}