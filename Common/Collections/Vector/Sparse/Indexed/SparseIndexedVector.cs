using System.Collections;
using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Regular;
using Common.Collections.Vector.Serialization;

namespace Common.Collections.Vector.Sparse.Indexed;

public class SparseIndexedVector<T> : ISparseVector<T>
    where T : INumber<T>
{
    private readonly IList<IndexedElement<T>> _indexedElements;
    internal IReadOnlyList<IndexedElement<T>> IndexedElements => _indexedElements.AsReadOnly();

    public IEnumerable<T> GetValuesOnly(bool includeZeroes = false)
    {
        if (includeZeroes == false)
        {
            return _indexedElements.Select(e => e.Value);
        }
        
        var values = new List<T>(Count);
        var currentIndex = 0;
        using var enumerator = _indexedElements.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var element = enumerator.Current;
            values.AddRange(Enumerable.Repeat(T.Zero, element.Index - 1 - currentIndex));
            values.Add(element.Value);
            currentIndex = element.Index;
        }

        for (int i = currentIndex + 1; i < Count; i++)
        {
            values.Add(T.Zero);
        }

        return values;
    }
    public IEnumerable<IndexedElement<T>> GetIndexedElements(bool includeZeroes = false) 
        => includeZeroes
            ? _indexedElements.AsEnumerable()
            : this.AsEnumerable();

    public T this[int index]
    {
        get
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException();
            }

            var element =
                _indexedElements
                    .FirstOrDefault(
                         e => e.Index == index
                        ,IndexedElement<T>.Default
                    );

            return element.Index == -1 ? T.Zero : element.Value;
        }
    }

    public int Count { private init; get; }
    public int NonZeroCount => _indexedElements.Count;
    public double Sparsity => (double) _indexedElements.Count / Count;

    // public SparseIndexedVector(IEnumerable<Element<T>> elements) : this(elements.Select(e => e.Value)) { }
    public SparseIndexedVector(IEnumerable<T> elements)
    {
        var count = -1;
        var gotCountWithoutEnumeration = elements.TryGetNonEnumeratedCount(out var approxCount);
        var finalElements = gotCountWithoutEnumeration 
            ? new List<IndexedElement<T>>(approxCount)
            : new List<IndexedElement<T>>();
        
        using var enumerator = elements.GetEnumerator();
        while (enumerator.MoveNext())
        {
            count += 1;
            var current = enumerator.Current;
            if (current != T.Zero)
            {
                finalElements.Add(new IndexedElement<T>(count, current));
            }
        }

        _indexedElements = finalElements;
        Count = count + 1;
    }
    public SparseIndexedVector(int count, IEnumerable<IndexedElement<T>> elements)
    {
        Count = count;
        _indexedElements = 
            elements
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index)
                .ToList();
    }
    internal SparseIndexedVector(int count, in IList<IndexedElement<T>> elements)
    {
        Count = count;
        _indexedElements = elements;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IndexedElement<T>> GetEnumerator() => GetEnumerator(false);
    public IEnumerator<IndexedElement<T>> GetEnumerator(bool includeZeroes) 
        => includeZeroes 
            ? new IndexedEnumerator(_indexedElements, Count) 
            : _indexedElements
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index)
                .GetEnumerator();

    public IVector<T> ToRegularVector() => new Regular.Vector<T>(GetValuesOnly(true));
    public ISparseVector<T> ToSparseVector() => this;

    public ISerializedVector Serialize() => this.Serialize(null);
    public async ValueTask<ISerializedVector> SerializeAsync() => await this.SerializeAsync(null);

    internal struct IndexedEnumerator : IEnumerator<IndexedElement<T>>
    {
        private readonly IList<IndexedElement<T>> _elements;
        private readonly int _count;
        private int _currentIndex = -1;
        private int _currentElementIndex = 0;

        public IndexedElement<T> Current 
            => _elements[_currentElementIndex].Index == _currentIndex 
                ? _elements[_currentElementIndex] 
                : new IndexedElement<T>(_currentIndex, T.Zero);
        object IEnumerator.Current => Current;
        
        public IndexedEnumerator(IList<IndexedElement<T>> elements, int count)
        {
            _elements = elements;
            _count = count;
        }

        public bool MoveNext()
        {
            _currentIndex += 1;
            if (_currentElementIndex + 1 < _elements.Count && _elements[_currentElementIndex + 1].Index == _currentIndex)
            {
                _currentElementIndex += 1;
            }
            return _currentIndex < _count;
        }
        public void Reset()
        {
            _currentIndex = -1;
            _currentElementIndex = 0;
        }

        public void Dispose()
        {
            
        }
    }
}