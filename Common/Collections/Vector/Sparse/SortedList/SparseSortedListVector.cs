using System.Numerics;
using Common.Collections.Element;

namespace Common.Collections.Vector.Sparse.SortedList;
/*
public sealed class SparseSortedListVector<T> : ISparseVector<T> 
    where T : INumber<T>
{
    private readonly SortedList<T, IList<int>> _sortedList;
    internal SortedList<T, IList<T>> InternalSortedList => _sortedList;

    public IEnumerable<T> GetValuesOnly(bool includeZeroes)
        => includeZeroes
            ? _sortedList
                .SelectMany(v => Enumerable.Repeat(v.Key, v.Value.Count))
            : _sortedList
                .Where(v => v.Key != T.Zero)
                .SelectMany(v => Enumerable.Repeat(v.Key, v.Value.Count));

    public IEnumerable<IndexedElement<T>> GetIndexedElements(bool includeZeroes)
        => includeZeroes
            ? _sortedList
                .SelectMany(v =>
                    v.Value.Select(i => new IndexedElement<T>(i, v.Key))
                )
            : _sortedList
                .Where(v => v.Key != T.Zero)
                .SelectMany(v => 
                    v.Value.Select(i => new IndexedElement<T>(i, v.Key))
                );

    public int Count { private init; get; }
    public int NonZeroCount { private init; get; }
    public double Sparsity => ((double) NonZeroCount) / Count;
    
    
}
*/