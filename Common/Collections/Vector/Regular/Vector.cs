using System.Collections;
using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Serialization;
using Common.Collections.Vector.Sparse;
using Common.Collections.Vector.Sparse.Indexed;

namespace Common.Collections.Vector.Regular;

public sealed class Vector<T> : IVector<T>
    where T : INumber<T>
{
    private readonly IList<T> _values;
    internal IList<T> Values => _values;
    
    public IEnumerable<T> GetValuesOnly(bool includeZeroes = true) 
        => includeZeroes
            ? _values.AsEnumerable()
            : _values.Where(v => v != T.Zero);

    public IEnumerable<IndexedElement<T>> GetIndexedElements(bool includeZeroes = true) 
        => includeZeroes
            ? this.AsEnumerable()
            : Enumerable
                .Range(0, _values.Count)
                .Where(i => _values[i] != T.Zero)
                .Select(i => new IndexedElement<T>(i, _values[i]));

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }
            return _values[index];
        }
    }

    public int Count => _values.Count;
    public int NonZeroCount => _values.Count(v => v != T.Zero);
    public double Sparsity => Count == 0 ? 1 : ((double) NonZeroCount) / Count;

    internal Vector(in IList<T> values) => _values = values;
    public Vector(IEnumerable<T> values) => _values = values.ToList();
    // public Vector(IEnumerable<Element<T>> values) : this(values.Select(e => e.Value)) { }
    // IndexedElement Constructor ???
    
    public IEnumerator<IndexedElement<T>> GetEnumerator() 
        => new IndexedEnumerator(_values, 0);
    public IEnumerator<IndexedElement<T>> GetEnumerator(int startingIndex) 
        => new IndexedEnumerator(_values, startingIndex);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IVector<T> ToRegularVector() => this;
    public ISparseVector<T> ToSparseVector() => new SparseIndexedVector<T>(_values);

    public ISerializedVector Serialize() => this.Serialize(null);
    public async ValueTask<ISerializedVector> SerializeAsync() => await this.SerializeAsync(null);

    internal struct IndexedEnumerator : IEnumerator<IndexedElement<T>>
    {
        private readonly IList<T> _values;
        private int _index;

        public IndexedElement<T> Current => new(_index, _values[_index]);
        object IEnumerator.Current => Current;
        
        public IndexedEnumerator(IList<T> values, int startingIndex = -1)
        {
            _values = values;
            _index = startingIndex;
            if (_values.Count == 0)
            {
                return;
            }
            if (startingIndex < -1 || startingIndex >= _values.Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        public bool MoveNext() => ++_index < _values.Count;
        public void Reset() => _index = -1;

        public void Dispose()
        {
            
        }
    }
}