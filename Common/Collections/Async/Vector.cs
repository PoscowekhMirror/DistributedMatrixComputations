namespace Common.Collections.Async;

public sealed class Vector<T> : IDisposable
{
    public int Count { private set; get; }
    public Stream Stream { private set; get; }

    public Vector(int count, Stream stream)
    {
        Count  = count ;
        Stream = stream;
    }
    
    public void Dispose()
    {
        Stream.Dispose();
    }

    public async ValueTask<IEnumerable<T>> GetValues()
    {
        await 
    }
}