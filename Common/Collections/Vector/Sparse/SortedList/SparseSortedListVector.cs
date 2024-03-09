using System.Collections;
using System.Collections.Immutable;
using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Serialization;
using Parquet.Serialization;

namespace Common.Collections.Vector.Sparse.SortedList;

public sealed class SparseSortedListVector<T> : ISparseVector<T> 
    where T : INumber<T>
{
    private readonly SortedList<T, SortedSet<int>> _sortedList;
    internal SortedList<T, SortedSet<int>> InternalSortedList => _sortedList;

    public IEnumerable<T> GetValuesOnly(bool includeZeroes = true) 
        => includeZeroes
            ? _sortedList
                .SelectMany(v => Enumerable.Repeat(v.Key, v.Value.Count))
            : _sortedList
                .Where(v => v.Key != T.Zero)
                .SelectMany(v => Enumerable.Repeat(v.Key, v.Value.Count));

    public IEnumerable<IndexedElement<T>> GetIndexedElements(bool includeZeroes = true) 
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

    public int Count => _sortedList.Count();
    public int NonZeroCount => _sortedList.Count(p => p.Key != T.Zero);
    public double Sparsity => ((double) NonZeroCount) / Count;

    internal SparseSortedListVector(SortedList<T, SortedSet<int>> values) => _sortedList = values;
    public SparseSortedListVector(IEnumerable<T> elements)
    {
        var indexCounter = 0;
        _sortedList = new SortedList<T, SortedSet<int>>(
            elements
                .Select(v => (indexCounter++, v))
                .GroupBy(p => p.v, p => p.Item1)
                .Select(g => 
                    new KeyValuePair<T, SortedSet<int>>(
                         g.Key
                        ,new SortedSet<int>(g)
                    )
                )
                .ToImmutableSortedDictionary()
        );
    }
    public SparseSortedListVector(IEnumerable<IndexedElement<T>> elements)
    {
        _sortedList = new SortedList<T, SortedSet<int>>(
            elements
                .GroupBy(e => e.Value, e => e.Index)
                .Select(g => 
                    new KeyValuePair<T, SortedSet<int>>(
                         g.Key
                        ,new SortedSet<int>(g)
                    )
                )
                .ToImmutableSortedDictionary()
        );
    }

    public T this[int index]
    {
        set
        {
            var pair = _sortedList
                .FirstOrDefault(p => p.Value.Contains(index), default);
            if (!pair.Value.Any())
            {
                var existingList = _sortedList
                    .FirstOrDefault(p => p.Key == value, default);
                existingList.Value.Add(index);
                return;
            }
            _sortedList.Add(value, new SortedSet<int>() {index});
        }
        get
        {
            var pair = _sortedList
                .FirstOrDefault(v => v.Value.Contains(index), default);
            if (!pair.Value.Any())
            {
                throw new KeyNotFoundException();
            }
            return pair.Key;
        }
    }

    public ISerializedVector Serialize()
    {
        using var dataStream = new MemoryStream();
        var schema = ParquetSerializer.SerializeAsync(
             _sortedList
            ,dataStream
            ,Collections.Serialization.Common.DefaultSerializerOptions
        ).Result;
        return new SerializedVector(
             nameof(SparseSortedListVector<T>)
            ,Count
            ,new SerializedVectorElements(dataStream.ToArray())
        );
    }
    public async ValueTask<ISerializedVector> SerializeAsync()
    {
        using var dataStream = new MemoryStream();
        var schema = await ParquetSerializer.SerializeAsync(
            _sortedList
            ,dataStream
            ,Collections.Serialization.Common.DefaultSerializerOptions
        );
        return new SerializedVector(
            nameof(SparseSortedListVector<T>)
            ,Count
            ,new SerializedVectorElements(dataStream.ToArray())
        );
    }

    public IVector<T> ToRegularVector() => this;
    public ISparseVector<T> ToSparseVector() => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IndexedElement<T>> GetEnumerator() => new Enumerator(this);

    internal struct Enumerator : IEnumerator<IndexedElement<T>>
    {
        private readonly IEnumerator<KeyValuePair<T, SortedSet<int>>> _internalEnumerator;
        internal IEnumerator<KeyValuePair<T, SortedSet<int>>> InternalEnumerator => _internalEnumerator;

        private IEnumerator<int>? _internalSetEnumerator = null;
        internal IEnumerator<int>? InternalSetEnumerator => _internalSetEnumerator;

        internal int InternalIndex { private set; get; } = -1;

        public IndexedElement<T> Current => new(
                 _internalSetEnumerator!.Current
                ,_internalEnumerator.Current.Key
            );
        object IEnumerator.Current => Current;
        
        public Enumerator(IEnumerator<KeyValuePair<T, SortedSet<int>>> enumerator) 
            => _internalEnumerator = enumerator;

        public Enumerator(SparseSortedListVector<T> vector)
            : this(vector._sortedList.GetEnumerator())
        {
            
        }
        
        public bool MoveNext()
        {
            if (_internalSetEnumerator is null || !_internalSetEnumerator.MoveNext())
            {
                var success = _internalEnumerator.MoveNext();
                if (!success)
                {
                    return false;
                }
                _internalSetEnumerator = _internalEnumerator.Current.Value.GetEnumerator();
                return _internalSetEnumerator.MoveNext();
            }
            return _internalSetEnumerator.MoveNext();
        }

        public void Reset()
        {
            _internalEnumerator.Reset();
            _internalSetEnumerator = null;
        }

        public void Dispose()
        {
            
        }
    }
}