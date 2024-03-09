using Common.Collections.Element;
using Common.Collections.Vector.Serialization;
using Common.Collections.Vector.Sparse;
using Common.Serialization;

namespace Common.Collections.Vector;

public interface IVector<T> : 
     IEnumerable<IndexedElement<T>>
    // ,ISerializable<ISerializedVector>
{
    IEnumerable<T> GetValuesOnly(bool includeZeroes, bool sort);
    IEnumerable<IndexedElement<T>> GetIndexedElements(bool includeZeroes, bool sort);

    T this[int index] { get; }
    
    int Count { get; }
    int NonZeroCount { get; }
    double Sparsity { get; }

    IVector<T> ToRegularVector();
    ISparseVector<T> ToSparseVector();

    ISerializedVector Serialize();
    ValueTask<ISerializedVector> SerializeAsync();
}