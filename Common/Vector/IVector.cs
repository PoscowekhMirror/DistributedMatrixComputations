using System.Numerics;
using System.Runtime.Serialization;
using Common.Vector.Serialization;

namespace Common.Vector;

public interface IVector<T> : IEnumerable<IndexedElement<T>>
{
    IEnumerable<T> GetValuesOnly();
    IEnumerable<IndexedElement<T>> GetIndexedElements();

    T this[int index] { get; }
    
    int Count { get; }
    int NonZeroCount { get; }
    double Sparsity { get; }

    ISerializedVector Serialize();
    ValueTask<ISerializedVector> SerializeAsync();
}