using System.Collections;
using System.Numerics;
using Common.Vector.Serialization;

namespace Common.Vector;

public sealed class Vector<T> : IVector<T>
    where T : INumber<T>
{
    private readonly IList<T> _values;
    internal IList<T> Values => _values;
    public IEnumerable<T> GetValuesOnly() => _values.AsEnumerable();
    public IEnumerable<IndexedElement<T>> GetIndexedElements() => this.AsEnumerable();

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